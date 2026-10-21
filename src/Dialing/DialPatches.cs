using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>Changes what using a gate does, when the server supports dialing.</summary>
    [HarmonyPatch(typeof(TeleportWorld))]
    internal static class DialPatches
    {
        /// <summary>Set while we hand over to the game's own "name this portal" window.</summary>
        public static bool Bypass;

        private static bool Active => PortalHeimSG1Plugin.DialingEnabled.Value && GateClient.ServerHasMod;

        private static bool TryGetInfo(TeleportWorld portal, out GateInfo info)
        {
            var id = GateRegistry.IdOf(portal);
            foreach (var gate in GateClient.Gates)
            {
                if (gate.Id == id)
                {
                    info = gate;
                    return true;
                }
            }

            info = default;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportWorld.Interact))]
        private static bool Interact_Prefix(TeleportWorld __instance, Humanoid human, bool hold, bool alt, ref bool __result)
        {
            if (Bypass || hold || !Active || human == null || human != Player.m_localPlayer) return true;
            // Let the game deal with gates in someone else's protected area (it shows its own message).
            if (!PrivateArea.CheckAccess(__instance.transform.position)) return true;
            // Gates linked the old way (two sharing a name) behave exactly as before, including the naming window.
            if (!TryGetInfo(__instance, out var info) || info.State == GateState.Linked) return true;

            // Alt+use dials the home gate straight away.
            if (alt && info.State == GateState.Idle && GateClient.TryGetHome(out var home)
                && home.State == GateState.Idle && home.Id != info.Id)
            {
                DialMenu.DialFrom(__instance, home);
                __result = true;
                return false;
            }

            DialMenu.Open(__instance);
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TeleportWorld.GetHoverText))]
        private static void HoverText_Postfix(TeleportWorld __instance, ref string __result)
        {
            if (!Active || !TryGetInfo(__instance, out var info) || info.State == GateState.Linked) return;

            int lastLine = __result.LastIndexOf('\n');
            if (lastLine < 0) return;

            string use = Localization.instance.Localize("$KEY_Use");
            string text = __result.Substring(0, lastLine);
            // A dialed gate carries a temporary tag; show its real name instead.
            string tag = __instance.GetText();
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith("~sg1:") && !string.IsNullOrEmpty(info.Name)) text = text.Replace(tag, info.Name);
            text += info.State == GateState.Dialing
                ? $"\n[<color=yellow><b>{use}</b></color>] Gate options"
                : $"\n[<color=yellow><b>{use}</b></color>] Dial a gate";
            if (info.State == GateState.Idle && GateClient.TryGetHome(out var home) && home.State == GateState.Idle && home.Id != info.Id)
                text += $"\n[<color=yellow><b>Shift+{use}</b></color>] Dial home ({DialMenu.DisplayName(home)})";
            __result = text;
        }
    }

    /// <summary>Gate pins on the map, and Shift+click on one to dial it from the gate you're standing at.</summary>
    internal static class DialMap
    {
        private static readonly Dictionary<string, Minimap.PinData> Pins = new Dictionary<string, Minimap.PinData>();
        private static Minimap _forMap;
        private static bool _dirty = true;
        private static bool _subscribed;

        public static void Tick()
        {
            if (!_subscribed)
            {
                _subscribed = true;
                GateClient.GatesChanged += () => _dirty = true;
            }

            var map = Minimap.instance;
            if (map != _forMap)
            {
                // A new map (a world was loaded or left): the old pins went with it.
                Pins.Clear();
                _forMap = map;
                _dirty = true;
            }

            if (map == null || !_dirty) return;
            _dirty = false;
            Rebuild(map);
        }

        private static void Rebuild(Minimap map)
        {
            bool show = PortalHeimSG1Plugin.DialingEnabled.Value && PortalHeimSG1Plugin.GatePins.Value && GateClient.ServerHasMod;
            var wanted = new Dictionary<string, GateInfo>();
            if (show)
                foreach (var gate in GateClient.Gates)
                    if (GateClient.IsVisible(gate.Id)) wanted[GateClient.Key(gate.Id)] = gate;

            var stale = new List<string>();
            foreach (var pair in Pins)
                if (!wanted.ContainsKey(pair.Key)) stale.Add(pair.Key);
            foreach (var key in stale)
            {
                map.RemovePin(Pins[key]);
                Pins.Remove(key);
            }

            foreach (var pair in wanted)
            {
                string label = DialMenu.DisplayName(pair.Value);
                if (Pins.TryGetValue(pair.Key, out var pin))
                {
                    pin.m_name = label;
                    pin.m_pos = pair.Value.Position;
                }
                else
                {
                    // Not saved with the map: the list comes from the server every time, so nothing goes stale in the save.
                    Pins[pair.Key] = map.AddPin(pair.Value.Position, PortalHeimSG1Plugin.PinIcon.Value, label, false, false);
                }
            }

            map.m_pinUpdateRequired = true;
        }

        /// <summary>True if the click was ours (on a gate pin).</summary>
        public static bool TryDialFromClick(Minimap map)
        {
            if (!PortalHeimSG1Plugin.DialingEnabled.Value || !GateClient.ServerHasMod) return false;
            if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return false;

            var pin = map.GetClosestPin(map.ScreenToWorldPoint(ZInput.pointerPosition), map.PinInteractRadius);
            if (pin == null) return false;

            string key = null;
            foreach (var pair in Pins)
                if (pair.Value == pin) key = pair.Key;
            if (key == null) return false;

            var player = Player.m_localPlayer;
            if (player == null) return true;

            GateInfo target = default;
            bool found = false;
            foreach (var gate in GateClient.Gates)
                if (GateClient.Key(gate.Id) == key)
                {
                    target = gate;
                    found = true;
                }

            var here = GateRegistry.Nearest(player.transform.position, 8f);
            if (!found) return true;
            if (here == null)
            {
                player.Message(MessageHud.MessageType.Center, "Stand at a gate to dial from the map.");
                return true;
            }

            DialMenu.DialFrom(here, target);
            return true;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    internal static class MapClickPatch
    {
        private static bool Prefix(Minimap __instance) => !DialMap.TryDialFromClick(__instance);
    }
}
