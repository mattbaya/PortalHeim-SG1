using System.Collections.Generic;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// What this player knows about dialing: whether the server supports it, the server's gate list, which gates this
    /// character has visited, and which one is its home gate. The visited and home lists are stored in the character's
    /// own save data, so they follow the character between worlds' servers and survive restarts.
    /// </summary>
    internal static class GateClient
    {
        private const string SeenKey = "PortalHeimSG1.Seen";
        private const string HomeKey = "PortalHeimSG1.Home";
        // A gate counts as visited once you've been this close to it.
        private const float VisitRange = 30f;

        public static bool ServerHasMod;
        public static string ServerVersion = "";
        public static float HelloSentAt = -1f;

        public static List<GateInfo> Gates { get; private set; } = new List<GateInfo>();
        public static event System.Action GatesChanged;

        private static Player _forPlayer;
        private static readonly HashSet<string> Seen = new HashSet<string>();
        private static string _home = "";
        private static float _nextVisitCheck;
        private static float _nextHello;
        private static bool _gaveUpLogged;

        public static string Key(ZDOID id) => id.UserID + ":" + id.ID;

        public static bool TryParse(string text, out ZDOID id)
        {
            id = ZDOID.None;
            if (string.IsNullOrEmpty(text)) return false;
            int colon = text.IndexOf(':');
            if (colon <= 0) return false;
            if (!long.TryParse(text.Substring(0, colon), out long user) || !uint.TryParse(text.Substring(colon + 1), out uint number)) return false;
            id = new ZDOID(user, number);
            return true;
        }

        public static void SetGates(List<GateInfo> gates)
        {
            Gates = gates;
            GatesChanged?.Invoke();
        }

        /// <summary>Call every frame while playing: says hello to the server, and notes the gates you walk past.</summary>
        public static void Tick()
        {
            var player = Player.m_localPlayer;
            if (player == null || ZNet.instance == null)
            {
                _forPlayer = null;
                ServerHasMod = false;
                HelloSentAt = -1f;
                _gaveUpLogged = false;
                return;
            }

            if (_forPlayer != player) LoadFor(player);
            if (!PortalHeimSG1Plugin.DialingEnabled.Value) return;

            // Keep saying hello until the server answers (it can take a moment after spawning).
            if (!ServerHasMod && Time.time >= _nextHello)
            {
                _nextHello = Time.time + 5f;
                if (HelloSentAt < 0f || Time.time - HelloSentAt < 40f) GateNet.SendHello();
                else if (!_gaveUpLogged)
                {
                    _gaveUpLogged = true;
                    PortalHeimSG1Plugin.Log.LogInfo("The server didn't answer: it doesn't have PortalHeim SG1, so dialing and gate pins are off here.");
                }
            }

            if (Time.time >= _nextVisitCheck)
            {
                _nextVisitCheck = Time.time + 1f;
                NoteNearbyGates(player);
            }
        }

        private static void LoadFor(Player player)
        {
            _forPlayer = player;
            Seen.Clear();
            player.m_customData.TryGetValue(SeenKey, out var seen);
            if (!string.IsNullOrEmpty(seen))
                foreach (var entry in seen.Split(';'))
                    if (entry.Length > 0) Seen.Add(entry);
            player.m_customData.TryGetValue(HomeKey, out var home);
            _home = home ?? "";
            ServerHasMod = false;
            HelloSentAt = -1f;
            _nextHello = 0f;
            _gaveUpLogged = false;
        }

        private static void NoteNearbyGates(Player player)
        {
            bool changed = false;
            foreach (var portal in GateRegistry.Loaded())
            {
                if (portal.m_nview == null || !portal.m_nview.IsValid()) continue;
                bool near = Vector3.Distance(portal.transform.position, player.transform.position) <= VisitRange;
                bool mine = false;
                var piece = portal.GetComponent<Piece>();
                if (piece != null) mine = piece.GetCreator() == player.GetPlayerID();
                if (!near && !mine) continue;

                changed |= Seen.Add(Key(portal.m_nview.GetZDO().m_uid));
            }

            if (changed) Save(player);
        }

        private static void Save(Player player)
        {
            player.m_customData[SeenKey] = string.Join(";", Seen);
            GatesChanged?.Invoke();
        }

        public static bool IsVisited(ZDOID id) => Seen.Contains(Key(id));

        /// <summary>Whether the player may see this gate in the list and on the map.</summary>
        public static bool IsVisible(ZDOID id) => !PortalHeimSG1Plugin.OnlyVisited.Value || IsVisited(id);

        public static bool IsHome(ZDOID id) => _home.Length > 0 && _home == Key(id);

        public static bool TryGetHome(out GateInfo home)
        {
            foreach (var gate in Gates)
            {
                if (IsHome(gate.Id))
                {
                    home = gate;
                    return true;
                }
            }

            home = default;
            return false;
        }

        public static void SetHome(ZDOID id, bool on)
        {
            var player = Player.m_localPlayer;
            if (player == null) return;
            _home = on ? Key(id) : "";
            player.m_customData[HomeKey] = _home;
            GatesChanged?.Invoke();
        }
    }

    /// <summary>The portals that are loaded near this player right now.</summary>
    internal static class GateRegistry
    {
        private static readonly List<TeleportWorld> All = new List<TeleportWorld>();

        public static void Add(TeleportWorld portal)
        {
            if (portal != null && !All.Contains(portal)) All.Add(portal);
        }

        public static List<TeleportWorld> Loaded()
        {
            All.RemoveAll(p => p == null);
            return All;
        }

        public static ZDOID IdOf(TeleportWorld portal) =>
            portal != null && portal.m_nview != null && portal.m_nview.IsValid() ? portal.m_nview.GetZDO().m_uid : ZDOID.None;

        /// <summary>The nearest loaded gate to the player, within range.</summary>
        public static TeleportWorld Nearest(Vector3 from, float range)
        {
            TeleportWorld best = null;
            float bestDistance = range;
            foreach (var portal in Loaded())
            {
                if (portal.m_nview == null || !portal.m_nview.IsValid()) continue;
                float d = Vector3.Distance(portal.transform.position, from);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = portal;
                }
            }

            return best;
        }
    }
}
