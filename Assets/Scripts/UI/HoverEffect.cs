using UnityEngine;
using UnityEngine.EventSystems;

namespace OperacaoResgate
{
    /// <summary>
    /// Efeito de interatividade para elementos de UI: aumenta levemente a escala e
    /// toca um som suave ao passar o mouse (hover), voltando ao normal ao sair.
    /// Dá "vida" e feedback aos botões e ícones do menu.
    /// </summary>
    public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        public float escalaHover = 1.08f;
        public float velocidade = 12f;
        public bool tocarSom = true;

        private Vector3 _escalaBase;
        private Vector3 _escalaAlvo;
        private bool _iniciado;

        void OnEnable()
        {
            if (!_iniciado)
            {
                _escalaBase = transform.localScale;
                _escalaAlvo = _escalaBase;
                _iniciado = true;
            }
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _escalaAlvo, velocidade * Time.unscaledDeltaTime);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _escalaAlvo = _escalaBase * escalaHover;
            if (tocarSom) AudioManager.Instance?.Play("hover", 0.6f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _escalaAlvo = _escalaBase;
        }

        public void OnPointerDown(PointerEventData e)
        {
            transform.localScale = _escalaBase * 0.94f;
        }
    }
}
