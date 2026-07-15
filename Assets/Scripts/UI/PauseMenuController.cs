using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>Menu de pausa: continuar, reiniciar fase ou voltar ao menu principal.</summary>
    public class PauseMenuController : MonoBehaviour
    {
        void Start()
        {
            var canvas = UIFactory.CriarCanvas("PauseCanvas", 300);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Painel(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.02f,0.04f,0.08f,0.82f));

            UIFactory.TextoAncorado(root, "PAUSA", 64, GameConfig.Dourado, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-260), new Vector2(0,-150), FontStyle.Bold);

            float by = 40f; float dy = 78f;
            UIFactory.Botao(root, "CONTINUAR", new Vector2(0, by), new Vector2(380, 64),
                GameConfig.AzulPremium, GameConfig.Creme, () => GameManager.Instance.Retomar());
            UIFactory.Botao(root, "REINICIAR FASE", new Vector2(0, by - dy), new Vector2(380, 64),
                new Color(0.16f,0.22f,0.34f,0.95f), GameConfig.Creme, () => GameManager.Instance.ReiniciarFase());
            UIFactory.Botao(root, "MENU PRINCIPAL", new Vector2(0, by - dy*2), new Vector2(380, 64),
                new Color(0.4f,0.12f,0.12f,0.95f), GameConfig.Creme, () => GameManager.Instance.AbrirMenu());

            UIFactory.TextoAncorado(root, "ESC para continuar", 18, new Color(1,1,1,0.6f), TextAnchor.LowerCenter,
                new Vector2(0,0), new Vector2(1,0), new Vector2(0,30), new Vector2(0,54));
        }
    }
}
