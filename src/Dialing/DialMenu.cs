using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// The window that opens when you use an idle gate: pick a gate to dial, mark a home gate, rename this gate, or
    /// hang up if it's already dialed. Enter dials the highlighted gate (your home gate to begin with), Esc closes it.
    /// </summary>
    internal static class DialMenu
    {
        private const int WindowId = 0x5C1;
        private const float CloseDistance = 10f;

        private static TeleportWorld _source;
        private static Vector2 _scroll;
        private static string _selected = "";
        private static bool _hadDefault;
        private static CursorLockMode _lockBefore;
        private static bool _visibleBefore;
        private static GUIStyle _title, _label, _small, _row, _rowSelected;

        public static bool IsOpen => _source != null;

        public static void Open(TeleportWorld portal)
        {
            if (portal == null) return;
            _source = portal;
            _scroll = Vector2.zero;
            _selected = "";
            _hadDefault = false;
            _lockBefore = Cursor.lockState;
            _visibleBefore = Cursor.visible;
            GateNet.RequestGates();
        }

        public static void Close()
        {
            if (_source == null) return;
            _source = null;
            Cursor.lockState = _lockBefore;
            Cursor.visible = _visibleBefore;
        }

        /// <summary>Sends a dial for the gate this player is standing at, if it's free. Used by the map and by quick-dial.</summary>
        public static bool DialFrom(TeleportWorld portal, GateInfo target)
        {
            var from = GateRegistry.IdOf(portal);
            if (from == ZDOID.None) return false;
            GateNet.RequestDial(from, target.Id);
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Dialing " + DisplayName(target) + "...");
            return true;
        }

        public static string DisplayName(GateInfo gate) => string.IsNullOrWhiteSpace(gate.Name) ? "(unnamed gate)" : gate.Name;

        public static void Draw()
        {
            if (_source == null) return;

            var player = Player.m_localPlayer;
            if (player == null || player.IsDead() || Vector3.Distance(player.transform.position, _source.transform.position) > CloseDistance)
            {
                Close();
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    e.Use();
                    Close();
                    return;
                }

                if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && TryDialSelected())
                {
                    e.Use();
                    return;
                }
            }

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            EnsureStyles(scale);

            float width = 520f * scale, height = 520f * scale;
            var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(rect, GUIContent.none);
            GUI.Window(WindowId, rect, DrawWindow, "Gate");
        }

        private static void DrawWindow(int id)
        {
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            var here = GateRegistry.IdOf(_source);
            GateInfo? self = null;
            foreach (var gate in GateClient.Gates)
                if (gate.Id == here) self = gate;

            GUILayout.Space(8f * scale);
            string name = self.HasValue ? DisplayName(self.Value) : "(this gate)";
            GUILayout.Label(name, _title);

            if (!self.HasValue)
            {
                GUILayout.Label("Asking the server...", _label);
                if (GUILayout.Button("Close", GUILayout.Height(30f * scale))) Close();
                return;
            }

            if (self.Value.State == GateState.Dialing)
            {
                string partner = "";
                foreach (var gate in GateClient.Gates)
                    if (gate.Id == self.Value.Partner) partner = DisplayName(gate);
                GUILayout.Label(partner.Length > 0
                    ? "Connected to " + partner + ". It closes by itself once everyone has gone."
                    : "This gate is set aside while another dial is open. It goes back to normal when that dial closes.", _label);
                GUILayout.Space(8f * scale);
                if (GUILayout.Button("Hang up", GUILayout.Height(34f * scale)))
                {
                    GateNet.RequestHangUp(here);
                    Close();
                    return;
                }
            }
            else
            {
                DrawList(here, self.Value, scale);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            // Renaming a gate that is mid-dial would cut the connection, so only offer it when the gate is free.
            if (self.Value.State != GateState.Dialing && GUILayout.Button("Rename this gate", GUILayout.Height(30f * scale))) Rename();
            if (GUILayout.Button("Close (Esc)", GUILayout.Height(30f * scale))) Close();
            GUILayout.EndHorizontal();
        }

        private static void DrawList(ZDOID here, GateInfo self, float scale)
        {
            var candidates = new List<GateInfo>();
            int hidden = 0;
            foreach (var gate in GateClient.Gates)
            {
                if (gate.Id == here) continue;
                if (GateClient.IsVisible(gate.Id)) candidates.Add(gate);
                else hidden++;
            }

            var origin = self.Position;
            candidates.Sort((a, b) => Vector3.Distance(a.Position, origin).CompareTo(Vector3.Distance(b.Position, origin)));

            if (!_hadDefault)
            {
                _hadDefault = true;
                foreach (var gate in candidates)
                    if (GateClient.IsHome(gate.Id) && GateClient.Dialable(gate.State)) _selected = GateClient.Key(gate.Id);
                if (_selected.Length == 0)
                    foreach (var gate in candidates)
                        if (gate.State == GateState.Idle)
                        {
                            _selected = GateClient.Key(gate.Id);
                            break;
                        }
            }

            if (self.State == GateState.Linked)
            {
                string pairedWith = "another gate";
                foreach (var gate in GateClient.Gates)
                    if (gate.Id == self.Partner) pairedWith = DisplayName(gate);
                GUILayout.Label("Paired with " + pairedWith + ". Dialing another gate sets that pair aside until the dial closes.", _small);
            }

            string note = candidates.Count == 0
                ? "No other gates to dial yet. Visit a gate and it will appear here."
                : "Pick a gate and press Dial (or Enter). The star marks your home gate.";
            GUILayout.Label(note, _small);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(300f * scale));
            foreach (var gate in candidates)
            {
                string key = GateClient.Key(gate.Id);
                bool free = GateClient.Dialable(gate.State);
                bool selected = key == _selected;
                int meters = Mathf.RoundToInt(Vector3.Distance(gate.Position, self.Position));

                GUILayout.BeginHorizontal(selected ? _rowSelected : _row);
                string star = GateClient.IsHome(gate.Id) ? "* " : "";
                string state = gate.State == GateState.Dialing ? "  (in use)" : (gate.State == GateState.Linked ? "  (paired)" : "");
                if (GUILayout.Button(star + DisplayName(gate) + "  -  " + meters + " m" + state, _label, GUILayout.ExpandWidth(true)) && free)
                    _selected = key;

                bool isHome = GateClient.IsHome(gate.Id);
                if (GUILayout.Button(isHome ? "Unset home" : "Set home", GUILayout.Width(100f * scale)))
                    GateClient.SetHome(gate.Id, !isHome);
                GUI.enabled = free;
                if (GUILayout.Button("Dial", GUILayout.Width(70f * scale)))
                {
                    _selected = key;
                    TryDialSelected();
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (hidden > 0 && PortalHeimSG1Plugin.OnlyVisited.Value)
                GUILayout.Label(hidden + (hidden == 1 ? " gate" : " gates") + " you haven't visited yet.", _small);
        }

        private static bool TryDialSelected()
        {
            if (_source == null || _selected.Length == 0) return false;
            foreach (var gate in GateClient.Gates)
            {
                if (GateClient.Key(gate.Id) != _selected || !GateClient.Dialable(gate.State)) continue;
                var portal = _source;
                Close();
                return DialFrom(portal, gate);
            }

            return false;
        }

        private static void Rename()
        {
            var portal = _source;
            Close();
            if (portal == null || Player.m_localPlayer == null) return;
            // Runs the game's own naming window.
            DialPatches.Bypass = true;
            try { portal.Interact(Player.m_localPlayer, false, false); }
            finally { DialPatches.Bypass = false; }
        }

        private static void EnsureStyles(float scale)
        {
            if (_title == null)
            {
                _title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _label = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.MiddleLeft };
                _small = new GUIStyle(GUI.skin.label) { wordWrap = true };
                _row = new GUIStyle();
                var tint = new Texture2D(1, 1);
                tint.SetPixel(0, 0, new Color(1f, 0.45f, 0.1f, 0.35f));
                tint.Apply();
                _rowSelected = new GUIStyle { normal = { background = tint } };
            }

            _title.fontSize = Mathf.RoundToInt(24f * scale);
            _label.fontSize = Mathf.RoundToInt(17f * scale);
            _small.fontSize = Mathf.RoundToInt(15f * scale);
            GUI.skin.button.fontSize = Mathf.RoundToInt(16f * scale);
        }
    }

    /// <summary>Stops the player running around and the camera spinning while the dial window is up.</summary>
    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class TakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (DialMenu.IsOpen) __result = false;
        }
    }

    /// <summary>
    /// The player controller has its own input gate, separate from the player's. Without this, a click in the dial
    /// window also made the player swing their weapon.
    /// </summary>
    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class ControllerTakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (DialMenu.IsOpen) __result = false;
        }
    }
}
