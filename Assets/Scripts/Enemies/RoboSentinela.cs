using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Robo sentinela terrestre inteligente. Comportamento:
    ///  - ESCANEIA a area varrendo um facho de luz (spot) para os lados, com olho brilhante.
    ///  - CAMINHA em patrulha e VIRA automaticamente ao chegar na borda ou bater em parede.
    ///  - DETECTA o jogador dentro do campo de visao (cone + alcance + linha de visada).
    ///  - DISPARA LASER com recarga quando o alvo esta a vista.
    ///  - EXPLODE ao morrer, com estouro de luz, fumaca, detritos e tremor de camera.
    /// Fica no chao por raycast e pode ser abatido a tiros/granada (IDanificavel).
    /// </summary>
    public class RoboSentinela : MonoBehaviour, IDanificavel
    {
        public float vida = 130f;
        private float _vidaMax;
        private float _alcanceVisao = 12f;
        private float _range = 5f;
        private Vector3 _origem;
        private bool _direita = true;
        private bool _morto;
        private bool _alerta;
        private float _cdTiro;
        private float _faseScan;
        private float _alturaBase;

        private Transform _visual;
        private SpriteRenderer _sr;
        private Light _olho;
        private Light _scan;

        public void Configurar(Sprite sprite, Vector3 origem, float range)
        {
            _origem = origem; _range = Mathf.Max(3f, range);
            _vidaMax = vida;

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 15;
            if (sprite != null)
            {
                float alvo = 2.2f, maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
                _visual.localPosition = new Vector3(0, sprite.bounds.size.y * _visual.localScale.y * 0.5f, 0);
            }
            _visual.gameObject.AddComponent<Billboard>();

            // olho vermelho brilhante
            _olho = new GameObject("olho").AddComponent<Light>();
            _olho.transform.SetParent(transform, false);
            _olho.transform.localPosition = new Vector3(0, 1.6f, -0.4f);
            _olho.color = new Color(1f, 0.2f, 0.15f); _olho.range = 3.5f; _olho.intensity = 2.2f;

            // facho de varredura (holofote girando)
            _scan = new GameObject("scan").AddComponent<Light>();
            _scan.transform.SetParent(transform, false);
            _scan.transform.localPosition = new Vector3(0, 1.5f, 0);
            _scan.type = LightType.Spot;
            _scan.color = new Color(1f, 0.85f, 0.5f); _scan.range = _alcanceVisao; _scan.spotAngle = 34f; _scan.intensity = 2.5f;

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2.2f; col.radius = 0.5f; col.center = new Vector3(0, 1.1f, 0);
            col.isTrigger = true;

            transform.position = new Vector3(origem.x, ChaoEm(origem.x), 0);
            _alturaBase = transform.position.y;
            _cdTiro = Random.Range(0.5f, 1.5f);

            AudioManager.Instance?.Play("alarm", 0.3f);
        }

        void Update()
        {
            if (_morto) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            if (_cdTiro > 0f) _cdTiro -= Time.deltaTime;

            var player = GameManager.Instance.Player;
            Vector3 pos = transform.position;

            // ---- deteccao por campo de visao ----
            bool vendo = false;
            if (player != null)
            {
                Vector3 d = player.transform.position - (pos + Vector3.up * 1.4f);
                float dist = d.magnitude;
                bool naFrente = Mathf.Sign(d.x) == (_direita ? 1f : -1f) || Mathf.Abs(d.x) < 1.5f;
                bool noCone = Mathf.Abs(d.y) < 4f;
                if (dist < _alcanceVisao && naFrente && noCone)
                {
                    // linha de visada: nao pode ter parede solida no caminho
                    if (!Physics.Raycast(pos + Vector3.up * 1.4f, d.normalized, dist - 0.5f, ~0, QueryTriggerInteraction.Ignore))
                        vendo = true;
                }
            }

            if (vendo) { _alerta = true; _direita = player.transform.position.x >= pos.x; }

            // ---- varredura do facho ----
            if (_alerta && player != null)
            {
                // aponta o facho para o jogador
                Vector3 dir = (player.transform.position - _scan.transform.position).normalized;
                _scan.transform.rotation = Quaternion.Slerp(_scan.transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                _olho.intensity = 2.8f + Mathf.Sin(Time.time * 12f) * 0.6f;
            }
            else
            {
                _faseScan += Time.deltaTime * 1.4f;
                float sweep = Mathf.Sin(_faseScan) * 45f;
                float baseAng = _direita ? 0f : 180f;
                _scan.transform.localRotation = Quaternion.Euler(35f, baseAng + sweep, 0f);
                _olho.intensity = 1.8f + Mathf.Sin(Time.time * 3f) * 0.4f;
            }

            // ---- movimento ----
            if (_alerta && player != null)
            {
                float dist = Mathf.Abs(player.transform.position.x - pos.x);
                if (dist > 6f) pos.x += (_direita ? 1f : -1f) * 2.2f * Time.deltaTime; // aproxima
                else pos.x += Mathf.Sin(Time.time * 2f) * 1.3f * Time.deltaTime;       // paceia no lugar
                // dispara laser
                if (_cdTiro <= 0f && dist < _alcanceVisao)
                {
                    DispararLaser(player);
                    _cdTiro = 1.2f;
                }
            }
            else
            {
                // patrulha: vai e volta em torno da origem, vira nas bordas
                pos.x += (_direita ? 1f : -1f) * 1.6f * Time.deltaTime;
                if (pos.x > _origem.x + _range) _direita = false;
                else if (pos.x < _origem.x - _range) _direita = true;
                // vira ao bater em parede
                if (Physics.Raycast(pos + Vector3.up * 1f, new Vector3(_direita ? 1f : -1f, 0, 0), 0.8f, ~0, QueryTriggerInteraction.Ignore))
                    _direita = !_direita;
            }

            // cola no chao
            float gy = ChaoEm(pos.x);
            pos.y = Mathf.Lerp(pos.y, gy, 12f * Time.deltaTime);
            _alturaBase = gy;
            transform.position = pos;

            if (_visual != null)
            {
                var s = _visual.localScale; s.x = Mathf.Abs(s.x) * (_direita ? 1f : -1f); _visual.localScale = s;
            }
        }

        private void DispararLaser(PlayerController player)
        {
            Vector3 origem = transform.position + Vector3.up * 1.4f + new Vector3((_direita ? 1f : -1f) * 0.6f, 0, 0);
            Vector3 alvo = player.transform.position + Vector3.up * 0.8f;
            Vector3 d = (alvo - origem).normalized;
            var proj = new GameObject("laserRobo");
            proj.transform.position = origem;
            proj.AddComponent<EnemyProjectile>().Iniciar(d, 15f, 14f, new Color(1f, 0.3f, 0.2f));
            FX.Flash(origem, new Color(1f, 0.4f, 0.2f), 3f, 3f, 0.08f);
            AudioManager.Instance?.PlayAt("laser", origem, 0.7f);
        }

        private float ChaoEm(float x)
        {
            var hits = Physics.RaycastAll(new Vector3(x, 30f, 0f), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float melhor = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.GetComponent<RoboSentinela>() != null) continue;
                if (h.point.y > melhor && h.point.y < 25f) melhor = h.point.y;
            }
            return float.IsNegativeInfinity(melhor) ? 0f : melhor;
        }

        public void LevarDano(float d, Vector3 origem)
        {
            if (_morto) return;
            vida -= d;
            _alerta = true;
            FX.Faiscas(transform.position + Vector3.up * 1.4f, new Color(1f, 0.9f, 0.4f), 6, 4f);
            StartCoroutine(Flash());
            if (vida <= 0f) Explodir();
        }

        private IEnumerator Flash()
        {
            if (_sr == null) yield break;
            _sr.color = new Color(1f, 0.5f, 0.5f);
            yield return new WaitForSeconds(0.06f);
            if (_sr != null) _sr.color = Color.white;
        }

        private void Explodir()
        {
            _morto = true;
            GameManager.Instance?.AdicionarPontos(230);
            Loot.DeInimigo(transform.position, false);
            if (_olho != null) Destroy(_olho.gameObject);
            if (_scan != null) Destroy(_scan.gameObject);
            StartCoroutine(SequenciaExplosao());
        }

        private IEnumerator SequenciaExplosao()
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.4f, 2f), 0);
                FX.Explosao(p, 0.9f, i == 0, i == 0);
                if (_visual != null) _visual.localScale *= 0.85f;
                yield return new WaitForSeconds(0.1f);
            }
            Destroy(gameObject);
        }
    }
}
