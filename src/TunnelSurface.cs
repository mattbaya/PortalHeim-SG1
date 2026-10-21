using System;
using System.Threading;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// The frames of the endless-tunnel animation. They're painted on a background thread (it's plain maths with
    /// no Unity calls) and uploaded as textures one per game frame, so the game never stutters.
    /// </summary>
    internal static class TunnelSurface
    {
        private static readonly Art.Pixels[] Computed = new Art.Pixels[Art.Tunnel.Frames];
        private static readonly Texture2D[] Textures = new Texture2D[Art.Tunnel.Frames];
        private static int _finished;     // frames the worker has completed
        private static int _uploaded;     // frames turned into textures
        private static bool _started;
        private static volatile string _error;

        public static bool Failed => _error != null;
        public static bool Ready => !Failed && _uploaded == Art.Tunnel.Frames;
        public static int FrameCount => Art.Tunnel.Frames;
        public static Texture2D FrameAt(int index) => Textures[index];

        public static void Start()
        {
            if (_started) return;
            _started = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    for (int i = 0; i < Art.Tunnel.Frames; i++)
                    {
                        Computed[i] = Art.Tunnel.Frame(i);
                        Interlocked.Increment(ref _finished);
                    }
                }
                catch (Exception e)
                {
                    _error = e.Message;
                }
            });
        }

        /// <summary>Call every frame from the main thread: turns one finished frame into a texture.</summary>
        public static void Pump()
        {
            if (!_started || Failed) return;
            if (_uploaded >= Art.Tunnel.Frames || _uploaded >= Volatile.Read(ref _finished)) return;

            var pixels = Computed[_uploaded];
            var texture = new Texture2D(pixels.Width, pixels.Height, TextureFormat.RGBA32, false) { name = $"SG1Tunnel{_uploaded}" };
            texture.SetPixelData(pixels.Rgba, 0);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, true);
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            Textures[_uploaded] = texture;
            Computed[_uploaded] = null;
            _uploaded++;

            if (_uploaded == Art.Tunnel.Frames) PortalHeimSG1Plugin.Log.LogInfo($"Tunnel ready ({_uploaded} frames).");
        }

        public static string Status => Failed ? "FAILED" : Ready ? "ready" : "loading";

        public static string Error => _error;
    }
}
