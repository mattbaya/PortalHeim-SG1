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
        public const string Version = "0.1.3";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Reskin;
        internal static ConfigEntry<Color> ChevruneColor;
        internal static ConfigEntry<float> LockSeconds;
        internal static ConfigEntry<ShadingMode> Shading;
        internal static ConfigEntry<InteriorMode> Interior;
        internal static ConfigEntry<float> Volume;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Reskin = Config.Bind("Look", "Reskin", true,
                "Turn every portal into a ring gate with runes and chevrunes. Takes effect for portals loaded after this is changed " +
                "(restart the game to be sure). Set to false to leave portals as they are.");
            ChevruneColor = Config.Bind("Look", "ChevruneColor", new Color(1f, 0.45f, 0.1f, 1f),
                "The glow colour of a locked chevrune.");
            LockSeconds = Config.Bind("Look", "SecondsPerChevrune", 0.5f,
                new ConfigDescription("Seconds between each Chevrune lighting up when a gate connects (nine in all).", new AcceptableValueRange<float>(0.1f, 4f)));
            Interior = Config.Bind("Look", "Interior", InteriorMode.Water,
                "What fills an open gate: Water (a rippling pool) or Vanilla (the portal's own swirl). Takes effect for portals loaded after this is changed.");
            Shading = Config.Bind("Look", "Shading", ShadingMode.PortalShader,
                "How the stone is shaded. PortalShader reuses the portal's own shader. If the ring looks wrong or is missing, try Standard. " +
                "Takes effect on next game start.");
            Volume = Config.Bind("Sound", "Volume", 1.0f,
                new ConfigDescription("Volume of the gate sounds you provide (also follows the game's sound-effects volume).", new AcceptableValueRange<float>(0f, 1f)));

            StartCoroutine(GateSounds.LoadAll());
            if (Interior.Value == InteriorMode.Water) StartCoroutine(WaterSurface.Build());

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(PortalHeimSG1Plugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded. Sound files go in {GateSounds.Folder}");
        }

        private void Update()
        {
            WaterSurface.Tick();
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
}
