using System.Collections.Generic;
using System.Linq;
using System.Text;
using PortalHeimSG1.Art;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PortalHeimSG1
{
    /// <summary>
    /// Replaces the spinning graphic on the loading screen shown during a long portal trip with a
    /// miniature gate: the rune ring slowly turning inside a frame of lit Chevrunes, with rippling water in the middle.
    /// </summary>
    internal static class TravelScreen
    {
        private const float Size = 420f;

        private static Sprite _runes, _frame;
        private static bool _loggedVanilla;

        public static void Build(Hud hud)
        {
            if (PortalHeimSG1Plugin.Travel.Value != TravelMode.Gate) return;
            var panel = hud != null ? hud.m_teleportingProgress : null;
            if (panel == null)
            {
                PortalHeimSG1Plugin.Log.LogWarning("Couldn't find the teleport loading panel; leaving the loading screen as it is.");
                return;
            }
            if (panel.transform.Find("SG1Travel") != null) return;

            EnsureSprites();
            LogVanillaOnce(panel.transform);

            var root = new GameObject("SG1Travel", typeof(RectTransform));
            root.transform.SetParent(panel.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Size, Size);
            rect.anchoredPosition = Vector2.zero;

            // Back to front: the water, the turning rune ring, then the frame with its Chevrunes.
            var water = NewImage(root.transform, "Water", null, Size * Travel.WaterRadius * 2f + 6f);
            var runes = NewImage(root.transform, "Runes", _runes, Size);
            NewImage(root.transform, "Frame", _frame, Size);

            // The game's own spinner is drawn by Images under this panel (text stays). Hide those.
            var vanilla = panel.GetComponentsInChildren<Graphic>(true)
                .Where(g => !g.transform.IsChildOf(root.transform) && !(g is TMP_Text))
                .ToList();

            var animator = root.AddComponent<TravelAnimator>();
            animator.Setup(water, (RectTransform)runes.transform, vanilla);
            PortalHeimSG1Plugin.Log.LogInfo($"Travel screen built; hid {vanilla.Count} of the game's own graphics on the teleport panel.");
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = sprite != null;
            return image;
        }

        private static void EnsureSprites()
        {
            if (_runes != null && _frame != null) return;
            _runes = ToSprite(Travel.RuneRing(), "SG1TravelRunes");
            _frame = ToSprite(Travel.Frame(), "SG1TravelFrame");
        }

        private static Sprite ToSprite(Pixels canvas, string name)
        {
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false) { name = name };
            texture.SetPixelData(canvas.Rgba, 0);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, true);
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            var sprite = Sprite.Create(texture, new Rect(0, 0, canvas.Width, canvas.Height), new Vector2(0.5f, 0.5f));
            sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        /// <summary>Writes what the game's teleport panel is made of to the log once, so it can be tuned if needed.</summary>
        private static void LogVanillaOnce(Transform panel)
        {
            if (_loggedVanilla) return;
            _loggedVanilla = true;
            var text = new StringBuilder("The game's teleport panel contains:");
            int lines = 0;
            void Walk(Transform t, int depth)
            {
                if (lines++ > 40) return;
                var parts = t.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name);
                text.Append("\n  ").Append(new string(' ', depth * 2)).Append(t.name).Append(" [").Append(string.Join(", ", parts)).Append(']');
                foreach (Transform child in t) Walk(child, depth + 1);
            }
            Walk(panel, 0);
            PortalHeimSG1Plugin.Log.LogInfo(text.ToString());
        }
    }

    /// <summary>Turns the rune ring, plays the water, and keeps the game's own spinner hidden.</summary>
    internal class TravelAnimator : MonoBehaviour
    {
        private const float WaterFramesPerSecond = 16f;
        private const float RuneDegreesPerSecond = -22f;

        private Image _water;
        private RectTransform _runes;
        private List<Graphic> _vanilla;
        private Sprite[] _waterSprites;
        private float _clock;

        public void Setup(Image water, RectTransform runes, List<Graphic> vanilla)
        {
            _water = water;
            _runes = runes;
            _vanilla = vanilla;
        }

        private void Update()
        {
            // Loading screens can run with the clock stopped, so use real time.
            float dt = Time.unscaledDeltaTime;
            _runes.Rotate(0f, 0f, RuneDegreesPerSecond * dt);

            if (_waterSprites == null && WaterSurface.Ready) BuildWaterSprites();
            if (_waterSprites == null) return;

            _clock += dt;
            _water.sprite = _waterSprites[(int)(_clock * WaterFramesPerSecond) % _waterSprites.Length];
            _water.enabled = true;
        }

        private void LateUpdate()
        {
            // Something may switch the game's own graphics back on; keep them off while this panel is up.
            foreach (var graphic in _vanilla)
                if (graphic != null && graphic.enabled) graphic.enabled = false;
        }

        private void BuildWaterSprites()
        {
            var sprites = new Sprite[WaterSurface.FrameCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                var texture = WaterSurface.FrameAt(i);
                sprites[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprites[i].hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }
            _waterSprites = sprites;
        }
    }
}
