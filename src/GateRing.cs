using UnityEngine;

namespace PortalHeimSG1
{
    internal enum ChevruneState { Off, Dim, Lit }

    /// <summary>One Chevrune: the arrow it shows while idle, and the rune it turns into when it locks.</summary>
    internal class ChevruneParts
    {
        public MeshRenderer Arrow;
        public MeshRenderer Rune;
    }

    /// <summary>
    /// Brings a gate to life. When a gate goes live (someone walks up to either end of a connected portal),
    /// the rune ring spins and the Chevrunes lock one by one, then the gate opens and fills with water. When
    /// everyone has gone it shuts down again. It only watches the portal and never changes what it does.
    /// </summary>
    internal class GateRing : MonoBehaviour
    {
        private enum Phase { Idle, Dialing, Open }

        public static readonly Color Dark = new Color(0.16f, 0.14f, 0.12f);

        // Part of each step spent turning the ring before the Chevrune locks; it turns one Chevrune's width per step.
        private const float SpinShareOfStep = 0.65f;
        private const float DegreesPerStep = 72f;
        // How long an arrow takes to turn into its rune (and back).
        private const float MorphSeconds = 0.22f;

        // Which Chevrunes lock on each step. They sit around the ring starting from the top (0), going round:
        // so 4 and 5 are the bottom pair, then the pairs climb the sides, and the top one locks last.
        private static readonly int[][] LockOrder =
        {
            new[] { 4, 5 },
            new[] { 3, 6 },
            new[] { 2, 7 },
            new[] { 1, 8 },
            new[] { 0 },
        };
        // The Stone Portal's order: the top one locks first and the pairs come down the sides to the bottom.
        private static readonly int[][] LockOrderTopDown =
        {
            new[] { 0 },
            new[] { 1, 8 },
            new[] { 2, 7 },
            new[] { 3, 6 },
            new[] { 4, 5 },
        };

        /// <summary>True for the Stone Portal: its chevrunes lock from the top down.</summary>
        public bool TopDown;

        // This gate's own look (set from its saved data): its own lit/dim materials, if it has a colour.
        private Material _onMaterial, _dimMaterial;
        private string _styleSignature;
        private float _nextStyleCheck;

        /// <summary>The storage hatch in front of the gate, which lifts while its storage is open.</summary>
        public Transform Lid;
        public float LidTarget;
        private float _lid;
        private const float LidSeconds = 0.6f;
        private const float LidOpenAngle = 75f;
        private int[][] Order => TopDown ? LockOrderTopDown : LockOrder;
        // Sounds are only made for gates this close (they can't be heard from further anyway).
        private const float SoundRange = 45f;
        // Standing within the portal's activation range plus this much counts as being at the gate.
        private const float IncomingMargin = 3f;
        // The gate you step out of after a trip is the incoming one too: it starts up as you arrive, and stays quiet for this long.
        private const float ArrivalQuietSeconds = 15f;
        // A gate stays live this long after the last person leaves range, so standing at the edge doesn't flicker it.
        private const float LiveGraceSeconds = 2f;
        // The water splashes open, then settles; it drains away faster than it fills.
        private const float WaterFillSeconds = 0.9f;
        private const float WaterDrainSeconds = 0.25f;

        private TeleportWorld _portal;
        private Transform _inner;
        private ChevruneParts[] _chevrunes;
        private ChevruneState[] _state;
        private float[] _morph;              // 0 = arrow, 1 = rune
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
        private bool _wasHumming;
        private bool _previewing;
        private float _previewHoldUntil;
        private float _nextLiveCheck;
        private float _liveUntil;
        private ChevruneState? _idleLook;   // what the Chevrunes show while idle, to avoid redoing it every frame

        public void Setup(TeleportWorld portal, Transform inner, ChevruneParts[] chevrunes, MeshRenderer water, ParticleSystemRenderer[] swirl)
        {
            _portal = portal;
            _inner = inner;
            _chevrunes = chevrunes;
            _state = new ChevruneState[chevrunes.Length];
            _morph = new float[chevrunes.Length];
            _water = water;
            _swirl = swirl ?? new ParticleSystemRenderer[0];
            _template = portal.m_target_found != null ? portal.m_target_found.GetComponentInChildren<AudioSource>() : null;

            _idleSource = GateSounds.Configure(gameObject.AddComponent<AudioSource>(), _template, loop: true);
            _spinSource = GateSounds.Configure(gameObject.AddComponent<AudioSource>(), _template, loop: true);

            ShowIdleLook(Connected());
            for (int i = 0; i < _chevrunes.Length; i++) ApplyMorph(i);
        }

