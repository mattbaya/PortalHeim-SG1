using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;
using UnityEngine.Networking;

namespace PortalHeimSG1
{
    /// <summary>
    /// Sounds ship with the mod (see CREDITS.txt) and can be replaced by files in BepInEx/config/PortalHeim_SG1/sounds/.
    /// A moment with no sound at all just stays silent (or keeps the game's own sound).
    /// </summary>
    internal static class GateSounds
    {
        // The sound moments, and the file names (any of .ogg .wav .mp3) that fill them.
        public const string Chevrune = "chevrune";     // each chevrune locks
        public const string Spin = "spin";           // the inner ring turning (loops while it spins)
        public const string Activate = "activate";   // the gate opens
        public const string Idle = "idle";           // the open gate's hum (loops)
        public const string Teleport = "teleport";   // someone steps through
        public const string Close = "close";         // the gate shuts down
        public const string Travel = "travel";       // the music while you're in the portal tunnel (loops)
        public const string TravelFx = "travelfx";   // a second bass line layered faintly under it (loops)

        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        public static string Folder => Path.Combine(Paths.ConfigPath, "PortalHeim_SG1", "sounds");

        public static int Count => Clips.Count;

        private static readonly string[] Expected = { Chevrune, Spin, Activate, Idle, Teleport, Close, Travel, TravelFx };

        /// <summary>"6/6 sounds", or which are missing.</summary>
        public static string Summary
        {
            get
            {
                var missing = System.Array.FindAll(Expected, n => !Clips.ContainsKey(n));
                return missing.Length == 0 ? $"{Expected.Length}/{Expected.Length} sounds" : $"{Expected.Length - missing.Length}/{Expected.Length} sounds (missing: {string.Join(", ", missing)})";
            }
        }

        public static bool Has(string name) => Clips.ContainsKey(name);

        public static AudioClip Get(string name) => Clips.TryGetValue(name, out var clip) ? clip : null;

        /// <summary>
        /// Loads the sounds that ship with the mod, then anything in the user's own sounds folder, which
        /// replaces a bundled sound of the same name. Also creates that folder with a note about what goes in it.
        /// </summary>
        public static IEnumerator LoadAll(string bundledFolder)
        {
            Clips.Clear();

            if (PortalHeimSG1Plugin.UseBundledSounds.Value)
            {
                if (Directory.Exists(bundledFolder)) yield return Safely(LoadFolder(bundledFolder), "bundled sounds");
                else PortalHeimSG1Plugin.Log.LogWarning($"The bundled sounds folder isn't there: {bundledFolder}");
            }

            string folder = Folder;
            bool ready = false;
            try
            {
                Directory.CreateDirectory(folder);
                string note = Path.Combine(folder, "README.txt");
                if (!File.Exists(note))
                    File.WriteAllText(note,
                        "PortalHeim_SG1 comes with its own gate sounds. To use your own instead, put clips here\n" +
                        "(.ogg, .wav or .mp3), named for the moment they play. A file here replaces the bundled\n" +
                        "sound with the same name.\n\n" +
                        "chevrune  - plays as each chevrune locks\n" +
                        "spin      - the inner ring turning\n" +
                        "activate  - the gate opens\n" +
                        "idle      - the open gate's hum (loops; replaces the game's own portal hum)\n" +
                        "teleport  - someone steps through\n" +
                        "close     - the gate shuts down\n" +
                        "travel    - the music while you're in the portal tunnel (loops)\n" +
                        "travelfx  - a second bass line layered faintly under it (loops)\n\n" +
                        "Name each file exactly as above, e.g. activate.ogg. Changes are picked up the next time the game starts.\n");
                ready = true;
            }
            catch (System.Exception e)
            {
                PortalHeimSG1Plugin.Log.LogWarning($"Couldn't prepare the sounds folder: {e.Message}");
            }

            if (ready) yield return Safely(LoadFolder(folder), "your sounds");
            PortalHeimSG1Plugin.Log.LogInfo($"{Clips.Count} gate sound(s) ready ({string.Join(", ", Clips.Keys)}).");
        }

        /// <summary>Runs a loading routine, logging (and recording) any exception instead of letting the coroutine die silently.</summary>
        private static IEnumerator Safely(IEnumerator routine, string what)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext()) yield break;
                    current = routine.Current;
                }
                catch (System.Exception e)
                {
                    Diagnostics.RecordFailure(what, e);
                    PortalHeimSG1Plugin.Log.LogError($"Couldn't load the {what}: {e}");
                    yield break;
                }
                yield return current;
            }
        }

        private static IEnumerator LoadFolder(string folder)
        {
            foreach (var path in Directory.GetFiles(folder))
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                AudioType type;
                switch (ext)
                {
                    case ".ogg": type = AudioType.OGGVORBIS; break;
                    case ".wav": type = AudioType.WAV; break;
                    case ".mp3": type = AudioType.MPEG; break;
                    default: continue;
                }

                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                using (var request = UnityWebRequestMultimedia.GetAudioClip(new System.Uri(path).AbsoluteUri, type))
                {
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        PortalHeimSG1Plugin.Log.LogWarning($"Couldn't load {Path.GetFileName(path)}: {request.error}");
                        continue;
                    }

                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip == null)
                    {
                        PortalHeimSG1Plugin.Log.LogWarning($"{Path.GetFileName(path)} isn't a readable sound file; skipping it.");
                        continue;
                    }
                    clip.name = name;
                    clip.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    Clips[name] = clip;
                    PortalHeimSG1Plugin.Log.LogInfo($"Loaded sound '{name}' ({clip.length:0.0}s) from {Path.GetFileName(folder)}");
                }
            }
        }

        /// <summary>Plays a one-off clip at a position (so it comes from the gate), if that sound was provided.</summary>
        public static void PlayAt(string name, Vector3 position, AudioSource template)
        {
            var clip = Get(name);
            if (clip == null) return;

            var go = new GameObject($"SG1Sound_{name}");
            go.transform.position = position;
            var source = Configure(go.AddComponent<AudioSource>(), template, loop: false);
            source.clip = clip;
            source.Play();
            Object.Destroy(go, clip.length + 0.5f);
        }

        /// <summary>Sets up a 3D source that follows the game's own sound-effects volume.</summary>
        public static AudioSource Configure(AudioSource source, AudioSource template, bool loop)
        {
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 4f;
            source.maxDistance = 40f;
            source.volume = Mathf.Clamp01(PortalHeimSG1Plugin.Volume.Value);
            // Use the same mixer group as the game's portal sound so the SFX volume slider applies.
            if (template != null) source.outputAudioMixerGroup = template.outputAudioMixerGroup;
            return source;
        }
    }
}
