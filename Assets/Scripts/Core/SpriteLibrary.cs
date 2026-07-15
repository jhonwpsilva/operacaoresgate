using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Carrega texturas da pasta Resources e as converte em Sprites em tempo de execucao.
    /// Mantem cache para evitar recarregar. Centraliza o acesso a todos os sprites do jogo.
    /// </summary>
    public static class SpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> _texCache = new Dictionary<string, Texture2D>();

        /// <summary>Caminho relativo dentro de Resources, sem extensao. Ex: "Sprites/player/pose_00".</summary>
        public static Sprite Get(string resourcePath, float pixelsPerUnit = 200f)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            string key = resourcePath + "@" + pixelsPerUnit;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            Texture2D tex = GetTexture(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[SpriteLibrary] Textura nao encontrada: {resourcePath}");
                return null;
            }
            var rect = new Rect(0, 0, tex.width, tex.height);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit,
                                       0, SpriteMeshType.FullRect);
            sprite.name = resourcePath;
            _cache[key] = sprite;
            return sprite;
        }

        public static Texture2D GetTexture(string resourcePath)
        {
            if (_texCache.TryGetValue(resourcePath, out var t) && t != null) return t;
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex != null) _texCache[resourcePath] = tex;
            return tex;
        }

        /// <summary>Carrega todas as poses do player em ordem (pose_00..pose_NN).</summary>
        public static Sprite[] LoadPlayerPoses(int count = 18, float ppu = 200f)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                var sp = Get($"Sprites/player/pose_{i:00}", ppu);
                if (sp != null) list.Add(sp);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Cria um Sprite com bordas 9-slice (para botoes/paineis arredondados que
        /// nao distorcem ao redimensionar). 'border' = pixels de canto fixo.
        /// </summary>
        public static Sprite GetSliced(string resourcePath, float border, float pixelsPerUnit = 100f)
        {
            string key = resourcePath + "@sliced" + border + "@" + pixelsPerUnit;
            if (_cache.TryGetValue(key, out var s) && s != null) return s;

            Texture2D tex = GetTexture(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[SpriteLibrary] Textura (sliced) nao encontrada: {resourcePath}");
                return null;
            }
            var rect = new Rect(0, 0, tex.width, tex.height);
            var b = new Vector4(border, border, border, border);
            var sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit,
                                       0, SpriteMeshType.FullRect, b);
            sprite.name = resourcePath + "_sliced";
            _cache[key] = sprite;
            return sprite;
        }

        /// <summary>Cria uma textura 1x1 de cor solida (util para barras de UI).</summary>
        public static Texture2D SolidTexture(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            t.SetPixel(0, 0, c); t.Apply();
            t.wrapMode = TextureWrapMode.Clamp;
            return t;
        }
    }
}