        private void OnDisable() => CountWater(false);

        private void OnDestroy()
        {
            CountWater(false);
            if (_onMaterial != null) Destroy(_onMaterial);
            if (_dimMaterial != null) Destroy(_dimMaterial);
        }

        /// <summary>Picks up this gate's colour and runes from its saved data (every half second).</summary>
        private void UpdateStyle()
        {
            if (Time.time < _nextStyleCheck) return;
            _nextStyleCheck = Time.time + 0.5f;
            var view = _portal.m_nview;
            if (view == null || !view.IsValid()) return;

            var zdo = view.GetZDO();
            string color = zdo.GetString(GateStyle.KeyColor, "");
            string runes = zdo.GetString(GateStyle.KeyRunes, "");
            string signature = color + "|" + runes;
            if (signature == _styleSignature) return;
            _styleSignature = signature;

            if (_onMaterial != null) Destroy(_onMaterial);
            if (_dimMaterial != null) Destroy(_dimMaterial);
            _onMaterial = _dimMaterial = null;
            if (GateStyle.TryParse(color, out var c) && GateBuilder.ChevruneOn != null)
            {
                _onMaterial = new Material(GateBuilder.ChevruneOn) { name = "SG1ChevruneOnCustom" };
                GateBuilder.Tint(_onMaterial, c);
                _dimMaterial = new Material(GateBuilder.ChevruneDim) { name = "SG1ChevruneDimCustom" };
                GateBuilder.Tint(_dimMaterial, Color.Lerp(Dark, c, Mathf.Clamp01(PortalHeimSG1Plugin.DormantGlow.Value)));
            }

            var glyphs = GateStyle.RunesFor(runes);
            for (int i = 0; i < _chevrunes.Length && i < glyphs.Length; i++)
            {
                var filter = _chevrunes[i].Rune != null ? _chevrunes[i].Rune.GetComponent<MeshFilter>() : null;
                if (filter != null) filter.sharedMesh = GateStyle.RuneMesh(glyphs[i]);
                SetChevrune(i, _state[i]);
            }
        }

        /// <summary>Lifts the storage hatch like a boot while the storage is open, and closes it again.</summary>
        private void UpdateLid()
        {
            if (Lid == null) return;
            if (_lid == LidTarget) return;
            _lid = Mathf.MoveTowards(_lid, LidTarget, Time.deltaTime / LidSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, _lid);
            Lid.localRotation = Quaternion.Euler(Mathf.Lerp(GateBuilder.HatchClosedAngle, -LidOpenAngle, eased), 0f, 0f);
        }

        /// <summary>
        /// A gate that's opening but isn't the one the local player is standing at (someone else dialed it, or it's the
        /// far end of the trip). Its chevrune lock sounds can be switched off.
        /// </summary>
        private bool IsIncoming
        {
            get
            {
                if (_previewing || Player.m_localPlayer == null || _portal == null) return false;
                if (Time.time - PortalHeimSG1Plugin.ArrivedAt < ArrivalQuietSeconds) return true;
                var root = _portal.m_proximityRoot != null ? _portal.m_proximityRoot : _portal.transform;
                return Vector3.Distance(Player.m_localPlayer.transform.position, root.position) > _portal.m_activationRange + IncomingMargin;
            }
        }

        private bool Near => Player.m_localPlayer != null
            && Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < SoundRange;

        private bool Connected() => _portal != null && _portal.enabled && _portal.m_nview != null && _portal.m_nview.IsValid() && _portal.HaveTarget();

        private void Update()
        {
            if (_portal == null) return;
            UpdateStyle();
            UpdateLid();

            bool connected = Connected();
            UpdateLive(connected);
            // A gate is active when it's connected and someone is at either end (or while previewing).
            bool active = (connected && Time.time < _liveUntil) || _previewing;

            switch (_phase)
            {
                case Phase.Idle:
                    if (active) StartDialing(connected);
                    else ShowIdleLook(connected);
                    break;
                case Phase.Dialing:
                    if (!active) Shutdown(connected); else UpdateDialing();
                    break;
                case Phase.Open:
                    if (!active) Shutdown(connected); else UpdateOpen();
                    break;
            }

            UpdateMorph();
            UpdateWater();
            UpdateHum();
        }

