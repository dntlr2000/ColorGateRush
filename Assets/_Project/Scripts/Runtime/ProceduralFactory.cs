using System.Collections.Generic;
using UnityEngine;

namespace ColorGateRush
{
    public static class ProceduralFactory
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        private static readonly Dictionary<PrimitiveType, Mesh> MeshCache = new Dictionary<PrimitiveType, Mesh>();
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

        // Returns the cached material used by runtime ParticleSystem feedback.
        public static Material ParticleMaterial()
        {
            const string key = "particle_runtime";
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Material material = RuntimeMaterialProvider.CreateParticle("M_" + key);
            MaterialCache[key] = material;
            return material;
        }

        // Creates or returns a cached opaque material with shader fallback support.
        public static Material SolidMaterial(string key, Color color, float emissionStrength = 0f)
        {
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Material material = RuntimeMaterialProvider.CreateOpaque("M_" + key, color, emissionStrength);
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
            Material material = RuntimeMaterialProvider.CreateTransparent("M_" + cacheKey, transparent, alpha);
            MaterialCache[cacheKey] = material;
            return material;
        }

        // Creates a procedural primitive with generic component creation and trigger configuration applied.
        public static GameObject Primitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool isTrigger)
        {
            GameObject go = new GameObject(name);
            go.name = name;
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.position = position;
            go.transform.localScale = SanitizeScale(scale);

            MeshFilter meshFilter = EnsureMeshFilter(go);
            meshFilter.sharedMesh = GetPrimitiveMesh(type);

            MeshRenderer renderer = EnsureMeshRenderer(go);
            renderer.enabled = true;
            ApplyMaterial(renderer, material, name);

            Collider collider = EnsureColliderForPrimitive(go, type);
            if (collider != null)
            {
                collider.isTrigger = isTrigger;
            }

            ValidateGeneratedVisual(go, name, requireCollider: true);
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

        // Ensures a MeshFilter exists without using string-based component creation.
        public static MeshFilter EnsureMeshFilter(GameObject target)
        {
            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter : target.AddComponent<MeshFilter>();
        }

        // Ensures a MeshRenderer exists without using string-based component creation.
        public static MeshRenderer EnsureMeshRenderer(GameObject target)
        {
            MeshRenderer meshRenderer = target.GetComponent<MeshRenderer>();
            return meshRenderer != null ? meshRenderer : target.AddComponent<MeshRenderer>();
        }

        // Ensures a BoxCollider exists without using string-based component creation.
        public static BoxCollider EnsureBoxCollider(GameObject target)
        {
            BoxCollider collider = target.GetComponent<BoxCollider>();
            return collider != null ? collider : target.AddComponent<BoxCollider>();
        }

        // Ensures a SphereCollider exists without using string-based component creation.
        public static SphereCollider EnsureSphereCollider(GameObject target)
        {
            SphereCollider collider = target.GetComponent<SphereCollider>();
            return collider != null ? collider : target.AddComponent<SphereCollider>();
        }

        // Ensures a CapsuleCollider exists without using string-based component creation.
        public static CapsuleCollider EnsureCapsuleCollider(GameObject target)
        {
            CapsuleCollider collider = target.GetComponent<CapsuleCollider>();
            return collider != null ? collider : target.AddComponent<CapsuleCollider>();
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

        // Assigns a material while falling back to a visible magenta-free runtime material if needed.
        public static void ApplyMaterial(Renderer renderer, Material material, string context)
        {
            if (renderer == null)
            {
                return;
            }

            Material safeMaterial = RuntimeMaterialProvider.IsMaterialUsable(material)
                ? material
                : SolidMaterial("fallback_runtime_visible" + ThemeSuffix(), Color.white, 0f);
            renderer.sharedMaterial = safeMaterial;
            renderer.enabled = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!RuntimeMaterialProvider.IsMaterialUsable(material))
            {
                Debug.LogWarning("Applied fallback material for procedural object: " + context);
            }
#endif
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

        // Validates generated render/collider state in editor and development builds only.
        public static void ValidateGeneratedVisual(GameObject target, string context, bool requireCollider)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (target == null)
            {
                Debug.LogWarning("Generated visual is null: " + context);
                return;
            }

            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            Collider collider = target.GetComponent<Collider>();
            Vector3 scale = target.transform.lossyScale;
            if (!target.activeSelf || renderer == null || !renderer.enabled)
            {
                Debug.LogWarning("Generated object has no enabled renderer: " + context);
            }

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning("Generated object has no mesh: " + context);
            }

            if (!RuntimeMaterialProvider.IsMaterialUsable(renderer != null ? renderer.sharedMaterial : null))
            {
                Debug.LogWarning("Generated object has invalid material/shader: " + context);
            }

            if (requireCollider && collider == null)
            {
                Debug.LogWarning("Generated gameplay object has no collider: " + context);
            }

            if (Mathf.Abs(scale.x) < 0.0001f || Mathf.Abs(scale.y) < 0.0001f || Mathf.Abs(scale.z) < 0.0001f)
            {
                Debug.LogWarning("Generated object has near-zero scale: " + context);
            }
#endif
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

            GameObject glow = VisualPrimitive(
                PrimitiveType.Sphere,
                "ShardGlow",
                shard.transform,
                Vector3.zero,
                Vector3.one * 1.32f,
                TransparentMaterial("shard_glow_" + colorId + ThemeSuffix(), GameConstants.ToUnityColor(colorId), VisualTheme.Current().ShardGlowAlpha));
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localRotation = Quaternion.identity;
            glow.transform.localScale = Vector3.one * 1.32f;
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

        // Returns a cached built-in mesh, or a procedural fallback mesh when built-in resources are unavailable.
        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (MeshCache.TryGetValue(type, out Mesh cached) && cached != null)
            {
                return cached;
            }

            Mesh mesh = Resources.GetBuiltinResource<Mesh>(BuiltinMeshName(type));
            if (mesh == null)
            {
                mesh = CreateFallbackMesh(type);
            }

            MeshCache[type] = mesh;
            return mesh;
        }

        // Returns Unity's built-in mesh resource name for a primitive type.
        private static string BuiltinMeshName(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    return "Sphere.fbx";
                case PrimitiveType.Capsule:
                    return "Capsule.fbx";
                case PrimitiveType.Cylinder:
                    return "Cylinder.fbx";
                case PrimitiveType.Plane:
                    return "Plane.fbx";
                case PrimitiveType.Quad:
                    return "Quad.fbx";
                default:
                    return "Cube.fbx";
            }
        }

        // Creates a minimal procedural fallback mesh for player builds that cannot load a built-in primitive mesh.
        private static Mesh CreateFallbackMesh(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    return BuildSphereMesh();
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    return BuildCylinderMesh();
                case PrimitiveType.Plane:
                case PrimitiveType.Quad:
                    return BuildQuadMesh();
                default:
                    return BuildCubeMesh();
            }
        }

        // Adds the gameplay collider that best matches the primitive silhouette.
        private static Collider EnsureColliderForPrimitive(GameObject target, PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    return EnsureSphereCollider(target);
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    return EnsureCapsuleCollider(target);
                default:
                    return EnsureBoxCollider(target);
            }
        }

        // Clamps procedural scale away from zero so generated renderers cannot vanish.
        private static Vector3 SanitizeScale(Vector3 scale)
        {
            return new Vector3(
                Mathf.Abs(scale.x) < 0.0001f ? 0.0001f : scale.x,
                Mathf.Abs(scale.y) < 0.0001f ? 0.0001f : scale.y,
                Mathf.Abs(scale.z) < 0.0001f ? 0.0001f : scale.z);
        }

        // Builds a unit cube mesh for runtime fallback rendering.
        private static Mesh BuildCubeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "CGR_FallbackCube";
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f)
            };
            int[] triangles =
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Builds a low-poly sphere mesh for runtime fallback rendering.
        private static Mesh BuildSphereMesh()
        {
            const int latSegments = 8;
            const int lonSegments = 12;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float theta = Mathf.PI * lat / latSegments;
                float y = Mathf.Cos(theta) * 0.5f;
                float radius = Mathf.Sin(theta) * 0.5f;
                for (int lon = 0; lon <= lonSegments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegments;
                    vertices.Add(new Vector3(Mathf.Cos(phi) * radius, y, Mathf.Sin(phi) * radius));
                }
            }

            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * (lonSegments + 1) + lon;
                    int next = current + lonSegments + 1;
                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);
                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "CGR_FallbackSphere";
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Builds a low-poly cylinder mesh for runtime fallback rendering.
        private static Mesh BuildCylinderMesh()
        {
            const int segments = 16;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices.Add(new Vector3(x, -0.5f, z));
                vertices.Add(new Vector3(x, 0.5f, z));
            }

            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -0.5f, 0f));
            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0.5f, 0f));
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int bottom = i * 2;
                int top = bottom + 1;
                int nextBottom = next * 2;
                int nextTop = nextBottom + 1;
                triangles.Add(bottom);
                triangles.Add(top);
                triangles.Add(nextTop);
                triangles.Add(bottom);
                triangles.Add(nextTop);
                triangles.Add(nextBottom);
                triangles.Add(bottomCenter);
                triangles.Add(nextBottom);
                triangles.Add(bottom);
                triangles.Add(topCenter);
                triangles.Add(top);
                triangles.Add(nextTop);
            }

            Mesh mesh = new Mesh();
            mesh.name = "CGR_FallbackCylinder";
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Builds a unit quad mesh for runtime fallback rendering.
        private static Mesh BuildQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "CGR_FallbackQuad";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
            ApplyParticleMaterial(ps);
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
            ApplyParticleMaterial(ps);
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

        // Assigns the explicit runtime particle material to prevent player-build shader fallback surprises.
        private static void ApplyParticleMaterial(ParticleSystem particleSystem)
        {
            if (particleSystem == null)
            {
                return;
            }

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = ParticleMaterial();
            }
        }

        // Returns a stable cache suffix for the currently active visual theme mode.
        private static string ThemeSuffix()
        {
            return "_theme" + VisualTheme.ActiveThemeIndex + (GameSettings.ColorAssistEnabled ? "_assist" : "_default");
        }
    }
}
