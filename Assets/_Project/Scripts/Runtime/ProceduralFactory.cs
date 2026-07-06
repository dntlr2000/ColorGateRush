using System.Collections.Generic;
using UnityEngine;

namespace ColorGateRush
{
    public static class ProceduralFactory
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static readonly Queue<TextMesh> FloatingTextPool = new Queue<TextMesh>();

        // Returns a cached solid material for a gameplay color.
        public static Material ColorMaterial(ColorId colorId)
        {
            return SolidMaterial("color_" + colorId + ThemeSuffix(), GameConstants.ToUnityColor(colorId), VisualTheme.Current().ShardGlowAlpha);
        }

        // Returns a translucent material for the runner's ground accent.
        public static Material PlayerAccentMaterial(ColorId colorId)
        {
            return TransparentMaterial("player_accent_" + colorId + ThemeSuffix(), GameConstants.ToUnityColor(colorId), 0.42f);
        }

        // Returns the cached dark material used by generated track slabs.
        public static Material TrackMaterial()
        {
            return SolidMaterial("track" + ThemeSuffix(), VisualTheme.Current().TrackBaseColor);
        }

        // Returns the cached material used by lane guide strips.
        public static Material LaneStripMaterial()
        {
            return SolidMaterial("lane_strip" + ThemeSuffix(), VisualTheme.Current().TrackAccentColor, 0.12f);
        }

        // Returns the cached material used by raised track side rails.
        public static Material TrackEdgeMaterial()
        {
            return SolidMaterial("track_edge" + ThemeSuffix(), VisualTheme.Current().TrackEdgeColor);
        }

        // Returns the cached material used by rhythmic track accent stripes.
        public static Material TrackAccentMaterial()
        {
            return TransparentMaterial("track_accent" + ThemeSuffix(), VisualTheme.Current().TrackAccentColor, 0.56f);
        }

        // Returns the cached material used by procedural backdrop panels.
        public static Material BackdropTopMaterial()
        {
            return TransparentMaterial("backdrop_top" + ThemeSuffix(), VisualTheme.Current().BackdropTopColor, VisualTheme.Current().BackdropTopColor.a);
        }

        // Returns the cached material used by low backdrop panels.
        public static Material BackdropBottomMaterial()
        {
            return TransparentMaterial("backdrop_bottom" + ThemeSuffix(), VisualTheme.Current().BackdropBottomColor, VisualTheme.Current().BackdropBottomColor.a);
        }

        // Returns the cached material used by side panels outside the lane track.
        public static Material SidePanelMaterial()
        {
            return TransparentMaterial("side_panel" + ThemeSuffix(), VisualTheme.Current().SidePanelColor, VisualTheme.Current().SidePanelColor.a);
        }

        // Returns the cached material used by obstacle blocks.
        public static Material ObstacleMaterial()
        {
            return SolidMaterial("obstacle" + ThemeSuffix(), VisualTheme.Current().ObstacleColor, 0.08f);
        }

        // Returns the cached material used by warning stripes on obstacle geometry.
        public static Material ObstacleWarningMaterial()
        {
            return SolidMaterial("obstacle_warning" + ThemeSuffix(), VisualTheme.Current().ObstacleWarningColor, 0.18f);
        }

        // Returns the cached translucent material used by positive gate panels.
        public static Material GatePanelMaterial(ColorId colorId)
        {
            Color color = Color.Lerp(GameConstants.ToUnityColor(colorId), VisualTheme.Current().GatePanelColor, 0.25f);
            return TransparentMaterial("gate_panel_" + colorId + ThemeSuffix(), color, VisualTheme.Current().GatePanelColor.a);
        }

        // Returns the cached material used by finish-line geometry.
        public static Material FinishMaterial()
        {
            return SolidMaterial("finish" + ThemeSuffix(), VisualTheme.Current().FinishColor, 0.16f);
        }

        // Returns the cached material used by dark finish checker tiles.
        public static Material FinishDarkMaterial()
        {
            return SolidMaterial("finish_dark" + ThemeSuffix(), VisualTheme.Current().TrackEdgeColor);
        }

        // Creates or returns a cached opaque material with shader fallback support.
        public static Material SolidMaterial(string key, Color color, float emissionStrength = 0f)
        {
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Material material = new Material(FindDefaultShader());
            material.name = "M_" + key;
            SetMaterialColor(material, color);
            ConfigureLitSurface(material, color, emissionStrength);
            MaterialCache[key] = material;
            return material;
        }

