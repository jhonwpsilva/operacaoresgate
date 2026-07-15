using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>
    /// Tela de vitoria final: arte do soldado comemorando, mensagem "MISSAO CONCLUIDA",
    /// pontuacao final e opcoes de jogar de novo ou voltar ao menu.
    /// </summary>
    public class VictoryController : MonoBehaviour
    {
        void Start()
        {
            var canvas = UIFactory.CriarCanvas("VitoriaCanvas", 400);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            var bg = SpriteLibrary.Get("Backgrounds/bg_victory", 100f);
            var bgImg = UIFactory.Imagem(root, bg, Vector2.zero, new Vector2(1920,1080), Color.white);
            bgImg.rectTransform.anchorMin = Vector2.zero; bgImg.rectTransform.anchorMax = Vector2.one;
            bgImg.rectTransform.offsetMin = Vector2.zero; bgImg.rectTransform.offsetMax = Vector2.zero;
            bgImg.preserveAspect = false;
            UIFactory.Painel(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f,0.06f,0.02f,0.35f));

            // sombra + texto dourado
            UIFactory.TextoAncorado(root, "MISSAO CONCLUIDA", 84, new Color(0,0,0,0.7f), TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(4,-204), new Vector2(4,-104), FontStyle.Bold);
            UIFactory.TextoAncorado(root, "MISSAO CONCLUIDA", 84, GameConfig.Dourado, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-200), new Vector2(0,-100), FontStyle.Bold);
            UIFactory.TextoAncorado(root, "O resgate foi um sucesso. A paz comeca a ser reconstruida.", 26,
                GameConfig.Creme, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-262), new Vector2(0,-212));

            var gm = GameManager.Instance;
            if (gm != null)
                UIFactory.TextoAncorado(root, $"Pontuacao final: {gm.Pontos:N0}", 34, GameConfig.VerdeHUD, TextAnchor.MiddleCenter,
                    new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-300,-10), new Vector2(300,50), FontStyle.Bold);

            UIFactory.Botao(root, "JOGAR NOVAMENTE", new Vector2(0,-180), new Vector2(400, 64),
                GameConfig.AzulPremium, GameConfig.Creme, () => GameManager.Instance.ReiniciarJogo());
            UIFactory.Botao(root, "MENU PRINCIPAL", new Vector2(0,-256), new Vector2(400, 64),
                new Color(0.16f,0.22f,0.34f,0.95f), GameConfig.Creme, () => GameManager.Instance.AbrirMenu());
        }
    }
}
