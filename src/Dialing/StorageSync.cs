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
                if (zdo == null || seen.Contains(zdo.m_uid) || GateServer.StateOf(zdo) != GateState.Linked) continue;
                var other = ZDOMan.instance.GetZDO(GateServer.PartnerOf(zdo));
                if (other == null || GateServer.StateOf(other) != GateState.Linked || GateServer.PartnerOf(other) != zdo.m_uid) continue;
                seen.Add(zdo.m_uid);
                seen.Add(other.m_uid);
                SyncPair(zdo, other);
            }
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
