using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Plataforma que vai-e-volta entre dois pontos. Leva o jogador junto quando ele
    /// esta em cima (carry), usando deteccao por contato.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        public Vector3 deslocamento = new Vector3(0, 3, 0);
        public float velocidade = 2f;

        private Vector3 _a, _b;
        private float _t;
        private Vector3 _ultimaPos;
        private Transform _passageiro;

        public void Configurar(Vector3 centro, Vector3 desloc, float vel)
        {
            _a = centro - desloc * 0.5f;
            _b = centro + desloc * 0.5f;
            deslocamento = desloc; velocidade = vel;
            transform.position = _a;
            _ultimaPos = transform.position;
        }

        void FixedUpdate()
        {
            _t += Time.fixedDeltaTime * velocidade * 0.5f;
            float p = (Mathf.Sin(_t) + 1f) * 0.5f;
            Vector3 nova = Vector3.Lerp(_a, _b, p);
            Vector3 delta = nova - _ultimaPos;

            transform.position = nova;

            // carregar o jogador
            if (_passageiro != null)
            {
                var rb = _passageiro.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.position += new Vector3(delta.x, delta.y, 0);
            }
            _ultimaPos = nova;
        }

        void OnCollisionStay(Collision c)
        {
            if (c.collider.GetComponent<PlayerController>() != null)
            {
                // so carrega se o player estiver acima
                if (c.transform.position.y > transform.position.y + 0.1f)
                    _passageiro = c.transform;
            }
        }
        void OnCollisionExit(Collision c)
        {
            if (c.collider.GetComponent<PlayerController>() != null && c.transform == _passageiro)
                _passageiro = null;
        }
    }
}
