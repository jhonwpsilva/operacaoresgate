using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace OperacaoResgate
{
    /// <summary>
    /// Fabrica de elementos de UI (uGUI) construidos 100% por codigo: canvas, paineis,
    /// textos, botoes e barras. Garante fonte legivel mesmo sem assets de fonte importados.
    /// </summary>
    public static class UIFactory
    {
        private static Font _fonte;

        public static Font Fonte()
        {
            if (_fonte != null) return _fonte;
            // tenta a fonte embutida do Unity; cai para Arial dinamica
            _fonte = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_fonte == null) _fonte = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_fonte == null) _fonte = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return _fonte;
        }

        public static Canvas CriarCanvas(string nome, int ordem)
        {
            var go = new GameObject(nome);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordem;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Painel(Transform pai, Vector2 anchorMin, Vector2 anchorMax,
                                           Vector2 offsetMin, Vector2 offsetMax, Color cor)
        {
            var go = new GameObject("Painel");
            go.transform.SetParent(pai, false);
            var img = go.AddComponent<Image>();
            img.color = cor;
            var rt = img.rectTransform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        public static Image Imagem(Transform pai, Sprite sprite, Vector2 pos, Vector2 tam, Color cor)
        {
            var go = new GameObject("Img");
            go.transform.SetParent(pai, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.color = cor;
            img.preserveAspect = true;
            if (sprite == null) img.color = cor;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
            return img;
        }

        public static Text Texto(Transform pai, string txt, int tamanho, Color cor,
                                 TextAnchor align, Vector2 pos, Vector2 tam, FontStyle estilo = FontStyle.Normal)
        {
            var go = new GameObject("Texto");
            go.transform.SetParent(pai, false);
            var t = go.AddComponent<Text>();
            t.text = txt; t.font = Fonte(); t.fontSize = tamanho; t.color = cor;
            t.alignment = align; t.fontStyle = estilo;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;
            return t;
        }

        public static Text TextoAncorado(Transform pai, string txt, int tamanho, Color cor,
                                          TextAnchor align, Vector2 anchorMin, Vector2 anchorMax,
                                          Vector2 offsetMin, Vector2 offsetMax, FontStyle estilo = FontStyle.Normal)
        {
            var go = new GameObject("Texto");
            go.transform.SetParent(pai, false);
            var t = go.AddComponent<Text>();
            t.text = txt; t.font = Fonte(); t.fontSize = tamanho; t.color = cor;
            t.alignment = align; t.fontStyle = estilo;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return t;
        }

        public static Button Botao(Transform pai, string txt, Vector2 pos, Vector2 tam,
                                   Color corFundo, Color corTexto, UnityAction aoClicar, int fonte = 30)
        {
            var go = new GameObject("Botao");
            go.transform.SetParent(pai, false);
            var img = go.AddComponent<Image>();
            img.color = corFundo;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = tam;

            var btn = go.AddComponent<Button>();
            var cores = btn.colors;
            cores.normalColor = corFundo;
            cores.highlightedColor = Color.Lerp(corFundo, Color.white, 0.2f);
            cores.pressedColor = Color.Lerp(corFundo, Color.black, 0.2f);
            cores.selectedColor = cores.highlightedColor;
            cores.fadeDuration = 0.08f;
            btn.colors = cores;
            if (aoClicar != null) btn.onClick.AddListener(aoClicar);

            var label = Texto(go.transform, txt, fonte, corTexto, TextAnchor.MiddleCenter, Vector2.zero, tam, FontStyle.Bold);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero; label.rectTransform.offsetMax = Vector2.zero;

            return btn;
        }

        /// <summary>Barra de progresso simples (fundo + preenchimento). Retorna a Image de preenchimento.</summary>
        public static Image Barra(Transform pai, Vector2 anchorMin, Vector2 anchorMax,
                                  Vector2 offsetMin, Vector2 offsetMax, Color corFundo, Color corFrente,
                                  out RectTransform fillRT)
        {
            var fundo = Painel(pai, anchorMin, anchorMax, offsetMin, offsetMax, corFundo);
            fundo.gameObject.name = "BarraFundo";

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fundo, false);
            var fill = fillGO.AddComponent<Image>();
            fill.color = corFrente;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
            fillRT = frt;
            return fill;
        }
    }
}