        // ---------------------------------------------------------------- is anyone there?

        /// <summary>
        /// Twice a second, checks whether someone is standing at either end: the game's own portal glow is on
        /// (a player near this end who can go through), or a player is near the far portal.
        /// </summary>
        private void UpdateLive(bool connected)
        {
            if (Time.time < _nextLiveCheck) return;
            _nextLiveCheck = Time.time + 0.5f;
            if (connected && (ThisEndLive() || OtherEndOccupied())) _liveUntil = Time.time + LiveGraceSeconds;
        }

        private bool ThisEndLive()
        {
            if (_portal.m_target_found != null) return _portal.m_target_found.m_active;

            // No glow object to follow (an unusual portal): fall back to a plain distance check.
            var root = _portal.m_proximityRoot != null ? _portal.m_proximityRoot : _portal.transform;
            return Player.GetClosestPlayer(root.position, _portal.m_activationRange) != null;
        }

        /// <summary>Whether any player is standing near the portal at the other end of this connection.</summary>
        private bool OtherEndOccupied()
        {
            if (_portal.m_nview == null || !_portal.m_nview.IsValid() || ZDOMan.instance == null) return false;

            var id = _portal.m_nview.GetZDO().GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
            if (id == ZDOID.None) return false;
            var far = ZDOMan.instance.GetZDO(id); // only if we already know about it; never asks the server for it
            if (far == null) return false;

            var position = far.GetPosition();
            float range = _portal.m_activationRange;

            // Players loaded on this machine (including others close by).
            foreach (var player in Player.GetAllPlayers())
                if (player != null && Vector3.Distance(player.transform.position, position) <= range) return true;

            // Players the server tells us about, if they share their position.
            if (ZNet.instance != null)
                foreach (var info in ZNet.instance.GetPlayerList())
                    if (info.m_publicPosition && Vector3.Distance(info.m_position, position) <= range) return true;

            return false;
        }

        // ---------------------------------------------------------------- start up and shut down

        private void StartDialing(bool connected)
        {
            _phase = Phase.Dialing;
            _locked = 0;
            _phaseTime = 0f;
            _spinDirection = 1f;
            _idleLook = null;
            SetAll(connected ? ChevruneState.Dim : ChevruneState.Off);
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
                // This step's Chevrunes (a left and right pair, or the last one) turn from arrows into runes.
                foreach (int index in Order[_locked]) SetChevrune(index, ChevruneState.Lit);
                // No thud per chevrune: the chevrune sound plays once, as the gate opens (see PlayOpening).
                _locked++;
                _phaseTime = 0f;
                _spinDirection = -_spinDirection;

                if (_locked >= Order.Length)
                {
                    _spinSource.Stop();
                    _phase = Phase.Open;
                    if (_previewing) _previewHoldUntil = Time.time + 5f;
                    PlayOpening();
                }
                else
                {
                    StartSpinSound();
                }
            }
        }

        /// <summary>
        /// The sound as the gate finishes opening. The gate you start from plays its opening sound. An incoming gate, by default,
        /// plays one chevrune thud together with the opening sound, instead of the spaced thuds.
        /// </summary>
        private void PlayOpening()
        {
            // Both ends, the same: one chevrune sound together with the opening sound.
            if (IsIncoming && PortalHeimSG1Plugin.IncomingGate.Value == IncomingSounds.Silent) return;
            if (PortalHeimSG1Plugin.ChevruneThuds.Value) PlaySound(GateSounds.Chevrune);
            if (PortalHeimSG1Plugin.OpeningSound.Value) PlaySound(GateSounds.Activate);
        }

        private void UpdateOpen()
        {
            // The ring slowly turns while the gate is open.
            _inner.Rotate(0f, 0f, 3f * Time.deltaTime);

            // A preview ends after a few seconds; the gate then follows who's really nearby.
            if (_previewing && Time.time >= _previewHoldUntil) _previewing = false;
        }

