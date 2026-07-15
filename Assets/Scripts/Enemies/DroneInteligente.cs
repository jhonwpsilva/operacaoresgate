using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Drone de combate inteligente que VOA SUAVEMENTE. Comportamento:
    ///  - PATRULHA uma faixa horizontal com flutuacao senoidal.
    ///  - DETECTA o jogador num raio e passa a segui-lo pelo alto.
    ///  - ATIRA rajadas de laser quando alinhado.
    ///  - DESVIA DE OBSTACULOS: lanca raios curtos (frente/baixo) e sobe/contorna quando
    ///    encontra parede ou plataforma no caminho.
    ///  - EXPLODE ao morrer (queda + estouro). Pode ser abatido (IDanificavel).
    /// </summary>
    public class DroneInteligente : MonoBehaviour, IDanificavel
    {
        public float vida = 60f;
        private float _range = 6f;
        private Vector3 _origem;
        private bool _direita = true;
        private bool _alerta;
        private bool _morto;
        private float _cdTiro;
        private float _fase;
        private float _alturaVoo = 4.5f;

        private Transform _visual;
        private SpriteRenderer _sr;
        private Light _luz;

        public void Configurar(Sprite sprite, Vector3 origem, float range)
        {
            _origem = origem; _range = Mathf.Max(4f, range);
            _alturaVoo = Mathf.Max(3.5f, origem.y);

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 16;
            if (sprite != null)
            {
                float alvo = 1.4f, maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
            }
            _visual.gameObject.AddComponent<Billboard>();

            _luz = new GameObject("luzDrone").AddComponent<Light>();
            _luz.transform.SetParent(transform, false);
            _luz.transform.localPosition = new Vector3(0, -0.3f, 0);
            _luz.color = new Color(0.4f, 0.8f, 1f); _luz.range = 4f; _luz.intensity = 1.8f;

            var col = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.7f; col.isTrigger = true;

            transform.position = new Vector3(origem.x, _alturaVoo, 0);
            _cdTiro = Random.Range(0.8f, 1.8f);
        }

        void Update()
        {
            if (_morto) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            if (_cdTiro > 0f) _cdTiro -= Time.deltaTime;
            _fase += Time.deltaTime;

            var player = GameManager.Instance.Player;
            Vector3 pos = transform.position;

            // deteccao simples por raio
            if (player != null)
            {
                float dist = Vector3.Distance(pos, player.transform.position);
                if (dist < 13f) { _alerta = true; }
                else if (dist > 20f) _alerta = false;
            }

            float alvoX, alvoY;
            if (_alerta && player != null)
            {
                alvoX = player.transform.position.x;
                alvoY = player.transform.position.y + 3.5f;
                _direita = alvoX >= pos.x;
                float dist = Mathf.Abs(alvoX - pos.x);
                if (_cdTiro <= 0f && dist < 12f && Mathf.Abs((player.transform.position.y) - pos.y) < 6f)
                {
                    DispararLaser(player);
                    _cdTiro = 1.1f;
                }
            }
            else
            {
                // patrulha
                float osc = Mathf.Sin(_fase * 0.8f) * _range;
                alvoX = _origem.x + osc;
                alvoY = _alturaVoo;
                _direita = Mathf.Cos(_fase * 0.8f) >= 0;
            }

            // ---- desvio de obstaculos ----
            Vector3 dirH = new Vector3(_direita ? 1f : -1f, 0, 0);
            if (Physics.Raycast(pos, dirH, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                alvoY += 3f;   // parede na frente -> sobe
            if (Physics.Raycast(pos, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                alvoY = Mathf.Max(alvoY, pos.y + 2f); // chao/plataforma perto -> sobe

            // voo suave + flutuacao
            pos.x = Mathf.Lerp(pos.x, alvoX, 2.2f * Time.deltaTime);
            float bob = Mathf.Sin(_fase * 3f) * 0.25f;
            pos.y = Mathf.Lerp(pos.y, alvoY, 2.6f * Time.deltaTime) + bob * Time.deltaTime * 4f;
            transform.position = pos;

            if (_visual != null)
            {
                var s = _visual.localScale; s.x = Mathf.Abs(s.x) * (_direita ? 1f : -1f); _visual.localScale = s;
            }
            if (_luz != null) _luz.intensity = 1.8f + Mathf.Sin(_fase * 8f) * 0.4f;
        }

        private void DispararLaser(PlayerController player)
        {
            Vector3 origem = transform.position + Vector3.down * 0.3f;
            Vector3 d = (player.transform.position + Vector3.up * 0.6f - origem).normalized;
            var proj = new GameObject("laserDrone");
            proj.transform.position = origem;
            proj.AddComponent<EnemyProjectile>().Iniciar(d, 14f, 10f, new Color(0.4f, 0.8f, 1f));
            FX.Flash(origem, new Color(0.4f, 0.8f, 1f), 2.5f, 2.5f, 0.06f);
            AudioManager.Instance?.PlayAt("laser", origem, 0.6f);
        }

        public void LevarDano(float d, Vector3 origem)
        {
            if (_morto) return;
            vida -= d;
            _alerta = true;
            FX.Faiscas(transform.position, new Color(0.6f, 0.9f, 1f), 5, 4f);
            if (vida <= 0f) Explodir();
        }

        private void Explodir()
        {
            _morto = true;
            GameManager.Instance?.AdicionarPontos(120);
            Loot.DeInimigo(transform.position + Vector3.down * 2f, false);
            if (_luz != null) Destroy(_luz.gameObject);
            StartCoroutine(Queda());
        }

        private IEnumerator Queda()
        {
            float vy = 0f;
            while (transform.position.y > 0.5f)
            {
                vy += 12f * Time.deltaTime;
                transform.position += Vector3.down * vy * Time.deltaTime;
                if (_visual != null) _visual.Rotate(0, 0, 300f * Time.deltaTime);
                if (Random.value < 0.3f) FX.Fumaca(transform.position, 1);
                yield return null;
            }
            FX.Explosao(transform.position, 0.8f, true, true);
            Destroy(gameObject);
        }
    }
}
