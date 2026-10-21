using System.Collections.Generic;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>What a gate is doing, as far as dialing is concerned.</summary>
    internal enum GateState
    {
        /// <summary>Not connected to anything: free to dial from or to.</summary>
        Idle = 0,
        /// <summary>Connected the vanilla way (two gates sharing a name). Dialing leaves these alone.</summary>
        Linked = 1,
        /// <summary>Connected by a dial; it closes by itself once nobody is near.</summary>
        Dialing = 2,
    }

    /// <summary>
    /// The server's half of dialing (the dedicated server, or the host's own game, or your game when you play alone).
    /// Valheim only lets the server change which two portals are connected, so dialing is decided here.
    ///
    /// A dial works with the game's own rules: it gives both gates one temporary shared tag and links them, which is
    /// exactly what the game does for two portals with the same name. When the dial ends the original names are put
    /// back and the game re-pairs gates by name as usual. Each gate's real name is kept in its own saved field the whole
    /// time, so a crash or a stop mid-dial is repaired the next time the world loads.
    /// </summary>
    internal static class GateServer
    {
        private static readonly int KeyOriginalName = "sg1_orig".GetStableHashCode();

        private class Dial
        {
            public ZDOID A, B;
            public float Start, LastNear;
        }

        private static readonly List<Dial> Dials = new List<Dial>();
        private static float _nextTick;
        private static float _serverUpSince = -1f;
        private static bool _repaired;
        private static int _counter;
        private static int _lastSignature;
        private static bool _sentList;

        public static bool IsServer => ZNet.instance != null && ZNet.instance.IsServer();

        public static void Tick()
        {
            if (!IsServer || ZDOMan.instance == null || Game.instance == null)
            {
                _serverUpSince = -1f;
                _repaired = false;
                _sentList = false;
                Dials.Clear();
                return;
            }

            if (Time.time < _nextTick) return;
            _nextTick = Time.time + 1f;

            if (_serverUpSince < 0f) _serverUpSince = Time.time;
            // Give the world a few seconds to finish loading before looking through it.
            if (Time.time - _serverUpSince < 5f) return;

            if (!_repaired)
            {
                _repaired = true;
                RepairLeftovers();
            }

            // Tell everyone whenever a gate is built, removed, renamed, or starts or stops being connected, so the
            // map and the dial lists stay current without anyone having to ask.
            int signature = Signature(Snapshot());
            if (!_sentList || signature != _lastSignature)
            {
                _sentList = true;
                _lastSignature = signature;
                GateNet.BroadcastGates();
            }

            if (Dials.Count == 0) return;

            var positions = PlayerPositions();
            bool changed = false;
            for (int i = Dials.Count - 1; i >= 0; i--)
            {
                var dial = Dials[i];
                var a = ZDOMan.instance.GetZDO(dial.A);
                var b = ZDOMan.instance.GetZDO(dial.B);
                bool gone = a == null || b == null;
                if (!gone && AnyoneNear(positions, a.GetPosition(), b.GetPosition())) dial.LastNear = Time.time;

                bool idle = Time.time - dial.LastNear > PortalHeimSG1Plugin.DialIdleSeconds.Value;
                bool tooLong = Time.time - dial.Start > PortalHeimSG1Plugin.DialMaxSeconds.Value;
                if (gone || idle || tooLong)
                {
                    Close(a, b);
                    Dials.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                Game.instance.ConnectPortals();
                GateNet.BroadcastGates();
            }
        }

        /// <summary>Gates left over from a previous run that stopped mid-dial: give them their names back.</summary>
        private static void RepairLeftovers()
        {
            int fixedCount = 0;
            foreach (var zdo in ZDOMan.instance.GetPortalList())
            {
                if (HasStoredName(zdo) && !IsInDial(zdo.m_uid))
                {
                    Restore(zdo);
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                PortalHeimSG1Plugin.Log.LogInfo($"Put the names back on {fixedCount} gate(s) left mid-dial.");
                Game.instance.ConnectPortals();
            }
        }

        private static bool IsInDial(ZDOID id) => Dials.Exists(d => d.A == id || d.B == id);

        private static bool HasStoredName(ZDO zdo) => zdo.GetString(KeyOriginalName, "").Length > 0;

        public static string NameOf(ZDO zdo)
        {
            string stored = zdo.GetString(KeyOriginalName, "");
            return stored.Length > 0 ? stored.Substring(2) : zdo.GetString(ZDOVars.s_tag);
        }

        public static GateState StateOf(ZDO zdo)
        {
            if (HasStoredName(zdo)) return GateState.Dialing;
            return zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) == ZDOID.None ? GateState.Idle : GateState.Linked;
        }

        public static ZDOID PartnerOf(ZDO zdo) => zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);

        /// <summary>Connects two gates. Returns null on success, or a short reason to show the player.</summary>
        public static string TryDial(long sender, ZDOID srcId, ZDOID dstId)
        {
            if (!PortalHeimSG1Plugin.DialingEnabled.Value) return "Dialing is switched off on this server.";
            if (srcId == dstId) return "That is this gate.";

            var src = ZDOMan.instance.GetZDO(srcId);
            var dst = ZDOMan.instance.GetZDO(dstId);
            if (src == null || dst == null || !IsPortal(src) || !IsPortal(dst)) return "That gate is gone.";

            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals)) return "Portals are blocked in this world.";

            if (TryGetPlayerPosition(sender, out var where))
            {
                float range = PortalHeimSG1Plugin.DialRange.Value;
                if (Vector3.Distance(where, src.GetPosition()) > range) return "You are too far from the gate.";
            }

            if (StateOf(src) != GateState.Idle) return "This gate is already connected.";
            if (StateOf(dst) != GateState.Idle) return "That gate is busy.";

            string session = "~sg1:" + (++_counter) + ":" + Random.Range(1000, 9999);
            Begin(src, session);
            Begin(dst, session);
            src.SetConnection(ZDOExtraData.ConnectionType.Portal, dst.m_uid);
            dst.SetConnection(ZDOExtraData.ConnectionType.Portal, src.m_uid);
            ZDOMan.instance.ForceSendZDO(src.m_uid);
            ZDOMan.instance.ForceSendZDO(dst.m_uid);

            Dials.Add(new Dial { A = srcId, B = dstId, Start = Time.time, LastNear = Time.time });
            PortalHeimSG1Plugin.Log.LogInfo($"Dialed '{NameOf(src)}' -> '{NameOf(dst)}'.");
            GateNet.BroadcastGates();
            return null;
        }

        /// <summary>Ends a dial from either gate.</summary>
        public static string TryHangUp(ZDOID gateId)
        {
            var dial = Dials.Find(d => d.A == gateId || d.B == gateId);
            if (dial == null) return "This gate isn't dialed.";

            Close(ZDOMan.instance.GetZDO(dial.A), ZDOMan.instance.GetZDO(dial.B));
            Dials.Remove(dial);
            Game.instance.ConnectPortals();
            GateNet.BroadcastGates();
            return null;
        }

        private static void Begin(ZDO zdo, string sessionTag)
        {
            zdo.SetOwner(ZDOMan.GetSessionID());
            // Marked with "o:" so an empty original name can be told apart from "nothing stored".
            zdo.Set(KeyOriginalName, "o:" + zdo.GetString(ZDOVars.s_tag));
            zdo.Set(ZDOVars.s_tag, sessionTag);
        }

        private static void Close(ZDO a, ZDO b)
        {
            if (a != null) Restore(a);
            if (b != null) Restore(b);
        }

        private static void Restore(ZDO zdo)
        {
            string stored = zdo.GetString(KeyOriginalName, "");
            if (stored.Length == 0) return;

            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(ZDOVars.s_tag, stored.Substring(2));
            zdo.Set(KeyOriginalName, "");
            zdo.SetConnection(ZDOExtraData.ConnectionType.Portal, ZDOID.None);
            ZDOMan.instance.ForceSendZDO(zdo.m_uid);
        }

        private static bool IsPortal(ZDO zdo)
        {
            foreach (var portal in ZDOMan.instance.GetPortalList())
                if (portal == zdo) return true;
            return false;
        }

        /// <summary>Every gate in the world, for the list sent to players.</summary>
        public static List<GateInfo> Snapshot()
        {
            var list = new List<GateInfo>();
            if (!IsServer || ZDOMan.instance == null) return list;

            foreach (var zdo in ZDOMan.instance.GetPortalList())
            {
                list.Add(new GateInfo
                {
                    Id = zdo.m_uid,
                    Name = NameOf(zdo),
                    Position = zdo.GetPosition(),
                    State = StateOf(zdo),
                    Partner = PartnerOf(zdo),
                });
            }

            return list;
        }

        /// <summary>A number that changes whenever anything a player sees about the gates changes (order doesn't matter).</summary>
        private static int Signature(List<GateInfo> gates)
        {
            unchecked
            {
                int sum = gates.Count * 7919;
                foreach (var gate in gates)
                {
                    int h = 17;
                    h = h * 31 + gate.Id.GetHashCode();
                    h = h * 31 + (gate.Name ?? "").GetHashCode();
                    h = h * 31 + (int)gate.State;
                    h = h * 31 + gate.Partner.GetHashCode();
                    h = h * 31 + Mathf.RoundToInt(gate.Position.x) * 73 + Mathf.RoundToInt(gate.Position.z);
                    sum += h;
                }

                return sum;
            }
        }

        // --- Where the players are (the server knows every player's position) ---

        private static List<Vector3> PlayerPositions()
        {
            var list = new List<Vector3>();
            foreach (var peer in ZNet.instance.GetPeers()) list.Add(peer.m_refPos);
            if (Player.m_localPlayer != null) list.Add(Player.m_localPlayer.transform.position);
            return list;
        }

        internal static bool TryGetPlayerPosition(long sender, out Vector3 position)
        {
            var peer = ZNet.instance.GetPeer(sender);
            if (peer != null)
            {
                position = peer.m_refPos;
                return true;
            }

            if (Player.m_localPlayer != null)
            {
                position = Player.m_localPlayer.transform.position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        private static bool AnyoneNear(List<Vector3> positions, Vector3 a, Vector3 b)
        {
            float range = PortalHeimSG1Plugin.DialKeepOpenRange.Value;
            foreach (var p in positions)
                if (Vector3.Distance(p, a) <= range || Vector3.Distance(p, b) <= range) return true;
            return false;
        }
    }

    internal struct GateInfo
    {
        public ZDOID Id;
        public string Name;
        public Vector3 Position;
        public GateState State;
        public ZDOID Partner;
    }
}
