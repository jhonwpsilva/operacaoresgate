using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace OperacaoResgate
{
    /// <summary>
    /// Menu principal profissional: usa a arte de capa em tela cheia e monta por codigo
    /// os botoes (JOGAR, OPCOES, LOJA, SAIR) com icones, o cracha do COMANDANTE com barra
    /// de nivel, os icones de conquista e os botoes de redes sociais (que abrem os links).
    /// Inclui som e animacao de hover em todos os elementos interativos.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // ---- Links de redes sociais ----
        private const string URL_FACEBOOK  = "https://www.facebook.com/jonata.j.silva";
        private const string URL_INSTAGRAM = "https://www.instagram.com/jonatajhonw";
        private const string URL_YOUTUBE   = "https://www.youtube.com/@JonataPSilva";
        private const string URL_GITHUB    = "https://github.com/jhonwpsilva";

        private Transform _root;
        private GameObject _modalAtual;

        // sprites de UI
        private Sprite _btnGold, _btnDark, _panel, _iconBox, _avatarFrame, _barBg, _barFill;

        void Start()
        {
            CarregarSprites();
            Montar();
        }

        private void CarregarSprites()
        {
            _btnGold     = SpriteLibrary.GetSliced("Sprites/ui/btn_gold", 20f);
            _btnDark     = SpriteLibrary.GetSliced("Sprites/ui/btn_dark", 20f);
            _panel       = SpriteLibrary.GetSliced("Sprites/ui/panel", 18f);
            _iconBox     = SpriteLibrary.GetSliced("Sprites/ui/icon_box", 16f);
            _avatarFrame = SpriteLibrary.GetSliced("Sprites/ui/avatar_frame", 14f);
            _barBg       = SpriteLibrary.GetSliced("Sprites/ui/bar_bg", 12f);
            _barFill     = SpriteLibrary.GetSliced("Sprites/ui/bar_fill", 12f);
        }

        private void Montar()
        {
            var canvas = UIFactory.CriarCanvas("MenuCanvas", 200);
            canvas.transform.SetParent(transform, false);
            _root = canvas.transform;

            FundoCapa(_root);
            BotoesPrincipais(_root);
            // O cracha do COMANDANTE foi movido para dentro do jogo (HUD).
            PlacarRecorde(_root);
            IconesConquista(_root);
            IconesSociais(_root);
            Rodape(_root);
        }

        // placar de recorde/medalhas (canto superior esquerdo)
        private void PlacarRecorde(Transform root)
        {
            var go = new GameObject("Placar");
            go.transform.SetParent(root, false);
            var img = go.AddComponent<Image>();
            img.sprite = _panel; img.type = Image.Type.Sliced; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(20, -18);
            rt.sizeDelta = new Vector2(250, 70);

            UIFactory.TextoAncorado(go.transform, $"RECORDE: {SaveSystem.Recorde:N0}".Replace(",", "."), 19, GameConfig.Dourado,
                TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(16, 0), new Vector2(-10, -6), FontStyle.Bold).raycastTarget = false;
            UIFactory.TextoAncorado(go.transform, $"MEDALHAS: {SaveSystem.MedalhasTotais}", 16, new Color(0.85f, 0.9f, 1f),
                TextAnchor.MiddleLeft, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(16, 6), new Vector2(-10, 0)).raycastTarget = false;
        }

        // =====================================================
        //  Fundo (capa em tela cheia)
        // =====================================================
        private void FundoCapa(Transform root)
        {
            var bg = SpriteLibrary.Get("Backgrounds/bg_capa", 100f);
            var img = UIFactory.Imagem(root, bg, Vector2.zero, new Vector2(1920, 1080), Color.white);
            img.rectTransform.anchorMin = Vector2.zero; img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = Vector2.zero; img.rectTransform.offsetMax = Vector2.zero;
            img.preserveAspect = false;
            img.raycastTarget = false;

            // Sombreado da faixa dos botoes.
            // Antes: preto CHAPADO a 32% cobrindo 55% da tela -> apagava justamente o
            // nome do jogo (que esta pintado na arte entre 21.9% e 47.9% da altura).
            // Agora: DEGRADE de verdade, so na faixa dos botoes (0 -> 24% da altura),
            // opaco embaixo e ja transparente antes de encostar no titulo. Os botoes
            // ganham contraste e o titulo fica limpo e brilhante.
            var grad = UIFactory.Painel(root, new Vector2(0f, 0f), new Vector2(1f, ALTURA_TITULO),
                                        Vector2.zero, Vector2.zero, Color.white);
            var gimg = grad.GetComponent<Image>();
            gimg.sprite = GradienteInferior();
            gimg.type = Image.Type.Simple;
            gimg.raycastTarget = false;
        }

        /// <summary>Degrade vertical: transparente em cima, escuro embaixo (1px de largura, esticado).</summary>
        private static Sprite _gradCache;
        private static Sprite GradienteInferior()
        {
            if (_gradCache != null) return _gradCache;
            const int H = 64;
            var tex = new Texture2D(1, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < H; y++)
            {
                float t = 1f - (y / (float)(H - 1));      // 1 na base, 0 no topo
                float a = Mathf.SmoothStep(0f, 0.82f, t * t);
                tex.SetPixel(0, y, new Color(0.02f, 0.03f, 0.05f, a));
            }
            tex.Apply();
            _gradCache = Sprite.Create(tex, new Rect(0, 0, 1, H), new Vector2(0.5f, 0.5f), 100f);
            return _gradCache;
        }

        // =====================================================
        //  Botoes principais
        // =====================================================
        // O NOME DO JOGO ESTA PINTADO NA ARTE (bg_capa.png), nao e' um Text da UI.
        // Medido no pixel: o titulo "OPERACAO RESGATE" ocupa de 21.9% a 47.9% da ALTURA
        // da arte. Como a capa e' esticada para preencher a tela, essa fracao e' a mesma
        // em QUALQUER resolucao ou proporcao de janela.
        // O bug: os botoes eram posicionados em PIXEL ABSOLUTO a partir da base (y=40,
        // pilha de 300px de altura) -> subiam ate 31% da tela e cobriam o titulo.
        // A correcao: um TETO NORMALIZADO (20.5% da altura). A pilha de botoes pendura
        // a partir dele para BAIXO. Matematicamente impossivel encostar no titulo.
        private const float TETO_BOTOES   = 0.205f;  // teto da pilha (fracao da altura)
        private const float ALTURA_TITULO = 0.24f;   // onde comeca o nome do jogo (+ folga)

        private void BotoesPrincipais(Transform root)
        {
            Color textoEscuro = new Color(0.10f, 0.10f, 0.12f);
            Color textoClaro  = GameConfig.Creme;

            var cont = new GameObject("BotoesPrincipais");
            cont.transform.SetParent(root, false);
            var crt = cont.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, TETO_BOTOES);   // ancora em FRACAO da tela
            crt.anchorMax = new Vector2(0.5f, TETO_BOTOES);
            crt.pivot     = new Vector2(0.5f, 1f);            // pivo no TOPO: cresce p/ baixo
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(600f, 126f);

            // A pilha: JOGAR em destaque + 3 secundarios lado a lado (cabe na faixa livre
            // abaixo do titulo e da a JOGAR o peso visual que um menu principal pede).
            // Posicoes relativas ao CENTRO do container (BotaoMenu ancora em 0.5,0.5).
            const float ALT_PRIM = 66f, ALT_SEC = 48f;

            BotaoMenu(cont.transform, "JOGAR", _btnGold, IconeSprite("icon_jogar"),
                      new Vector2(0f, 30f), new Vector2(560f, ALT_PRIM), textoEscuro, 36,
                      () => GameManager.Instance.NovoJogo());

            BotaoMenu(cont.transform, "OPCOES", _btnDark, null,
                      new Vector2(-192f, -39f), new Vector2(176f, ALT_SEC), textoClaro, 24, AbrirOpcoes);

            BotaoMenu(cont.transform, "LOJA", _btnDark, null,
                      new Vector2(0f, -39f), new Vector2(176f, ALT_SEC), textoClaro, 24, AbrirLoja);

            BotaoMenu(cont.transform, "SAIR", _btnDark, null,
                      new Vector2(192f, -39f), new Vector2(176f, ALT_SEC), textoClaro, 24, Sair);
        }

        private Sprite IconeSprite(string nome) => SpriteLibrary.Get("Sprites/icons/" + nome, 100f);

        private Button BotaoMenu(Transform pai, string texto, Sprite bg, Sprite icone,
                                 Vector2 pos, Vector2 tam, Color corTexto, int tamFonte, UnityAction onClick)
        {
            var go = new GameObject("Btn_" + texto);
            go.transform.SetParent(pai, false);
            var img = go.AddComponent<Image>();
            img.sprite = bg; img.type = Image.Type.Sliced;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cores.selectedColor = Color.white;
            cores.fadeDuration = 0.1f;
            btn.colors = cores;
            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
                btn.onClick.AddListener(() => AudioManager.Instance?.Play("click"));
            }

            var label = UIFactory.Texto(go.transform, texto, tamFonte, corTexto,
                                        TextAnchor.MiddleCenter, Vector2.zero, tam, FontStyle.Bold);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(24, 0); label.rectTransform.offsetMax = new Vector2(-24, 0);
            label.raycastTarget = false;

            if (icone != null)
            {
                float ic = tam.y * 0.5f;
                // botao dourado: icone escuro; botoes escuros: icone dourado claro (premium)
                Color corIcone = corTexto.r < 0.5f ? corTexto : GameConfig.OuroSuave;
                var iconImg = UIFactory.Imagem(go.transform, icone,
                                               new Vector2(tam.x * 0.5f - 28f - ic * 0.5f, 0),
                                               new Vector2(ic, ic), corIcone);
                iconImg.raycastTarget = false;
            }

            go.AddComponent<HoverEffect>();
            return btn;
        }

        // =====================================================
        //  Cracha do comandante (canto superior direito)
        // =====================================================
        private void CrachaComandante(Transform root)
        {
            var go = new GameObject("CrachaComandante");
            go.transform.SetParent(root, false);
            var img = go.AddComponent<Image>();
            img.sprite = _panel; img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-24, -18);
            rt.sizeDelta = new Vector2(440, 96);

            // avatar (esquerda)
            var avatar = SpriteLibrary.Get("Sprites/icons/avatar_comandante", 100f);
            var avImg = UIFactory.Imagem(go.transform, avatar, Vector2.zero, new Vector2(68, 68), Color.white);
            var avrt = avImg.rectTransform;
            avrt.anchorMin = avrt.anchorMax = new Vector2(0, 0.5f);
            avrt.pivot = new Vector2(0, 0.5f);
            avrt.anchoredPosition = new Vector2(14, 0);
            avImg.raycastTarget = false;
            // moldura do avatar
            var frame = UIFactory.Imagem(go.transform, _avatarFrame, Vector2.zero, new Vector2(74, 74), GameConfig.Dourado);
            frame.type = Image.Type.Sliced;
            var frt = frame.rectTransform;
            frt.anchorMin = frt.anchorMax = new Vector2(0, 0.5f);
            frt.pivot = new Vector2(0, 0.5f);
            frt.anchoredPosition = new Vector2(11, 0);
            frame.raycastTarget = false;

            // textos
            UIFactory.TextoAncorado(go.transform, "COMANDANTE", 20, GameConfig.Creme, TextAnchor.LowerLeft,
                                    new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(92, 2), new Vector2(-58, -8), FontStyle.Bold);
            UIFactory.TextoAncorado(go.transform, "NIVEL 25", 15, new Color(0.7f, 0.78f, 0.9f), TextAnchor.UpperLeft,
                                    new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(92, -34), new Vector2(-58, -2));

            // barra de nivel (75%)
            var barFundo = new GameObject("BarFundo");
            barFundo.transform.SetParent(go.transform, false);
            var bfImg = barFundo.AddComponent<Image>();
            bfImg.sprite = _barBg; bfImg.type = Image.Type.Sliced; bfImg.raycastTarget = false;
            var bfrt = bfImg.rectTransform;
            bfrt.anchorMin = new Vector2(0, 0); bfrt.anchorMax = new Vector2(1, 0);
            bfrt.pivot = new Vector2(0.5f, 0);
            bfrt.offsetMin = new Vector2(92, 14); bfrt.offsetMax = new Vector2(-16, 30);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(barFundo.transform, false);
            var fImg = fill.AddComponent<Image>();
            fImg.sprite = _barFill; fImg.type = Image.Type.Sliced; fImg.color = GameConfig.Dourado;
            fImg.raycastTarget = false;
            var frt2 = fImg.rectTransform;
            frt2.anchorMin = Vector2.zero; frt2.anchorMax = Vector2.one;
            frt2.offsetMin = new Vector2(2, 2); frt2.offsetMax = new Vector2(-2, -2);
            fImg.type = Image.Type.Filled;
            fImg.fillMethod = Image.FillMethod.Horizontal;
            fImg.fillOrigin = 0;
            fImg.fillAmount = 0.75f;

            UIFactory.TextoAncorado(go.transform, "75%", 13, GameConfig.Creme, TextAnchor.MiddleRight,
                                    new Vector2(1, 0), new Vector2(1, 0), new Vector2(-20, 12), new Vector2(-2, 32), FontStyle.Bold);

            // insignia (estrela) no canto
            var star = UIFactory.Imagem(go.transform, IconeSprite("icon_star"), Vector2.zero, new Vector2(26, 26), GameConfig.Dourado);
            var srt = star.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 1);
            srt.anchoredPosition = new Vector2(-10, -8);
            star.raycastTarget = false;
        }

        // =====================================================
        //  Icones de conquista (canto inferior esquerdo)
        // =====================================================
        private void IconesConquista(Transform root)
        {
            var cont = new GameObject("Conquistas");
            cont.transform.SetParent(root, false);
            var rt = cont.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(24, 24);
            rt.sizeDelta = new Vector2(220, 64);

            string[] icones = { "icon_medal", "icon_trophy", "icon_clipboard" };
            string[] nomes  = { "Conquistas", "Ranking", "Missoes" };
            for (int i = 0; i < icones.Length; i++)
            {
                string nome = nomes[i];
                IconeBotaoQuadrado(cont.transform, IconeSprite(icones[i]), new Vector2(i * 72, 0),
                                   64, GameConfig.Creme, () => Toast(nome + " — em breve"));
            }
        }

        // =====================================================
        //  Icones de redes sociais (canto inferior direito)
        // =====================================================
        private void IconesSociais(Transform root)
        {
            var cont = new GameObject("RedesSociais");
            cont.transform.SetParent(root, false);
            var rt = cont.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-24, 26);
            rt.sizeDelta = new Vector2(260, 56);

            // da direita para a esquerda: GitHub, YouTube, Instagram, Facebook
            IconeBotaoQuadrado(cont.transform, IconeSprite("social_github"),    new Vector2(-0,   0), 54, GameConfig.Creme, () => Abrir(URL_GITHUB));
            IconeBotaoQuadrado(cont.transform, IconeSprite("social_youtube"),   new Vector2(-64,  0), 54, GameConfig.Creme, () => Abrir(URL_YOUTUBE));
            IconeBotaoQuadrado(cont.transform, IconeSprite("social_instagram"), new Vector2(-128, 0), 54, GameConfig.Creme, () => Abrir(URL_INSTAGRAM));
            IconeBotaoQuadrado(cont.transform, IconeSprite("social_facebook"),  new Vector2(-192, 0), 54, GameConfig.Creme, () => Abrir(URL_FACEBOOK));
        }

        private Button IconeBotaoQuadrado(Transform pai, Sprite icone, Vector2 pos, float tam, Color tint, UnityAction onClick)
        {
            var go = new GameObject("IconBtn");
            go.transform.SetParent(pai, false);
            var img = go.AddComponent<Image>();
            img.sprite = _iconBox; img.type = Image.Type.Sliced;
            var rt = img.rectTransform;
            // ancora ao canto do container conforme pivot do pai
            rt.anchorMin = rt.anchorMax = pai.GetComponent<RectTransform>().pivot;
            rt.pivot = pai.GetComponent<RectTransform>().pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(tam, tam);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cores = btn.colors;
            cores.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            cores.fadeDuration = 0.1f;
            btn.colors = cores;
            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
                btn.onClick.AddListener(() => AudioManager.Instance?.Play("click"));
            }

            if (icone != null)
            {
                var ic = UIFactory.Imagem(go.transform, icone, Vector2.zero, new Vector2(tam * 0.56f, tam * 0.56f), tint);
                ic.raycastTarget = false;
            }
            go.AddComponent<HoverEffect>();
            return btn;
        }

        private void Abrir(string url)
        {
            Application.OpenURL(url);
        }

        // =====================================================
        //  Modais: Opcoes / Loja / Como Jogar / Fases
        // =====================================================
        private GameObject CriarModal(string titulo)
        {
            FecharModal();
            var modal = new GameObject("Modal_" + titulo);
            modal.transform.SetParent(_root, false);
            var rt = modal.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            UIFactory.Painel(modal.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.02f, 0.03f, 0.06f, 0.93f));
            UIFactory.TextoAncorado(modal.transform, titulo, 48, GameConfig.Dourado, TextAnchor.MiddleCenter,
                                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -150), new Vector2(0, -78), FontStyle.Bold);
            _modalAtual = modal;
            return modal;
        }

        private void FecharModal()
        {
            if (_modalAtual != null) { Destroy(_modalAtual); _modalAtual = null; }
        }

        private void AbrirOpcoes()
        {
            var m = CriarModal("OPCOES");

            bool somLigado = AudioListener.volume > 0.01f;
            Button somBtn = null;
            somBtn = BotaoMenu(m.transform, somLigado ? "SOM: LIGADO" : "SOM: DESLIGADO", _btnDark, null,
                new Vector2(0, 60), new Vector2(460, 60), GameConfig.Creme, 26, null);
            somBtn.onClick.AddListener(() =>
            {
                AudioListener.volume = AudioListener.volume > 0.01f ? 0f : 1f;
                var t = somBtn.GetComponentInChildren<Text>();
                if (t != null) t.text = AudioListener.volume > 0.01f ? "SOM: LIGADO" : "SOM: DESLIGADO";
                AudioManager.Instance?.Play("click");
            });

            BotaoMenu(m.transform, "COMO JOGAR", _btnDark, null, new Vector2(0, -8), new Vector2(460, 58), GameConfig.Creme, 26, AbrirComoJogar);
            BotaoMenu(m.transform, "SELECIONAR FASE", _btnDark, null, new Vector2(0, -78), new Vector2(460, 58), GameConfig.Creme, 26, AbrirFases);
            BotaoMenu(m.transform, "RESETAR PROGRESSO", _btnDark, IconeSprite("icon_reset"), new Vector2(0, -148), new Vector2(460, 58),
                      new Color(1f, 0.7f, 0.6f), 24, ConfirmarReset);
            BotaoMenu(m.transform, "VOLTAR", _btnGold, null, new Vector2(0, -226), new Vector2(300, 56), new Color(0.1f,0.1f,0.12f), 26, FecharModal);
        }

        private void ConfirmarReset()
        {
            var m = CriarModal("RESETAR PROGRESSO");
            UIFactory.TextoAncorado(m.transform,
                "Isto vai apagar TODO o progresso:\nfases desbloqueadas, recorde e medalhas.\n\nTem certeza?",
                26, new Color(1f, 0.88f, 0.85f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440, -30), new Vector2(440, 120));
            BotaoMenu(m.transform, "SIM, APAGAR", _btnDark, null, new Vector2(-130, -150), new Vector2(280, 60),
                      new Color(1f, 0.6f, 0.55f), 26, () => { SaveSystem.Resetar(); Toast("Progresso apagado"); AbrirOpcoes(); });
            BotaoMenu(m.transform, "CANCELAR", _btnGold, null, new Vector2(170, -150), new Vector2(260, 60),
                      new Color(0.1f,0.1f,0.12f), 26, AbrirOpcoes);
        }

        private void AbrirLoja()
        {
            var m = CriarModal("LOJA");
            UIFactory.TextoAncorado(m.transform,
                "Arsenal e melhorias chegam em breve.\n\nAqui voce vai poder desbloquear novas armas,\ncoletes, drones de apoio e habilidades para o soldado.",
                26, new Color(0.88f, 0.92f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-440, -40), new Vector2(440, 120));
            // vitrine ilustrativa com icones
            string[] icones = { "icon_medal", "icon_star", "icon_trophy" };
            for (int i = 0; i < icones.Length; i++)
                IconeBotaoQuadrado(m.transform, IconeSprite(icones[i]), new Vector2(-110 + i * 110, -30), 80, GameConfig.Dourado, () => Toast("Em breve"));
            BotaoMenu(m.transform, "VOLTAR", _btnGold, null, new Vector2(0, -240), new Vector2(300, 58), new Color(0.1f,0.1f,0.12f), 26, FecharModal);
        }

        private void AbrirComoJogar()
        {
            var m = CriarModal("COMO JOGAR");
            string ajuda =
                "MOVIMENTO\n" +
                "A / D ou setas .......... andar\n" +
                "SHIFT (segurar) ......... correr\n" +
                "ESPACO .................. pular (no ar = duplo salto)\n" +
                "S / seta baixo .......... agachar\n" +
                "W / seta cima (escada) .. escalar\n\n" +
                "COMBATE\n" +
                "J ou clique esquerdo .... atirar\n" +
                "E ....................... interagir\n\n" +
                "SISTEMA\n" +
                "ESC ou P ................ pausar\n\n" +
                "OBJETIVO\n" +
                "Atravesse a zona contaminada, derrote os mutantes e maquinas,\n" +
                "colete suprimentos e alcance o ponto de extracao. Na fase final,\n" +
                "desative o nucleo de comando inimigo para concluir o resgate.";
            UIFactory.TextoAncorado(m.transform, ajuda, 22, GameConfig.Creme, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, -210), new Vector2(460, 250));
            BotaoMenu(m.transform, "VOLTAR", _btnGold, null, new Vector2(0, -350), new Vector2(300, 58), new Color(0.1f,0.1f,0.12f), 26, AbrirOpcoes);
        }

        private void AbrirFases()
        {
            var m = CriarModal("SELECIONAR FASE");
            string[] nomes = { "1 - Favela Contaminada", "2 - Ruinas Urbanas", "3 - Campo Radioativo", "4 - Linha de Frente", "5 - Sala de Comando" };
            for (int i = 0; i < nomes.Length; i++)
            {
                int idx = i;
                bool liberada = SaveSystem.FaseLiberada(idx);
                if (liberada)
                {
                    BotaoMenu(m.transform, nomes[i], _btnDark, null, new Vector2(0, 140 - i * 74), new Vector2(560, 62),
                              GameConfig.Creme, 26, () => GameManager.Instance.NivelAtualInicial(idx));
                }
                else
                {
                    var b = BotaoMenu(m.transform, nomes[i], _btnDark, IconeSprite("icon_lock"),
                                      new Vector2(0, 140 - i * 74), new Vector2(560, 62),
                                      new Color(0.55f, 0.57f, 0.62f), 25, () => Toast("Conclua a fase anterior para liberar"));
                    var img = b.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.5f, 0.5f, 0.55f, 1f);
                }
            }
            BotaoMenu(m.transform, "VOLTAR", _btnGold, null, new Vector2(0, 140 - nomes.Length * 74 - 16), new Vector2(300, 58),
                      new Color(0.1f,0.1f,0.12f), 26, AbrirOpcoes);
        }

        // =====================================================
        //  Toast (aviso rapido)
        // =====================================================
        private void Toast(string msg)
        {
            var go = new GameObject("Toast");
            go.transform.SetParent(_root, false);
            var img = go.AddComponent<Image>();
            img.sprite = _panel; img.type = Image.Type.Sliced; img.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -120);
            rt.sizeDelta = new Vector2(460, 70);
            UIFactory.TextoAncorado(go.transform, msg, 24, GameConfig.Creme, TextAnchor.MiddleCenter,
                                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, FontStyle.Bold);
            Destroy(go, 1.6f);
        }

        // CARIMBO DE BUILD. Se o rodape do menu NAO mostrar este texto, o Unity NAO esta
        // rodando este codigo (provavel erro vermelho no Console -> ele usa o ultimo build
        // valido e ignora qualquer mudanca).
        public const string BUILD = "FIX-04";

        private void Rodape(Transform root)
        {
            UIFactory.TextoAncorado(root, "UniFECAF · Game Development · OPERACAO RESGATE  ·  build " + BUILD, 16,
                new Color(1f, 0.82f, 0.32f, 0.95f), TextAnchor.LowerCenter,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 6), new Vector2(0, 26)).raycastTarget = false;
        }

        private void Sair()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
