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
        private static Rect _rect;
        private static bool _placed;
        private static string _selected = "";
        private static bool _hadDefault;
        private static string _renaming = "";
        private static string _expanded = "";
        private static GUIStyle _nameStyle;
        private static string _renameText = "";
        private static CursorLockMode _lockBefore;
        private static bool _visibleBefore;
        private static GUIStyle _title, _label, _small, _row, _rowSelected;

        public static bool IsOpen => _source != null;

        public static void Open(TeleportWorld portal)
        {
            if (portal == null) return;
            _source = portal;
            _placed = false;
            _scroll = Vector2.zero;
            _selected = "";
            _hadDefault = false;
            _renaming = "";
            _expanded = "";
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
            GateClient.NoteDialed(target.Id);
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

                if (_renaming.Length == 0 && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && TryDialSelected())
                {
                    e.Use();
                    return;
                }
            }

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            EnsureStyles(scale);

            float width = 680f * scale, height = 580f * scale;
            if (!_placed)
            {
                var saved = PortalHeimSG1Plugin.DialWindowPos.Value;
                float x = saved.x < 0f ? (Screen.width - width) / 2f : saved.x;
                float y = saved.y < 0f ? (Screen.height - height) / 2f : saved.y;
                _rect = new Rect(x, y, width, height);
                _placed = true;
            }

            _rect.width = width;
            _rect.height = height;
            _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, Screen.width - width));
            _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, Screen.height - height));
            GUI.Box(_rect, GUIContent.none);
            _rect = GUI.Window(WindowId, _rect, DrawWindow, "Gate");
            // Remember where it was dropped (not on every frame of the drag).
            if (Event.current.type == EventType.MouseUp)
                PortalHeimSG1Plugin.DialWindowPos.Value = new Vector2(_rect.x, _rect.y);
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
            if (self.Value.State != GateState.Dialing && GUILayout.Button("Pick on map", GUILayout.Height(30f * scale)))
            {
                var from = _source;
                Close();
                DialMap.BeginPick(from);
                return;
            }

            if (PortalHeimSG1Plugin.StorageEnabled.Value && GUILayout.Button("Open storage", GUILayout.Height(30f * scale)))
            {
                var portal = _source;
                Close();
                PortalChest.TryOpen(portal, Player.m_localPlayer);
                return;
            }

            if (self.Value.State != GateState.Dialing && GUILayout.Button("Rename this gate", GUILayout.Height(30f * scale))) Rename();
            if (GUILayout.Button("Close (Esc)", GUILayout.Height(30f * scale))) Close();
            GUILayout.EndHorizontal();
            // Drag the window by any part that isn't a control.
            GUI.DragWindow();
        }

        private static void DrawList(ZDOID here, GateInfo self, float scale)
        {
            var candidates = new List<GateInfo>();
            int hidden = 0;
            foreach (var gate in GateClient.Gates)
            {
                if (gate.Id == here) continue;
                // A gate paired with this one isn't something to dial.
                if (self.State == GateState.Linked && gate.Id == self.Partner) continue;
                if (!GateClient.IsVisible(gate.Id)) { hidden++; continue; }
                // A pair of gates is one entry: the one with the lower id stands for both.
                if (gate.State == GateState.Linked && gate.Partner != ZDOID.None && gate.Partner != here
                    && GateClient.IsVisible(gate.Partner) && string.CompareOrdinal(GateClient.Key(gate.Partner), GateClient.Key(gate.Id)) < 0)
                    continue;
                candidates.Add(gate);
            }

            var origin = self.Position;
            // How far an entry is: for a pair, the nearer of its two gates.
            float Dist(GateInfo g)
            {
                float d = Vector3.Distance(g.Position, origin);
                if (g.State == GateState.Linked)
                    foreach (var other in GateClient.Gates)
                        if (other.Id == g.Partner) d = Mathf.Min(d, Vector3.Distance(other.Position, origin));
                return d;
            }

            bool byDistance = PortalHeimSG1Plugin.SortByDistance.Value;
            // Favorites first, then the ones you dialed lately (latest first), then the rest; nearest first within each.
            int Group(GateInfo g) => GateClient.IsFavorite(g.Id) ? 0 : GateClient.RecentRank(g.Id) >= 0 ? 1 : 2;
            candidates.Sort((a, b) =>
            {
                int byGroup = Group(a).CompareTo(Group(b));
                if (byGroup != 0) return byGroup;
                if (Group(a) == 1) return GateClient.RecentRank(a.Id).CompareTo(GateClient.RecentRank(b.Id));
                if (byDistance) return Dist(a).CompareTo(Dist(b));
                int byName = string.Compare(a.Name ?? "", b.Name ?? "", System.StringComparison.CurrentCultureIgnoreCase);
                return byName != 0 ? byName : Dist(a).CompareTo(Dist(b));
            });

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

            if (GUILayout.Button(byDistance ? "Sorted nearest first (click for A-Z)" : "Sorted A-Z (click for nearest first)", GUILayout.Height(24f * scale)))
                PortalHeimSG1Plugin.SortByDistance.Value = !byDistance;

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(330f * scale));
            int lastGroup = -1;
            foreach (var gate in candidates)
            {
                int group = GateClient.IsFavorite(gate.Id) ? 0 : GateClient.RecentRank(gate.Id) >= 0 ? 1 : 2;
                if (group != lastGroup)
                {
                    lastGroup = group;
                    if (candidates.Exists(g => (GateClient.IsFavorite(g.Id) || GateClient.RecentRank(g.Id) >= 0) && g.Id != gate.Id) || group != 2)
                        GUILayout.Label(group == 0 ? "Favorites" : group == 1 ? "Recently dialed" : "All gates", _small);
                }

                string key = GateClient.Key(gate.Id);
                bool free = GateClient.Dialable(gate.State);
                bool selected = key == _selected;
                int meters = Mathf.RoundToInt(Dist(gate));

                if (_renaming == key)
                {
                    DrawRenameRow(gate);
                    continue;
                }

                // A row: its icon, its name in its own colour with how far it is, and the buttons.
                var sprite = GateStyle.SpriteOf(gate.Icon);
                GateStyle.TryParse(gate.Color, out var tint);
                string nameColor = string.IsNullOrEmpty(gate.Color) ? "#ffd9a0" : GateStyle.Hex(tint);
                bool isHome = GateClient.IsHome(gate.Id);
                bool isFav = GateClient.IsFavorite(gate.Id);
                string state = gate.State == GateState.Dialing ? "in use" : gate.State == GateState.Linked ? "pair" : "";
                string line = meters + " m" + (state.Length > 0 ? "  -  " + state : "") + (isHome ? "  -  home" : "");

                GUILayout.BeginHorizontal(selected ? _rowSelected : _row, GUILayout.Height(46f * scale));
                var iconBox = GUILayoutUtility.GetRect(40f * scale, 40f * scale, GUILayout.Width(40f * scale));
                if (sprite != null) GateStyle.DrawSprite(iconBox, sprite, Color.white);
                else if (!string.IsNullOrEmpty(gate.Color)) { var old = GUI.color; GUI.color = tint; GUI.DrawTexture(new Rect(iconBox.x + 10f * scale, iconBox.y + 10f * scale, 20f * scale, 20f * scale), Texture2D.whiteTexture); GUI.color = old; }

                GUILayout.BeginVertical();
                if (GUILayout.Button((isFav ? "\u2605 " : "") + "<color=" + nameColor + "><b>" + DisplayName(gate) + "</b></color>", _nameStyle, GUILayout.ExpandWidth(true)) && free)
                    _selected = key;
                GUILayout.Label(line, _small);
                GUILayout.EndVertical();

                GUI.enabled = free;
                if (GUILayout.Button("Dial", GUILayout.Width(76f * scale), GUILayout.Height(34f * scale)))
                {
                    _selected = key;
                    TryDialSelected();
                }

                GUI.enabled = true;
                if (GUILayout.Button(isFav ? "Unfav" : "Fav", GUILayout.Width(64f * scale), GUILayout.Height(34f * scale)))
                    GateClient.SetFavorite(gate.Id, !isFav);
                if (GUILayout.Button(_expanded == key ? "Less" : "More", GUILayout.Width(64f * scale), GUILayout.Height(34f * scale)))
                    _expanded = _expanded == key ? "" : key;
                GUILayout.EndHorizontal();

                if (_expanded == key)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(46f * scale);
                    if (GUILayout.Button(isHome ? "Unset home" : "Set home", GUILayout.Height(28f * scale))) GateClient.SetHome(gate.Id, !isHome);
                    GUI.enabled = gate.State != GateState.Dialing;
                    if (GUILayout.Button(gate.State == GateState.Linked ? "Rename pair" : "Rename", GUILayout.Height(28f * scale)))
                    {
                        _renaming = key;
                        _renameText = gate.Name ?? "";
                    }

                    if (GUILayout.Button("Style...", GUILayout.Height(28f * scale)))
                    {
                        var target = gate;
                        Close();
                        StyleWindow.Open(target);
                        return;
                    }

                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(2f * scale);
            }

            GUILayout.EndScrollView();

            if (hidden > 0 && PortalHeimSG1Plugin.OnlyVisited.Value)
                GUILayout.Label(hidden + (hidden == 1 ? " gate" : " gates") + " you haven't visited yet.", _small);
        }

        /// <summary>A gate's row while it's being renamed: a text box and OK / Cancel. The name changes for everyone.</summary>
        private static void DrawRenameRow(GateInfo gate)
        {
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            GUILayout.BeginHorizontal(_rowSelected);
            GUI.SetNextControlName("SG1Rename");
            _renameText = GUILayout.TextField(_renameText, 24, GUILayout.ExpandWidth(true));
            var e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
            if (enter) e.Use();
            if (GUILayout.Button("OK", GUILayout.Width(60f * scale)) || enter)
            {
                GateNet.RequestRename(gate.Id, _renameText, gate.State == GateState.Linked);
                _renaming = "";
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80f * scale))) _renaming = "";
            GUILayout.EndHorizontal();
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
                _nameStyle = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleLeft, wordWrap = false };
                var tint = new Texture2D(1, 1);
                tint.SetPixel(0, 0, new Color(1f, 0.45f, 0.1f, 0.35f));
                tint.Apply();
                _rowSelected = new GUIStyle { normal = { background = tint } };
            }

            _title.fontSize = Mathf.RoundToInt(24f * scale);
            _label.fontSize = Mathf.RoundToInt(17f * scale);
            _small.fontSize = Mathf.RoundToInt(14f * scale);
            _nameStyle.fontSize = Mathf.RoundToInt(19f * scale);
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

    /// <summary>Keeps the pointer steady while the dial or style window is up (the game otherwise takes the mouse back every frame).</summary>
    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    internal static class KeepCursorPatch
    {
        private static void Postfix()
        {
            if (!DialMenu.IsOpen && !StyleWindow.IsOpen) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
