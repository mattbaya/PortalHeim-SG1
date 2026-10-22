using System;
using System.Collections.Generic;
using System.Linq;
using PortalHeimSG1.Art;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>Attaches the stone ring, rotating rune ring and chevrunes to a portal, and hides its wooden frame.</summary>
    internal static class GateBuilder
    {
        // Where the ring is centred, in the portal's own space. By default it sits low enough that the bottom of
        // the opening is level with the ground (the outer ring's lower part is buried), so there's no lip to step over.
        // (The vanilla swirl sits at y = 1.37, which is where an unsunk ring would be centred.)
        private static Vector3 GateCenter => new Vector3(0f, PortalHeimSG1Plugin.SinkRing.Value ? GateMeshes.InnerRingIn + 0.02f : 1.50f, 0f);

        // The portal's base slab: about 3 m wide, 0.8 m deep, 0.1 m tall. The ramps lead up to its top edge.
        private const float SlabHalfDepth = 0.395f;
        private const float SlabTop = 0.10f;
        private const float RampHalfWidth = 1.45f;
        // How steep the ramp is (rise over run). It runs out as far as it needs to reach its depth at this gentle angle.
        private const float RampSlope = 0.32f;
        private static Mesh _ramp;

        private const int MaxBuildAttempts = 3;

        private static Mesh _outerRing, _innerRing, _chevrune, _waterDisc;
        private static Mesh[] _runes;

        // The rune on each Chevrune, in ring order starting from the top and going round: Othala (home) at the top,
        // then V-A-L-H-E-I-M (Wunjo, Ansuz, Laguz, Hagalaz, Ehwaz, Isa, Mannaz), then Dagaz.
        private static readonly int[] RuneAt = { 23, 7, 3, 20, 8, 18, 10, 19, 22 };
        private static Material _outerMaterial, _innerMaterial;
        private static bool _built;
        private static int _failures;
        private static int _attached;

        /// <summary>Shared chevrune materials: one dark, one glowing. Chevrunes just switch between them.</summary>
        public static Material ChevruneOff { get; private set; }
        public static Material ChevruneOn { get; private set; }
        public static Material ChevruneDim { get; private set; }

        public static void Attach(TeleportWorld portal)
        {
            if (portal == null || portal.GetComponentInChildren<GateRing>(true) != null) return;
            EnsureAssets(portal);
            if (!_built) return;

            var root = new GameObject("SG1Gate").transform;
            root.SetParent(portal.transform, false);
            root.localPosition = GateCenter;

            Add(root, "OuterRing", _outerRing, _outerMaterial);
            var inner = Add(root, "InnerRing", _innerRing, _innerMaterial);

            if (PortalHeimSG1Plugin.Ramps.Value) AddRamps(portal, root);

            var chevrunes = new List<ChevruneParts>();
            float radius = (GateMeshes.OuterRingIn + GateMeshes.OuterRadius) * 0.5f;
            for (int i = 0; i < GateMeshes.ChevruneCount; i++)
            {
                // The first chevrune sits at the top; the rest follow around the ring.
                float angle = 90f + 360f * i / GateMeshes.ChevruneCount;
                var holder = new GameObject($"Chevrune{i}").transform;
                holder.SetParent(root, false);
                holder.localPosition = Quaternion.Euler(0, 0, angle) * new Vector3(radius, 0, 0);
                holder.localRotation = Quaternion.Euler(0, 0, angle);

                // The arrow it shows while idle, and the rune it turns into when it locks (hidden until then).
                var arrow = Add(holder, "Arrow", _chevrune, ChevruneOff).GetComponent<MeshRenderer>();
                var rune = Add(holder, "Rune", _runes[i], ChevruneOff).GetComponent<MeshRenderer>();
                foreach (var r in new[] { arrow, rune })
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                rune.enabled = false;
                rune.transform.localScale = Vector3.zero;
                chevrunes.Add(new ChevruneParts { Arrow = arrow, Rune = rune });
            }

            int hidden = HideWoodenFrame(portal, root);

            // The water that fills the gate when it opens (hidden until then).
            MeshRenderer water = null;
            if (PortalHeimSG1Plugin.Interior.Value == InteriorMode.Water && WaterSurface.Material != null)
            {
                var disc = Add(root, "Water", _waterDisc, WaterSurface.Material);
                disc.transform.localPosition = new Vector3(0f, 0f, 0f);
                water = disc.GetComponent<MeshRenderer>();
                water.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                water.receiveShadows = false;
                water.enabled = false;
            }

            var swirl = portal.m_target_found != null
                ? portal.m_target_found.GetComponentsInChildren<ParticleSystemRenderer>(true)
                : new ParticleSystemRenderer[0];

            var ring = root.gameObject.AddComponent<GateRing>();
            ring.Setup(portal, inner.transform, chevrunes.ToArray(), water, swirl);

            // Say what happened for the first few gates, so problems are easy to diagnose from the log.
            _attached++;
            Diagnostics.Attached++;
            if (_attached <= 3)
                PortalHeimSG1Plugin.Log.LogInfo(
                    $"Gate #{_attached} attached to '{portal.name}': hid {hidden} frame renderer(s), " +
                    $"ring at {root.position}, stone shader '{_outerMaterial.shader.name}', " +
                    $"portal enabled={portal.enabled}.");
        }

        /// <summary>A gentle ramp up to the base slab on the front (and the back, if enabled), with collision, so anyone can walk in (even overencumbered).</summary>
        private static void AddRamps(TeleportWorld portal, Transform gateRoot)
        {
            // Take the layer of the portal's own base collider, so the ramps behave like part of the portal
            // (including for the hammer's hover and removal).
            int layer = 0;
            foreach (var collider in portal.GetComponentsInChildren<Collider>(true))
                if (!collider.isTrigger && !collider.transform.IsChildOf(gateRoot)) { layer = collider.gameObject.layer; break; }

            // The front is the side you step out of; the back ramp is optional.
            var sides = PortalHeimSG1Plugin.BackRamp.Value ? new[] { 1f, -1f } : new[] { 1f };
            foreach (float side in sides)
            {
                // Parented under the gate (so the frame-hiding leaves it alone) but placed in portal space.
                var holder = new GameObject(side > 0 ? "RampFront" : "RampBack");
                holder.transform.SetParent(gateRoot, false);
                holder.transform.localPosition = -gateRoot.localPosition;
                holder.transform.localRotation = Quaternion.Euler(0f, side > 0 ? 0f : 180f, 0f);
                holder.layer = layer;

                holder.AddComponent<MeshFilter>().sharedMesh = _ramp;
                var renderer = holder.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _outerMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var collision = holder.AddComponent<MeshCollider>();
                collision.sharedMesh = _ramp;
                collision.convex = true;
            }
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
        /// Hides everything that draws the wooden portal: the frame model and any other mesh that isn't an
        /// effect, the debris (shown only when the portal is destroyed) or our own gate. It uses
        /// forceRenderingOff, which stays off whatever the portal's LODGroup does, and also disables the
        /// LODGroup so it can't switch anything back on.
        /// </summary>
        private static int HideWoodenFrame(TeleportWorld portal, Transform ourGate)
        {
            int hidden = 0;
            foreach (var lod in portal.GetComponentsInChildren<LODGroup>(true))
                lod.enabled = false;

            foreach (var renderer in portal.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer) continue;
                if (renderer.transform.IsChildOf(ourGate)) continue; // our own ring, chevrunes and water
                if (IsEffectOrDebris(renderer.transform, portal.transform) && renderer != portal.m_model) continue;

                renderer.forceRenderingOff = true;
                hidden++;
            }

            // Belt and braces: the portal's own frame model, whatever else was or wasn't found above.
            if (portal.m_model != null && !portal.m_model.forceRenderingOff)
            {
                portal.m_model.forceRenderingOff = true;
                hidden++;
            }
            return hidden;
        }

        private static bool IsEffectOrDebris(Transform t, Transform portalRoot)
        {
            for (; t != null && t != portalRoot; t = t.parent)
            {
                string name = t.name.ToLowerInvariant();
                if (name.Contains("destruction") || name.Contains("target_found") || name.Contains("particle")) return true;
            }
            return false;
        }

        /// <summary>
        /// Builds the shared meshes, textures and materials once. If that fails it logs exactly which step
        /// failed and tries again for the next portal (up to a few times), rather than giving up for the session.
        /// </summary>
        private static void EnsureAssets(TeleportWorld portal)
        {
            if (_built || _failures >= MaxBuildAttempts) return;

            string step = "start";
            try
            {
                step = "meshes";
                _outerRing = GateMeshes.Annulus("SG1OuterRing", GateMeshes.OuterRingIn, GateMeshes.OuterRadius, GateMeshes.OuterHalfDepth);
                _innerRing = GateMeshes.Annulus("SG1InnerRing", GateMeshes.InnerRingIn, GateMeshes.InnerRingOut, GateMeshes.InnerHalfDepth);
                _chevrune = GateMeshes.Chevrune();
                _runes = System.Array.ConvertAll(RuneAt, GateMeshes.Rune);
                // The ramp starts well below the ground and runs far enough out to meet it however the ground falls away,
                // so there's no lip to step onto (the part under the ground is never seen).
                float depth = PortalHeimSG1Plugin.RampDepth.Value;
                float run = (SlabTop + depth) / RampSlope;
                _ramp = GateMeshes.Ramp(RampHalfWidth, SlabHalfDepth, SlabHalfDepth + run, SlabTop, -depth);
                // Slightly wider than the opening so its edge tucks under the inner ring.
                _waterDisc = GateMeshes.Disc(GateMeshes.InnerRingIn + 0.03f, 64);

                step = "stone textures";
                var outerCanvas = Textures.OuterRing(GateMeshes.OuterRadius, GateMeshes.OuterRingIn, GateMeshes.OuterRadius);
                var innerCanvas = Textures.InnerRing(GateMeshes.OuterRadius, GateMeshes.InnerRingIn, GateMeshes.InnerRingOut, 36);

                step = "stone materials";
                // By default clone the portal's own material: its shader is known to render in this game.
                var template = PortalHeimSG1Plugin.Shading.Value == ShadingMode.PortalShader && portal.m_model != null
                    ? portal.m_model.sharedMaterial : null;
                _outerMaterial = StoneMaterial(template, outerCanvas, "SG1OuterStone");
                _innerMaterial = StoneMaterial(template, innerCanvas, "SG1InnerStone");

                step = "chevrune materials";
                var flat = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default") ?? _outerMaterial.shader;
                ChevruneOff = Flat(flat, "SG1ChevruneOff", GateRing.Dark);
                ChevruneOn = Flat(flat, "SG1ChevruneOn", PortalHeimSG1Plugin.ChevruneColor.Value);
                ChevruneDim = Flat(flat, "SG1ChevruneDim", DimColor());
                PortalHeimSG1Plugin.ChevruneColor.SettingChanged += (_, __) =>
                {
                    Tint(ChevruneOn, PortalHeimSG1Plugin.ChevruneColor.Value);
                    Tint(ChevruneDim, DimColor());
                };
                PortalHeimSG1Plugin.DormantGlow.SettingChanged += (_, __) => Tint(ChevruneDim, DimColor());

                // Valheim unloads unused assets when you leave a world; keep ours so the next world still has them.
                foreach (var obj in new UnityEngine.Object[] { _outerRing, _innerRing, _chevrune, _waterDisc, _ramp, _outerMaterial, _innerMaterial, ChevruneOff, ChevruneOn, ChevruneDim })
                    if (obj != null) obj.hideFlags |= HideFlags.DontUnloadUnusedAsset;

                _built = true;
                PortalHeimSG1Plugin.Log.LogInfo(
                    $"Gate art built. Stone shader '{_outerMaterial.shader.name}', chevrune shader '{flat.name}', " +
                    $"shading mode {PortalHeimSG1Plugin.Shading.Value}.");
            }
            catch (Exception e)
            {
                _failures++;
                _built = false;
                Diagnostics.RecordFailure(step, e);
                PortalHeimSG1Plugin.Log.LogError(
                    $"Couldn't build the gate art (failed at: {step}, attempt {_failures} of {MaxBuildAttempts}); " +
                    $"portals stay as they are for now. {e}");
            }
        }

        private static readonly string[] ExtraTextures =
        {
            "_BumpMap", "_MetallicGlossMap", "_EmissiveTex", "_EmissionMap", "_OcclusionMap", "_ParallaxMap", "_DetailAlbedoMap", "_DetailNormalMap",
        };

        /// <summary>How a connected gate's Chevrunes look while nobody is near: the lit colour, faded toward dark.</summary>
        private static Color DimColor() =>
            Color.Lerp(GateRing.Dark, PortalHeimSG1Plugin.ChevruneColor.Value, Mathf.Clamp01(PortalHeimSG1Plugin.DormantGlow.Value));

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

        private static Material StoneMaterial(Material template, Pixels canvas, string name)
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
