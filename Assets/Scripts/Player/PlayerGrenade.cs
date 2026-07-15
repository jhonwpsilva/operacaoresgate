using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Granadas de fragmentacao do jogador. Guarda o estoque (reabastecido por itens de
    /// municao) e arremessa na direcao da mira com forca de arco. A granada tem fisica
    /// realista: cai com gravidade, QUICA no chao e nas paredes perdendo energia, faz
    /// CONTAGEM REGRESSIVA piscando e apitando, e ao fim EXPLODE com dano em area,
    /// particulas, luz, som 3D e tremor de camera. Alerta os inimigos por perto.
    /// </summary>
    public class PlayerGrenade : MonoBehaviour
    {
        public int Estoque { get; private set; } = 4;
        public int EstoqueMax { get; private set; } = 6;
        private float _cd;
        private Sprite _sp;

        void Awake()
        {
            _sp = SpriteLibrary.Get("Sprites/props/bomba", 200f);
        }

        void Update()
        {
            if (_cd > 0f) _cd -= Time.deltaTime;
        }

        public bool PodeLancar => Estoque > 0 && _cd <= 0f;

        public void Lancar(Vector3 origem, Vector3 dir)
        {
            if (!PodeLancar) return;
            Estoque--;
            _cd = 0.6f;

            var go = new GameObject("granadaJogador");
            go.transform.position = origem;
            // arco na direcao da mira + impulso para cima
            Vector3 vel = new Vector3(dir.x * 9f, Mathf.Max(4f, dir.y * 8f + 5f), 0f);
            go.AddComponent<GrenadeProjectile>().Iniciar(vel, 70f, 3.2f, _sp);
            AudioManager.Instance?.Play("click", 0.5f);
        }

        public void Reabastecer(int qtd)
        {
            Estoque = Mathf.Min(EstoqueMax, Estoque + qtd);
        }
    }

    /// <summary>Granada em voo: quica, conta regressiva e explode com dano em area.</summary>
    public class GrenadeProjectile : MonoBehaviour
    {
        private Vector3 _vel;
        private float _dano, _raio;
        private float _timer = 2.4f;
        private bool _explodiu;
        private Transform _visual;
        private SpriteRenderer _sr;

        public void Iniciar(Vector3 vel, float dano, float raio, Sprite sprite)
        {
            _vel = vel; _dano = dano; _raio = raio;
            AudioManager.Instance?.Play("bomb_whistle", 0.4f);

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            if (sprite != null)
            {
                _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
                _sr.sprite = sprite; _sr.sortingOrder = 14;
                float alvo = 0.55f, maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
                _visual.gameObject.AddComponent<Billboard>();
            }
            else
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(_visual, false); s.transform.localScale = Vector3.one * 0.35f;
                var c = s.GetComponent<Collider>(); if (c != null) Destroy(c);
                _sr = null;
            }
        }

        void Update()
        {
            if (_explodiu) return;

            _vel.y -= GameConfig.Gravidade * 0.8f * Time.deltaTime;
            Vector3 novo = transform.position + _vel * Time.deltaTime;

            // quique no chao (procura o topo do solo abaixo)
            float chao = ChaoEm(novo.x);
            if (novo.y <= chao + 0.15f && _vel.y < 0f)
            {
                novo.y = chao + 0.15f;
                _vel.y = -_vel.y * 0.5f;      // perde energia vertical
                _vel.x *= 0.65f;              // atrito horizontal
                if (Mathf.Abs(_vel.y) > 0.8f) AudioManager.Instance?.Play("granada_quique", 0.5f);
            }

            // quique lateral em paredes (raycast horizontal curto)
            if (Mathf.Abs(_vel.x) > 0.1f)
            {
                Vector3 dirH = new Vector3(Mathf.Sign(_vel.x), 0, 0);
                if (Physics.Raycast(transform.position, dirH, out var h, 0.3f, ~0, QueryTriggerInteraction.Ignore)
                    && !h.collider.isTrigger)
                {
                    _vel.x = -_vel.x * 0.5f;
                }
            }

            transform.position = novo;
            if (_visual != null) _visual.Rotate(0, 0, 300f * Time.deltaTime);

            // contagem regressiva: pisca e apita cada vez mais rapido
            _timer -= Time.deltaTime;
            if (_sr != null)
            {
                float piscaVel = Mathf.Lerp(3f, 18f, 1f - Mathf.Clamp01(_timer / 2.4f));
                float b = Mathf.PingPong(Time.time * piscaVel, 1f);
                _sr.color = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.15f), b);
            }
            if (_timer <= 0f) Explodir();
        }

        private float ChaoEm(float x)
        {
            var hits = Physics.RaycastAll(new Vector3(x, transform.position.y + 5f, 0f), Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore);
            float melhor = 0f; bool achou = false;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.point.y <= transform.position.y + 0.2f && (!achou || h.point.y > melhor)) { melhor = h.point.y; achou = true; }
            }
            return achou ? melhor : 0f;
        }

        private void Explodir()
        {
            if (_explodiu) return;
            _explodiu = true;

            FX.Explosao(transform.position, 1.2f, true, true);
            CombatEvents.EmitirAlerta(transform.position, 12f);

            var cols = Physics.OverlapSphere(transform.position, _raio);
            foreach (var c in cols)
            {
                var inimigo = c.GetComponentInParent<EnemyController>();
                if (inimigo != null && inimigo.EstaVivo) { inimigo.LevarDano(_dano, transform.position); continue; }
                var boss = c.GetComponentInParent<BossController>();
                if (boss != null && boss.EstaVivo) { boss.LevarDano(_dano * 0.2f); continue; }
                var dano = c.GetComponentInParent<IDanificavel>();
                if (dano != null) { dano.LevarDano(_dano, transform.position); continue; }
                var heli = c.GetComponentInParent<HelicopterEnemy>();
                if (heli != null) heli.LevarDano(_dano, transform.position);
            }
            Destroy(gameObject, 0.05f);
        }
    }
}
