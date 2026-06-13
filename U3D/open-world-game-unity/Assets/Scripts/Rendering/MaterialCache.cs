using System.Collections.Generic;
using UnityEngine;

namespace Rendering
{
    /// <summary>
    /// 全局材质缓存池，解决重复 new Material() 导致的 GPU 内存泄漏问题。
    /// 所有渲染器通过此单例获取共享材质，同一颜色复用同一实例。
    /// </summary>
    public static class MaterialCache
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        private static readonly Dictionary<ColorKey, Material> _colorMats = new();
        private static readonly Dictionary<string, Material> _namedMats = new();
        private static Shader _cachedLitShader;
        private static Shader _cachedUnlitShader;

        private static Shader LitShader
        {
            get
            {
                if (_cachedLitShader == null)
                {
                    _cachedLitShader = Shader.Find(LitShaderName);
                    if (_cachedLitShader == null) _cachedLitShader = Shader.Find("Standard");
                }
                return _cachedLitShader;
            }
        }

        private static Shader UnlitShader
        {
            get
            {
                if (_cachedUnlitShader == null)
                {
                    _cachedUnlitShader = Shader.Find(UnlitShaderName);
                    if (_cachedUnlitShader == null) _cachedUnlitShader = Shader.Find("Standard");
                }
                return _cachedUnlitShader;
            }
        }

        /// <summary>获取指定颜色的 Lit 材质（共享实例，不要对其调用 Destroy）</summary>
        public static Material GetLit(Color color)
        {
            var key = new ColorKey(color);
            if (_colorMats.TryGetValue(key, out var mat) && mat != null)
                return mat;

            mat = new Material(LitShader) { color = color };
            _colorMats[key] = mat;
            return mat;
        }

        /// <summary>获取指定颜色的 Unlit 材质（共享实例，不要对其调用 Destroy）</summary>
        public static Material GetUnlit(Color color)
        {
            var key = new ColorKey(color, unlit: true);
            if (_colorMats.TryGetValue(key, out var mat) && mat != null)
                return mat;

            mat = new Material(UnlitShader) { color = color };
            _colorMats[key] = mat;
            return mat;
        }

        /// <summary>获取带半透明的 Lit 材质</summary>
        public static Material GetLitTransparent(Color color, float alpha = 0.6f)
        {
            var c = new Color(color.r, color.g, color.b, alpha);
            var key = new ColorKey(c, transparent: true);
            if (_colorMats.TryGetValue(key, out var mat) && mat != null)
                return mat;

            mat = new Material(LitShader) { color = c };
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            _colorMats[key] = mat;
            return mat;
        }

        /// <summary>获取带自发光参数的 Lit 材质</summary>
        public static Material GetLitWithEmission(Color color, float emissionScale = 0.3f)
        {
            var key = new ColorKey(color, emission: true);
            if (_colorMats.TryGetValue(key, out var mat) && mat != null)
                return mat;

            mat = new Material(LitShader) { color = color };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionScale);
            _colorMats[key] = mat;
            return mat;
        }

        /// <summary>获取指定参数（光泽度、金属度）的 Lit 材质</summary>
        public static Material GetLitWithParams(Color color, float glossiness, float metallic)
        {
            var key = new ColorKey(color, gloss: glossiness, metal: metallic);
            if (_colorMats.TryGetValue(key, out var mat) && mat != null)
                return mat;

            mat = new Material(LitShader) { color = color };
            mat.SetFloat("_Glossiness", glossiness);
            mat.SetFloat("_Metallic", metallic);
            _colorMats[key] = mat;
            return mat;
        }

        /// <summary>获取命名材质（用于地形等特殊材质），key 如 "Terrain", "Water"</summary>
        public static Material GetNamed(string key)
        {
            if (_namedMats.TryGetValue(key, out var mat) && mat != null)
                return mat;

            mat = new Material(LitShader);
            _namedMats[key] = mat;
            return mat;
        }

        /// <summary>清理所有缓存材质，场景销毁时调用</summary>
        public static void ClearAll()
        {
            foreach (var mat in _colorMats.Values)
            {
                if (mat != null) Object.Destroy(mat);
            }
            _colorMats.Clear();

            foreach (var mat in _namedMats.Values)
            {
                if (mat != null) Object.Destroy(mat);
            }
            _namedMats.Clear();

            _cachedLitShader = null;
            _cachedUnlitShader = null;
        }

        /// <summary>获取缓存统计（用于调试）</summary>
        public static string Stats => $"MaterialCache: {_colorMats.Count} color mats, {_namedMats.Count} named mats";

        private readonly struct ColorKey
        {
            private readonly int _hash;
            public ColorKey(Color c, bool transparent = false, bool unlit = false, bool emission = false,
                float gloss = -1f, float metal = -1f)
            {
                unchecked
                {
                    _hash = c.GetHashCode();
                    _hash = (_hash * 397) ^ transparent.GetHashCode();
                    _hash = (_hash * 397) ^ unlit.GetHashCode();
                    _hash = (_hash * 397) ^ emission.GetHashCode();
                    _hash = (_hash * 397) ^ gloss.GetHashCode();
                    _hash = (_hash * 397) ^ metal.GetHashCode();
                }
            }

            public override int GetHashCode() => _hash;
            public override bool Equals(object obj) => obj is ColorKey other && _hash == other._hash;
        }
    }
}
