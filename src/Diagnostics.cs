using System.Collections;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Counts what the mod has done and shows a one-line status on screen shortly after you spawn, so it's
    /// obvious at a glance whether the mod is running and whether it's working.
    /// </summary>
    internal static class Diagnostics
    {
        public static int Attached;
        public static int Failures;
        public static string LastError;

        public static void RecordFailure(string what, System.Exception e)
        {
            Failures++;
            string text = e != null ? e.Message : "";
            LastError = $"{what}: {(text.Length > 70 ? text.Substring(0, 70) + "..." : text)}";
        }

        /// <summary>Waits for the world's portals to load, then shows the status.</summary>
        public static IEnumerator ReportSoon()
        {
            yield return new WaitForSeconds(12f);
            Report();
        }

        public static void Report()
        {
            if (!PortalHeimSG1Plugin.ShowStatus.Value || MessageHud.instance == null) return;

            int portals = Object.FindObjectsByType<TeleportWorld>(FindObjectsSortMode.None).Length;
            int gates = Object.FindObjectsByType<GateRing>(FindObjectsSortMode.None).Length;
            string water = PortalHeimSG1Plugin.Interior.Value != InteriorMode.Water ? "water off"
                : WaterSurface.Failed ? "water FAILED" : WaterSurface.Ready ? "water ready" : "water loading";
            string travel = PortalHeimSG1Plugin.Travel.Value == TravelMode.Tunnel ? $", tunnel {TunnelSurface.Status}" : "";
            string text = $"PortalHeim SG1 {PortalHeimSG1Plugin.Version}: {gates} of {portals} portals are gates, {water}, {GateSounds.Summary}{travel}";
            if (Failures > 0) text += $" | ERROR: {LastError}";
            else if (portals > 0 && gates == 0) text += " | no gates attached, see BepInEx log";

            MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
            PortalHeimSG1Plugin.Log.LogInfo(text);
        }
    }
}
