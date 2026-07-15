using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Fundo do cenario em camadas, NITIDO e sem emenda:
    ///   1) Ceu solido bem ao fundo (so garante que nunca sobra vazio).
    ///   2) Painel da arte da fase: UMA copia da pintura (sem repetir, sem espelhar),
    ///      mostrada com zoom (janela de UV) para ficar nitida, e que faz "pan" suave
    ///      da esquerda pra direita conforme voce avanca = parallax de profundidade.
    ///   3) Faixa de rua (asfalto) logo abaixo da linha de jogo.
    /// O painel recentraliza na camera e e dimensionado para preencher a tela inteira,
    /// entao a arte cobre tudo sem corte e sem distorcao.
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        private Camera _cam;
        private float _comprimento = 60f;

        // --- painel da arte ---
        private Transform _pano;
        private Material _panoMat;
        private const float PanoZ       = 22f;   // profundidade do painel (atras de tudo)
        private const float PanoZoom    = 0.62f; // fracao da arte visivel (menor = mais zoom/nitido)
        private const float PanoOffsetY = 0.30f; // enquadramento vertical (horizonte na parte baixa)

        // --- faixa de rua (piso) ---
        private Transform _rua;
        private Material _ruaMat;
        private const float RuaTile    = 16f;
        private const float RuaTopoY   = 0.10f;
        private const float RuaAltura  = 34f;
        private const float RuaLargura = 80f;
        private const float RuaZ       = 3f;

        // --- ceu de preenchimento ---
        private Transform _ceu;
        private const float CeuZ = 80f;

        public void Configurar(string textura, float comprimento)
        {
            _comprimento = Mathf.Max(10f, comprimento);

            Color ceuCor = CorCeu(textura);
            _ceu = CriarQuad("ceuFundo", CeuZ, MatUnlitCor(ceuCor));
            _ceu.localScale = new Vector3(600f, 320f, 1f);

            var tex = SpriteLibrary.GetTexture("Backgrounds/" + textura);
            _panoMat = MatUnlitTex(tex, new Color(0.10f, 0.12f, 0.16f), TextureWrapMode.Clamp);
            _panoMat.mainTextureScale = new Vector2(PanoZoom, PanoZoom); // mostra so uma janela = nitido
            _pano = CriarQuad("painelArte", PanoZ, _panoMat);

            var ruaTex = SpriteLibrary.GetTexture("Backgrounds/tex_rua");
            _ruaMat = MatUnlitTex(ruaTex, new Color(0.14f, 0.14f, 0.15f), TextureWrapMode.Repeat);
            _rua = CriarQuad("faixaRua", RuaZ, _ruaMat);
            _rua.localScale = new Vector3(RuaLargura, RuaAltura, 1f);
            _ruaMat.mainTextureScale = new Vector2(RuaLargura / RuaTile, 1f);

            PegarCamera();
            PosicionarTudo();
        }

        void Start() { PegarCamera(); }

        void LateUpdate()
        {
            if (_cam == null) { PegarCamera(); if (_cam == null) return; }
            PosicionarTudo();
        }

        private void PosicionarTudo()
        {
            float cx = _cam != null ? _cam.transform.position.x : 0f;
            float cy = _cam != null ? _cam.transform.position.y : 0f;

            if (_ceu != null) _ceu.position = new Vector3(cx, cy, _ceu.position.z);

            if (_pano != null && _cam != null)
            {
                // dimensiona o painel para PREENCHER a tela na profundidade dele
                float dist = PanoZ - _cam.transform.position.z;
                float h = 2f * dist * Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float w = h * _cam.aspect;
                _pano.localScale = new Vector3(w * 1.06f, h * 1.06f, 1f);

                // compensa a leve inclinacao da camera (6 graus) para centralizar
                float tilt = dist * Mathf.Tan(6f * Mathf.Deg2Rad);
                _pano.position = new Vector3(cx, cy + tilt, PanoZ);

                // PAN: desliza a janela da esquerda (inicio) pra direita (fim) da pintura
                float t = Mathf.Clamp01(cx / _comprimento);
                if (_panoMat != null)
                    _panoMat.mainTextureOffset = new Vector2(t * (1f - PanoZoom), PanoOffsetY);
            }

            if (_rua != null)
            {
                float centroY = RuaTopoY - RuaAltura * 0.5f;
                _rua.position = new Vector3(cx, centroY, _rua.position.z);
                if (_ruaMat != null)
                    _ruaMat.mainTextureOffset = new Vector2(cx / RuaTile, 0f);
            }
        }

        private void PegarCamera()
        {
            if (_cam != null) return;
            if (Camera.main != null) _cam = Camera.main;
        }

        // ---------- helpers ----------
        private Transform CriarQuad(string nome, float z, Material mat)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>(); mf.mesh = QuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.transform.localPosition = new Vector3(0f, 0f, z);
            return go.transform;
        }

        private Material MatUnlitTex(Texture2D tex, Color fallback, TextureWrapMode wrap)
        {
            var m = new Material(Shader.Find("Unlit/Texture"));
            if (tex != null)
            {
                m.mainTexture = tex;
                tex.wrapMode = wrap;
                tex.filterMode = FilterMode.Bilinear;
            }
            else
            {
                m.mainTexture = SpriteLibrary.SolidTexture(fallback);
            }
            return m;
        }

        private Material MatUnlitCor(Color c)
        {
            var m = new Material(Shader.Find("Unlit/Texture"));
            m.mainTexture = SpriteLibrary.SolidTexture(c);
            return m;
        }

        private static Color CorCeu(string textura)
        {
            switch (textura)
            {
                case "bg_favela":     return new Color(0.06f, 0.11f, 0.06f);
                case "bg_ruinas":     return new Color(0.14f, 0.08f, 0.05f);
                case "bg_industrial": return new Color(0.10f, 0.07f, 0.05f);
                case "bg_rua":        return new Color(0.12f, 0.08f, 0.06f);
                case "bg_boss":       return new Color(0.03f, 0.06f, 0.07f);
                default:              return new Color(0.06f, 0.08f, 0.10f);
            }
        }

        private static Mesh QuadMesh()
        {
            var m = new Mesh();
            m.vertices = new Vector3[]{ new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(0.5f,0.5f,0), new Vector3(-0.5f,0.5f,0)};
            m.uv = new Vector2[]{ new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1)};
            m.triangles = new int[]{0,2,1, 0,3,2};
            m.RecalculateNormals();
            return m;
        }
    }
}
