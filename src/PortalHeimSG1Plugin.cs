using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    public enum InteriorMode
    {
        /// <summary>A pool of rippling water fills the open gate.</summary>
        Water,
        /// <summary>The portal's own blue swirl.</summary>
        Vanilla
    }

    public enum TravelMode
    {
        /// <summary>An endless tunnel of swirling cloud fills the loading screen during a long portal trip.</summary>
        Tunnel,
        /// <summary>A miniature gate (turning rune ring, Chevrunes, rippling water) on the teleport loading screen.</summary>
        Gate,
        /// <summary>The game's own spinner.</summary>
        Vanilla
    }

    public enum ShadingMode
    {
        /// <summary>Reuse the portal's own shader for the stone (safest: it's known to render).</summary>
        PortalShader,
        /// <summary>Use Unity's Standard shader instead (try this if the ring looks wrong or missing).</summary>
        Standard
    }

    [BepInPlugin(Guid, Name, Version)]
    public class PortalHeimSG1Plugin : BaseUnityPlugin
    {
        public const string Guid = "mbaya.PortalHeimSG1";
        public const string Name = "PortalHeim SG1";
        public const string Version = "0.3.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Reskin;
        internal static ConfigEntry<Color> ChevruneColor;
        internal static ConfigEntry<float> LockSeconds;
        internal static ConfigEntry<float> DormantGlow;
        internal static ConfigEntry<bool> Ramps;
        internal static ConfigEntry<bool> SinkRing;
        internal static ConfigEntry<ShadingMode> Shading;
        internal static ConfigEntry<InteriorMode> Interior;
        internal static ConfigEntry<TravelMode> Travel;
        internal static ConfigEntry<float> Volume;
        internal static ConfigEntry<bool> UseBundledSounds;
        internal static ConfigEntry<bool> TravelSounds;
        internal static ConfigEntry<float> TravelMusicVolume;
        internal static ConfigEntry<float> TravelEffectVolume;
        internal static ConfigEntry<bool> ShowStatus;
        internal static ConfigEntry<KeyboardShortcut> PreviewKey;
        internal static PortalHeimSG1Plugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            Reskin = Config.Bind("Look", "Reskin", true,
                "Turn every portal into a ring gate with runes and chevrunes. Takes effect for portals loaded after this is changed " +
                "(restart the game to be sure). Set to false to leave portals as they are.");
            ChevruneColor = Config.Bind("Look", "ChevruneColor", new Color(1f, 0.45f, 0.1f, 1f),
                "The glow colour of a locked chevrune.");
            LockSeconds = Config.Bind("Look", "SecondsPerChevrune", 0.5f,
                new ConfigDescription("Seconds between each Chevrune lighting up when a gate connects (nine in all).", new AcceptableValueRange<float>(0.1f, 4f)));
            Ramps = Config.Bind("Look", "Ramps", true,
                "Add a gentle ramp up to the portal's base on both sides, so anyone can walk in, even when overencumbered. " +
                "Takes effect for portals loaded after this is changed.");
            SinkRing = Config.Bind("Look", "SinkRing", true,
                "Sit the ring low so the bottom of the opening is level with the ground (the lower part of the outer ring is buried), " +
                "instead of having a stone lip to step over. Takes effect for portals loaded after this is changed.");
            DormantGlow = Config.Bind("Look", "DormantGlow", 0.3f,
                new ConfigDescription("How brightly a connected gate's Chevrunes glow while nobody is near it (0 = dark, 1 = fully lit). " +
                    "They light up fully, one at a time, when someone walks up.", new AcceptableValueRange<float>(0f, 1f)));
            Interior = Config.Bind("Look", "Interior", InteriorMode.Water,
                "What fills an open gate: Water (a rippling pool) or Vanilla (the portal's own swirl). Takes effect for portals loaded after this is changed.");
            Travel = Config.Bind("Look", "TravelScreen", TravelMode.Tunnel,
                "The graphic shown on the loading screen during a long portal trip: Tunnel (an endless swirling tunnel filling the screen), " +
                "Gate (a miniature gate with a turning rune ring and rippling water) or Vanilla (the game's own spinner). " +
                "Takes effect next time you load a world.");
            Shading = Config.Bind("Look", "Shading", ShadingMode.PortalShader,
                "How the stone is shaded. PortalShader reuses the portal's own shader. If the ring looks wrong or is missing, try Standard. " +
                "Takes effect on next game start.");
            PreviewKey = Config.Bind("Diagnostics", "PreviewKey", new KeyboardShortcut(KeyCode.F8),
                "Stand near a gate and press this to replay the opening sequence (Chevrunes locking, the water, and all the sounds) " +
                "without having to disconnect and reconnect the portal.");
            ShowStatus = Config.Bind("Diagnostics", "ShowStatusOnSpawn", true,
                "Show a one-line status message a few seconds after you spawn (how many portals are gates, whether the water and sounds loaded, any error). " +
                "Handy while testing; turn off once everything works.");
            TravelSounds = Config.Bind("Sound", "TravelSounds", true,
                "Play bass music while you're travelling through a portal (on the loading screen).");
            TravelMusicVolume = Config.Bind("Sound", "TravelMusicVolume", 0.75f,
                new ConfigDescription("Volume of the travel music.", new AcceptableValueRange<float>(0f, 1f)));
            TravelEffectVolume = Config.Bind("Sound", "TravelEffectVolume", 0.5f,
                new ConfigDescription("Volume of the optional second sound (a 'travelfx' clip you add) layered under the travel music. Nothing ships for it.", new AcceptableValueRange<float>(0f, 1f)));
            UseBundledSounds = Config.Bind("Sound", "UseBundledSounds", true,
                "Play the sounds that come with the mod. Turn off for silence (or to use only your own clips). Takes effect on next game start.");
            Volume = Config.Bind("Sound", "Volume", 1.0f,
                new ConfigDescription("Volume of the gate sounds you provide (also follows the game's sound-effects volume).", new AcceptableValueRange<float>(0f, 1f)));

            StartCoroutine(GateSounds.LoadAll(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "sounds")));
            if (Interior.Value == InteriorMode.Water || Travel.Value == TravelMode.Gate) StartCoroutine(WaterSurface.Build());
            if (Travel.Value == TravelMode.Tunnel) TunnelSurface.Start();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(PortalHeimSG1Plugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded. Sound files go in {GateSounds.Folder}");
        }

        private void Update()
        {
            WaterSurface.Tick();
            TunnelSurface.Pump();

            if (PreviewKey.Value.IsDown() && Player.m_localPlayer != null
                && !(Chat.instance != null && Chat.instance.HasFocus()) && !global::Console.IsVisible() && !TextInput.IsVisible())
            {
                GateRing nearest = null;
                float best = 30f;
                foreach (var ring in FindObjectsByType<GateRing>(FindObjectsSortMode.None))
                {
                    float d = Vector3.Distance(ring.transform.position, Player.m_localPlayer.transform.position);
                    if (d < best) { best = d; nearest = ring; }
                }

                if (nearest != null)
                {
                    nearest.Preview();
                    Log.LogInfo($"Previewing the gate {best:0} m away. {GateSounds.Summary}");
                }
                else
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "No gate within 30 m to preview.");
                }
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(TeleportWorld))]
    internal static class TeleportWorldPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(TeleportWorld.Awake))]
        private static void Awake_Postfix(TeleportWorld __instance)
        {
            if (!PortalHeimSG1Plugin.Reskin.Value) return;
            try
            {
                GateBuilder.Attach(__instance);
            }
            catch (System.Exception e)
            {
                Diagnostics.RecordFailure("portal", e);
                PortalHeimSG1Plugin.Log.LogError($"Couldn't turn a portal into a gate; leaving it as it is. {e}");
            }
        }

        // TeleportWorld.Teleport returns early when the trip is refused (blocked portals, carrying ore...).
        // The player's teleporting flag only flips when a teleport really starts, so compare before and after.
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportWorld.Teleport))]
        private static void Teleport_Prefix(Player player, out bool __state) => __state = player != null && player.IsTeleporting();

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TeleportWorld.Teleport))]
        private static void Teleport_Postfix(TeleportWorld __instance, Player player, bool __state)
        {
            if (__state || player == null || !player.IsTeleporting()) return;
            var ring = __instance.GetComponentInChildren<GateRing>();
            if (ring != null) ring.OnTeleport();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class SpawnPatch
    {
        private static void Postfix()
        {
            if (PortalHeimSG1Plugin.Instance != null)
                PortalHeimSG1Plugin.Instance.StartCoroutine(Diagnostics.ReportSoon());
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    internal static class HudPatches
    {
        private static void Postfix(Hud __instance)
        {
            try
            {
                TravelSound.Attach(__instance);
                TravelScreen.Build(__instance);
            }
            catch (System.Exception e)
            {
                Diagnostics.RecordFailure("travel screen", e);
                PortalHeimSG1Plugin.Log.LogError($"Couldn't change the teleport loading screen; leaving it as it is. {e}");
            }
        }
    }
}
