using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Brings a gate to life: when the portal connects, the rune ring spins and the chevrunes lock one
    /// by one before the gate opens and fills with water; when it disconnects they go dark again. It only
    /// watches the portal's own state and never changes what the portal does.
    /// </summary>
    internal class GateRing : MonoBehaviour
    {
        private enum Phase { Idle, Dialing, Open }

        public static readonly Color Dark = new Color(0.16f, 0.14f, 0.12f);

        // Part of each step spent turning the ring before the Chevrune locks; it turns one Chevrune's width per step.
        private const float SpinShareOfStep = 0.65f;
        private const float DegreesPerStep = 40f;
        // Beyond this distance there's no point animating; the gate just snaps to its state.
        private const float AnimateRange = 70f;
        // The water splashes open, then settles; it drains away faster than it fills.
        private const float WaterFillSeconds = 0.9f;
        private const float WaterDrainSeconds = 0.25f;

        private TeleportWorld _portal;
        private Transform _inner;
        private Renderer[] _chevrunes;
        private MeshRenderer _water;
        private ParticleSystemRenderer[] _swirl;
        private AudioSource _template;
        private AudioSource _idleSource;
        private AudioSource _spinSource;

        private Phase _phase = Phase.Idle;
        private int _locked;
        private float _phaseTime;
        private float _spinDirection = 1f;
        private float _waterProgress;       // 0 = empty, 1 = full
        private bool _waterCounted;
        private bool _swirlHidden;
        private bool _wasConnected;
        private bool _wasHumming;
        private bool _previewing;
        private float _previewHoldUntil;

        public void Setup(TeleportWorld portal, Transform inner, Renderer[] chevrunes, MeshRenderer water, ParticleSystemRenderer[] swirl)
        {
            _portal = portal;
            _inner = inner;
            _chevrunes = chevrunes;
            _water = water;
            _swirl = swirl ?? new ParticleSystemRenderer[0];
            _template = portal.m_target_found != null ? portal.m_target_found.GetComponentInChildren<AudioSource>() : null;

            _idleSource = GateSounds.Configure(gameObject.AddComponent<AudioSource>(), _template, loop: true);
            _spinSource = GateSounds.Configure(gameObject.AddComponent<AudioSource>(), _template, loop: true);

            _wasConnected = Connected();
            SnapTo(_wasConnected ? Phase.Open : Phase.Idle);
            if (_wasConnected) _waterProgress = 1f; // already open when the world loads: no splash
        }

        private void OnDisable() => CountWater(false);

        private void OnDestroy() => CountWater(false);

        private bool Connected() => _portal != null && _portal.enabled && _portal.m_nview != null && _portal.m_nview.IsValid() && _portal.HaveTarget();

        private void Update()
        {
            if (_portal == null) return;

            bool connected = Connected();
            bool near = Player.m_localPlayer != null
                && Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < AnimateRange;

            if (connected != _wasConnected)
            {
                _wasConnected = connected;
                if (!connected) Close(near);
                else if (near) StartDialing();
                else SnapTo(Phase.Open);
            }

            switch (_phase)
            {
                case Phase.Dialing: UpdateDialing(); break;
                case Phase.Open: UpdateOpen(); break;
            }

            UpdateWater();
            UpdateHum(connected);
        }

        // ---------------------------------------------------------------- dialing

        private void StartDialing()
        {
            _phase = Phase.Dialing;
            _locked = 0;
            _phaseTime = 0f;
            _spinDirection = 1f;
            for (int i = 0; i < _chevrunes.Length; i++) SetChevrune(i, false);
            StartSpinSound();
        }

        private void UpdateDialing()
        {
            _phaseTime += Time.deltaTime;
            float seconds = Mathf.Max(0.1f, PortalHeimSG1Plugin.LockSeconds.Value);
            float spinSeconds = seconds * SpinShareOfStep;

            // The ring spins for the first part of each step, then stops while the chevrune locks.
            if (_phaseTime < spinSeconds)
                _inner.Rotate(0f, 0f, _spinDirection * (DegreesPerStep / spinSeconds) * Time.deltaTime);

            if (_phaseTime >= spinSeconds && _spinSource.isPlaying) _spinSource.Stop();

            if (_phaseTime >= seconds)
            {
                SetChevrune(_locked, true);
                GateSounds.PlayAt(GateSounds.Chevrune, transform.position, _template);
                _locked++;
                _phaseTime = 0f;
                _spinDirection = -_spinDirection;

                if (_locked >= _chevrunes.Length)
                {
                    _spinSource.Stop();
                    _phase = Phase.Open;
                    if (_previewing) _previewHoldUntil = Time.time + 5f;
                    GateSounds.PlayAt(GateSounds.Activate, transform.position, _template);
                }
                else
                {
                    StartSpinSound();
                }
            }
        }

        private void UpdateOpen()
        {
            // The ring slowly turns while the gate is open.
            _inner.Rotate(0f, 0f, 3f * Time.deltaTime);

            // A preview on a gate that isn't really connected ends by closing again.
            if (_previewing && Time.time >= _previewHoldUntil)
            {
                _previewing = false;
                if (!Connected()) Close(true);
            }
        }

        /// <summary>Replays the whole opening sequence (and its sounds), for testing.</summary>
        public void Preview()
        {
            _previewing = true;
            _previewHoldUntil = float.MaxValue;
            StartDialing();
        }

        private void Close(bool near)
        {
            if (near && _phase != Phase.Idle) GateSounds.PlayAt(GateSounds.Close, transform.position, _template);
            _spinSource.Stop();
            SnapTo(Phase.Idle);
        }

        private void SnapTo(Phase phase)
        {
            _phase = phase;
            for (int i = 0; i < _chevrunes.Length; i++) SetChevrune(i, phase == Phase.Open);
        }

        private void SetChevrune(int index, bool lit)
        {
            if (index < 0 || index >= _chevrunes.Length) return;
            // Swap between two shared materials (the game's hover/damage highlighting can clear
            // per-renderer overrides, but it leaves material assignments alone).
            _chevrunes[index].sharedMaterial = lit ? GateBuilder.ChevruneOn : GateBuilder.ChevruneOff;
        }

        // ---------------------------------------------------------------- water

        /// <summary>
        /// The pool fills the opening once the gate is open: it swells in with a little overshoot (a
        /// splash), then settles. It drains away quickly when the gate closes. While the water is
        /// usable, the game's own swirl is hidden so the two don't fight.
        /// </summary>
        private void UpdateWater()
        {
            bool hideSwirl = PortalHeimSG1Plugin.Interior.Value == InteriorMode.Water && WaterSurface.Usable;
            if (hideSwirl != _swirlHidden)
            {
                _swirlHidden = hideSwirl;
                foreach (var renderer in _swirl)
                    if (renderer != null) renderer.forceRenderingOff = hideSwirl;
            }

            if (_water == null) return;

            bool wantWater = _phase == Phase.Open && WaterSurface.Ready;
            _waterProgress = Mathf.Clamp01(_waterProgress + (wantWater ? 1f / WaterFillSeconds : -1f / WaterDrainSeconds) * Time.deltaTime);

            bool show = _waterProgress > 0.001f;
            if (_water.enabled != show) _water.enabled = show;
            CountWater(show);

            if (show)
            {
                // Ease out with overshoot: the surface pops slightly past full size, then settles.
                float t = _waterProgress - 1f;
                float eased = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
                _water.transform.localScale = new Vector3(eased, eased, 1f);
            }
        }

        private void CountWater(bool showing)
        {
            if (showing == _waterCounted) return;
            _waterCounted = showing;
            WaterSurface.Showing += showing ? 1 : -1;
        }

        // ---------------------------------------------------------------- sound

        private void StartSpinSound()
        {
            var clip = GateSounds.Get(GateSounds.Spin);
            if (clip == null) return;
            _spinSource.clip = clip;
            _spinSource.Play();
        }

        /// <summary>The open gate's hum plays while the game's own portal glow is on (you're near and can go through).</summary>
        private void UpdateHum(bool connected)
        {
            var hum = GateSounds.Get(GateSounds.Idle);
            if (hum == null) return;

            bool humming = connected && _portal.m_target_found != null && _portal.m_target_found.m_active;
            if (humming != _wasHumming)
            {
                _wasHumming = humming;
                if (humming) { _idleSource.clip = hum; _idleSource.Play(); }
                else _idleSource.Stop();
            }

            // Replace the game's own hum rather than layering ours over it.
            if (_template != null) _template.mute = true;
        }

        /// <summary>Called when someone steps through.</summary>
        public void OnTeleport()
        {
            GateSounds.PlayAt(GateSounds.Teleport, transform.position, _template);
        }
    }
}
