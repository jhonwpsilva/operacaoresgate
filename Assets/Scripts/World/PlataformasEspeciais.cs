using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Plataforma QUEBRAVEL. Ao ser pisada, comeca a rachar (tremor + cor avermelhada) e,
    /// apos um instante, QUEBRA: o colisor some, o bloco despenca e some. Reaparece depois
    /// de alguns segundos para o jogador tentar de novo. Anexada a um cubo pelo LevelBuilder.
    /// </summary>
    public class PlataformaQuebravel : MonoBehaviour
    {
        private MeshRenderer _mr;
        private Collider _col;
        private Color _corBase;
        private Vector3 _posBase;
        private bool _rachando, _caiu;
        private float _tempoAte = 0.8f;
        private float _respawn = 4f;

        public void Configurar()
        {
            _mr = GetComponent<MeshRenderer>();
            _col = GetComponent<Collider>();
            _posBase = transform.position;
            if (_mr != null) _corBase = _mr.material.color;
        }

        void OnCollisionStay(Collision c)
        {
            if (_rachando || _caiu) return;
            if (c.collider.GetComponent<PlayerController>() != null &&
                c.transform.position.y > transform.position.y)
                StartCoroutine(Rachar());
        }

        private IEnumerator Rachar()
        {
            _rachando = true;
            AudioManager.Instance?.PlayAt("granada_quique", transform.position, 0.4f);
            float t = 0f;
            while (t < _tempoAte)
            {
                t += Time.deltaTime;
                float shake = Mathf.Sin(t * 60f) * 0.04f;
                transform.position = _posBase + new Vector3(shake, 0, 0);
                if (_mr != null) _mr.material.color = Color.Lerp(_corBase, new Color(0.8f, 0.3f, 0.2f), t / _tempoAte);
                yield return null;
            }
            StartCoroutine(Cair());
        }

        private IEnumerator Cair()
        {
            _caiu = true; _rachando = false;
            if (_col != null) _col.enabled = false;
            AudioManager.Instance?.PlayAt("explosion", transform.position, 0.25f);
            FX.Fumaca(transform.position, 3);

            float vy = 0f, t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime; vy += 14f * Time.deltaTime;
                transform.position += Vector3.down * vy * Time.deltaTime;
                yield return null;
            }
            if (_mr != null) _mr.enabled = false;
            foreach (Transform ch in transform) ch.gameObject.SetActive(false);

            yield return new WaitForSeconds(_respawn);
            // reaparece
            transform.position = _posBase;
            if (_mr != null) { _mr.enabled = true; _mr.material.color = _corBase; }
            if (_col != null) _col.enabled = true;
            foreach (Transform ch in transform) ch.gameObject.SetActive(true);
            FX.Flash(_posBase, new Color(0.5f, 0.8f, 1f), 3f, 2f, 0.3f);
            _caiu = false;
        }
    }

    /// <summary>
    /// Plataforma QUE CAI: firme por um instante depois que voce pisa, entao DESPENCA de
    /// vez. Reaparece no lugar apos alguns segundos. Otima para travessias de timing.
    /// </summary>
    public class PlataformaQueCai : MonoBehaviour
    {
        private MeshRenderer _mr;
        private Collider _col;
        private Vector3 _posBase;
        private bool _acionada;
        private float _atraso = 0.45f;
        private float _respawn = 3.5f;

        public void Configurar()
        {
            _mr = GetComponent<MeshRenderer>();
            _col = GetComponent<Collider>();
            _posBase = transform.position;
        }

        void OnCollisionStay(Collision c)
        {
            if (_acionada) return;
            if (c.collider.GetComponent<PlayerController>() != null &&
                c.transform.position.y > transform.position.y)
                StartCoroutine(Sequencia());
        }

        private IEnumerator Sequencia()
        {
            _acionada = true;
            // tremor de aviso
            float t = 0f; Vector3 b = _posBase;
            while (t < _atraso)
            {
                t += Time.deltaTime;
                transform.position = b + new Vector3(Mathf.Sin(t * 70f) * 0.05f, 0, 0);
                yield return null;
            }
            if (_col != null) _col.enabled = false;
            AudioManager.Instance?.PlayAt("granada_quique", transform.position, 0.4f);

            float vy = 0f, td = 0f;
            while (td < 1.3f)
            {
                td += Time.deltaTime; vy += 15f * Time.deltaTime;
                transform.position += Vector3.down * vy * Time.deltaTime;
                yield return null;
            }
            if (_mr != null) _mr.enabled = false;
            foreach (Transform ch in transform) ch.gameObject.SetActive(false);

            yield return new WaitForSeconds(_respawn);
            transform.position = _posBase;
            if (_mr != null) _mr.enabled = true;
            if (_col != null) _col.enabled = true;
            foreach (Transform ch in transform) ch.gameObject.SetActive(true);
            FX.Flash(_posBase, new Color(0.5f, 0.8f, 1f), 3f, 2f, 0.3f);
            _acionada = false;
        }
    }

    /// <summary>
    /// Plataforma ELEVATORIA suave: sobe e desce entre dois pontos usando uma CURVA DE
    /// ANIMACAO (aceleracao/desaceleracao), sem trancos. Carrega o jogador junto. Combina
    /// com a plataforma movel para criar percursos verticais elegantes.
    /// </summary>
    public class PlataformaElevatoria : MonoBehaviour
    {
        private Vector3 _a, _b;
        private float _velocidade = 1.5f;
        private float _t;
        private int _dir = 1;
        private Vector3 _ultima;
        private Transform _passageiro;
        private AnimationCurve _curva;

        public void Configurar(Vector3 centro, float altura, float velocidade)
        {
            _a = centro;
            _b = centro + Vector3.up * altura;
            _velocidade = velocidade;
            _curva = AnimationCurve.EaseInOut(0, 0, 1, 1);
            transform.position = _a;
            _ultima = _a;
        }

        void FixedUpdate()
        {
            _t += _dir * _velocidade * 0.25f * Time.fixedDeltaTime;
            if (_t >= 1f) { _t = 1f; _dir = -1; }
            else if (_t <= 0f) { _t = 0f; _dir = 1; }

            float k = _curva.Evaluate(_t);
            Vector3 nova = Vector3.Lerp(_a, _b, k);
            Vector3 delta = nova - _ultima;
            transform.position = nova;

            if (_passageiro != null)
            {
                var rb = _passageiro.GetComponent<Rigidbody>();
                if (rb != null) rb.position += new Vector3(0, delta.y, 0);
            }
            _ultima = nova;
        }

        void OnCollisionStay(Collision c)
        {
            if (c.collider.GetComponent<PlayerController>() != null &&
                c.transform.position.y > transform.position.y + 0.1f)
                _passageiro = c.transform;
        }
        void OnCollisionExit(Collision c)
        {
            if (c.collider.GetComponent<PlayerController>() != null && c.transform == _passageiro)
                _passageiro = null;
        }
    }
}
