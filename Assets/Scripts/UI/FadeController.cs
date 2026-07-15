using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>
    /// Transicoes de tela (fade preto). Mantem um canvas dedicado no topo de tudo.
    /// Metodos estaticos retornam IEnumerator para uso em corrotinas do GameManager.
    /// </summary>
    public class FadeController : MonoBehaviour
    {
        private static FadeController _inst;
        private Image _img;

        private static FadeController Instancia()
        {
            if (_inst != null) return _inst;
            var go = new GameObject("FadeController");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<FadeController>();
            _inst.Montar();
            return _inst;
        }

        private void Montar()
        {
            var canvas = UIFactory.CriarCanvas("FadeCanvas", 5000);
            canvas.transform.SetParent(transform, false);
            var rt = UIFactory.Painel(canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0,0,0,0));
            _img = rt.GetComponent<Image>();
            _img.raycastTarget = false;
        }

        public static IEnumerator FadeOut(float dur)
        {
            var f = Instancia();
            yield return f.Anim(0f, 1f, dur);
        }

        public static IEnumerator FadeIn(float dur)
        {
            var f = Instancia();
            yield return f.Anim(1f, 0f, dur);
        }

        private IEnumerator Anim(float de, float para, float dur)
        {
            float t = 0f;
            Color c = _img.color;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(de, para, t / dur);
                _img.color = new Color(0, 0, 0, a);
                yield return null;
            }
            _img.color = new Color(0, 0, 0, para);
        }
    }
}
