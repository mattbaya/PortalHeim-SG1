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

    public enum HatchStyles { GullWing, CellarDoors }

    public enum IncomingSounds
    {
        /// <summary>One chevrune thud, together with the opening sound, as the gate opens.</summary>
        Burst,
        /// <summary>Nothing: no chevrune thuds and no opening sound.</summary>
        Silent,
        /// <summary>The full sequence, same as the gate you start from.</summary>
        Normal
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
        public const string Version = "0.10.7";

        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> Reskin;
        internal static ConfigEntry<Color> ChevruneColor;
        internal static ConfigEntry<float> LockSeconds;
        internal static ConfigEntry<float> DormantGlow;
        internal static ConfigEntry<bool> Ramps;
        internal static ConfigEntry<bool> BackRamp;
        internal static ConfigEntry<float> RampDepth;
        internal static ConfigEntry<float> RampSlope;
        internal static ConfigEntry<bool> ComboEnabled;
        internal static ConfigEntry<bool> StonePortalStyle;
        internal static ConfigEntry<bool> SortByDistance;
        internal static ConfigEntry<bool> StorageEnabled;
        internal static ConfigEntry<HatchStyles> HatchStyle;
        internal static ConfigEntry<int> StorageColumns;
        internal static ConfigEntry<int> StorageRows;
        internal static ConfigEntry<float> ComboBenchDistance;
        internal static ConfigEntry<bool> SinkRing;
        internal static ConfigEntry<ShadingMode> Shading;
        internal static ConfigEntry<InteriorMode> Interior;
        internal static ConfigEntry<TravelMode> Travel;
        internal static ConfigEntry<float> Volume;
        internal static ConfigEntry<bool> UseBundledSounds;
        internal static ConfigEntry<bool> ChevruneThuds;
        internal static ConfigEntry<bool> OpeningSound;
        internal static ConfigEntry<IncomingSounds> IncomingGate;
        internal static ConfigEntry<bool> TravelSounds;
        internal static ConfigEntry<float> TravelMusicVolume;
        internal static ConfigEntry<float> TravelEffectVolume;
        internal static ConfigEntry<bool> AllowAllItems;
        internal static ConfigEntry<float> ActivationRange;
        internal static ConfigEntry<bool> CartsFollow;
        internal static ConfigEntry<float> CartExitDistance;
        internal static ConfigEntry<bool> DialingEnabled;
        internal static ConfigEntry<bool> AllowDialingLinkedGates;
        internal static ConfigEntry<bool> OnlyVisited;
        internal static ConfigEntry<Vector2> DialWindowPos;
        internal static ConfigEntry<bool> GatePins;
        internal static ConfigEntry<bool> GateIconOnMap;
        internal static ConfigEntry<Minimap.PinType> PinIcon;
        internal static ConfigEntry<float> DialIdleSeconds;
        internal static ConfigEntry<float> DialMaxSeconds;
        internal static ConfigEntry<float> DialRange;
        internal static ConfigEntry<float> DialKeepOpenRange;
        internal static ConfigEntry<bool> ShowStatus;
        internal static ConfigEntry<KeyboardShortcut> PreviewKey;
        internal static PortalHeimSG1Plugin Instance;
        internal static bool Headless;
        /// <summary>When the local player last finished a teleport (the gate they arrive at counts as the incoming one for a while).</summary>
        internal static float ArrivedAt = -1000f;
        private static bool _wasTeleporting;

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
                "Add a gentle ramp up to the portal's base on the front (the side you step out of), so anyone can walk in, even when overencumbered. " +
                "Takes effect for portals loaded after this is changed.");
            RampDepth = Config.Bind("Look", "RampDepth", 1.5f,
                new ConfigDescription("How far below the portal's base the ramp starts, in meters. Deeper means it also runs further out, at the same gentle angle, so it meets the ground " +
                    "even where the ground falls away in front of the portal and there's no step up onto it. Takes effect for portals loaded after this is changed.", new AcceptableValueRange<float>(0.4f, 3f)));
            RampSlope = Config.Bind("Look", "RampSlope", 1.0f,
                new ConfigDescription("How steep the ramp is: height gained for each metre along. 1.0 is 45 degrees (the default) and 0.7 is about 35. A steeper ramp is much shorter " +
                    "for the same RampDepth. If your character slips on it, lower this. Takes effect for portals loaded after this is changed (restart the game).", new AcceptableValueRange<float>(0.15f, 1.2f)));
            BackRamp = Config.Bind("Look", "BackRamp", false,
                "Also add a ramp on the back of the portal. Off by default. Takes effect for portals loaded after this is changed.");
            SinkRing = Config.Bind("Look", "SinkRing", false,
                "Sit the ring low so the bottom of the opening is level with the ground (the lower part of the outer ring is buried), " +
                "instead of having a stone lip to step over. Takes effect for portals loaded after this is changed.");
            // One-time move to the current layout: the ring back at its normal height, the ramp doing the work of going down into the ground.
            var layout = Config.Bind("Look", "LayoutVersion", 0, "Internal: used to move old settings to new defaults. Leave it alone.");
            if (layout.Value < 1) SinkRing.Value = false;
            if (layout.Value < 2)
            {
                RampDepth.Value = 1.5f;
                RampSlope.Value = 1.0f;
            }

            layout.Value = 2;

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
            StorageEnabled = Config.Bind("Storage", "Enabled", true,
                "Every portal has a built-in chest: Shift+E on the gate (or the button in its dial window) opens it, and a hatch in front of the gate lifts. Needs this mod on the server too. Takes effect for portals loaded after this is changed.");
            HatchStyle = Config.Bind("Storage", "HatchStyle", HatchStyles.GullWing,
                "How the hatch in front of a gate opens: GullWing (two lids hinged down the middle, the outer edges lift, like a DeLorean) or CellarDoors (hinged at the outer edges, the middle lifts). Takes effect for portals loaded after this is changed.");
            StorageColumns = Config.Bind("Storage", "Columns", 6, new ConfigDescription("Width of a gate's storage, in slots.", new AcceptableValueRange<int>(1, 8)));
            StorageRows = Config.Bind("Storage", "Rows", 4, new ConfigDescription("Height of a gate's storage, in slots.", new AcceptableValueRange<int>(1, 8)));
            SortByDistance = Config.Bind("Dialing", "SortByDistance", false,
                "Sort the dial list nearest first instead of A to Z (favorites and recently dialed gates still come first). Also the button in the dial window.");
            StonePortalStyle = Config.Bind("Look", "StonePortalStyle", true,
                "Give the game's Stone Portal its own look: thicker, darker stone with chunkier runes, and chevrunes that lock from the top down. Takes effect for portals loaded after this is changed.");
            ComboEnabled = Config.Bind("Portals", "PortalAndWorkbench", true,
                "Add a 'Portal and Workbench' piece to the hammer's build list: a portal with a normal workbench built beside it (cost of both). Needs this mod on the server too. Takes effect when the game starts.");
            ComboBenchDistance = Config.Bind("Portals", "WorkbenchDistance", 3.6f,
                new ConfigDescription("How far to the side of the portal the combined piece puts its workbench, in metres.", new AcceptableValueRange<float>(2.5f, 8f)));
            AllowAllItems = Config.Bind("Portals", "AllowAllItems", true,
                "Let players carry anything through a portal, including ore, metal and other items the game normally keeps out. " +
                "Takes effect for portals loaded after this is changed. Each player's own setting applies to them.");
            ActivationRange = Config.Bind("Portals", "ActivationRange", 3f,
                new ConfigDescription("How close (in meters) you have to be before a portal wakes up: the ring starts its start-up sequence, the water fills in and the portal " +
                    "can be used. The game's own portals use 5. Takes effect for portals loaded after this is changed.", new AcceptableValueRange<float>(1f, 10f)));
            CartsFollow = Config.Bind("Portals", "CartsFollow", true,
                "Someone pulling a cart can walk through a portal and the cart comes with them, ready to keep pulling. Needs the mod on the server too.");
            CartExitDistance = Config.Bind("Portals", "CartExitDistance", 5.5f,
                new ConfigDescription("How much further from the gate, in metres, you come out when you're pulling a cart, so the cart behind you isn't inside the gate.", new AcceptableValueRange<float>(0f, 15f)));
            DialingEnabled = Config.Bind("Dialing", "Enabled", true,
                "Let players dial any gate they know, instead of only gates that share a name. Needs this mod on the server too " +
                "(a dedicated server, or the host's own game); without it, gates behave as in vanilla. Switch off on the server to turn dialing off for everyone.");
            AllowDialingLinkedGates = Config.Bind("Dialing", "AllowDialingLinkedGates", true,
                "Gates paired the old way (two sharing a name) can be dialed from, or dialed to, too. The gate they were paired with is set aside " +
                "until the dial closes, and then the original pair is put back. Set to false to leave paired gates alone. (Server setting for what's allowed.)");
            DialWindowPos = Config.Bind("Dialing", "DialWindowPosition", new Vector2(-1f, -1f),
                "Where the dial window sits (top-left corner, in pixels). Drag the window by any empty part to move it; -1 means centred.");
            OnlyVisited = Config.Bind("Dialing", "OnlyShowVisitedGates", true,
                "Only list (and put on the map) gates you have been near, or built yourself. Set to false to see every gate in the world.");
            GatePins = Config.Bind("Dialing", "ShowGatePins", true,
                "Show gates on the map. Shift+click a gate's pin while standing at an idle gate to dial it.");
            GateIconOnMap = Config.Bind("Dialing", "GateIconOnMap", true,
                "Mark gates on the map with this mod's own little gate (the powered-off gate sunk in the ground with its ramp, flat like the game's own pins). Set to false to use a built-in map icon instead.");
            PinIcon = Config.Bind("Dialing", "PinIcon", Minimap.PinType.Icon4,
                "Which of the map's built-in pin icons marks a gate when GateIconOnMap is off (it also decides which map icon filter hides the gate pins).");
            DialIdleSeconds = Config.Bind("Dialing", "CloseAfterIdleSeconds", 15f,
                new ConfigDescription("A dialed gate closes this many seconds after the last player leaves both gates. (Server setting.)", new AcceptableValueRange<float>(3f, 300f)));
            DialMaxSeconds = Config.Bind("Dialing", "MaxOpenSeconds", 300f,
                new ConfigDescription("A dialed gate always closes after this long, even if someone is standing by it. (Server setting.)", new AcceptableValueRange<float>(10f, 3600f)));
            DialRange = Config.Bind("Dialing", "DialRange", 15f,
                new ConfigDescription("How close you must be to a gate to dial from it. (Server setting.)", new AcceptableValueRange<float>(3f, 100f)));
            DialKeepOpenRange = Config.Bind("Dialing", "KeepOpenRange", 15f,
                new ConfigDescription("A dialed gate stays open while a player is this close to either gate. (Server setting.)", new AcceptableValueRange<float>(3f, 100f)));
            PreviewKey = Config.Bind("Diagnostics", "PreviewKey", new KeyboardShortcut(KeyCode.F8),
                "Stand near a gate and press this to replay the opening sequence (Chevrunes locking, the water, and all the sounds) " +
                "without having to disconnect and reconnect the portal.");
            ShowStatus = Config.Bind("Diagnostics", "ShowStatusMessage", false,
                "Show a one-line status message a few seconds after you spawn (how many portals are gates, whether the water and sounds loaded, any error). " +
                "Handy while testing; turn off once everything works.");
            TravelSounds = Config.Bind("Sound", "TravelSounds", true,
                "Play bass music while you're travelling through a portal (on the loading screen).");
            TravelMusicVolume = Config.Bind("Sound", "TravelMusicVolume", 0.75f,
                new ConfigDescription("Volume of the travel music.", new AcceptableValueRange<float>(0f, 1f)));
            TravelEffectVolume = Config.Bind("Sound", "TravelEffectVolume", 0.5f,
                new ConfigDescription("Volume of the optional second sound (a 'travelfx' clip you add) layered under the travel music. Nothing ships for it.", new AcceptableValueRange<float>(0f, 1f)));
            ChevruneThuds = Config.Bind("Sound", "ChevruneThuds", true,
                "Play the chevrune thud once, as a gate opens (on both the gate you dial from and the one that receives you).");
            OpeningSound = Config.Bind("Sound", "OpeningSound", true,
                "Play the gate-opening rumble and splash when the start-up sequence finishes. (What an incoming gate does is set by IncomingGate.)");
            IncomingGate = Config.Bind("Sound", "IncomingGate", IncomingSounds.Burst,
                "What an incoming gate sounds like (one that's opening but isn't the gate you're standing at, including the gate you arrive at, for 15 seconds). " +
                "Burst = one chevrune thud together with the opening sound, as it opens. Silent = no chevrune or opening sounds. " +
                "Normal = the whole sequence, like the gate you start from. The gate you start from always plays everything.");
            UseBundledSounds = Config.Bind("Sound", "UseBundledSounds", true,
                "Play the sounds that come with the mod. Turn off for silence (or to use only your own clips). Takes effect on next game start.");
            Volume = Config.Bind("Sound", "Volume", 1.0f,
                new ConfigDescription("Volume of the gate sounds you provide (also follows the game's sound-effects volume).", new AcceptableValueRange<float>(0f, 1f)));

            // A dedicated server has no screen or speakers: it only needs the dialing logic.
            Headless = Application.isBatchMode;
            if (!Headless)
            {
                StartCoroutine(GateSounds.LoadAll(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "sounds")));
                if (Interior.Value == InteriorMode.Water || Travel.Value == TravelMode.Gate) StartCoroutine(WaterSurface.Build());
                if (Travel.Value == TravelMode.Tunnel) TunnelSurface.Start();
            }

            _harmony = new Harmony(Guid);
            SafePatch.All(_harmony, typeof(PortalHeimSG1Plugin).Assembly, Log);
            Log.LogInfo($"{Name} {Version} loaded. Sound files go in {GateSounds.Folder}");
        }

        private void OnGUI()
        {
            if (Headless) return;
            DialMenu.Draw();
            StyleWindow.Draw();
            DialMap.DrawHint();
        }

        private void Update()
        {
            GateServer.Tick();
            if (Headless) return;

            GateClient.Tick();
            TrackArrival();
            CartFollow.Tick();
            DialMap.Tick();
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

        private static void TrackArrival()
        {
            bool teleporting = Player.m_localPlayer != null && Player.m_localPlayer.IsTeleporting();
            if (_wasTeleporting && !teleporting) ArrivedAt = Time.time;
            _wasTeleporting = teleporting;
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
            GateRegistry.Add(__instance);
            try { PortalChest.Attach(__instance); }
            catch (System.Exception e) { PortalHeimSG1Plugin.Log.LogError("Couldn't add storage to a portal: " + e); }
            if (PortalHeimSG1Plugin.AllowAllItems.Value) __instance.m_allowAllItems = true;
            __instance.m_activationRange = PortalHeimSG1Plugin.ActivationRange.Value;
            if (!PortalHeimSG1Plugin.Reskin.Value || PortalHeimSG1Plugin.Headless) return;
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
