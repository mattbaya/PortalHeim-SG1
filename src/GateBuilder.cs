using System.Collections.Generic;
using System.Linq;
using PortalHeimSG1.Art;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>Attaches the stone ring, rotating rune ring and chevrons to a portal, and hides its wooden frame.</summary>
    internal static class GateBuilder
    {
        // Centre of the vanilla portal's opening, in the portal's own space (its swirl effect sits at y = 1.37).
        private static readonly Vector3 GateCenter = new Vector3(0f, 1.50f, 0f);

        private static Mesh _outerRing, _innerRing, _chevron;
        private static Material _outerMaterial, _innerMaterial;
        private static bool _built;

        /// <summary>Shared chevron materials: one dark, one glowing. Chevrons just switch between them.</summary>
        public static Material ChevronOff { get; private set; }
        public static Material ChevronOn { get; private set; }

        public static void Attach(TeleportWorld portal)
        {
            if (portal == null || portal.GetComponentInChildren<GateRing>(true) != null) return;
            EnsureAssets(portal);
            if (_outerRing == null) return;

            var root = new GameObject("SG1Gate").transform;
            root.SetParent(portal.transform, false);
            root.localPosition = GateCenter;

            Add(root, "OuterRing", _outerRing, _outerMaterial);
            var inner = Add(root, "InnerRing", _innerRing, _innerMaterial);

            var chevrons = new List<MeshRenderer>();
            float radius = (GateMeshes.OuterRingIn + GateMeshes.OuterRadius) * 0.5f;
            for (int i = 0; i < GateMeshes.ChevronCount; i++)
            {
                // The first chevron sits at the top; the rest follow around the ring.
                float angle = 90f + 360f * i / GateMeshes.ChevronCount;
                var go = Add(root, $"Chevron{i}", _chevron, ChevronOff);
                go.transform.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(radius, 0, 0);
                go.transform.localRotation = Quaternion.Euler(0, 0, angle);
                var chevronRenderer = go.GetComponent<MeshRenderer>();
                chevronRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                chevronRenderer.receiveShadows = false;
                chevrons.Add(chevronRenderer);
            }

            HideWoodenFrame(portal);

            var ring = root.gameObject.AddComponent<GateRing>();
            ring.Setup(portal, inner.transform, chevrons.ToArray());
        }

        private static GameObject Add(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return go;
        }

        /// <summary>
        /// The frame is on a LODGroup, which keeps re-enabling its renderers, so switch rendering off
        /// instead (it stays off whatever the LODGroup does).
        /// </summary>
        private static void HideWoodenFrame(TeleportWorld portal)
        {
            // m_model is the portal's frame mesh (the effects and the destruction debris are separate).
            if (portal.m_model != null) portal.m_model.forceRenderingOff = true;
        }

        private static void EnsureAssets(TeleportWorld portal)
        {
            if (_built) return;
            _built = true;
            try
            {
                _outerRing = GateMeshes.Annulus("SG1OuterRing", GateMeshes.OuterRingIn, GateMeshes.OuterRadius, GateMeshes.OuterHalfDepth);
                _innerRing = GateMeshes.Annulus("SG1InnerRing", GateMeshes.InnerRingIn, GateMeshes.InnerRingOut, GateMeshes.InnerHalfDepth);
                _chevron = GateMeshes.Chevron();

                // Clone the portal's own material: its shader is known to render in this game, whatever
                // shader variants the build kept.
                var template = portal.m_model != null ? portal.m_model.sharedMaterial : null;
                _outerMaterial = StoneMaterial(template, Textures.OuterRing(GateMeshes.OuterRadius, GateMeshes.OuterRingIn, GateMeshes.OuterRadius), "SG1OuterStone");
                _innerMaterial = StoneMaterial(template, Textures.InnerRing(GateMeshes.OuterRadius, GateMeshes.InnerRingIn, GateMeshes.InnerRingOut, 36), "SG1InnerStone");
                // Flat, unlit colour. If neither colour shader is in this build, fall back to the portal's own shader.
                var flat = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? (template != null ? template.shader : null);
                PortalHeimSG1Plugin.Log.LogInfo($"Chevron shader: {(flat != null ? flat.name : "none")}");
                ChevronOff = Flat(flat, "SG1ChevronOff", GateRing.Dark);
                ChevronOn = Flat(flat, "SG1ChevronOn", PortalHeimSG1Plugin.ChevronColor.Value);
                PortalHeimSG1Plugin.ChevronColor.SettingChanged += (_, __) => Tint(ChevronOn, PortalHeimSG1Plugin.ChevronColor.Value);

                // Valheim unloads unused assets when you leave a world; keep ours so the next world still has them.
                foreach (var obj in new UnityEngine.Object[] { _outerRing, _innerRing, _chevron, _outerMaterial, _innerMaterial, ChevronOff, ChevronOn })
                    if (obj != null) obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }
            catch (System.Exception e)
            {
                PortalHeimSG1Plugin.Log.LogError($"Couldn't build the gate art; portals stay as they are. {e}");
                _outerRing = null;
            }
        }

        private static readonly string[] ExtraTextures =
        {
            "_BumpMap", "_MetallicGlossMap", "_EmissiveTex", "_EmissionMap", "_OcclusionMap", "_ParallaxMap", "_DetailAlbedoMap", "_DetailNormalMap",
        };

        private static Material Flat(Shader shader, string name, Color color)
        {
            var material = new Material(shader) { name = name };
            Tint(material, color);
            return material;
        }

        private static void Tint(Material material, Color color)
        {
            if (material == null) return;
            material.color = color;
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color);
        }

        private static Material StoneMaterial(Material template, Canvas canvas, string name)
        {
            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, true) { name = name + "Tex" };
            // SetPixelData takes just the base level; Apply then builds the mip chain.
            // (LoadRawTextureData would demand the whole chain's bytes and throw.)
            texture.SetPixelData(canvas.Rgba, 0);
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(true, true); // also drops the CPU copy; the texture is never read back
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            Material material;
            if (template != null)
            {
                material = new Material(template) { name = name };
                foreach (var property in ExtraTextures)
                    if (material.HasProperty(property)) material.SetTexture(property, null);
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            }
            else
            {
                material = new Material(Shader.Find("Standard") ?? Shader.Find("Sprites/Default")) { name = name };
            }

            material.mainTexture = texture;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            return material;
        }
    }
}
