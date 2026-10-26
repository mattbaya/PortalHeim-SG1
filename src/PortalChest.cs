using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// A chest built into every portal. It's a normal Container on the portal itself, so it saves with the gate and works from
    /// either side. Shift+E on the gate (or the button in the dial window) opens it, and the hatch in front lifts while it's open.
    /// </summary>
    internal static class PortalChest
    {
        public static void Attach(TeleportWorld portal)
        {
            if (!PortalHeimSG1Plugin.StorageEnabled.Value || portal == null || portal.GetComponent<Container>() != null) return;
            var view = portal.m_nview;
            if (view == null || !view.IsValid()) return;

            var container = portal.gameObject.AddComponent<Container>();
            container.m_name = "Gate storage";
            container.m_width = PortalHeimSG1Plugin.StorageColumns.Value;
            container.m_height = PortalHeimSG1Plugin.StorageRows.Value;
            // The container was set up with its default size as it was added, so make it again at ours and load what the gate holds.
            container.m_inventory = new Inventory(container.m_name, null, container.m_width, container.m_height);
            container.m_inventory.m_onChanged += container.OnContainerChanged;
            container.Load();
        }

        /// <summary>Opens the gate's storage for this player, if it has any. Returns true if it did.</summary>
        public static bool TryOpen(TeleportWorld portal, Humanoid human)
        {
            var container = portal != null ? portal.GetComponent<Container>() : null;
            if (container == null || human == null) return false;
            if (container.m_nview != null && container.m_nview.IsValid() && container.m_nview.GetZDO().GetInt(StorageSync.KeyBusy) > 0)
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "The other end's storage is open");
                return true;
            }

            container.Interact(human, false, false);
            return true;
        }

        /// <summary>The gate that owns the chest that's open (so its hatch can lift).</summary>
        public static GateRing RingOf(Container container) =>
            container != null ? container.GetComponentInChildren<GateRing>(true) : null;
    }

    /// <summary>Whenever a gate's storage changes, note when, so the server knows which end has the newest contents.</summary>
    [HarmonyPatch(typeof(Container), "Save")]
    internal static class StorageStampPatch
    {
        private static void Postfix(Container __instance)
        {
            if (__instance.GetComponent<TeleportWorld>() == null || __instance.m_nview == null || !__instance.m_nview.IsValid()) return;
            __instance.m_nview.GetZDO().Set(StorageSync.KeyStamp, StorageSync.Now());
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    internal static class StorageOpenedPatch
    {
        private static void Postfix(Container container)
        {
            var ring = PortalChest.RingOf(container);
            if (ring != null) ring.LidTarget = 1f;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class StorageClosedPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            var ring = PortalChest.RingOf(__instance.m_currentContainer);
            if (ring != null) ring.LidTarget = 0f;
        }
    }

    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.GetHoverText))]
    internal static class StorageHoverPatch
    {
        private static void Postfix(TeleportWorld __instance, ref string __result)
        {
            if (!PortalHeimSG1Plugin.StorageEnabled.Value || __instance.GetComponent<Container>() == null) return;
            __result += "\n[<color=yellow><b>Shift+E</b></color>] Open storage";
        }
    }
}
