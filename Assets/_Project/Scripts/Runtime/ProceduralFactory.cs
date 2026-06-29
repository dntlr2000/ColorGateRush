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
            string assistSuffix = GameSettings.ColorAssistEnabled ? "_assist" : "_default";
            return SolidMaterial("color_" + colorId + assistSuffix, GameConstants.ToUnityColor(colorId));
        }

        // Returns the cached dark material used by generated track slabs.
        public static Material TrackMaterial()
        {
            return SolidMaterial("track", new Color(0.05f, 0.07f, 0.12f));
        }

        // Returns the cached material used by lane guide strips.
        public static Material LaneStripMaterial()
        {
            return SolidMaterial("lane_strip", new Color(0.13f, 0.18f, 0.28f));
        }

        // Returns the cached material used by obstacle blocks.
        public static Material ObstacleMaterial()
        {
            return SolidMaterial("obstacle", new Color(1.0f, 0.26f, 0.12f));
        }

        // Returns the cached material used by finish-line geometry.
        public static Material FinishMaterial()
        {
            return SolidMaterial("finish", new Color(1.0f, 0.95f, 0.62f));
        }

        // Creates or returns a cached opaque material with shader fallback support.
        public static Material SolidMaterial(string key, Color color)
        {
            if (MaterialCache.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            Material material = new Material(FindDefaultShader());
            material.name = "M_" + key;
            SetMaterialColor(material, color);
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

        // Attaches a color-assist symbol above a world object using built-in TextMesh.
        public static TextMesh AttachColorSymbol(Transform parent, ColorId colorId, Vector3 localPosition, float size)
        {
            GameObject go = new GameObject("ColorSymbol_" + colorId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            TextMesh text = go.AddComponent<TextMesh>();
            text.text = GameConstants.ColorSymbol(colorId);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = GameSettings.ColorAssistEnabled ? size * 1.25f : size;
            text.fontSize = GameSettings.ColorAssistEnabled ? 72 : 56;
            text.color = GameSettings.ColorAssistEnabled ? Color.white : Color.black;
            return text;
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
            Burst(position, color, 18, 0.16f, 0.35f);
        }

        // Emits a vertical ring-like burst used when the runner crosses a color gate.
        public static void GateBurst(Vector3 position, Color color)
        {
            RingBurst(position, color, 28, 0.18f, 0.45f, 0.75f);
        }

        // Emits a larger white and gold burst used when the runner finishes a run.
        public static void FinishBurst(Vector3 position)
        {
            Burst(position, Color.white, 36, 0.28f, 0.65f);
            RingBurst(position + Vector3.up * 0.15f, GameConstants.ToUnityColor(ColorId.Yellow), 42, 0.22f, 0.7f, 1.1f);
        }

        // Emits the red burst used for wrong shards and obstacle failures.
        public static void FailBurst(Vector3 position)
        {
            Burst(position, Color.red, 32, 0.24f, 0.45f);
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
