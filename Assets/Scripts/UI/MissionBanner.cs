using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>
    /// Banner exibido no inicio de cada fase, anunciando a missao e o objetivo.
    /// Aparece suavemente, fica alguns segundos e some — dando "sentido" a missao.
    /// </summary>
    public class MissionBanner : MonoBehaviour
    {
        public static void Mostrar(Transform pai, int numeroFase, string nome, string objetivo)
        {
            var go = new GameObject("MissionBanner");
            go.transform.SetParent(pai, false);
            go.AddComponent<MissionBanner>().Construir(numeroFase, nome, objetivo);
        }

        private CanvasGroup _cg;

        private void Construir(int numeroFase, string nome, string objetivo)
        {
            var canvas = UIFactory.CriarCanvas("BannerCanvas", 260);
            canvas.transform.SetParent(transform, false);
            _cg = canvas.gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;

            // faixa central escura
            var faixa = UIFactory.Painel(canvas.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                                         new Vector2(0, -90), new Vector2(0, 90), new Color(0.02f, 0.03f, 0.06f, 0.82f));

            // linha dourada superior e inferior
            UIFactory.Painel(canvas.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                             new Vector2(0, 88), new Vector2(0, 92), GameConfig.Dourado);
            UIFactory.Painel(canvas.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                             new Vector2(0, -92), new Vector2(0, -88), GameConfig.Dourado);

            UIFactory.TextoAncorado(canvas.transform, $"MISSAO {numeroFase}", 26, GameConfig.Dourado,
                TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(0, 36), new Vector2(0, 78), FontStyle.Bold);

            UIFactory.TextoAncorado(canvas.transform, nome.ToUpper(), 44, GameConfig.Creme,
                TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(0, -8), new Vector2(0, 40), FontStyle.Bold);

            UIFactory.TextoAncorado(canvas.transform, objetivo, 22, new Color(0.82f, 0.88f, 1f),
                TextAnchor.MiddleCenter, new Vector2(0, 0.5f), new Vector2(1, 0.5f),
                new Vector2(0, -64), new Vector2(0, -16));

            StartCoroutine(Animar());
        }

        private IEnumerator Animar()
        {
            // fade in
            float t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; _cg.alpha = t / 0.4f; yield return null; }
            _cg.alpha = 1f;
            // segura
            yield return new WaitForSeconds(2.6f);
            // fade out
            t = 0f;
            while (t < 0.5f) { t += Time.deltaTime; _cg.alpha = 1f - t / 0.5f; yield return null; }
            Destroy(gameObject);
        }
    }
}
