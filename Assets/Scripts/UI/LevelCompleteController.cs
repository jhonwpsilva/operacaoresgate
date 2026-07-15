using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>Tela exibida ao concluir uma fase (nao-final): mostra resumo e segue.</summary>
    public class LevelCompleteController : MonoBehaviour
    {
        void Start()
        {
            var canvas = UIFactory.CriarCanvas("FaseCompletaCanvas", 300);
            canvas.transform.SetParent(transform, false);
            var root = canvas.transform;

            UIFactory.Painel(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.02f,0.06f,0.04f,0.85f));

            var gm = GameManager.Instance;
            string nomeFase = gm != null && gm.NivelData != null ? gm.NivelData.nome : "";

            UIFactory.TextoAncorado(root, "SETOR LIBERADO", 60, GameConfig.VerdeHUD, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-240), new Vector2(0,-150), FontStyle.Bold);
            UIFactory.TextoAncorado(root, nomeFase, 30, GameConfig.Creme, TextAnchor.MiddleCenter,
                new Vector2(0,1), new Vector2(1,1), new Vector2(0,-300), new Vector2(0,-250));

            if (gm != null)
            {
                string resumo = $"Pontos: {gm.Pontos:N0}\nSuprimentos coletados: {gm.ItensColetados}/{gm.ItensNoNivel}\nVidas restantes: {gm.Vidas}";
                UIFactory.TextoAncorado(root, resumo, 26, GameConfig.Creme, TextAnchor.MiddleCenter,
                    new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(-300,-40), new Vector2(300,90));
            }

            UIFactory.Botao(root, "PROXIMO SETOR →", new Vector2(0,-170), new Vector2(420, 66),
                GameConfig.AzulPremium, GameConfig.Creme, () => GameManager.Instance.ProximaFase());

            UIFactory.TextoAncorado(root, "ENTER para avancar", 18, new Color(1,1,1,0.6f), TextAnchor.LowerCenter,
                new Vector2(0,0), new Vector2(1,0), new Vector2(0,30), new Vector2(0,54));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                GameManager.Instance.ProximaFase();
        }
    }
}
