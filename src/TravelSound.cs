using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Plays the travel bass line on a loop while the teleport loading screen is up, fading in as you enter the
    /// tunnel and out as you arrive. It watches the same panel the game shows for a long portal trip.
    /// </summary>
    internal class TravelSound : MonoBehaviour
    {
        private const float FadeInSeconds = 0.4f;
        private const float FadeOutSeconds = 0.6f;

        private GameObject _panel;
        private AudioSource _source;
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
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;               // a plain stereo sound, not placed in the world
            _source.ignoreListenerPause = true;      // keep playing while the game is loading
            // The game's interface sound group, so the player's volume settings apply.
            if (AudioMan.instance != null) _source.outputAudioMixerGroup = AudioMan.instance.m_guiMixer;
        }

        private void Update()
        {
            var clip = GateSounds.Get(GateSounds.Travel);
            bool wanted = PortalHeimSG1Plugin.TravelBass.Value && clip != null && _panel != null && _panel.activeInHierarchy;

            // Real time: the clock can be stopped while a world loads.
            float step = Time.unscaledDeltaTime / (wanted ? FadeInSeconds : FadeOutSeconds);
            _fade = Mathf.MoveTowards(_fade, wanted ? 1f : 0f, step);

            if (_fade > 0f)
            {
                if (!_source.isPlaying)
                {
                    _source.clip = clip;
                    _source.Play();
                }
                _source.volume = _fade * Mathf.Clamp01(PortalHeimSG1Plugin.TravelVolume.Value);
            }
            else if (_source.isPlaying)
            {
                _source.Stop();
            }
        }
    }
}
