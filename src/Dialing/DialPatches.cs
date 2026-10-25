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
            if (Bypass || hold || human == null || human != Player.m_localPlayer) return true;
            // Shift+E opens the gate's storage.
            if (alt && PrivateArea.CheckAccess(__instance.transform.position) && PortalChest.TryOpen(__instance, human))
            {
                __result = true;
                return false;
            }

            if (!Active) return true;
            // Let the game deal with gates in someone else's protected area (it shows its own message).
            if (!PrivateArea.CheckAccess(__instance.transform.position)) return true;
            // Gates linked the old way (two sharing a name) behave exactly as before, including the naming window.
            if (!TryGetInfo(__instance, out var info) || (info.State == GateState.Linked && !GateClient.Dialable(info.State))) return true;

            DialMenu.Open(__instance);
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TeleportWorld.GetHoverText))]
        private static void HoverText_Postfix(TeleportWorld __instance, ref string __result)
        {
            if (!Active || !TryGetInfo(__instance, out var info) || (info.State == GateState.Linked && !GateClient.Dialable(info.State))) return;

            int lastLine = __result.LastIndexOf('\n');
            if (lastLine < 0) return;

            string use = Localization.instance.Localize("$KEY_Use");
            string text = __result.Substring(0, lastLine);
            // A dialed gate carries a temporary tag; show its real name instead.
            string tag = __instance.GetText();
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith("~sg1:") && !string.IsNullOrEmpty(info.Name)) text = text.Replace(tag, info.Name);
            text += info.State == GateState.Dialing
                ? $"\n[<color=yellow><b>{use}</b></color>] Gate options (hang up)"
                : $"\n[<color=yellow><b>{use}</b></color>] Dial a gate";
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
        private static Sprite _icon;

        // "Pick on map": the dial window opens the map, and a click on a gate there dials it from this gate.
        private static TeleportWorld _pickFrom;
        private static bool _pickSawMap;
        private static GUIStyle _hint;

        public static bool Picking => _pickFrom != null;

        public static void BeginPick(TeleportWorld from)
        {
            if (from == null || Minimap.instance == null) return;
            _pickFrom = from;
            _pickSawMap = false;
            Minimap.instance.SetMapMode(Minimap.MapMode.Large);
        }

        private static void UpdatePick()
        {
            if (_pickFrom == null) return;

            var player = Player.m_localPlayer;
            if (player == null || Vector3.Distance(player.transform.position, _pickFrom.transform.position) > 20f)
            {
                _pickFrom = null;
                return;
            }

            if (Minimap.IsOpen()) _pickSawMap = true;
            else if (_pickSawMap) _pickFrom = null;   // the map was closed: cancelled
        }

        /// <summary>The banner at the top of the map while picking a gate.</summary>
        public static void DrawHint()
        {
            if (_pickFrom == null || !Minimap.IsOpen()) return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            if (_hint == null) _hint = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _hint.fontSize = Mathf.RoundToInt(22f * scale);
            string name = "this gate";
            var id = GateRegistry.IdOf(_pickFrom);
            foreach (var gate in GateClient.Gates)
                if (gate.Id == id && !string.IsNullOrWhiteSpace(gate.Name)) name = gate.Name;

            float width = 640f * scale, height = 46f * scale;
            GUI.Box(new Rect((Screen.width - width) / 2f, 14f * scale, width, height), "Click a gate to dial it from " + name + "   (Esc to cancel)", _hint);
        }

        /// <summary>The gate-shaped map icon, painted in code the first time it's needed.</summary>
        private static Sprite GateIcon()
        {
            if (_icon != null) return _icon;
            var canvas = Art.MapIcon.Draw();
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false) { name = "SG1MapIcon" };
            texture.SetPixelData(canvas.Rgba, 0);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, false);
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            _icon = Sprite.Create(texture, new Rect(0, 0, canvas.Width, canvas.Height), new Vector2(0.5f, 0.5f), 100f);
            _icon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return _icon;
        }

        public static void Tick()
        {
            if (!_subscribed)
            {
                _subscribed = true;
                GateClient.GatesChanged += () => _dirty = true;
            }

            UpdatePick();

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
                    if (PortalHeimSG1Plugin.GateIconOnMap.Value) pin.m_icon = GateStyle.SpriteOf(pair.Value.Icon) ?? GateIcon();
                }
                else
                {
                    // Not saved with the map: the list comes from the server every time, so nothing goes stale in the save.
                    var added = map.AddPin(pair.Value.Position, PortalHeimSG1Plugin.PinIcon.Value, label, false, false);
                    if (PortalHeimSG1Plugin.GateIconOnMap.Value) added.m_icon = GateStyle.SpriteOf(pair.Value.Icon) ?? GateIcon();
                    Pins[pair.Key] = added;
                }
            }

            map.m_pinUpdateRequired = true;
        }

        /// <summary>True if the click was ours (on a gate pin).</summary>
        public static bool TryDialFromClick(Minimap map)
        {
            if (!PortalHeimSG1Plugin.DialingEnabled.Value || !GateClient.ServerHasMod) return false;
            bool picking = _pickFrom != null;
            if (!picking && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) return false;

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

            var here = picking ? _pickFrom : GateRegistry.Nearest(player.transform.position, 8f);
            if (!found) return true;
            if (here == null)
            {
                player.Message(MessageHud.MessageType.Center, "Stand at a gate to dial from the map.");
                return true;
            }

            DialMenu.DialFrom(here, target);
            if (picking)
            {
                _pickFrom = null;
                map.SetMapMode(Minimap.MapMode.None);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    internal static class MapClickPatch
    {
        private static bool Prefix(Minimap __instance) => !DialMap.TryDialFromClick(__instance);
    }
}
