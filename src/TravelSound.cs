using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Plays the travel music (and an optional second layer) on a loop while the teleport loading screen is up, fading in
    /// as you enter the tunnel and out as you arrive. It watches the same panel the game shows for a long portal trip.
    /// </summary>
    internal class TravelSound : MonoBehaviour
    {
        private const float FadeInSeconds = 0.4f;
        private const float FadeOutSeconds = 0.6f;

        private GameObject _panel;
        private AudioSource _music;
        private AudioSource _layer;
        private float _fade;

        public static void Attach(Hud hud)
        {
            var panel = hud != null ? hud.m_teleportingProgress : null;
            if (panel == null || hud.transform.Find("SG1TravelSound") != null) return;

            var go = new GameObject("SG1TravelSound");
            go.transform.SetParent(hud.transform, false);
            go.AddComponent<TravelSound>().Setup(panel);
        }

        private void Setup(GameObject panel)
        {
            _panel = panel;
            _music = NewSource();
            _layer = NewSource();
        }

        private AudioSource NewSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;                // a plain stereo sound, not placed in the world
            source.ignoreListenerPause = true;       // keep playing while the game is loading
            // The game's interface sound group, so the player's volume settings apply.
            if (AudioMan.instance != null) source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
            return source;
        }

        // The transit sound is played at half the volume it used to be, whatever the volume settings say.
        private const float OverallLevel = 0.5f;

        private void Update()
        {
            bool wanted = PortalHeimSG1Plugin.TravelSounds.Value && _panel != null && _panel.activeInHierarchy;

            // Real time: the clock can be stopped while a world loads.
            float step = Time.unscaledDeltaTime / (wanted ? FadeInSeconds : FadeOutSeconds);
            _fade = Mathf.MoveTowards(_fade, wanted ? 1f : 0f, step);

            Drive(_music, GateSounds.Get(GateSounds.Travel), PortalHeimSG1Plugin.TravelMusicVolume.Value);
            Drive(_layer, GateSounds.Get(GateSounds.TravelFx), PortalHeimSG1Plugin.TravelEffectVolume.Value);
        }

        /// <summary>Plays a looping clip at the faded volume while the panel is up, and stops it once it has faded out.</summary>
        private void Drive(AudioSource source, AudioClip clip, float volume)
        {
            if (clip != null && _fade > 0f)
            {
                if (!source.isPlaying)
                {
                    source.clip = clip;
                    source.Play();
                }
                source.volume = _fade * Mathf.Clamp01(volume) * OverallLevel;
            }
            else if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
