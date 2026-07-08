using System;
using UnityEngine;

namespace ColorGateRush
{
    public static class RuntimeMaterialProvider
    {
        private const string MaterialRoot = "ColorGateRush/Materials/";
        private const string OpaqueBaseMaterialPath = MaterialRoot + "CGR_UnlitOpaque";
        private const string TransparentBaseMaterialPath = MaterialRoot + "CGR_UnlitTransparent";
        private const string ParticleBaseMaterialPath = MaterialRoot + "CGR_ParticleUnlit";

        private static Material _opaqueBaseMaterial;
        private static Material _transparentBaseMaterial;
        private static Material _particleBaseMaterial;

        // Creates an opaque runtime material from the project shader provider.
        public static Material CreateOpaque(string name, Color color, float emissionStrength)
        {
            Material material = CreateMaterialFromBase(name, GetOpaqueBaseMaterial(), "Universal Render Pipeline/Unlit", "Sprites/Default", "UI/Default");
            SetMaterialColor(material, color);
            ConfigureLitSurface(material, color, emissionStrength);
            ValidateMaterial(material, name);
            return material;
        }

        // Creates a transparent runtime material from the project shader provider.
        public static Material CreateTransparent(string name, Color color, float alpha)
        {
            Color transparent = color;
            transparent.a = Mathf.Clamp01(alpha);
            Material material = CreateMaterialFromBase(name, GetTransparentBaseMaterial(), "Universal Render Pipeline/Unlit", "Sprites/Default", "UI/Default");
            SetMaterialColor(material, transparent);
            ConfigureTransparency(material);
            ValidateMaterial(material, name);
            return material;
        }

        // Creates a particle material that does not depend on Unity's implicit default particle material.
        public static Material CreateParticle(string name)
        {
            Material material = CreateMaterialFromBase(name, GetParticleBaseMaterial(), "Universal Render Pipeline/Particles/Unlit", "Universal Render Pipeline/Unlit", "Sprites/Default");
            SetMaterialColor(material, Color.white);
            ConfigureTransparency(material);
            ValidateMaterial(material, name);
            return material;
        }

        // Returns true when a material has a supported shader and can render in player builds.
        public static bool IsMaterialUsable(Material material)
        {
            return material != null && material.shader != null && material.shader.isSupported;
        }

        // Loads the build-included opaque base material from Resources.
        private static Material GetOpaqueBaseMaterial()
        {
            if (!IsMaterialUsable(_opaqueBaseMaterial))
            {
                _opaqueBaseMaterial = LoadBaseMaterial(OpaqueBaseMaterialPath);
            }

            return _opaqueBaseMaterial;
        }

        // Loads the build-included transparent base material from Resources.
        private static Material GetTransparentBaseMaterial()
        {
            if (!IsMaterialUsable(_transparentBaseMaterial))
            {
                _transparentBaseMaterial = LoadBaseMaterial(TransparentBaseMaterialPath);
            }

            return _transparentBaseMaterial;
        }

        // Loads the build-included particle base material from Resources.
        private static Material GetParticleBaseMaterial()
        {
            if (!IsMaterialUsable(_particleBaseMaterial))
            {
                _particleBaseMaterial = LoadBaseMaterial(ParticleBaseMaterialPath);
            }

            return _particleBaseMaterial;
        }

        // Loads a material asset that keeps the exact runtime shader variant referenced by the player build.
        private static Material LoadBaseMaterial(string resourcePath)
        {
            Material material = Resources.Load<Material>(resourcePath);
            if (IsMaterialUsable(material))
            {
                return material;
            }

            WarnDevelopmentOnly("Missing or unsupported runtime base material at Resources/" + resourcePath + ".");
            return Resources.GetBuiltinResource<Material>("Default-Material.mat");
        }

        // Creates a clone from a Resources material asset, with a limited fallback for editor diagnostics.
        private static Material CreateMaterialFromBase(string name, Material baseMaterial, params string[] fallbackShaderNames)
        {
            if (IsMaterialUsable(baseMaterial))
            {
                Material material = new Material(baseMaterial);
                material.name = name;
                return material;
            }

            Shader fallback = FindSupportedShader(fallbackShaderNames);
            if (fallback != null && fallback.isSupported)
            {
                Material material = new Material(fallback);
                material.name = name;
                return material;
            }

            Material builtin = Resources.GetBuiltinResource<Material>("Default-Material.mat");
            if (IsMaterialUsable(builtin))
            {
                Material material = new Material(builtin);
                material.name = name;
                return material;
            }

            throw new InvalidOperationException("No usable runtime material could be created for " + name + ".");
        }

        // Finds a limited fallback shader only when Resources material loading has failed.
        private static Shader FindSupportedShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null && shader.isSupported)
                {
                    return shader;
                }
            }

            Material builtin = Resources.GetBuiltinResource<Material>("Default-Material.mat");
            if (IsMaterialUsable(builtin))
            {
                return builtin.shader;
            }

            throw new InvalidOperationException("No supported fallback runtime shader was found for Color Gate Rush procedural materials.");
        }

        // Applies base color through URP and fallback material color properties.
        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        // Applies optional lit material properties only when the active shader supports them.
        private static void ConfigureLitSurface(Material material, Color baseColor, float emissionStrength)
        {
            if (material == null)
            {
                return;
            }

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

        // Configures alpha blending for URP and common fallback shaders.
        private static void ConfigureTransparency(Material material)
        {
            if (material == null)
            {
                return;
            }

            material.renderQueue = 3000;
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

        // Warns during editor/development builds when a runtime material cannot render safely.
        private static void ValidateMaterial(Material material, string context)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!IsMaterialUsable(material))
            {
                WarnDevelopmentOnly("Invalid procedural material for " + context + ".");
            }
#endif
        }

        // Emits build diagnostics only in editor/development builds to avoid release log spam.
        private static void WarnDevelopmentOnly(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(message);
#endif
        }
    }
}
