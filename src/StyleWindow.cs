using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>The window for dressing up a gate: its colour, a word spelled in runes on its chevrunes, and an icon.</summary>
    internal static class StyleWindow
    {
        private const int WindowId = 0x5C2;
        private static readonly Color[] Swatches =
        {
            new Color(1f, 0.55f, 0.1f), new Color(1f, 0.32f, 0.32f), new Color(1f, 0.93f, 0.35f), new Color(0.61f, 0.8f, 0.39f),
            new Color(0.3f, 0.82f, 0.88f), new Color(0.39f, 0.71f, 0.96f), new Color(0.73f, 0.41f, 0.78f), new Color(1f, 1f, 1f),
        };

        private static GateInfo _gate;
        private static bool _open;
        private static string _color = "", _runes = "", _icon = "", _query = "", _message = "";
        private static bool _pair = true;
        private static bool _picking;
        private static Vector2 _scroll;
        private static List<(string prefab, string label)> _results = new List<(string, string)>();
        private static Rect _rect;
        private static CursorLockMode _lockBefore;
        private static bool _visibleBefore;

        public static bool IsOpen => _open;
        private static float Scale => Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);

        public static void Open(GateInfo gate)
        {
            _gate = gate;
            _open = true;
            _color = gate.Color ?? "";
            _runes = gate.Runes ?? "";
            _icon = gate.Icon ?? "";
            _query = "";
            _message = "";
            _picking = false;
            _pair = true;
            _results = GateStyle.Search("");
            _lockBefore = Cursor.lockState;
            _visibleBefore = Cursor.visible;
            float s = Scale;
            _rect = new Rect((Screen.width - 520f * s) / 2f, (Screen.height - 560f * s) / 2f, 520f * s, 560f * s);
        }

        public static void Close()
        {
            if (!_open) return;
            _open = false;
            Cursor.lockState = _lockBefore;
            Cursor.visible = _visibleBefore;
        }

        public static void Draw()
        {
            if (!_open) return;
            var player = Player.m_localPlayer;
            if (player == null || player.IsDead())
            {
                Close();
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                e.Use();
                if (_picking) _picking = false; else Close();
                return;
            }

            float s = Scale;
            GUI.skin.label.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.button.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.textField.fontSize = Mathf.RoundToInt(16f * s);
            GUI.skin.toggle.fontSize = Mathf.RoundToInt(16f * s);
            GUI.Box(_rect, GUIContent.none);
            _rect = GUI.Window(WindowId, _rect, DrawWindow, "Style: " + DialMenu.DisplayName(_gate));
        }

        private static void DrawWindow(int id)
        {
            float s = Scale;
            GUILayout.Space(6f * s);
            if (_picking) DrawPicker(s); else DrawMain(s);
            GUILayout.FlexibleSpace();
            if (_message.Length > 0) GUILayout.Label(_message);
            if (!_picking && GUILayout.Button("Close (Esc)", GUILayout.Height(28f * s))) Close();
            GUI.DragWindow();
        }

        private static void DrawMain(float s)
        {
            GUILayout.Label("Colour of its chevrunes:");
            GUILayout.BeginHorizontal();
            var old = GUI.color;
            foreach (var swatch in Swatches)
            {
                var rect = GUILayoutUtility.GetRect(30f * s, 24f * s, GUILayout.Width(30f * s));
                GUI.color = swatch;
                if (GUI.Button(rect, GUIContent.none)) _color = GateStyle.Hex(swatch);
            }

            GUI.color = old;
            _color = GUILayout.TextField(_color ?? "", 9, GUILayout.Width(90f * s));
            if (GUILayout.Button("Default", GUILayout.Width(80f * s))) _color = "";
            GUILayout.EndHorizontal();

            GUILayout.Space(8f * s);
            GUILayout.Label("Spell a word in runes (up to nine letters; the rest keep the usual runes):");
            GUILayout.BeginHorizontal();
            _runes = GUILayout.TextField(_runes ?? "", 12, GUILayout.Height(28f * s));
            if (GUILayout.Button("Use its name", GUILayout.Width(130f * s))) _runes = GateStyle.Clean(_gate.Name);
            if (GUILayout.Button("Default", GUILayout.Width(80f * s))) _runes = "";
            GUILayout.EndHorizontal();
            GUILayout.Label("Runes: " + (GateStyle.Clean(_runes).Length > 0 ? GateStyle.Clean(_runes) : "the usual ones"));

            GUILayout.Space(8f * s);
            GUILayout.BeginHorizontal();
            var sprite = GateStyle.SpriteOf(_icon);
            var box = GUILayoutUtility.GetRect(40f * s, 40f * s, GUILayout.Width(40f * s));
            if (sprite != null) GateStyle.DrawSprite(box, sprite, Color.white);
            GUILayout.Label(sprite != null ? "Icon: " + GateStyle.NameOf(_icon) : "No icon");
            if (GUILayout.Button("Choose icon...", GUILayout.Width(150f * s), GUILayout.Height(30f * s)))
            {
                _picking = true;
                _results = GateStyle.Search(_query);
            }

            if (GUILayout.Button("No icon", GUILayout.Width(90f * s), GUILayout.Height(30f * s))) _icon = "";
            GUILayout.EndHorizontal();

            if (_gate.State == GateState.Linked)
                _pair = GUILayout.Toggle(_pair, " Apply to both gates of this pair");

            GUILayout.Space(8f * s);
            GUILayout.Label("The icon shows next to the gate in the list and on the map; the colour and runes show on the gate itself.");
            GUILayout.Space(8f * s);
            if (GUILayout.Button("Save", GUILayout.Height(34f * s)))
            {
                GateNet.RequestStyle(_gate.Id, _color, _runes, _icon, _pair && _gate.State == GateState.Linked);
                _message = "Saved.";
            }
        }

        private static void DrawPicker(float s)
        {
            GUILayout.Label("Search the game's items by name:");
            string before = _query;
            _query = GUILayout.TextField(_query, 30, GUILayout.Height(28f * s));
            if (_query != before) _results = GateStyle.Search(_query);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(380f * s));
            foreach (var (prefab, label) in _results)
            {
                GUILayout.BeginHorizontal();
                var box = GUILayoutUtility.GetRect(32f * s, 32f * s, GUILayout.Width(32f * s));
                GateStyle.DrawSprite(box, GateStyle.SpriteOf(prefab), Color.white);
                if (GUILayout.Button(label, GUILayout.Height(32f * s)))
                {
                    _icon = prefab;
                    _picking = false;
                }

                GUILayout.EndHorizontal();
            }

            if (_results.Count == 0) GUILayout.Label("Nothing matches that.");
            GUILayout.EndScrollView();
            if (GUILayout.Button("Back", GUILayout.Height(30f * s))) _picking = false;
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class StyleTakeInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (StyleWindow.IsOpen) __result = false;
        }
    }

    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class StyleControllerInputPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (StyleWindow.IsOpen) __result = false;
        }
    }
}