        private void Shutdown(bool connected)
        {
            if (_phase != Phase.Idle) PlaySound(GateSounds.Close);
            _spinSource.Stop();
            _phase = Phase.Idle;
            ShowIdleLook(connected, force: true);
        }

        /// <summary>Replays the whole start-up sequence (and its sounds), for testing.</summary>
        public void Preview()
        {
            _previewing = true;
            _previewHoldUntil = float.MaxValue;
            if (_phase != Phase.Idle) Shutdown(Connected());
            StartDialing(Connected());
        }

        /// <summary>A connected gate with nobody near glows faintly; an unconnected one is dark.</summary>
        private void ShowIdleLook(bool connected, bool force = false)
        {
            var look = connected ? ChevruneState.Dim : ChevruneState.Off;
            if (!force && _idleLook == look) return;
            _idleLook = look;
            SetAll(look);
        }

        private void SetAll(ChevruneState state)
        {
            for (int i = 0; i < _chevrunes.Length; i++) SetChevrune(i, state);
        }

        private void SetChevrune(int index, ChevruneState state)
        {
            if (index < 0 || index >= _chevrunes.Length) return;
            _state[index] = state;
            // Swap between shared materials (the game's hover/damage highlighting can clear
            // per-renderer overrides, but it leaves material assignments alone).
            var material = state == ChevruneState.Lit ? (_onMaterial ?? GateBuilder.ChevruneOn)
                : state == ChevruneState.Dim ? (_dimMaterial ?? GateBuilder.ChevruneDim)
                : GateBuilder.ChevruneOff;
            _chevrunes[index].Arrow.sharedMaterial = material;
            _chevrunes[index].Rune.sharedMaterial = material;
        }

        // ---------------------------------------------------------------- arrow <-> rune

        /// <summary>Eases each Chevrune toward arrow or rune, depending on whether it's locked.</summary>
        private void UpdateMorph()
        {
            float step = Time.deltaTime / MorphSeconds;
            for (int i = 0; i < _chevrunes.Length; i++)
            {
                float target = _state[i] == ChevruneState.Lit ? 1f : 0f;
                if (_morph[i] == target) continue;
                _morph[i] = Mathf.MoveTowards(_morph[i], target, step);
                ApplyMorph(i);
            }
        }

        /// <summary>The arrow shrinks away as the rune pops in (with a little overshoot).</summary>
        private void ApplyMorph(int i)
        {
            float p = _morph[i];
            float arrow = Mathf.Clamp01(1f - p * 2.2f);
            float runeT = Mathf.Clamp01((p - 0.35f) / 0.65f);
            float rune = 0f;
            if (runeT > 0f)
            {
                float t = runeT - 1f;
                rune = 1f + 2.70158f * t * t * t + 1.70158f * t * t;
            }

            var parts = _chevrunes[i];
            bool showArrow = arrow > 0.001f, showRune = rune > 0.001f;
            if (parts.Arrow.enabled != showArrow) parts.Arrow.enabled = showArrow;
            if (parts.Rune.enabled != showRune) parts.Rune.enabled = showRune;
            if (showArrow) parts.Arrow.transform.localScale = Vector3.one * arrow;
            if (showRune) parts.Rune.transform.localScale = Vector3.one * rune;
        }

        // ---------------------------------------------------------------- water

        /// <summary>
        /// The pool fills the opening once the gate has finished starting up: it swells in with a little
        /// overshoot (a splash), then settles. It drains away quickly when the gate shuts down. While the water
        /// is usable, the game's own swirl is hidden so the two don't fight.
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

        private void PlaySound(string name)
        {
            if (Near) GateSounds.PlayAt(name, transform.position, _template);
        }

        private void StartSpinSound()
        {
            var clip = GateSounds.Get(GateSounds.Spin);
            if (clip == null || !Near) return;
            _spinSource.clip = clip;
            _spinSource.Play();
        }

        /// <summary>The hum plays while the gate is open (after the start-up sequence, until it shuts down).</summary>
        private void UpdateHum()
        {
            var hum = GateSounds.Get(GateSounds.Idle);
            if (hum == null) return;

            bool humming = _phase == Phase.Open;
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
            PlaySound(GateSounds.Teleport);
        }
    }
}
