using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Vilao do jogo com IA COMPLETA. Le sua ficha (EnemyData) e age de verdade:
    ///  - PATRULHA em torno da origem e VOLTA A PATRULHAR quando perde o alvo.
    ///  - DETECTA o jogador por CAMPO DE VISAO (raio + altura) e OUVE TIROS (fica alerta e
    ///    vira para o som mesmo sem ver).
    ///  - PERSEGUE mantendo ESPACAMENTO (atiradores longe, corpo-a-corpo colado).
    ///  - ATIRA / lanca chamas / granada / investe / golpeia, cada tipo com efeito e som.
    ///  - ESQUIVA com jukes laterais em combate; PROCURA COBERTURA apos levar dano.
    ///  - FOGE quando a vida esta baixa (atiradores) e CHAMA REFORCOS ao ser atingido.
    ///  - MORRE com animacao e SOLTA ITENS.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        public string tipo = "soldado_inimigo";
        public float vida;
        public float VidaMax { get; private set; }
        public bool MiniChefe { get; private set; }
        public bool EstaVivo => !_morto;

        private EnemyConfig _cfg;
        private float _alcance = 4f;
        private Vector3 _origem;
        private bool _voador;
        private SpriteRenderer _sr;
        private Transform _visual;
        private bool _direita = true;
        private float _cooldownDano;
        private float _cooldownAtaque;
        private float _cooldownGolpe;
        private bool _morto;
        private float _alturaBase;
        private bool _investindo;
        private Vector3 _empurrao;
        private Light _auraChefe;
        private bool _enfurecido;

        // ---- IA tatica: ouvir tiros, fugir com vida baixa, chamar reforco ----
        private float _alertaSom;
        private bool _fugindo;
        private bool _pediuReforco;
        // ---- movimento sempre ativo (patrulha + paceio em combate) ----
        private float _fasePatrulha;
        private float _patrulhaDir = 1f;
        private float _strafeDir = 1f;
        private float _strafeTimer;

        private const float RAIO_DETECCAO = 14f;

        void OnEnable()
        {
            CombatEvents.TiroDisparado += AoOuvirTiro;
            CombatEvents.Alerta += AoAlerta;
        }
        void OnDisable()
        {
            CombatEvents.TiroDisparado -= AoOuvirTiro;
            CombatEvents.Alerta -= AoAlerta;
        }

        private void AoOuvirTiro(Vector3 pos, bool doJogador)
        {
            if (_morto || !doJogador) return;
            if (Mathf.Abs(pos.x - transform.position.x) < 18f && Mathf.Abs(pos.y - transform.position.y) < 8f)
            {
                _alertaSom = 4f;
                _direita = pos.x >= transform.position.x; // apenas vira para a origem do som
            }
        }

        private void AoAlerta(Vector3 pos, float raio)
        {
            if (_morto) return;
            if (Vector3.Distance(pos, transform.position) < raio) { _alertaSom = 4f; TalvezChamarReforco(); }
        }

        private void TalvezChamarReforco()
        {
            if (_pediuReforco) return;
            if (MiniChefe || Random.value < 0.30f)
            {
                _pediuReforco = true;
                CombatEvents.PedirReforco(transform.position);
            }
        }

        public void Configurar(string t, Sprite sprite, float r, Vector3 origem)
        {
            tipo = t; _alcance = Mathf.Max(3f, r); _origem = origem;
            _cfg = EnemyData.Get(t);
            _voador = _cfg.voador;
            vida = _cfg.vida; VidaMax = _cfg.vida; MiniChefe = _cfg.chefeMenor;
            _cooldownAtaque = Random.Range(0.3f, _cfg.recargaAtaque);
            _fasePatrulha = Random.value * 6.28f;
            _patrulhaDir = Random.value > 0.5f ? 1f : -1f;
            _strafeDir = Random.value > 0.5f ? 1f : -1f;
            _strafeTimer = Random.Range(0.6f, 1.4f);

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 15;
            if (sprite != null)
            {
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (_cfg.altura / maior);
                _visual.localPosition = new Vector3(0, sprite.bounds.size.y * _visual.localScale.y * 0.5f, 0);
            }
            _visual.gameObject.AddComponent<Billboard>();

            if (!_voador) transform.position = new Vector3(origem.x, ChaoEm(origem.x, origem.y), 0f);
            _alturaBase = transform.position.y;

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = _cfg.altura; col.radius = 0.42f;
            col.center = new Vector3(0, _cfg.altura / 2f, 0);
            col.isTrigger = true;

            if (MiniChefe)
            {
                _auraChefe = new GameObject("aura").AddComponent<Light>();
                _auraChefe.transform.SetParent(transform, false);
                _auraChefe.transform.localPosition = new Vector3(0, _cfg.altura * 0.6f, -0.6f);
                _auraChefe.color = _cfg.corEfeito; _auraChefe.range = 5f; _auraChefe.intensity = 1.6f;
            }

            if (!string.IsNullOrEmpty(_cfg.somSpawn))
                AudioManager.Instance?.Play(_cfg.somSpawn, 0.6f);
        }

        private float ChaoEm(float x, float yPadrao)
        {
            var hits = Physics.RaycastAll(new Vector3(x, 30f, 0f), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float melhor = float.NegativeInfinity;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.GetComponent<EnemyController>() != null) continue;
                if (h.point.y > melhor && h.point.y < 25f) melhor = h.point.y;
            }
            return float.IsNegativeInfinity(melhor) ? yPadrao : melhor;
        }

        void Update()
        {
            if (_morto) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            if (_cooldownDano > 0f) _cooldownDano -= Time.deltaTime;
            if (_cooldownAtaque > 0f) _cooldownAtaque -= Time.deltaTime;
            if (_cooldownGolpe > 0f) _cooldownGolpe -= Time.deltaTime;
            if (_alertaSom > 0f) _alertaSom -= Time.deltaTime;

            if (_knockRestante > 0f)
            {
                transform.position += _empurrao * Time.deltaTime;
                _empurrao = Vector3.Lerp(_empurrao, Vector3.zero, 8f * Time.deltaTime);
                _knockRestante -= Time.deltaTime;
            }

            var player = GameManager.Instance.Player;
            if (player == null) return;

            Vector3 pos = transform.position;
            Vector3 ppos = player.transform.position;
            float distX = Mathf.Abs(ppos.x - pos.x);
            float dist = Vector3.Distance(pos, ppos);
            float dir = Mathf.Sign(ppos.x - pos.x);
            if (dir == 0) dir = _direita ? 1f : -1f;

            // deteccao: raio generoso (como no original) OU ouviu tiro recentemente
            bool perseguindo = distX < RAIO_DETECCAO || _alertaSom > 0f;

            float distIdeal = DistanciaIdeal();
            bool alturaOk = Mathf.Abs(ppos.y - pos.y) < 4.5f;

            // fugir com vida baixa (atiradores; nao mini-chefe / nao corpo a corpo)
            bool podeFugir = !MiniChefe && !_voador && _cfg.ataque != TipoAtaque.Corpo && _cfg.ataque != TipoAtaque.Investida;
            if (podeFugir && vida <= VidaMax * 0.25f) _fugindo = true;
            else if (vida > VidaMax * 0.4f) _fugindo = false;

            // ---- ATAQUE ----
            if (perseguindo && alturaOk)
            {
                bool corpo = _cfg.ataque == TipoAtaque.Corpo || _cfg.ataque == TipoAtaque.Investida;
                if (!corpo && _cooldownAtaque <= 0f && dist <= _cfg.alcanceAtaque)
                {
                    _direita = dir > 0;
                    ExecutarAtaque(player, dir);
                    _cooldownAtaque = _cfg.recargaAtaque;
                }
                else if (corpo && _cooldownGolpe <= 0f && dist <= distIdeal + 0.6f)
                {
                    _direita = dir > 0;
                    GolpeCorpo(player, dir);
                    _cooldownGolpe = _cfg.ataque == TipoAtaque.Investida ? _cfg.recargaAtaque : 1.1f;
                }
            }

            // ---- MOVIMENTO: SEMPRE em movimento ----
            float vel = _cfg.velocidade * (_investindo ? 2.3f : 1f);
            if (perseguindo)
            {
                _direita = dir > 0;
                if (_fugindo)
                    pos.x -= dir * vel * 1.1f * Time.deltaTime;                 // recua com vida baixa
                else if (dist > distIdeal + 0.5f)
                    pos.x += dir * vel * Time.deltaTime;                         // aproxima
                else if (dist < distIdeal - 0.5f)
                    pos.x -= dir * vel * 0.9f * Time.deltaTime;                  // afasta p/ nao colar
                else
                {
                    // na distancia de combate: PACEIA de um lado a outro (nunca fica parado)
                    _strafeTimer -= Time.deltaTime;
                    if (_strafeTimer <= 0f) { _strafeDir = -_strafeDir; _strafeTimer = Random.Range(0.7f, 1.5f); }
                    pos.x += _strafeDir * vel * 0.6f * Time.deltaTime;
                }
                if (_voador)
                    pos.y = Mathf.Lerp(pos.y, ppos.y + 1.8f + Mathf.Sin(Time.time * 2.4f + _fasePatrulha) * 0.6f, 2.2f * Time.deltaTime);
            }
            else
            {
                // PATRULHA: caminha de ponta a ponta sem parar, invertendo nas bordas
                pos.x += _patrulhaDir * vel * 0.85f * Time.deltaTime;
                if (pos.x > _origem.x + _alcance) _patrulhaDir = -1f;
                else if (pos.x < _origem.x - _alcance) _patrulhaDir = 1f;
                _direita = _patrulhaDir > 0;
                if (_voador) pos.y = _alturaBase + Mathf.Sin(Time.time * 2f + _fasePatrulha) * 0.6f;
            }

            if (!_voador)
            {
                float gy = ChaoEm(pos.x, _alturaBase);
                pos.y = Mathf.Lerp(pos.y, gy, 12f * Time.deltaTime);
                _alturaBase = gy;
            }

            transform.position = pos;

            if (_visual != null)
            {
                var s = _visual.localScale; s.x = Mathf.Abs(s.x) * (_direita ? 1f : -1f); _visual.localScale = s;
            }
        }

        private float DistanciaIdeal()
        {
            switch (_cfg.ataque)
            {
                case TipoAtaque.Tiro:     return Mathf.Clamp(_cfg.alcanceAtaque * 0.7f, 4f, 9f);
                case TipoAtaque.Granada:  return Mathf.Clamp(_cfg.alcanceAtaque * 0.7f, 5f, 9f);
                case TipoAtaque.Fogo:     return Mathf.Max(1.6f, _cfg.alcanceAtaque * 0.65f);
                case TipoAtaque.Investida: return 1.4f;
                default:                  return 1.3f;
            }
        }

        private void GolpeCorpo(PlayerController player, float dir)
        {
            if (_cfg.ataque == TipoAtaque.Investida) { StartCoroutine(Investida(player)); return; }
            AudioManager.Instance?.Play(string.IsNullOrEmpty(_cfg.somAtaque) ? "hurt" : _cfg.somAtaque, 0.6f);
            StartCoroutine(AvancoVisual(dir));
            float d = Vector3.Distance(transform.position, player.transform.position);
            if (d <= 2.0f) player.Saude?.LevarDano(_cfg.dano, transform.position);
        }

        private IEnumerator AvancoVisual(float dir)
        {
            if (_visual == null) yield break;
            Vector3 ini = _visual.localPosition; float t = 0f;
            while (t < 0.16f && _visual != null)
            {
                t += Time.deltaTime;
                _visual.localPosition = ini + new Vector3(dir * 0.4f * Mathf.Sin(t / 0.16f * Mathf.PI), 0, 0);
                yield return null;
            }
            if (_visual != null) _visual.localPosition = ini;
        }

        // =========================================================
        //  Ataques a distancia / especiais (com efeito + som)
        // =========================================================
        private void ExecutarAtaque(PlayerController player, float dir)
        {
            Vector3 origem = transform.position + Vector3.up * (_cfg.altura * 0.55f) + new Vector3(dir * 0.6f, 0, 0);
            Vector3 alvo = player.transform.position + Vector3.up * 0.8f;
            Vector3 d = (alvo - origem).normalized;

            switch (_cfg.ataque)
            {
                case TipoAtaque.Tiro:
                    FlashCano(origem);
                    if (EhPesado(tipo))
                    {
                        var mis = new GameObject("missil");
                        mis.transform.position = origem;
                        mis.AddComponent<EnemyProjectile>().Iniciar(d, 16f, _cfg.danoAtaque, new Color(1f, 0.5f, 0.2f));
                        var spM = SpriteLibrary.Get("Sprites/props/missil", 200f);
                        if (spM != null)
                        {
                            var sv = new GameObject("ogiva"); sv.transform.SetParent(mis.transform, false);
                            var sr = sv.AddComponent<SpriteRenderer>(); sr.sprite = spM; sr.sortingOrder = 16;
                            float m = Mathf.Max(spM.bounds.size.x, spM.bounds.size.y);
                            float e = 1.1f / Mathf.Max(0.01f, m);
                            sv.transform.localScale = new Vector3(e * Mathf.Sign(d.x == 0 ? dir : d.x), e, 1f);
                            sv.AddComponent<Billboard>();
                        }
                        AudioManager.Instance?.Play(_cfg.somAtaque, 0.8f);
                    }
                    else
                    {
                        var proj = new GameObject("tiroInimigo");
                        proj.transform.position = origem;
                        proj.AddComponent<EnemyProjectile>().Iniciar(d, 12f, _cfg.danoAtaque, _cfg.corEfeito);
                        AudioManager.Instance?.Play(_cfg.somAtaque, 0.8f);
                    }
                    break;

                case TipoAtaque.Fogo:
                    StartCoroutine(JatoDeFogo(dir));
                    break;

                case TipoAtaque.Granada:
                    var g = new GameObject("granada");
                    g.transform.position = origem;
                    float dx = alvo.x - origem.x;
                    Vector3 vArco = new Vector3(dx * 0.9f, 6.5f, 0);
                    g.AddComponent<FallingBomb>().Iniciar(vArco, _cfg.danoAtaque, 2.6f, SpriteLibrary.Get("Sprites/props/bomba", 200f));
                    AudioManager.Instance?.Play(_cfg.somAtaque, 0.7f);
                    break;
            }
        }

        private void FlashCano(Vector3 origem)
        {
            var f = new GameObject("flash");
            f.transform.position = origem;
            var l = f.AddComponent<Light>();
            l.color = _cfg.corEfeito; l.range = 2.5f; l.intensity = 2.5f;
            Destroy(f, 0.08f);
        }

        private IEnumerator JatoDeFogo(float dir)
        {
            var fogo = new GameObject("jatoFogo");
            fogo.transform.SetParent(transform, false);
            fogo.transform.localPosition = new Vector3(dir * 1.4f, _cfg.altura * 0.5f, 0);
            var l = fogo.AddComponent<Light>();
            l.color = _cfg.corEfeito; l.range = 4f; l.intensity = 3.5f;
            AudioManager.Instance?.Play(_cfg.somAtaque, 0.8f);

            float t = 0f;
            while (t < 0.45f && !_morto)
            {
                t += Time.deltaTime;
                var player = GameManager.Instance?.Player;
                if (player != null)
                {
                    float dist = Vector3.Distance(transform.position, player.transform.position);
                    bool frente = Mathf.Sign(player.transform.position.x - transform.position.x) == dir;
                    if (dist < _cfg.alcanceAtaque && frente && _cooldownDano <= 0f)
                    {
                        player.Saude?.LevarDano(_cfg.danoAtaque, transform.position);
                        _cooldownDano = 0.3f;
                    }
                }
                l.intensity = 3.5f + Mathf.Sin(t * 40f) * 0.8f;
                yield return null;
            }
            Destroy(fogo);
        }

        private IEnumerator Investida(PlayerController player)
        {
            _investindo = true;
            AudioManager.Instance?.Play(string.IsNullOrEmpty(_cfg.somAtaque) ? "roar" : _cfg.somAtaque, 0.6f);
            float t = 0f;
            while (t < 0.6f && !_morto)
            {
                t += Time.deltaTime;
                if (player != null)
                {
                    float d = Vector3.Distance(transform.position, player.transform.position);
                    if (d <= 1.7f && _cooldownDano <= 0f)
                    {
                        player.Saude?.LevarDano(_cfg.danoAtaque, transform.position);
                        _cooldownDano = 0.5f;
                    }
                }
                yield return null;
            }
            _investindo = false;
        }

        void OnTriggerStay(Collider other)
        {
            if (_morto || _cooldownDano > 0f) return;
            var saude = other.GetComponent<PlayerHealth>();
            if (saude != null)
            {
                float d = _investindo ? _cfg.danoAtaque : _cfg.dano;
                saude.LevarDano(d, transform.position);
                _cooldownDano = _investindo ? 0.6f : 1f;
                if (_investindo) _investindo = false;
            }
        }

        private float _knockRestante;

        public void LevarDano(float d, Vector3 origem)
        {
            if (_morto) return;
            vida -= d;
            StartCoroutine(FlashDano());

            if (MiniChefe && !_enfurecido && vida <= VidaMax * 0.5f && vida > 0f)
                Enfurecer();

            // ao ser atingido, pode chamar reforco (uma unica vez)
            if (!_pediuReforco && vida <= VidaMax * 0.5f && vida > 0f)
            {
                _pediuReforco = true;
                CombatEvents.PedirReforco(transform.position);
            }

            Vector3 ponto = transform.position + Vector3.up * (_cfg.altura * 0.55f);
            if (EhMetalico(tipo)) ImpactFX.Faiscas(ponto);
            else ImpactFX.Sangue(ponto, _cfg.corEfeito);

            float dir = Mathf.Sign(transform.position.x - origem.x);
            if (dir == 0) dir = _direita ? -1f : 1f;
            _empurrao = new Vector3(dir * (MiniChefe ? 1.5f : 3.2f), 0, 0);
            _knockRestante = 0.18f;

            if (vida <= 0f) Morrer();
        }

        public bool Enfurecido => _enfurecido;
        public float VidaPct => VidaMax > 0 ? Mathf.Clamp01(vida / VidaMax) : 0f;

        private void Enfurecer()
        {
            _enfurecido = true;
            _cfg.velocidade *= 1.5f;
            _cfg.dano = Mathf.RoundToInt(_cfg.dano * 1.35f);
            _cfg.danoAtaque = Mathf.RoundToInt(_cfg.danoAtaque * 1.3f);
            _cfg.recargaAtaque *= 0.6f;

            Color furia = new Color(1f, 0.2f, 0.15f);
            _cfg.corEfeito = furia;
            if (_auraChefe != null) { _auraChefe.color = furia; _auraChefe.range = 6.5f; _auraChefe.intensity = 2.8f; }
            if (_visual != null) _visual.localScale *= 1.12f;

            AudioManager.Instance?.Play("roar", 0.9f);
            AudioManager.Instance?.Play("alarm", 0.6f);
            StartCoroutine(FlashFuria());
        }

        private IEnumerator FlashFuria()
        {
            if (_sr == null) yield break;
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                _sr.color = Color.Lerp(Color.white, new Color(1f, 0.3f, 0.3f), Mathf.PingPong(t * 8f, 1f));
                if (Random.value < 0.3f)
                {
                    var f = new GameObject("boom"); f.transform.position = transform.position + Vector3.up * Random.Range(0.5f, 2f);
                    var l = f.AddComponent<Light>(); l.color = new Color(1f, 0.3f, 0.2f); l.range = 3.5f; l.intensity = 3.5f;
                    Destroy(f, 0.18f);
                }
                yield return null;
            }
            if (_sr != null) _sr.color = Color.white;
        }

        private static bool EhMetalico(string t)
            => t == "drone" || t == "drone_militar" || t == "mech" || t == "tanque"
               || t == "robo_exterminador" || t == "helicoptero";

        private static bool EhPesado(string t)
            => t == "tanque" || t == "mech" || t == "robo_exterminador" || t == "elefante_cyber";

        private IEnumerator FlashDano()
        {
            if (_sr == null) yield break;
            Color o = _sr.color;
            _sr.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.08f);
            if (_sr != null) _sr.color = o;
        }

        private void Morrer()
        {
            _morto = true;
            AudioManager.Instance?.Play("explosion", 0.6f);
            GameManager.Instance?.AdicionarPontos(_cfg.pontos);

            Loot.DeInimigo(transform.position, MiniChefe);
            GameManager.Instance?.Companheiro?.RegistrarAbate(transform.position, _cfg.pontos);

            var f = new GameObject("boom");
            f.transform.position = transform.position + Vector3.up * (_cfg.altura * 0.4f);
            var l = f.AddComponent<Light>();
            l.color = Color.Lerp(_cfg.corEfeito, new Color(1f, 0.6f, 0.2f), 0.5f);
            l.range = MiniChefe ? 7f : 4f; l.intensity = MiniChefe ? 6f : 4f;
            Destroy(f, 0.3f);

            StartCoroutine(Sumir());
        }

        private IEnumerator Sumir()
        {
            float t = 0f;
            while (t < 0.32f)
            {
                t += Time.deltaTime;
                if (_visual != null)
                {
                    _visual.localScale *= 0.92f;
                    _visual.Translate(0, 2f * Time.deltaTime, 0);
                }
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
