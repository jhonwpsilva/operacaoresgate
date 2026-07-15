using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>
    /// HUD do jogo. Barras de VIDA, ESCUDO e ENERGIA; MUNICAO + ARMA EQUIPADA + GRANADAS
    /// com aviso de RECARGA; MIRA DINAMICA que abre com o recuo; pontuacao, itens, vidas,
    /// objetivo, mini-mapa tatico, contador de inimigos, barra do chefe, barra de mini-chefe
    /// e painel do companheiro K9. Le tudo do GameManager/Player e alimenta a musica dinamica.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private Image _vidaFill, _energiaFill, _escudoFill, _bossFill, _miniBossFill;
        private Text _pontosTxt, _itensTxt, _vidasTxt, _objetivoTxt, _faseTxt, _inimigosTxt, _miniBossTxt;
        private Text _armaTxt, _municaoTxt, _granadaTxt;
        private RectTransform _minimapaJogador, _minimapaMeta, _minimapaArea;
        private GameObject _bossPainel, _miniBossPainel, _armaPainel;
        private EnemyController _miniRef;
        private float _timerScan;
        private int _vivos;
        // sprites de UI premium (mesmo tema visual da capa)
        private Sprite _panelSpr, _barBgSpr, _barFillSpr, _avatarFrameSpr;
        // painel do companheiro K9
        private GameObject _k9Painel;
        private Image _k9VidaFill, _k9EnergiaFill, _k9XpFill;
        private Text _k9Txt, _k9EstadoTxt;
        // mira dinamica
        private RectTransform _canvasRect, _miraRoot;
        private RectTransform[] _miraTicks;

        void Start()
        {
            Montar();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnHUDMudou += Atualizar;
                GameManager.Instance.OnEstadoMudou += AoMudarEstado;
            }
            Atualizar();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnHUDMudou -= Atualizar;
                GameManager.Instance.OnEstadoMudou -= AoMudarEstado;
            }
        }

        private void Montar()
        {
            var canvas = UIFactory.CriarCanvas("HUDCanvas", 100);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;
            _canvasRect = canvas.GetComponent<RectTransform>();

            CarregarSpritesUI();

            // ---- Painel esquerdo: CRACHA DO COMANDANTE + VIDA / ESCUDO / ENERGIA ----
            //  (a mesma identidade visual da capa, agora dentro do jogo)
            var esq = PainelPremium(root, new Vector2(0,1), new Vector2(0,1),
                                    new Vector2(24,-306), new Vector2(414,-24));

            CrachaComandanteHUD(esq);

            // divisoria dourada entre o cracha e as barras
            var divis = UIFactory.Painel(esq, new Vector2(0,1), new Vector2(1,1),
                                         new Vector2(16,-106), new Vector2(-16,-104), new Color(0.84f,0.64f,0.22f,0.55f));
            divis.GetComponent<Image>().raycastTarget = false;

            UIFactory.TextoAncorado(esq, "VIDA", 15, GameConfig.VerdeHUD, TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(0,1), new Vector2(18,-132), new Vector2(120,-114), FontStyle.Bold);
            _vidaFill = BarraPremium(esq, new Vector2(0,1), new Vector2(1,1),
                                     new Vector2(18,-154), new Vector2(-18,-134), GameConfig.VermelhoAlerta);

            UIFactory.TextoAncorado(esq, "ESCUDO", 13, new Color(0.5f,0.75f,1f), TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(0,1), new Vector2(18,-176), new Vector2(120,-158), FontStyle.Bold);
            _escudoFill = BarraPremium(esq, new Vector2(0,1), new Vector2(1,1),
                                       new Vector2(18,-198), new Vector2(-18,-178), new Color(0.42f,0.72f,1f));

            UIFactory.TextoAncorado(esq, "ENERGIA", 13, new Color(0.55f,0.8f,1f), TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(0,1), new Vector2(18,-220), new Vector2(140,-202), FontStyle.Bold);
            _energiaFill = BarraPremium(esq, new Vector2(0,1), new Vector2(1,1),
                                        new Vector2(18,-242), new Vector2(-18,-222), GameConfig.AzulPremium);

            _vidasTxt = UIFactory.TextoAncorado(esq, "VIDAS  x3", 18, GameConfig.Creme, TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(1,1), new Vector2(18,-280), new Vector2(-16,-248), FontStyle.Bold);
            AtualizarTextoVidas();

            // ---- Painel superior direito: pontos + itens ----
            var direita = PainelPremium(root, new Vector2(1,1), new Vector2(1,1),
                                        new Vector2(-344,-110), new Vector2(-24,-24));

            _pontosTxt = UIFactory.TextoAncorado(direita, "PONTOS  0", 26, GameConfig.Dourado, TextAnchor.MiddleRight,
                                    new Vector2(0,0.5f), new Vector2(1,1), new Vector2(12,0), new Vector2(-16,-6), FontStyle.Bold);
            _itensTxt = UIFactory.TextoAncorado(direita, "ITENS  0/0", 22, GameConfig.Creme, TextAnchor.MiddleRight,
                                    new Vector2(0,0), new Vector2(1,0.5f), new Vector2(12,6), new Vector2(-16,0));

            // ---- Painel de arma/municao/granada (acima do mini-mapa) ----
            _armaPainel = PainelPremium(root, new Vector2(1,0), new Vector2(1,0),
                                        new Vector2(-344,116), new Vector2(-24,214)).gameObject;
            _armaTxt = UIFactory.TextoAncorado(_armaPainel.transform, "FUZIL", 20, GameConfig.Dourado, TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(1,1), new Vector2(12,-32), new Vector2(-12,-6), FontStyle.Bold);
            _municaoTxt = UIFactory.TextoAncorado(_armaPainel.transform, "30 / 120", 24, GameConfig.Creme, TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(1,1), new Vector2(12,-62), new Vector2(-12,-34), FontStyle.Bold);
            _granadaTxt = UIFactory.TextoAncorado(_armaPainel.transform, "GRANADAS x4", 16, new Color(1f,0.7f,0.4f), TextAnchor.MiddleLeft,
                                    new Vector2(0,1), new Vector2(1,1), new Vector2(12,-90), new Vector2(-12,-64), FontStyle.Bold);

            // ---- Indicador de fase + objetivo (rodape) — mesmo padrao da capa ----
            var rodape = PainelPremium(root, new Vector2(0.5f,0), new Vector2(0.5f,0),
                                       new Vector2(-330,24), new Vector2(330,74));
            _faseTxt = UIFactory.TextoAncorado(rodape, "FASE", 18, GameConfig.Dourado, TextAnchor.MiddleCenter,
                                    new Vector2(0,0.5f), new Vector2(1,1), new Vector2(8,2), new Vector2(-8,-4), FontStyle.Bold);
            _objetivoTxt = UIFactory.TextoAncorado(rodape, "Objetivo", 16, new Color(0.85f,0.9f,1f), TextAnchor.MiddleCenter,
                                    new Vector2(0,0), new Vector2(1,0.5f), new Vector2(8,4), new Vector2(-8,-2));

            // ---- Mini-mapa (canto inferior direito) ----
            var mapa = PainelPremium(root, new Vector2(1,0), new Vector2(1,0),
                                     new Vector2(-344,24), new Vector2(-24,108));
            _minimapaArea = mapa;
            UIFactory.TextoAncorado(mapa, "MAPA TATICO", 12, new Color(0.5f,0.7f,0.9f), TextAnchor.UpperLeft,
                                    new Vector2(0,1), new Vector2(1,1), new Vector2(8,-18), new Vector2(-8,-2));
            _minimapaMeta = UIFactory.Imagem(mapa, null, Vector2.zero, new Vector2(10,10), GameConfig.VerdeHUD).rectTransform;
            _minimapaJogador = UIFactory.Imagem(mapa, null, Vector2.zero, new Vector2(12,12), Color.white).rectTransform;

            // ---- Barra do chefe (oculta por padrao) ----
            _bossPainel = new GameObject("BossPainel");
            _bossPainel.transform.SetParent(root, false);
            var bp = _bossPainel.AddComponent<RectTransform>();
            bp.anchorMin = new Vector2(0.5f,1); bp.anchorMax = new Vector2(0.5f,1);
            bp.pivot = new Vector2(0.5f,1);
            bp.anchoredPosition = new Vector2(0,-20); bp.sizeDelta = new Vector2(700,46);
            UIFactory.TextoAncorado(_bossPainel.transform, "NUCLEO DE COMANDO INIMIGO", 18, GameConfig.VermelhoAlerta,
                                    TextAnchor.MiddleCenter, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-4), new Vector2(0,18), FontStyle.Bold);
            _bossFill = BarraPremium(_bossPainel.transform, new Vector2(0,0), new Vector2(1,0),
                                     new Vector2(0,4), new Vector2(0,24), GameConfig.VermelhoAlerta);
            _bossPainel.SetActive(false);

            // ---- Contador de inimigos (topo centro) ----
            _inimigosTxt = UIFactory.TextoAncorado(root, "INIMIGOS: 0", 18, new Color(1f,0.78f,0.5f),
                                    TextAnchor.MiddleCenter, new Vector2(0.5f,1), new Vector2(0.5f,1),
                                    new Vector2(-160,-44), new Vector2(160,-18), FontStyle.Bold);

            // ---- Barra de mini-chefe (oculta por padrao) ----
            _miniBossPainel = new GameObject("MiniBossPainel");
            _miniBossPainel.transform.SetParent(root, false);
            var mbp = _miniBossPainel.AddComponent<RectTransform>();
            mbp.anchorMin = new Vector2(0.5f,1); mbp.anchorMax = new Vector2(0.5f,1);
            mbp.pivot = new Vector2(0.5f,1);
            mbp.anchoredPosition = new Vector2(0,-52); mbp.sizeDelta = new Vector2(560,40);
            _miniBossTxt = UIFactory.TextoAncorado(_miniBossPainel.transform, "MINI-CHEFE", 16, new Color(1f,0.6f,0.3f),
                                    TextAnchor.MiddleCenter, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-2), new Vector2(0,16), FontStyle.Bold);
            _miniBossFill = BarraPremium(_miniBossPainel.transform, new Vector2(0,0), new Vector2(1,0),
                                     new Vector2(0,2), new Vector2(0,18), new Color(1f,0.5f,0.15f));
            _miniBossPainel.SetActive(false);

            // ---- Painel do companheiro K9 (canto inferior esquerdo) ----
            _k9Painel = PainelPremium(root, new Vector2(0,0), new Vector2(0,0),
                                      new Vector2(24,24), new Vector2(324,150)).gameObject;
            var k9sp = SpriteLibrary.Get("Sprites/companion/k9_alpha", 200f);
            var k9ic = UIFactory.Imagem(_k9Painel.transform, k9sp, Vector2.zero, new Vector2(58,58), Color.white);
            var k9rt = k9ic.rectTransform; k9rt.anchorMin = k9rt.anchorMax = new Vector2(0,1); k9rt.pivot = new Vector2(0,1);
            k9rt.anchoredPosition = new Vector2(10,-8); k9ic.raycastTarget = false;
            _k9Txt = UIFactory.TextoAncorado(_k9Painel.transform, "K9-CYBER ALPHA · Nv.1", 16, new Color(0.6f,0.85f,1f),
                                    TextAnchor.UpperLeft, new Vector2(0,1), new Vector2(1,1), new Vector2(76,-10), new Vector2(-8,-30), FontStyle.Bold);
            _k9EstadoTxt = UIFactory.TextoAncorado(_k9Painel.transform, "SEGUINDO", 13, GameConfig.VerdeHUD,
                                    TextAnchor.UpperLeft, new Vector2(0,1), new Vector2(1,1), new Vector2(76,-32), new Vector2(-8,-50), FontStyle.Bold);
            _k9VidaFill = BarraPremium(_k9Painel.transform, new Vector2(0,0), new Vector2(1,0),
                                    new Vector2(12,84), new Vector2(-12,98), new Color(0.9f,0.3f,0.3f));
            _k9EnergiaFill = BarraPremium(_k9Painel.transform, new Vector2(0,0), new Vector2(1,0),
                                    new Vector2(12,62), new Vector2(-12,76), new Color(0.3f,0.7f,1f));
            _k9XpFill = BarraPremium(_k9Painel.transform, new Vector2(0,0), new Vector2(1,0),
                                    new Vector2(12,46), new Vector2(-12,56), GameConfig.Dourado);
            UIFactory.TextoAncorado(_k9Painel.transform, "VIDA   ENERGIA   XP", 10, new Color(1,1,1,0.45f),
                                    TextAnchor.LowerLeft, new Vector2(0,0), new Vector2(1,0), new Vector2(12,28), new Vector2(-12,44));

            // ---- Mira dinamica (cruz que abre com o recuo) ----
            CriarMira(root);

            // dica de controles (some depois)
            var dica = UIFactory.TextoAncorado(root, "A/D mover · SHIFT correr · ESPACO pular(2x) · S agachar · CTRL rolar · MOUSE mirar · CLIQUE/J atirar · R recarregar · Q/1-4 trocar arma · G granada · V faca · E interagir",
                                    13, new Color(1,1,1,0.6f), TextAnchor.LowerCenter,
                                    new Vector2(0,0), new Vector2(1,0), new Vector2(0,96), new Vector2(0,120));
            Destroy(dica.gameObject, 12f);
        }

        private void CriarMira(Transform root)
        {
            var go = new GameObject("Mira");
            go.transform.SetParent(root, false);
            _miraRoot = go.AddComponent<RectTransform>();
            _miraRoot.anchorMin = _miraRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _miraRoot.pivot = new Vector2(0.5f, 0.5f);
            _miraRoot.sizeDelta = new Vector2(4, 4);

            _miraTicks = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                var t = new GameObject("tick");
                t.transform.SetParent(_miraRoot, false);
                var img = t.AddComponent<Image>();
                img.color = new Color(1f, 0.9f, 0.4f, 0.85f);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                bool vertical = i < 2;
                rt.sizeDelta = vertical ? new Vector2(3, 11) : new Vector2(11, 3);
                _miraTicks[i] = rt;
            }
        }

        // ==============================================================
        //  Estilo premium (mesmos sprites/tema da capa)
        // ==============================================================
        private void CarregarSpritesUI()
        {
            _panelSpr       = SpriteLibrary.GetSliced("Sprites/ui/panel", 18f);
            _barBgSpr       = SpriteLibrary.GetSliced("Sprites/ui/bar_bg", 12f);
            _barFillSpr     = SpriteLibrary.GetSliced("Sprites/ui/bar_fill", 12f);
            _avatarFrameSpr = SpriteLibrary.GetSliced("Sprites/ui/avatar_frame", 14f);
        }

        // Painel com o sprite fatiado da capa (borda + fundo escuro translucido)
        private RectTransform PainelPremium(Transform pai, Vector2 anchorMin, Vector2 anchorMax,
                                            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("PainelHUD");
            go.transform.SetParent(pai, false);
            var img = go.AddComponent<Image>();
            img.sprite = _panelSpr; img.type = Image.Type.Sliced;
            img.color = new Color(0.10f, 0.13f, 0.20f, 0.92f);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        // Barra com os sprites bar_bg + bar_fill da capa. Retorna a Image de preenchimento
        // (Filled/Horizontal) — compativel com o fillAmount usado no Update().
        private Image BarraPremium(Transform pai, Vector2 anchorMin, Vector2 anchorMax,
                                   Vector2 offsetMin, Vector2 offsetMax, Color corFrente)
        {
            var fundoGO = new GameObject("BarraFundo");
            fundoGO.transform.SetParent(pai, false);
            var fundo = fundoGO.AddComponent<Image>();
            fundo.sprite = _barBgSpr; fundo.type = Image.Type.Sliced;
            fundo.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
            fundo.raycastTarget = false;
            var brt = fundo.rectTransform;
            brt.anchorMin = anchorMin; brt.anchorMax = anchorMax;
            brt.offsetMin = offsetMin; brt.offsetMax = offsetMax;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fundo.transform, false);
            var fill = fillGO.AddComponent<Image>();
            fill.sprite = _barFillSpr;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.color = corFrente;
            fill.raycastTarget = false;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
            return fill;
        }

        // Cracha do COMANDANTE dentro do jogo — igual ao da capa (avatar, nivel, barra, estrela)
        private void CrachaComandanteHUD(Transform painel)
        {
            var avatar = SpriteLibrary.Get("Sprites/icons/avatar_comandante", 100f);
            var avImg = UIFactory.Imagem(painel, avatar, Vector2.zero, new Vector2(60, 60), Color.white);
            var avrt = avImg.rectTransform;
            avrt.anchorMin = avrt.anchorMax = new Vector2(0, 1); avrt.pivot = new Vector2(0, 1);
            avrt.anchoredPosition = new Vector2(16, -9); avImg.raycastTarget = false;

            var frame = UIFactory.Imagem(painel, _avatarFrameSpr, Vector2.zero, new Vector2(66, 66), GameConfig.Dourado);
            frame.type = Image.Type.Sliced;
            var frt = frame.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0, 1); frt.pivot = new Vector2(0, 1);
            frt.anchoredPosition = new Vector2(13, -6); frame.raycastTarget = false;

            UIFactory.TextoAncorado(painel, "COMANDANTE", 19, GameConfig.Creme, TextAnchor.LowerLeft,
                                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -40), new Vector2(-46, -8), FontStyle.Bold);
            UIFactory.TextoAncorado(painel, "NIVEL 25", 13, new Color(0.7f, 0.78f, 0.9f), TextAnchor.UpperLeft,
                                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -66), new Vector2(-70, -40));
            UIFactory.TextoAncorado(painel, "75%", 12, GameConfig.Creme, TextAnchor.MiddleRight,
                                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(88, -66), new Vector2(-16, -40), FontStyle.Bold);

            var nivelFill = BarraPremium(painel, new Vector2(0, 1), new Vector2(1, 1),
                                         new Vector2(88, -86), new Vector2(-16, -70), GameConfig.Dourado);
            nivelFill.fillAmount = 0.75f;

            var star = UIFactory.Imagem(painel, SpriteLibrary.Get("Sprites/icons/icon_star", 100f), Vector2.zero, new Vector2(22, 22), GameConfig.Dourado);
            var srt = star.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(1, 1); srt.pivot = new Vector2(1, 1);
            srt.anchoredPosition = new Vector2(-10, -8); star.raycastTarget = false;
        }

        private void AoMudarEstado()
        {
            bool chefe = GameManager.Instance != null && GameManager.Instance.NivelData != null
                         && GameManager.Instance.NivelData.ehChefe
                         && GameManager.Instance.Estado == EstadoJogo.Jogando;
            if (_bossPainel != null) _bossPainel.SetActive(chefe);
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var player = gm.Player;

            if (player != null && player.Saude != null)
            {
                if (_vidaFill != null) _vidaFill.fillAmount = Mathf.Lerp(_vidaFill.fillAmount, player.Saude.Vida / GameConfig.VidaMaxima, 0.25f);
                if (_energiaFill != null) _energiaFill.fillAmount = Mathf.Lerp(_energiaFill.fillAmount, player.Saude.Energia / GameConfig.EnergiaMaxima, 0.25f);
                if (_escudoFill != null) _escudoFill.fillAmount = Mathf.Lerp(_escudoFill.fillAmount, player.Saude.EscudoPct, 0.25f);
            }

            AtualizarArma(player);
            AtualizarMira(player);
            AtualizarMinimapa();
            AtualizarBoss();
            AtualizarCombate();
            AtualizarK9();
        }

        private void AtualizarArma(PlayerController player)
        {
            if (_armaPainel == null) return;
            bool mostrar = player != null && player.Armas != null;
            if (_armaPainel.activeSelf != mostrar) _armaPainel.SetActive(mostrar);
            if (!mostrar) return;

            var w = player.Armas;
            if (_armaTxt != null) _armaTxt.text = w.ArmaNome;
            if (_municaoTxt != null)
            {
                if (w.Recarregando) { _municaoTxt.text = "RECARREGANDO..."; _municaoTxt.color = new Color(1f,0.7f,0.3f); }
                else
                {
                    _municaoTxt.text = w.MunicaoPente + " / " + w.MunicaoReserva;
                    _municaoTxt.color = w.MunicaoPente == 0 ? new Color(1f,0.4f,0.35f) : GameConfig.Creme;
                }
            }
            if (_granadaTxt != null && player.Granadas != null)
                _granadaTxt.text = "GRANADAS x" + player.Granadas.Estoque;
        }

        private void AtualizarMira(PlayerController player)
        {
            if (_miraRoot == null || _canvasRect == null) return;
            bool mostrar = player != null && player.Saude != null && !player.Saude.Morto
                           && GameManager.Instance != null && GameManager.Instance.Estado == EstadoJogo.Jogando;
            _miraRoot.gameObject.SetActive(mostrar);
            if (!mostrar) return;

            Vector2 lp;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Input.mousePosition, null, out lp))
                _miraRoot.anchoredPosition = lp;

            float spread = player.Armas != null ? player.Armas.EspalhamentoVisual : 0f;
            float gap = 9f + spread * 26f;
            if (_miraTicks != null && _miraTicks.Length == 4)
            {
                _miraTicks[0].anchoredPosition = new Vector2(0, gap);
                _miraTicks[1].anchoredPosition = new Vector2(0, -gap);
                _miraTicks[2].anchoredPosition = new Vector2(gap, 0);
                _miraTicks[3].anchoredPosition = new Vector2(-gap, 0);
            }
        }

        private void AtualizarK9()
        {
            if (_k9Painel == null) return;
            var dog = GameManager.Instance != null ? GameManager.Instance.Companheiro : null;
            bool mostrar = dog != null;
            if (_k9Painel.activeSelf != mostrar) _k9Painel.SetActive(mostrar);
            if (!mostrar) return;

            if (_k9VidaFill != null) _k9VidaFill.fillAmount = Mathf.Lerp(_k9VidaFill.fillAmount, dog.VidaPct, 0.2f);
            if (_k9EnergiaFill != null) _k9EnergiaFill.fillAmount = Mathf.Lerp(_k9EnergiaFill.fillAmount, dog.EnergiaPct, 0.2f);
            if (_k9XpFill != null) _k9XpFill.fillAmount = Mathf.Lerp(_k9XpFill.fillAmount, dog.XPPct, 0.2f);
            if (_k9Txt != null) _k9Txt.text = $"{dog.Nome} · Nv.{dog.Nivel}";
            if (_k9EstadoTxt != null)
            {
                _k9EstadoTxt.text = dog.EstadoTexto;
                Color c;
                switch (dog.Estado)
                {
                    case EstadoK9.Alerta:      c = new Color(1f,0.3f,0.25f); break;
                    case EstadoK9.Curando:     c = GameConfig.VerdeHUD; break;
                    case EstadoK9.Comemorando: c = GameConfig.Dourado; break;
                    case EstadoK9.Furtivo:     c = new Color(0.4f,0.7f,0.8f); break;
                    default:                   c = new Color(0.6f,0.85f,1f); break;
                }
                _k9EstadoTxt.color = c;
            }
        }

        private void AtualizarCombate()
        {
            var gm = GameManager.Instance;
            bool faseChefe = gm != null && gm.NivelData != null && gm.NivelData.ehChefe;

            _timerScan -= Time.deltaTime;
            if (_timerScan <= 0f)
            {
                _timerScan = 0.25f;
                _vivos = 0; _miniRef = null;
                foreach (var e in FindObjectsOfType<EnemyController>())
                {
                    if (!e.EstaVivo) continue;
                    _vivos++;
                    if (e.MiniChefe && _miniRef == null) _miniRef = e;
                }
            }

            // musica dinamica: intensidade cresce com inimigos ativos e chefe
            float intensidade = Mathf.Clamp01(_vivos / 6f);
            if (faseChefe) intensidade = Mathf.Max(intensidade, 0.7f);
            AudioManager.Instance?.SetCombatIntensity(intensidade);

            if (_inimigosTxt != null)
            {
                _inimigosTxt.gameObject.SetActive(!faseChefe);
                if (!faseChefe)
                {
                    _inimigosTxt.text = _vivos > 0 ? $"INIMIGOS: {_vivos}" : "AREA LIMPA — siga em frente";
                    _inimigosTxt.color = _vivos > 0 ? new Color(1f,0.78f,0.5f) : GameConfig.VerdeHUD;
                }
            }

            if (_miniBossPainel != null)
            {
                bool mostrar = _miniRef != null && _miniRef.EstaVivo;
                if (_miniBossPainel.activeSelf != mostrar) _miniBossPainel.SetActive(mostrar);
                if (mostrar)
                {
                    if (_miniBossFill != null)
                        _miniBossFill.fillAmount = Mathf.Lerp(_miniBossFill.fillAmount, _miniRef.VidaPct, 0.2f);
                    if (_miniBossTxt != null)
                    {
                        string nome = EnemyData.NomeExibicao(_miniRef.tipo);
                        _miniBossTxt.text = _miniRef.Enfurecido ? nome + "   [ FURIA ]" : nome;
                        _miniBossTxt.color = _miniRef.Enfurecido ? new Color(1f,0.3f,0.25f) : new Color(1f,0.6f,0.3f);
                        if (_miniBossFill != null)
                            _miniBossFill.color = _miniRef.Enfurecido ? new Color(1f,0.25f,0.2f) : new Color(1f,0.5f,0.15f);
                    }
                }
            }
        }

        private void AtualizarMinimapa()
        {
            if (_minimapaArea == null || _minimapaJogador == null) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Player == null || gm.NivelData == null) return;

            float comprimento = gm.NivelData.comprimento + 12f;
            float inicio = gm.NivelData.spawnPlayer.x - 6f;
            float largura = _minimapaArea.rect.width - 24f;
            float yPos = -8f;

            float px = Mathf.InverseLerp(inicio, comprimento, gm.Player.transform.position.x);
            _minimapaJogador.anchoredPosition = new Vector2(-largura/2f + px * largura, yPos);

            if (_minimapaMeta != null)
                _minimapaMeta.anchoredPosition = new Vector2(largura/2f, yPos);
        }

        private void AtualizarBoss()
        {
            if (_bossFill == null || _bossPainel == null || !_bossPainel.activeSelf) return;
            var boss = FindObjectOfType<BossController>();
            if (boss != null)
                _bossFill.fillAmount = Mathf.Lerp(_bossFill.fillAmount, boss.Vida / boss.VidaMax, 0.2f);
        }

        private void Atualizar()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (_pontosTxt != null) _pontosTxt.text = "PONTOS  " + gm.Pontos.ToString("N0");
            if (_itensTxt != null) _itensTxt.text = $"ITENS  {gm.ItensColetados}/{gm.ItensNoNivel}";
            AtualizarTextoVidas();
            if (gm.NivelData != null)
            {
                if (_faseTxt != null) _faseTxt.text = $"FASE {gm.NivelAtual + 1}/{LevelData.TotalNiveis} — {gm.NivelData.nome}";
                if (_objetivoTxt != null)
                    _objetivoTxt.text = gm.NivelData.ehChefe ? "Objetivo: desativar o nucleo de comando inimigo"
                                                             : "Objetivo: alcancar o ponto de extracao →";
            }
        }

        private void AtualizarTextoVidas()
        {
            if (_vidasTxt != null && GameManager.Instance != null)
                _vidasTxt.text = "VIDAS  x" + GameManager.Instance.Vidas;
        }
    }
}
