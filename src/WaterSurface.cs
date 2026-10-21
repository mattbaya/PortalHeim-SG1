using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// The rippling water that fills an open gate. The frames are painted in code (Art/Water.cs) a few at a
    /// time after the game starts, then played in a loop on one shared material, so every open gate ripples in step.
    /// </summary>
    internal static class WaterSurface
    {
        private const float FramesPerSecond = 16f;

        private static readonly List<Texture2D> Frames = new List<Texture2D>();
        private static Material _material;
        private static int _index;
        private static float _nextFrame;

        /// <summary>How many gates are showing water right now (frames only advance while there are some).</summary>
        public static int Showing;

        public static bool Failed { get; private set; }

        public static bool Ready => !Failed && Frames.Count == Art.Water.Frames && _material != null;

        /// <summary>True if the water can be used at all (so the vanilla swirl can be hidden).</summary>
        public static bool Usable => !Failed && Material != null;

        public static Material Material
        {
            get
            {
                if (_material != null || Failed) return _material;

                // A flat, unlit, see-through shader. Try a few; every one is a standard Unity shader.
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default") ?? Shader.Find("Particles/Standard Unlit");
                if (shader == null)
                {
                    PortalHeimSG1Plugin.Log.LogWarning("No see-through shader available; keeping the portal's own swirl.");
                    Failed = true;
                    return null;
                }

                _material = new Material(shader) { name = "SG1Water", renderQueue = 3000 };
                _material.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                PortalHeimSG1Plugin.Log.LogInfo($"Water shader: {shader.name}");
                return _material;
            }
        }

        /// <summary>Paints the frames over a number of game frames so there's no stutter.</summary>
        public static IEnumerator Build()
        {
            if (Material == null) yield break;
            Frames.Clear();

            for (int i = 0; i < Art.Water.Frames; i++)
            {
                yield return null; // one frame per game frame
                try
                {
                    var canvas = Art.Water.Frame(i);
                    var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false) { name = $"SG1Water{i}" };
                    texture.SetPixelData(canvas.Rgba, 0);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.Apply(false, true);
                    texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    Frames.Add(texture);
                }
                catch (System.Exception e)
                {
                    Failed = true;
                    PortalHeimSG1Plugin.Log.LogError($"Couldn't paint the water (frame {i}); keeping the portal's own swirl. {e}");
                    yield break;
                }
            }

            _material.mainTexture = Frames[0];
            PortalHeimSG1Plugin.Log.LogInfo($"Water ready ({Frames.Count} frames).");
        }

        /// <summary>Call every frame: advances the ripples while any gate is showing water.</summary>
        public static void Tick()
        {
            if (!Ready || Showing <= 0 || Time.time < _nextFrame) return;
            _nextFrame = Time.time + 1f / FramesPerSecond;
            _index = (_index + 1) % Frames.Count;
            _material.mainTexture = Frames[_index];
        }
    }
}
