using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Desenha por codigo os icones de itens que nao possuem arquivo PNG (escudo, dinheiro,
    /// XP, municao). Gera texturas RGBA nitidas com contorno e brilho, cacheadas para reuso.
    /// Usado como fallback pelo LevelBuilder/Loot quando o sprite do disco nao existe.
    /// </summary>
    public static class ProceduralSprites
    {
        private const int TAM = 64;
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>Retorna o icone do tipo (primeiro tenta o disco; senao desenha).</summary>
        public static Sprite ItemIcon(string tipo)
        {
            // itens que existem em disco tem prioridade
            var doDisco = SpriteLibrary.Get("Sprites/items/" + tipo, 200f);
            if (doDisco != null) return doDisco;
            if (tipo == "municao") { var m = SpriteLibrary.Get("Sprites/props/caixa_municao", 200f); if (m != null) return m; }

            if (_cache.TryGetValue(tipo, out var s) && s != null) return s;

            Texture2D tex;
            switch (tipo)
            {
                case "escudo":   tex = Escudo(); break;
                case "dinheiro": tex = Moeda(); break;
                case "xp":       tex = Estrela(); break;
                case "municao":  tex = Municao(); break;
                default:         tex = Moeda(); break;
            }
            var sp = Sprite.Create(tex, new Rect(0, 0, TAM, TAM), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect);
            sp.name = "proc_" + tipo;
            _cache[tipo] = sp;
            return sp;
        }

        private static Texture2D NovaTex()
        {
            var t = new Texture2D(TAM, TAM, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            var limpa = new Color(0, 0, 0, 0);
            var px = new Color[TAM * TAM];
            for (int i = 0; i < px.Length; i++) px[i] = limpa;
            t.SetPixels(px);
            return t;
        }

        private static void Set(Texture2D t, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= TAM || y >= TAM) return;
            t.SetPixel(x, y, c);
        }

        // escudo azul com cruz clara
        private static Texture2D Escudo()
        {
            var t = NovaTex();
            Color azul = new Color(0.20f, 0.45f, 0.85f);
            Color azulEsc = new Color(0.10f, 0.28f, 0.60f);
            Color clara = new Color(0.85f, 0.92f, 1f);
            float cx = TAM / 2f;
            for (int y = 0; y < TAM; y++)
            {
                for (int x = 0; x < TAM; x++)
                {
                    float ny = y / (float)TAM;           // 0 base, 1 topo
                    float largura = Mathf.Lerp(0.10f, 0.44f, Mathf.SmoothStep(0f, 1f, ny)); // afunila embaixo
                    float dx = Mathf.Abs(x - cx) / TAM;
                    bool dentro = dx < largura && ny > 0.06f && ny < 0.94f;
                    if (!dentro) continue;
                    bool borda = dx > largura - 0.045f || ny < 0.10f || ny > 0.90f;
                    Color c = borda ? azulEsc : azul;
                    // cruz clara no centro
                    bool cruzV = Mathf.Abs(x - cx) < 3.5f && ny > 0.30f && ny < 0.78f;
                    bool cruzH = Mathf.Abs(y - TAM * 0.56f) < 3.5f && dx < 0.20f;
                    if (cruzV || cruzH) c = clara;
                    Set(t, x, y, c);
                }
            }
            t.Apply();
            return t;
        }

        // moeda dourada com anel
        private static Texture2D Moeda()
        {
            var t = NovaTex();
            Color ouro = new Color(0.95f, 0.78f, 0.25f);
            Color ouroEsc = new Color(0.75f, 0.55f, 0.12f);
            Color brilho = new Color(1f, 0.95f, 0.7f);
            float cx = TAM / 2f, cy = TAM / 2f, r = TAM * 0.42f;
            for (int y = 0; y < TAM; y++)
            {
                for (int x = 0; x < TAM; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (d > r) continue;
                    Color c = ouro;
                    if (d > r - 4f) c = ouroEsc;                 // borda
                    else if (d < r * 0.62f && d > r * 0.5f) c = ouroEsc; // anel interno
                    // reflexo
                    if ((x - cx) * 0.6f + (cy - y) > r * 0.4f && d < r * 0.5f) c = brilho;
                    Set(t, x, y, c);
                }
            }
            t.Apply();
            return t;
        }

        // estrela de XP (5 pontas) ciano
        private static Texture2D Estrela()
        {
            var t = NovaTex();
            Color ciano = new Color(0.35f, 0.85f, 1f);
            Color cianoEsc = new Color(0.15f, 0.55f, 0.85f);
            float cx = TAM / 2f, cy = TAM / 2f;
            float rExt = TAM * 0.44f, rInt = TAM * 0.19f;
            for (int y = 0; y < TAM; y++)
            {
                for (int x = 0; x < TAM; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ang = Mathf.Atan2(dy, dx);
                    // raio da estrela nesse angulo (5 pontas)
                    float k = Mathf.Cos(ang * 5f - Mathf.PI / 2f);
                    float raio = Mathf.Lerp(rInt, rExt, (k + 1f) * 0.5f);
                    if (d > raio) continue;
                    Color c = d > raio - 3f ? cianoEsc : ciano;
                    Set(t, x, y, c);
                }
            }
            t.Apply();
            return t;
        }

        // caixa de municao (fallback caso nao ache o prop)
        private static Texture2D Municao()
        {
            var t = NovaTex();
            Color verde = new Color(0.30f, 0.42f, 0.25f);
            Color verdeEsc = new Color(0.18f, 0.28f, 0.15f);
            Color lat = new Color(0.85f, 0.75f, 0.35f);
            for (int y = 0; y < TAM; y++)
            {
                for (int x = 0; x < TAM; x++)
                {
                    float nx = x / (float)TAM, ny = y / (float)TAM;
                    if (nx < 0.14f || nx > 0.86f || ny < 0.24f || ny > 0.80f) continue;
                    bool borda = nx < 0.18f || nx > 0.82f || ny < 0.28f || ny > 0.76f;
                    Color c = borda ? verdeEsc : verde;
                    // "balas" douradas no topo
                    if (ny > 0.62f && ((x + 4) % 10 < 4)) c = lat;
                    Set(t, x, y, c);
                }
            }
            t.Apply();
            return t;
        }
    }
}
