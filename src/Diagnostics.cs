using System.Collections;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Counts what the mod has done and writes a one-line status to the log shortly after you spawn (and shows it on screen only if asked, or if something went wrong), so it's
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
            int portals = Object.FindObjectsByType<TeleportWorld>(FindObjectsSortMode.None).Length;
            int gates = Object.FindObjectsByType<GateRing>(FindObjectsSortMode.None).Length;
            string water = PortalHeimSG1Plugin.Interior.Value != InteriorMode.Water ? "water off"
                : WaterSurface.Failed ? "water FAILED" : WaterSurface.Ready ? "water ready" : "water loading";
            string travel = PortalHeimSG1Plugin.Travel.Value == TravelMode.Tunnel ? $", tunnel {TunnelSurface.Status}" : "";
            string text = $"PortalHeim SG1 {PortalHeimSG1Plugin.Version}: {gates} of {portals} portals are gates, {water}, {GateSounds.Summary}{travel}";
            if (Failures > 0) text += $" | ERROR: {LastError}";
            else if (portals > 0 && gates == 0 && PortalHeimSG1Plugin.Reskin.Value) text += " | no gates attached, see BepInEx log";

            // Always in the log; on screen only if asked for, or if something went wrong.
            PortalHeimSG1Plugin.Log.LogInfo(text);
            bool problem = Failures > 0;
            if ((PortalHeimSG1Plugin.ShowStatus.Value || problem) && MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }
    }
}