        // Creates or returns a cached transparent material compatible with URP and built-in shaders.
        public static Material TransparentMaterial(string key, Color color, float alpha)
        {
            string cacheKey = key + "_alpha_" + alpha.ToString("0.00");
            if (MaterialCache.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            Color transparent = color;
            transparent.a = alpha;
            Material material = new Material(FindDefaultShader());
            material.name = "M_" + cacheKey;
            SetMaterialColor(material, transparent);
            ConfigureTransparency(material);
            MaterialCache[cacheKey] = material;
            return material;
        }

        // Creates a Unity primitive with material and trigger configuration applied.
        public static GameObject Primitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool isTrigger)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = position;
            go.transform.localScale = scale;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = isTrigger;
            }

            return go;
        }

        // Creates a primitive intended only for visuals and disables its collider immediately.
        public static GameObject VisualPrimitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject go = Primitive(type, name, parent, position, scale, material, isTrigger: false);
            DisableCollider(go);
            return go;
        }

        // Creates a collectible shard whose silhouette is driven by the shared color visual profile.
        public static GameObject CreateShardVisual(Transform parent, string name, Vector3 position, ColorId colorId)
        {
            GameObject shard = CreateColorShape(parent, name, position, colorId, 0.58f, colliderEnabled: true, isTrigger: true);
            AttachShardGlow(shard, colorId);
            return shard;
        }

        // Creates a non-colliding shape marker for gates and other color cues.
        public static GameObject CreateColorShapeMarker(Transform parent, string name, Vector3 position, ColorId colorId, float size)
        {
            return CreateColorShape(parent, name, position, colorId, size, colliderEnabled: false, isTrigger: false);
        }

        // Creates a translucent ground accent that follows the player without adding collision.
        public static GameObject CreatePlayerAccent(Transform parent, ColorId colorId, Vector3 position)
        {
            GameObject accent = CreateColorShape(
                parent,
                "PlayerColorAccent",
                position,
                colorId,
                0.44f,
                colliderEnabled: false,
                isTrigger: false);
            ApplyPlayerAccentMaterial(accent, colorId);

            Collider collider = accent.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return accent;
        }

        // Disables a primitive collider so decorative objects cannot affect gameplay.
        public static void DisableCollider(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        // Applies the active color material to an already-created visual object.
        public static void ApplyColorMaterial(GameObject target, ColorId colorId)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = ColorMaterial(colorId);
            }
        }

        // Applies the active translucent player accent material to an already-created object.
        public static void ApplyPlayerAccentMaterial(GameObject target, ColorId colorId)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = PlayerAccentMaterial(colorId);
            }
        }

        // Builds the primitive, scale, and rotation for one color-specific shape.
        private static GameObject CreateColorShape(
            Transform parent,
            string name,
            Vector3 position,
            ColorId colorId,
            float size,
            bool colliderEnabled,
            bool isTrigger)
        {
            ColorVisualProfile profile = GameConstants.GetVisualProfile(colorId);
            PrimitiveType primitiveType = ShapePrimitive(profile.ShapeType);
            GameObject shape = Primitive(
                primitiveType,
                name,
                parent,
                position,
                ShapeScale(profile.ShapeType, size),
                ColorMaterial(colorId),
                isTrigger);
            shape.transform.rotation = ShapeRotation(profile.ShapeType);

            Collider collider = shape.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = colliderEnabled;
                collider.isTrigger = isTrigger;
            }

            return shape;
        }

        // Adds a soft non-colliding glow shell to make collectibles feel more rewarding.
        private static void AttachShardGlow(GameObject shard, ColorId colorId)
        {
            if (shard == null)
            {
                return;
            }

            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "ShardGlow";
            glow.transform.SetParent(shard.transform, false);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localRotation = Quaternion.identity;
            glow.transform.localScale = Vector3.one * 1.32f;

            Renderer renderer = glow.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = TransparentMaterial("shard_glow_" + colorId + ThemeSuffix(), GameConstants.ToUnityColor(colorId), VisualTheme.Current().ShardGlowAlpha);
            }

            DisableCollider(glow);
        }

        // Converts a color shape profile into a Unity primitive type.
        private static PrimitiveType ShapePrimitive(ColorShapeType shapeType)
        {
            switch (shapeType)
            {
                case ColorShapeType.Cube:
                case ColorShapeType.Diamond:
                    return PrimitiveType.Cube;
                case ColorShapeType.Capsule:
                    return PrimitiveType.Capsule;
                default:
                    return PrimitiveType.Sphere;
            }
        }

        // Returns a readable scale for each primitive silhouette.
        private static Vector3 ShapeScale(ColorShapeType shapeType, float size)
        {
            switch (shapeType)
            {
                case ColorShapeType.Cube:
                    return Vector3.one * size;
                case ColorShapeType.Capsule:
                    return new Vector3(size * 0.68f, size * 0.62f, size * 0.68f);
                case ColorShapeType.Diamond:
                    return Vector3.one * (size * 0.9f);
                default:
                    return Vector3.one * size;
            }
        }

        // Returns a stable display rotation so non-spherical shards have a clear silhouette.
        private static Quaternion ShapeRotation(ColorShapeType shapeType)
        {
            switch (shapeType)
            {
                case ColorShapeType.Diamond:
                    return Quaternion.Euler(0f, 45f, 45f);
                case ColorShapeType.Capsule:
                    return Quaternion.Euler(0f, 0f, 90f);
                default:
                    return Quaternion.identity;
            }
        }

        // Shows pooled floating score or feedback text near a gameplay event.
        public static void FloatingText(Vector3 position, string text, Color color)
        {
            TextMesh mesh = GetFloatingText();
            FloatingFeedback feedback = mesh.GetComponent<FloatingFeedback>();
            feedback.Play(mesh, text, color, position, 0.75f, ReleaseFloatingText);
        }

        // Gets a text mesh from a tiny local pool to reduce feedback allocations.
        private static TextMesh GetFloatingText()
        {
            while (FloatingTextPool.Count > 0)
            {
                TextMesh pooled = FloatingTextPool.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            GameObject go = new GameObject("FloatingFeedback");
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.28f;
            mesh.fontSize = 72;
            go.AddComponent<FloatingFeedback>();
            return mesh;
        }

        // Returns a floating text mesh to the pool after its animation completes.
        private static void ReleaseFloatingText(TextMesh textMesh)
        {
            if (textMesh == null)
            {
                return;
            }

            textMesh.gameObject.SetActive(false);
            FloatingTextPool.Enqueue(textMesh);
        }

        // Emits the short burst used for successful shard collection.
        public static void CollectBurst(Vector3 position, Color color)
        {
            Burst(position, color, ScaledParticleCount(18), 0.16f, 0.35f);
            RingBurst(position + Vector3.up * 0.08f, color, ScaledParticleCount(10), 0.08f, 0.28f, 0.38f);
            Burst(position + Vector3.up * 0.32f, Color.white, ScaledParticleCount(6), 0.055f, 0.22f);
        }

        // Emits a vertical ring-like burst used when the runner crosses a color gate.
        public static void GateBurst(Vector3 position, Color color)
        {
            RingBurst(position, color, ScaledParticleCount(30), 0.18f, 0.45f, 0.82f);
            Burst(position + Vector3.up * 0.2f, color, ScaledParticleCount(12), 0.10f, 0.32f);
            RingBurst(position + Vector3.up * 0.38f, Color.white, ScaledParticleCount(8), 0.055f, 0.24f, 0.46f);
        }

        // Emits a larger white and gold burst used when the runner finishes a run.
        public static void FinishBurst(Vector3 position)
        {
            Burst(position, VisualTheme.Current().FinishColor, ScaledParticleCount(42), 0.28f, 0.65f);
            RingBurst(position + Vector3.up * 0.15f, GameConstants.ToUnityColor(ColorId.Yellow), ScaledParticleCount(46), 0.22f, 0.7f, 1.1f);
            Burst(position + Vector3.up * 0.55f, Color.white, ScaledParticleCount(22), 0.13f, 0.48f);
        }

        // Emits the red burst used for wrong shards and obstacle failures.
        public static void FailBurst(Vector3 position)
        {
            Burst(position, VisualTheme.Current().ObstacleColor, ScaledParticleCount(34), 0.24f, 0.45f);
            RingBurst(position, VisualTheme.Current().ObstacleWarningColor, ScaledParticleCount(16), 0.12f, 0.32f, 0.55f);
            RingBurst(position + Vector3.up * 0.12f, Color.white, ScaledParticleCount(6), 0.05f, 0.20f, 0.36f);
        }

        // Emits a spherical one-shot particle burst at the requested world position.
        public static void Burst(Vector3 position, Color color, int count, float size, float lifetime)
        {
            GameObject go = new GameObject("ParticleBurst");
            go.transform.position = position;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;
            main.startSize = size;
            main.startLifetime = lifetime;
            main.startSpeed = 3.5f;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ps.Emit(count);
            Object.Destroy(go, lifetime + 0.3f);
        }

        // Scales one-shot particle counts with the active theme while keeping mobile-safe caps.
        private static int ScaledParticleCount(int baseCount)
        {
            return Mathf.Clamp(Mathf.RoundToInt(baseCount * VisualTheme.Current().VfxIntensity), 4, 64);
        }

        // Emits a circle-shaped burst for gate and finish feedback.
        private static void RingBurst(Vector3 position, Color color, int count, float size, float lifetime, float radius)
        {
            GameObject go = new GameObject("ParticleRingBurst");
            go.transform.position = position;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.startColor = color;
            main.startSize = size;
            main.startLifetime = lifetime;
            main.startSpeed = 2.5f;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;

            ps.Emit(count);
            Object.Destroy(go, lifetime + 0.3f);
        }

        // Chooses a render shader that works in URP first and falls back for built-in projects.
        private static Shader FindDefaultShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Diffuse");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Hidden/InternalErrorShader");
        }

        // Applies a color to whichever color property the active shader exposes.
        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        // Applies safe smoothness/emission properties when the active shader supports them.
        private static void ConfigureLitSurface(Material material, Color baseColor, float emissionStrength)
        {
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.68f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (emissionStrength <= 0f)
            {
                return;
            }

            Color emissionColor = baseColor * emissionStrength;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissionColor);
                material.EnableKeyword("_EMISSION");
            }
        }

        // Returns a stable cache suffix for the currently active visual theme mode.
        private static string ThemeSuffix()
        {
            return "_theme" + VisualTheme.ActiveThemeIndex + (GameSettings.ColorAssistEnabled ? "_assist" : "_default");
        }

        // Enables alpha blending flags for URP and built-in Standard compatible materials.
        private static void ConfigureTransparency(Material material)
        {
            material.renderQueue = 3000;

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }
}
