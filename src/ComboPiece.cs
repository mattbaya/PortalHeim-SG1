using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// An extra entry in the build list: a portal and a workbench placed together. The workbench is an ordinary workbench that
    /// is built next to the portal when you place it, so it can be taken down on its own, and the portal doesn't need one nearby.
    /// </summary>
    internal static class ComboPiece
    {
        public const string PrefabName = "portal_wood_sg1";
        private static GameObject _holder;
        /// <summary>The combined piece, once made.</summary>
        public static GameObject Prefab { get; private set; }

        public static bool IsCombo(Piece piece) => piece != null && Utils.GetPrefabName(piece.gameObject) == PrefabName;

        /// <summary>Makes the combined piece from the game's portal and workbench, and puts it in the hammer's list.</summary>
        public static void Register(ZNetScene scene)
        {
            if (!PortalHeimSG1Plugin.ComboEnabled.Value || scene == null) return;
            var portal = scene.GetPrefab("portal_wood");
            var bench = scene.GetPrefab("piece_workbench");
            if (portal == null || bench == null || scene.GetPrefab(PrefabName) != null) return;

            if (_holder == null)
            {
                _holder = new GameObject("PortalHeimSG1_Prefabs");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
            }

            var clone = Object.Instantiate(portal, _holder.transform);
            clone.name = PrefabName;
            var piece = clone.GetComponent<Piece>();
            var benchPiece = bench.GetComponent<Piece>();
            piece.m_name = "Portal and Workbench";
            piece.m_description = "A portal with a workbench built beside it. The workbench is a normal one and can be removed separately.";
            piece.m_craftingStation = null; // the workbench comes with it
            piece.m_resources = piece.m_resources.Concat(benchPiece.m_resources).ToArray();

            Prefab = clone;
            scene.m_prefabs.Add(clone);
            scene.m_namedPrefabs[PrefabName.GetStableHashCode()] = clone;

            var hammer = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab("Hammer") : null;
            var pieces = hammer != null ? hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces : null;
            if (pieces != null && !pieces.m_pieces.Contains(clone)) pieces.m_pieces.Add(clone);
            PortalHeimSG1Plugin.Log.LogInfo(pieces != null ? "Added the 'Portal and Workbench' piece to the hammer's build list." : "Made the 'Portal and Workbench' piece (it will be added to the build list when the hammer is used).");
        }

        /// <summary>Builds the workbench beside a combo portal that was just placed.</summary>
        public static void PlaceBench(Player player, Vector3 position, Quaternion rotation)
        {
            var bench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("piece_workbench") : null;
            if (bench == null) return;

            float side = PortalHeimSG1Plugin.ComboBenchDistance.Value;
            var at = position + rotation * Vector3.right * side;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(at, out float height)) at.y = height;
            var made = Object.Instantiate(bench, at, rotation);
            made.GetComponent<ZNetView>()?.GetZDO()?.Set(ZDOVars.s_creator, player.GetPlayerID());
        }
    }

    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    internal static class ComboRegisterPatch
    {
        private static void Postfix(ZNetScene __instance)
        {
            try { ComboPiece.Register(__instance); }
            catch (System.Exception e) { PortalHeimSG1Plugin.Log.LogError("Couldn't add the Portal and Workbench piece: " + e); }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    internal static class ComboPlacePatch
    {
        private static readonly int KeyBench = "sg1_bench".GetStableHashCode();
        private static readonly System.Collections.Generic.List<Piece> Found = new System.Collections.Generic.List<Piece>();

        // PlacePiece doesn't say whether it placed anything, so look for a Portal and Workbench that was just placed here and
        // hasn't had its workbench built yet.
        private static void Postfix(Player __instance, Vector3 pos)
        {
            if (!PortalHeimSG1Plugin.ComboEnabled.Value) return;
            Found.Clear();
            Piece.GetAllPiecesInRadius(pos, 1.5f, Found);
            foreach (var piece in Found)
            {
                if (!ComboPiece.IsCombo(piece) || piece.m_nview == null || !piece.m_nview.IsValid()) continue;
                var zdo = piece.m_nview.GetZDO();
                if (zdo.GetInt(KeyBench) == 1) continue;
                zdo.Set(KeyBench, 1);
                ComboPiece.PlaceBench(__instance, piece.transform.position, piece.transform.rotation);
            }
        }
    }

    /// <summary>
    /// Makes sure the combined piece is in the hammer's list whenever the game rebuilds the list of pieces you can place, and
    /// unlocked if you already know portals (the hammer may not have been ready when the piece was made).
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateAvailablePiecesList))]
    internal static class ComboAvailablePatch
    {
        private static void Prefix(Player __instance)
        {
            var combo = ComboPiece.Prefab;
            var table = __instance.m_buildPieces;
            if (combo == null || table == null || table.m_pieces == null) return;
            if (!table.m_pieces.Exists(p => p != null && p.name == "portal_wood")) return;   // only the hammer's list

            if (!table.m_pieces.Contains(combo)) table.m_pieces.Add(combo);
            var piece = combo.GetComponent<Piece>();
            if (piece != null && __instance.m_knownRecipes != null && __instance.m_knownRecipes.Contains("$piece_portal"))
                __instance.m_knownRecipes.Add(piece.m_name);
        }
    }
}
