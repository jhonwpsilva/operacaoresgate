using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Chefe final na sala de comando. Possui SISTEMA DE FASES: conforme perde vida,
    /// muda de comportamento (100→80→60→40→20% e MODO FURIA), ficando mais rapido,
    /// agressivo e perigoso. Cada fase altera velocidade, dano, padrao de ataque, cor
    /// do efeito e som. A cada transicao ele ruge, pisca e fica brevemente invulneravel.
    /// Derrotado, aciona a vitoria.
    /// </summary>
    public class BossController : MonoBehaviour
    {
        public float VidaMax = 100f;
        public float Vida { get; private set; }
        public bool Ativo { get; private set; }
        public int Fase => _fase;          // 0..4 (5 = furia)
        public bool EstaVivo => !_morto;

        private SpriteRenderer _sr;
        private Transform _visual;
        private Light _aura;
        private bool _direita = true;
        private float _cooldownTiro;
        private float _cooldownContato;
        private float _cooldownBomba;
        private bool _morto;
        private bool _invulneravel;
        private bool _investindo;
        private float _baseY;
        private int _fase = 0;
        private Sprite _spriteBomba;

        // ---- parametros por fase (0..4) ----
        // fase 4 = MODO FURIA
        private static readonly float[] Velocidade   = { 2.4f, 2.9f, 3.2f, 3.7f, 4.6f };
        private static readonly float[] RecargaTiro  = { 1.6f, 1.2f, 1.0f, 0.7f, 0.42f };
        private static readonly float[] VelProjetil  = { 9f,   10f,  11f,  12f,  14f };
        private static readonly float[] DanoContato  = { 22f,  24f,  26f,  28f,  34f };
        private static readonly float[] DanoProjetil = { 16f,  16f,  18f,  15f,  16f };
        private static readonly int[]   Rajada       = { 1,    3,    3,    5,    5 };   // projeteis por disparo
        private static readonly Color[] CorFase = {
            new Color(1f, 0.55f, 0.15f),   // laranja
            new Color(1f, 0.45f, 0.12f),   // laranja-vermelho
            new Color(1f, 0.30f, 0.18f),   // vermelho
            new Color(1f, 0.18f, 0.20f),   // vermelho intenso
            new Color(1f, 0.85f, 0.95f),   // branco-roxo (furia)
        };

        public void Configurar(Sprite sprite)
        {
            Vida = VidaMax; Ativo = true;
            _spriteBomba = SpriteLibrary.Get("Sprites/props/bomba", 200f);

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 16;
            if (sprite != null)
            {
                float alvo = 3.4f;
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
                _visual.localPosition = new Vector3(0, sprite.bounds.size.y * _visual.localScale.y * 0.5f, 0);
            }
            _visual.gameObject.AddComponent<Billboard>();
            _baseY = transform.position.y;

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 3.2f; col.radius = 0.8f; col.center = new Vector3(0, 1.6f, 0);
            col.isTrigger = true;

            _aura = new GameObject("auraBoss").AddComponent<Light>();
            _aura.transform.SetParent(transform, false);
            _aura.transform.localPosition = new Vector3(0, 2f, -1f);
            _aura.color = CorFase[0]; _aura.range = 7f; _aura.intensity = 2.2f;

            AudioManager.Instance?.Play("roar", 0.8f);
        }

        void Update()
        {
            if (_morto || !Ativo) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            var player = GameManager.Instance.Player;
            if (player == null) return;

            if (_cooldownTiro > 0f) _cooldownTiro -= Time.deltaTime;
            if (_cooldownContato > 0f) _cooldownContato -= Time.deltaTime;
            if (_cooldownBomba > 0f) _cooldownBomba -= Time.deltaTime;

            // pulso da aura (mais rapido nas fases finais)
            if (_aura != null)
                _aura.intensity = 2.2f + _fase * 0.6f + Mathf.Sin(Time.time * (4f + _fase * 2f)) * (0.5f + _fase * 0.3f);

            Vector3 pos = transform.position;
            float dx = player.transform.position.x - pos.x;
            float dir = Mathf.Sign(dx);
            _direita = dir > 0;
            float dist = Mathf.Abs(dx);

            float vel = Velocidade[_fase] * (_investindo ? 1.8f : 1f);
            if (_investindo) { pos.x += dir * vel * Time.deltaTime; }
            else
            {
                if (dist > 5f) pos.x += dir * vel * Time.deltaTime;
                else if (dist < 2.5f) pos.x -= dir * vel * 0.8f * Time.deltaTime;
            }
            pos.y = _baseY + Mathf.Sin(Time.time * 2f) * 0.15f;
            transform.position = pos;

            if (_visual != null)
            {
                var s = _visual.localScale; s.x = Mathf.Abs(s.x) * (_direita ? 1f : -1f); _visual.localScale = s;
            }

            if (_invulneravel) return; // durante transicao nao ataca

            // ---- ataques conforme a fase ----
            if (_cooldownTiro <= 0f && dist < 18f)
            {
                AtirarRajada(dir, Rajada[_fase]);
                _cooldownTiro = RecargaTiro[_fase];
            }

            // fases 2+ tambem soltam bombas
            if (_fase >= 2 && _cooldownBomba <= 0f && dist < 14f)
            {
                SoltarBomba(player);
                _cooldownBomba = (_fase >= 4) ? 1.6f : 2.6f;
            }

            // fases 3+ fazem investida ocasional quando perto
            if (_fase >= 3 && !_investindo && dist < 7f && Random.value < 0.004f)
                StartCoroutine(Investida());
        }

        // =========================================================
        //  Ataques
        // =========================================================
        private void AtirarRajada(float dir, int n)
        {
            AudioManager.Instance?.Play(_fase >= 4 ? "laser" : "shoot", 0.9f);
            Vector3 origem = transform.position + Vector3.up * 1.6f + new Vector3(dir * 0.8f, 0, 0);
            Color cor = CorFase[_fase];
            // leque de projeteis
            float arco = 18f; // graus totais
            for (int i = 0; i < n; i++)
            {
                float t = (n == 1) ? 0f : (i / (float)(n - 1) - 0.5f);
                float ang = t * arco * Mathf.Deg2Rad;
                Vector3 d = new Vector3(dir * Mathf.Cos(ang), Mathf.Sin(ang), 0);
                var proj = new GameObject("projetilBoss");
                proj.transform.position = origem;
                proj.AddComponent<EnemyProjectile>().Iniciar(d, VelProjetil[_fase], DanoProjetil[_fase], cor);
            }
            // flash do disparo
            var f = new GameObject("flashBoss"); f.transform.position = origem;
            var l = f.AddComponent<Light>(); l.color = cor; l.range = 3.5f; l.intensity = 3.5f;
            Destroy(f, 0.08f);
        }

        private void SoltarBomba(PlayerController player)
        {
            Vector3 origem = transform.position + Vector3.up * 2.2f;
            float dx = player.transform.position.x - origem.x;
            Vector3 vArco = new Vector3(dx * 0.85f, 6.5f, 0);
            var g = new GameObject("bombaBoss");
            g.transform.position = origem;
            g.AddComponent<FallingBomb>().Iniciar(vArco, 28f, 3f, _spriteBomba);
        }

        private IEnumerator Investida()
        {
            _investindo = true;
            AudioManager.Instance?.Play("roar", 0.6f);
            yield return new WaitForSeconds(0.8f);
            _investindo = false;
        }

        void OnTriggerStay(Collider other)
        {
            if (_morto || _cooldownContato > 0f) return;
            var saude = other.GetComponent<PlayerHealth>();
            if (saude != null)
            {
                saude.LevarDano(DanoContato[_fase], transform.position);
                _cooldownContato = _investindo ? 0.7f : 1.2f;
                if (_investindo) _investindo = false;
            }
        }

        public void LevarDano(float d)
        {
            if (_morto || _invulneravel) return;
            Vida -= d;
            GameManager.Instance?.NotificarHUD();
            StartCoroutine(Flash());
            ImpactFX.Faiscas(transform.position + Vector3.up * 1.8f);

            // checar mudanca de fase
            float pct = Vida / VidaMax;
            int faseAlvo = pct > 0.8f ? 0 : pct > 0.6f ? 1 : pct > 0.4f ? 2 : pct > 0.2f ? 3 : 4;
            if (faseAlvo > _fase) StartCoroutine(TransicaoFase(faseAlvo));

            if (Vida <= 0f) Derrotar();
        }

        private IEnumerator TransicaoFase(int novaFase)
        {
            _fase = novaFase;
            _invulneravel = true;

            bool furia = (novaFase >= 4);
            AudioManager.Instance?.Play("roar", 0.9f);
            AudioManager.Instance?.Play("alarm", furia ? 0.8f : 0.5f);

            if (_aura != null) _aura.color = CorFase[novaFase];

            // flash forte + leve crescimento (boss enfurece)
            float t = 0f;
            Vector3 escalaBase = _visual != null ? _visual.localScale : Vector3.one;
            while (t < 0.9f)
            {
                t += Time.deltaTime;
                if (_sr != null)
                    _sr.color = Color.Lerp(Color.white, CorFase[novaFase], Mathf.PingPong(t * 8f, 1f));
                if (_visual != null && furia)
                    _visual.localScale = escalaBase * (1f + 0.08f * (t / 0.9f) * Mathf.Sign(escalaBase.x));
                // explosoes de transicao
                if (Random.value < 0.25f)
                {
                    var f = new GameObject("boom");
                    f.transform.position = transform.position + Vector3.up * Random.Range(0.5f, 3f) + new Vector3(Random.Range(-1.5f, 1.5f), 0, 0);
                    var l = f.AddComponent<Light>(); l.color = CorFase[novaFase]; l.range = 4f; l.intensity = 4f;
                    Destroy(f, 0.2f);
                }
                yield return null;
            }
            if (_sr != null) _sr.color = Color.white;
            _invulneravel = false;
        }

        private IEnumerator Flash()
        {
            if (_sr == null || _invulneravel) yield break;
            Color o = _sr.color;
            _sr.color = new Color(1f, 0.5f, 0.5f, 1f);
            yield return new WaitForSeconds(0.06f);
            if (_sr != null && !_invulneravel) _sr.color = o;
        }

        private void Derrotar()
        {
            _morto = true; Ativo = false;
            AudioManager.Instance?.Play("explosion");
            GameManager.Instance?.AdicionarPontos(1000);
            StartCoroutine(ExplosaoFinal());
        }

        private IEnumerator ExplosaoFinal()
        {
            for (int i = 0; i < 8; i++)
            {
                var f = new GameObject("boom");
                f.transform.position = transform.position + Vector3.up * Random.Range(0.5f, 2.8f)
                                       + new Vector3(Random.Range(-1.4f, 1.4f), 0, 0);
                var l = f.AddComponent<Light>();
                l.color = new Color(1f, Random.Range(0.5f, 0.8f), 0.2f); l.range = 5.5f; l.intensity = 5.5f;
                Destroy(f, 0.3f);
                AudioManager.Instance?.Play("explosion", 0.5f);
                if (_visual != null) _visual.localScale *= 0.9f;
                yield return new WaitForSeconds(0.16f);
            }
            Destroy(gameObject);
            GameManager.Instance?.Vencer();
        }
    }

    /// <summary>Projetil disparado pelo chefe; some ao atingir o jogador ou expirar.</summary>
    public class BossProjectile : MonoBehaviour
    {
        private Vector3 _dir;
        private float _vel, _dano, _vida = 3f;

        public void Iniciar(Vector3 dir, float vel, float dano)
        {
            _dir = dir.normalized; _vel = vel; _dano = dano;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 0.45f;
            sphere.GetComponent<MeshRenderer>().material = MaterialUtil.Emissivo(new Color(1f, 0.55f, 0.15f), 1.5f);
            var c = sphere.GetComponent<Collider>(); if (c != null) Destroy(c);
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.3f;
            var luz = gameObject.AddComponent<Light>();
            luz.color = new Color(1f, 0.6f, 0.2f); luz.range = 2.5f; luz.intensity = 1.5f;
        }

        void Update()
        {
            transform.position += _dir * _vel * Time.deltaTime;
            _vida -= Time.deltaTime;
            if (_vida <= 0f) Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            var saude = other.GetComponent<PlayerHealth>();
            if (saude != null) { saude.LevarDano(_dano, transform.position); Destroy(gameObject); }
        }
    }
}
