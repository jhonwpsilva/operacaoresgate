using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>
    /// Tela de derrota: arte do campo de batalha, mensagem de falha e opcoes de
    /// tentar novamente (reinicia a fase) ou voltar ao menu.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        void Start()
        {
            var canvas = UIFactory.CriarCanvas("GameOverCanvas", 400);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            var bg = SpriteLibrary.Get("Backgrounds/bg_defeat", 100f);
            var bgImg = UIFactory.Imagem(root, bg, Vector2.zero, new Vector2(1920,1080), Color.white);
            bgImg.rectTransform.anchorMin = Vector2.zero; bgImg.rectTransform.anchorMax = Vector2.one;
            bgImg.rectTransform.offsetMin = Vector2.zero; bgImg.rectTransform.offsetMax = Vector2.zero;
            bgImg.preserveAspect = false;
            UIFactory.Painel(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.06f,0.02f,0.02f,0.55f));

            UIFactory.TextoAncorado(root, "MISSAO FRACASSADA", 80, new Color(0,0,0,0.7f), TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(4,-204), new Vector2(4,-104), FontStyle.Bold);
            UIFactory.TextoAncorado(root, "MISSAO FRACASSADA", 80, GameConfig.VermelhoAlerta, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-200), new Vector2(0,-100), FontStyle.Bold);
            UIFactory.TextoAncorado(root, "O soldado caiu em combate. Reagrupe e tente novamente.", 26,
                GameConfig.Creme, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-262), new Vector2(0,-212));

            UIFactory.Botao(root, "TENTAR NOVAMENTE", new Vector2(0,-160), new Vector2(420, 66),
                GameConfig.AzulPremium, GameConfig.Creme, () => GameManager.Instance.ReiniciarFase());
            UIFactory.Botao(root, "MENU PRINCIPAL", new Vector2(0,-238), new Vector2(420, 66),
                new Color(0.3f,0.1f,0.1f,0.95f), GameConfig.Creme, () => GameManager.Instance.AbrirMenu());

            UIFactory.TextoAncorado(root, "ENTER para tentar novamente", 18, new Color(1,1,1,0.65f), TextAnchor.LowerCenter,
                new Vector2(0,0), new Vector2(1,0), new Vector2(0,30), new Vector2(0,54));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                GameManager.Instance.ReiniciarFase();
        }
    }
}
