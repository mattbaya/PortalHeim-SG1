using System.Collections.Generic;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// How a gate is dressed up: a colour for its chevrunes, a word spelled out in runes, and an icon. Each is a short piece of text
    /// kept in the portal's own saved data, so everyone sees it and it goes into the gate list.
    /// </summary>
    internal static class GateStyle
    {
        public static readonly int KeyColor = "sg1_color".GetStableHashCode();
        public static readonly int KeyRunes = "sg1_runes".GetStableHashCode();
        public static readonly int KeyIcon = "sg1_icon".GetStableHashCode();

        // Elder Futhark, in its usual order: 0 Fehu, 1 Uruz, 2 Thurisaz, 3 Ansuz, 4 Raidho, 5 Kenaz, 6 Gebo, 7 Wunjo, 8 Hagalaz,
        // 9 Nauthiz, 10 Isa, 11 Jera, 12 Eihwaz, 13 Perthro, 14 Algiz, 15 Sowilo, 16 Tiwaz, 17 Berkano, 18 Ehwaz, 19 Mannaz,
        // 20 Laguz, 21 Ingwaz, 22 Dagaz, 23 Othala.
        private static readonly Dictionary<char, int> Letters = new Dictionary<char, int>
        {
            ['A'] = 3, ['B'] = 17, ['C'] = 5, ['D'] = 22, ['E'] = 18, ['F'] = 0, ['G'] = 6, ['H'] = 8, ['I'] = 10, ['J'] = 11,
            ['K'] = 5, ['L'] = 20, ['M'] = 19, ['N'] = 9, ['O'] = 23, ['P'] = 13, ['Q'] = 5, ['R'] = 4, ['S'] = 15, ['T'] = 16,
            ['U'] = 1, ['V'] = 7, ['W'] = 7, ['X'] = 14, ['Y'] = 12, ['Z'] = 14,
        };

        /// <summary>Keeps just the letters that have a rune, in capitals, up to the nine a gate has.</summary>
        public static string Clean(string word)
        {
            var text = new System.Text.StringBuilder();
            foreach (char c in (word ?? "").ToUpperInvariant())
                if (Letters.ContainsKey(c) && text.Length < GateMeshes.ChevruneCount) text.Append(c);
            return text.ToString();
        }

        /// <summary>The rune for each of the nine chevrunes: the word's letters first, then the usual runes for the rest.</summary>
        public static int[] RunesFor(string word)
        {
            word = Clean(word);
            var result = (int[])GateBuilder.DefaultRunes.Clone();
            for (int i = 0; i < word.Length && i < result.Length; i++) result[i] = Letters[word[i]];
            return result;
        }

        private static readonly Dictionary<int, Mesh> RuneMeshes = new Dictionary<int, Mesh>();

        public static Mesh RuneMesh(int glyph)
        {
            if (RuneMeshes.TryGetValue(glyph, out var mesh) && mesh != null) return mesh;
            mesh = GateMeshes.Rune(glyph);
            mesh.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            RuneMeshes[glyph] = mesh;
            return mesh;
        }

        public static bool TryParse(string hex, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(hex)) return false;
            return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out color);
        }

        public static string Hex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

        // ------------------------------------------------------------------ icons

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Sprite SpriteOf(string prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return null;
            if (Sprites.TryGetValue(prefab, out var cached) && cached != null) return cached;

            Sprite sprite = null;
            var item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = item != null ? item.GetComponent<ItemDrop>() : null;
            if (drop != null && drop.m_itemData.m_shared.m_icons != null && drop.m_itemData.m_shared.m_icons.Length > 0)
                sprite = drop.m_itemData.m_shared.m_icons[0];
            if (sprite == null && ZNetScene.instance != null)
            {
                var go = ZNetScene.instance.GetPrefab(prefab);
                var piece = go != null ? go.GetComponent<Piece>() : null;
                if (piece != null) sprite = piece.m_icon;
            }

            if (sprite != null) Sprites[prefab] = sprite;
            return sprite;
        }

        public static string NameOf(string prefab)
        {
            var item = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
            var drop = item != null ? item.GetComponent<ItemDrop>() : null;
            return drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_name) : prefab;
        }

        private static readonly string[] Common =
        {
            "SurtlingCore", "Coins", "Hammer", "Torch", "Wood", "Stone", "Coal", "Iron", "Silver", "BlackMetal", "Flametal", "Honey",
            "TrophyDeer", "TrophyBoar", "TrophyTroll", "TrophyEikthyr", "TrophyTheElder", "TrophyBonemass", "TrophyDragonQueen", "TrophyGoblinKing",
            "Ruby", "Amber", "Cryptkey", "Wishbone", "Ymirremains", "Thunderstone",
        };

        public static List<(string prefab, string label)> Search(string query)
        {
            var results = new List<(string, string)>();
            query = (query ?? "").Trim();
            if (ObjectDB.instance == null) return results;
            if (query.Length < 2)
            {
                foreach (var prefab in Common)
                    if (SpriteOf(prefab) != null) results.Add((prefab, NameOf(prefab)));
                return results;
            }

            foreach (var go in ObjectDB.instance.m_items)
            {
                if (results.Count >= 40) break;
                var drop = go != null ? go.GetComponent<ItemDrop>() : null;
                if (drop == null || drop.m_itemData.m_shared.m_icons == null || drop.m_itemData.m_shared.m_icons.Length == 0) continue;
                string label = Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
                if (label.IndexOf(query, System.StringComparison.CurrentCultureIgnoreCase) < 0 && go.name.IndexOf(query, System.StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                results.Add((go.name, label));
            }

            return results;
        }

        public static void DrawSprite(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null || sprite.texture == null) return;
            var texture = sprite.texture;
            var r = sprite.textureRect;
            var old = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, texture, new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height));
            GUI.color = old;
        }
    }
}
