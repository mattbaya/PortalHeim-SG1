using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// Lets someone pulling a cart take it through a gate. The game lets go of the cart the moment you start teleporting,
    /// and the area you left unloads with it, so the cart is carried across in three steps:
    ///   1. As you step through, we note which cart you were pulling and where it sat relative to you.
    ///   2. Once you've arrived, we ask the server to move that cart's record to the same spot beside you
    ///      (only the server knows about things in areas you have left).
    ///   3. When the cart appears here, we take hold of it and hook it back onto you.
    /// You come out further from the gate than usual, so the cart behind you isn't inside the gate. Needs the mod on the
    /// server (the same handshake as dialing).
    /// </summary>
    internal static class CartFollow
    {
        private class Trip
        {
            public ZDOID Cart;
            public Vector3 Relative;
            public Quaternion RelativeRotation;
            public float Started;
            public bool SawTeleport;
            public float ArrivedAt = -1f;
        }

        private static Trip _candidate;   // set just before a gate sends you through; becomes the trip if you really go
        private static Trip _trip;

        public static bool Enabled => PortalHeimSG1Plugin.CartsFollow.Value && GateClient.ServerHasMod;

        public static bool HasCandidate => _candidate != null;

        /// <summary>The cart this player is currently pulling, if any.</summary>
        private static Vagon PulledBy(Player player)
        {
            foreach (var cart in Vagon.m_instances)
            {
                if (cart != null && cart.m_attachedObject == player.gameObject && cart.m_nview != null && cart.m_nview.IsValid() && cart.IsAttached())
                    return cart;
            }

            return null;
        }

        /// <summary>Called as a gate is about to send the local player through.</summary>
        public static void Prepare(Player player)
        {
            _candidate = null;
            if (!Enabled || player == null || player != Player.m_localPlayer) return;

            var cart = PulledBy(player);
            if (cart == null) return;

            _candidate = new Trip
            {
                Cart = cart.m_nview.GetZDO().m_uid,
                Relative = player.transform.InverseTransformPoint(cart.transform.position),
                RelativeRotation = Quaternion.Inverse(player.transform.rotation) * cart.transform.rotation,
                Started = Time.time,
            };
        }

        /// <summary>Called once the gate has finished: if the trip really started, remember it; otherwise forget it.</summary>
        public static void Settle(bool started)
        {
            _trip = started ? _candidate : _trip;
            _candidate = null;
        }

        public static float ExtraExitDistance => HasCandidate ? PortalHeimSG1Plugin.CartExitDistance.Value : 0f;

        public static void Tick()
        {
            var player = Player.m_localPlayer;
            if (_trip == null || player == null) return;

            if (player.IsTeleporting())
            {
                _trip.SawTeleport = true;
                return;
            }

            if (!_trip.SawTeleport)
            {
                // The trip never started (it was refused): drop it.
                if (Time.time - _trip.Started > 5f) _trip = null;
                return;
            }

            // Arrived. Give the ground around you a moment to load before putting the cart down.
            if (_trip.ArrivedAt < 0f) _trip.ArrivedAt = Time.time;
            if (Time.time - _trip.ArrivedAt < 1f) return;

            var trip = _trip;
            _trip = null;
            Vector3 position = player.transform.TransformPoint(trip.Relative);
            Quaternion rotation = player.transform.rotation * trip.RelativeRotation;
            GateNet.RequestMoveCart(trip.Cart, position, rotation);
            PortalHeimSG1Plugin.Instance.StartCoroutine(Reattach(trip.Cart, player));
        }

        private static IEnumerator Reattach(ZDOID id, Player player)
        {
            float end = Time.time + 12f;
            while (Time.time < end && player != null)
            {
                yield return new WaitForSeconds(0.25f);

                Vagon cart = null;
                foreach (var candidate in Vagon.m_instances)
                    if (candidate != null && candidate.m_nview != null && candidate.m_nview.IsValid() && candidate.m_nview.GetZDO().m_uid == id) cart = candidate;
                if (cart == null) continue;

                if (!cart.m_nview.IsOwner())
                {
                    cart.m_nview.ClaimOwnership();
                    continue;
                }

                if (cart.m_body != null)
                {
                    cart.m_body.velocity = Vector3.zero;
                    cart.m_body.angularVelocity = Vector3.zero;
                }

                // The cart's record may still say it's attached from before the trip, which would make the cart's own code
                // let go instead of hooking on. Clear that, line its handle up with the player, and hook it on directly.
                var zdo = cart.m_nview.GetZDO();
                if (zdo.GetBool(ZDOVars.s_attachJointHash)) zdo.Set(ZDOVars.s_attachJointHash, false);

                Vector3 shift = (player.transform.position + cart.m_attachOffset) - cart.m_attachPoint.position;
                cart.transform.position += shift;
                if (cart.m_body != null) cart.m_body.position = cart.transform.position;

                cart.AttachTo(player.gameObject);
                yield break;
            }
        }

        /// <summary>The server's part: moves a cart that's no longer loaded for anyone to where its puller arrived.</summary>
        public static void ServerMove(long sender, ZDOID id, Vector3 position, Quaternion rotation)
        {
            var zdo = ZDOMan.instance.GetZDO(id);
            if (zdo == null || ZNetScene.instance == null) return;

            var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
            if (prefab == null || prefab.GetComponent<Vagon>() == null) return;

            // Only ever move a cart to where the player who asked is standing.
            if (GateServer.TryGetPlayerPosition(sender, out var where) && Vector3.Distance(where, position) > 25f) return;

            zdo.SetOwner(ZDOMan.GetSessionID());
            zdo.SetPosition(position);
            zdo.SetRotation(rotation);
            // Unowned, so the player's game takes charge of it when it appears there.
            zdo.SetOwner(0L);
            ZDOMan.instance.ForceSendZDO(id);
        }
    }

    [HarmonyPatch(typeof(TeleportWorld))]
    internal static class CartPortalPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TeleportWorld.Teleport))]
        private static void Teleport_Prefix(Player player) => CartFollow.Prepare(player);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(TeleportWorld.Teleport))]
        private static void Teleport_Postfix(Player player)
        {
            CartFollow.Settle(CartFollow.HasCandidate && player != null && player.IsTeleporting());
        }
    }

    /// <summary>Puts a cart puller down further from the gate, so the cart behind them clears it.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TeleportTo))]
    internal static class CartExitPatch
    {
        private static void Prefix(Player __instance, ref Vector3 pos, Quaternion rot)
        {
            float extra = CartFollow.ExtraExitDistance;
            if (extra > 0f && __instance == Player.m_localPlayer) pos += rot * Vector3.forward * extra;
        }
    }
}
