using System.Collections.Generic;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Two gates that are paired share one chest: whatever goes into one is in the other. Each gate keeps its own copy of the
    /// contents in its saved data (it is an ordinary chest), and the server copies the newest copy to the other gate of the pair.
    /// While a pair is set aside for a dial, nothing is copied (they aren't paired then); it picks up again when the pair is back.
    /// The server also marks each gate whose other end is open, so only one end can be open at a time.
    /// </summary>
    internal static class StorageSync
    {
        public static readonly int KeyStamp = "sg1_stamp".GetStableHashCode();
        public static readonly int KeyBusy = "sg1_busy".GetStableHashCode();
        private static float _next;

        public static long Now() => ZNet.instance != null ? (long)(ZNet.instance.GetTimeSeconds() * 1000.0) : 0L;

        private static bool IsOpen(ZDO zdo) => zdo.GetInt(ZDOVars.s_inUse) == 1;

        public static void Tick()
        {
            if (!PortalHeimSG1Plugin.StorageEnabled.Value || Time.time < _next) return;
            _next = Time.time + 2f;

            var seen = new HashSet<ZDOID>();
            foreach (var zdo in ZDOMan.instance.GetPortalList())
            {
                // Paired gates share a chest, and so do two gates in a dial (while it lasts).
                var state = GateServer.StateOf(zdo);
                if (zdo == null || seen.Contains(zdo.m_uid) || (state != GateState.Linked && state != GateState.Dialing)) continue;
                var other = ZDOMan.instance.GetZDO(GateServer.PartnerOf(zdo));
                if (other == null || GateServer.StateOf(other) != state || GateServer.PartnerOf(other) != zdo.m_uid) continue;
                seen.Add(zdo.m_uid);
                seen.Add(other.m_uid);
                SyncPair(zdo, other);
            }
        }

        // While a dial is open, the gate dialed from keeps its own contents aside here.
        private static readonly int KeyStash = "sg1_stash".GetStableHashCode();
        private static readonly int KeyStashStamp = "sg1_stashstamp".GetStableHashCode();
        private static readonly int KeyStashed = "sg1_stashed".GetStableHashCode();

        /// <summary>A gate dialing another puts its own contents aside and shows the dialed gate's contents instead.</summary>
        public static void Stash(ZDO source, ZDO destination)
        {
            if (!PortalHeimSG1Plugin.StorageEnabled.Value || source == null || destination == null || source.GetInt(KeyStashed) == 1) return;

            source.SetOwner(ZDOMan.GetSessionID());
            source.Set(KeyStash, source.GetByteArray(ZDOVars.s_items) ?? new byte[0]);
            source.Set(KeyStashStamp, source.GetLong(KeyStamp));
            source.Set(KeyStashed, 1);
            source.Set(ZDOVars.s_items, destination.GetByteArray(ZDOVars.s_items) ?? new byte[0]);
            source.Set(KeyStamp, destination.GetLong(KeyStamp));
            source.SetOwner(0L);
            ZDOMan.instance.ForceSendZDO(source.m_uid);
        }

        /// <summary>At the end of a dial: anything changed through the source gate goes to the destination.</summary>
        public static void Finish(ZDO source, ZDO destination)
        {
            if (!PortalHeimSG1Plugin.StorageEnabled.Value || source.GetInt(KeyStashed) != 1) return;
            if (source.GetLong(KeyStamp) > destination.GetLong(KeyStamp))
            {
                destination.SetOwner(ZDOMan.GetSessionID());
                destination.Set(ZDOVars.s_items, source.GetByteArray(ZDOVars.s_items) ?? new byte[0]);
                destination.Set(KeyStamp, source.GetLong(KeyStamp));
                destination.SetOwner(0L);
                ZDOMan.instance.ForceSendZDO(destination.m_uid);
            }
        }

        /// <summary>Gives a gate its own contents back (also used to clean up after a restart mid-dial).</summary>
        public static void RestoreStash(ZDO zdo)
        {
            if (zdo == null || zdo.GetInt(KeyStashed) != 1) return;
            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.Set(ZDOVars.s_items, zdo.GetByteArray(KeyStash) ?? new byte[0]);
            zdo.Set(KeyStamp, zdo.GetLong(KeyStashStamp));
            zdo.Set(KeyStash, new byte[0]);
            zdo.Set(KeyStashed, 0);
            zdo.SetOwner(0L);
            ZDOMan.instance.ForceSendZDO(zdo.m_uid);
        }

        private static void SetIfChanged(ZDO zdo, int key, int value)
        {
            if (zdo.GetInt(key) != value) zdo.Set(key, value);
        }

        private static void SyncPair(ZDO a, ZDO b)
        {
            // Tell each end whether the other is open.
            SetIfChanged(a, KeyBusy, IsOpen(b) ? 1 : 0);
            SetIfChanged(b, KeyBusy, IsOpen(a) ? 1 : 0);
            if (IsOpen(a) || IsOpen(b)) return;

            long stampA = a.GetLong(KeyStamp), stampB = b.GetLong(KeyStamp);
            if (stampA == stampB) return;

            var source = stampA > stampB ? a : b;
            var target = stampA > stampB ? b : a;
            byte[] items = source.GetByteArray(ZDOVars.s_items);

            target.SetOwner(ZDOMan.GetSessionID());
            // An emptied chest is copied too (an empty list is fine).
            target.Set(ZDOVars.s_items, items ?? new byte[0]);
            target.Set(KeyStamp, source.GetLong(KeyStamp));
            // Let a player's game take charge of the chest again when it's near it.
            target.SetOwner(0L);
            ZDOMan.instance.ForceSendZDO(target.m_uid);
        }
    }
}
