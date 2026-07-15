using UnityEngine;

namespace OperacaoResgate
{
    public enum EstadoPlayer { Idle, Andando, Correndo, Pulando, Caindo, Agachado, Escalando, Atacando, Ferido, Vitoria, Derrota, Rolando, Corpo, Recarregando, Arremesso }

    /// <summary>
    /// Controla o soldado jogavel num mundo 2.5D. Movimento com Rigidbody e fisica real
    /// (gravidade em Y, preso ao plano XY), deteccao de chao robusta (contato + raycast
    /// acima dos pes). Acoes completas: andar, correr, pular, duplo salto, agachar, ROLAR
    /// (esquiva com invulnerabilidade), escalar, MIRAR (cima/frente/baixo pelo mouse),
    /// ATIRAR (sistema de armas), RECARREGAR, TROCAR DE ARMA, LANCAR GRANADA, CORPO A CORPO,
    /// coletar itens e interagir. Toca PASSOS conforme a superficie.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        private Rigidbody _rb;
        private CapsuleCollider _col;
        private PlayerAnimator _anim;
        private PlayerHealth _health;
        private WeaponSystem _weapons;
        private PlayerGrenade _grenade;
        private PlayerCombat _combat;

        public EstadoPlayer Estado { get; private set; } = EstadoPlayer.Idle;
        public bool VoltadoDireita { get; private set; } = true;
        public bool NoChao { get; private set; }

        private bool _podeDuploPulo;
        private bool _agachado;
        private bool _emEscada;
        private float _alturaNormal = 1.6f;
        private float _alturaAgachado = 0.95f;
        private bool _controleAtivo = true;

        // acoes temporizadas (poses transitorias)
        private float _tempoAtaque;
        private float _tempoMira;   // segura a pose de MIRA (fuzil erguido) entre disparos
        private float _tempoMelee;
        private float _tempoGranada;

        // rolamento / esquiva
        private bool _rolando;
        private float _tempoRoll;
        private float _cdRoll;
        private float _dirRoll = 1f;

        // ---- movimentacao com inercia (aceleracao/desaceleracao naturais) ----
        private const float AcelSolo    = 60f;   // quao rapido ganha velocidade no chao
        private const float DesacelSolo = 75f;   // quao rapido freia no chao (pes "grudam")
        private const float ControleAr  = 0.35f; // fracao do controle enquanto no ar

        // CAUSA #3 DO "BATENDO NO CHAO": aqui era -3f.
        // Parado no chao, isso empurrava o corpo 3 m/s PARA DENTRO do piso a cada passo
        // de fisica (= 6 cm de penetracao por passo a 50 Hz). O solver do PhysX devolvia
        // o soldado pra cima no passo seguinte -> ele batia/vibrava no chao sem parar.
        // Basta o suficiente para nao "flutuar" em rampa/degrau, sem cravar no piso.
        private const float ColaChao = -1.2f;

        // CAUSA #4 DO TREMOR: mesmo com -1.2, empurrar o corpo PARA BAIXO enquanto ele ja
        // esta ENCOSTADO no piso re-penetra o colisor a cada passo de fisica, e o PhysX
        // devolve pra cima no passo seguinte -> micro-vibracao permanente em pe.
        // Regra nova: se ha CONTATO FISICO real, y fica em 0 (repouso de verdade); o
        // ColaChao so entra quando o chao foi visto apenas pelo raycast (rampa/degrau),
        // que e o unico caso em que ele e necessario.
        private float VyNoChao() => _contatoReal ? 0f : ColaChao;

        // ---- deslize (crouch + corrida em velocidade alta) ----
        private bool _deslizando;
        private float _tempoDeslize;
        private float _cdDeslize;
        private float _dirDeslize = 1f;
        private float _velDeslize;

        // mira
        private Vector3 _aimDir = Vector3.right;
        private float _pitchMira;
        private float _aimRawX;   // direcao horizontal crua do mouse (para virar em pe)

        // passos
        private float _stepTimer;

        public float AlcanceChao = 0.18f;

        // deteccao de chao
        private bool _contatoChao;
        private float _coyote;
        private bool _temCoyote;
        private float _bufferPulo;   // toque de pulo capturado no Update (evita perder input)

        // plano de queda (cair no vazio = perde vida)
        private float _quedaLimite = -9f;
        private bool _caiu;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.useGravity = false;
            _rb.mass = 1f;
            _rb.drag = 0f;
            // trava o "pulinho" de correcao: se ainda sobrar qualquer sobreposicao
            // (encaixe em quina/plataforma movel), o PhysX empurra o corpo pra fora
            // devagar em vez de arremessa-lo pra cima.
            _rb.maxDepenetrationVelocity = 3f;

            _col = gameObject.AddComponent<CapsuleCollider>();
            _col.height = _alturaNormal;
            _col.radius = 0.32f;
            _col.center = new Vector3(0, _alturaNormal / 2f, 0);
            _col.material = CriarMaterialSemAtrito();

            _anim = gameObject.AddComponent<PlayerAnimator>();
            _health = gameObject.AddComponent<PlayerHealth>();
            _weapons = gameObject.AddComponent<WeaponSystem>();
            _grenade = gameObject.AddComponent<PlayerGrenade>();
            _combat = gameObject.AddComponent<PlayerCombat>();

            gameObject.layer = 0;
        }

        private PhysicMaterial CriarMaterialSemAtrito()
        {
            var m = new PhysicMaterial("PlayerSemAtrito");
            m.dynamicFriction = 0f; m.staticFriction = 0f;
            m.frictionCombine = PhysicMaterialCombine.Minimum;
            m.bounciness = 0f; m.bounceCombine = PhysicMaterialCombine.Minimum;
            return m;
        }

        void Update()
        {
            if (!_controleAtivo) return;
            if (GameManager.Instance != null && GameManager.Instance.Estado != EstadoJogo.Jogando) return;

            // ---- PULO: o input TEM que ser lido aqui no Update ----
            // GetKeyDown so fica true por 1 frame de renderizacao. O FixedUpdate roda no
            // relogio da fisica (50Hz) e NAO acontece em todo frame, entao ler o toque la
            // dentro PERDIA pulos (principalmente o duplo pulo). Aqui guardamos o toque
            // por alguns ms e o FixedUpdate consome quando rodar.
            if (_bufferPulo > 0f) _bufferPulo -= Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                _bufferPulo = 0.12f;

            AtualizarMira();
            LerInteracao();
            LerAcoes();
            AtualizarPassos();

            if (_tempoAtaque > 0f) _tempoAtaque -= Time.deltaTime;
            if (_tempoMira > 0f) _tempoMira -= Time.deltaTime;
            if (_tempoMelee > 0f) _tempoMelee -= Time.deltaTime;
            if (_tempoGranada > 0f) _tempoGranada -= Time.deltaTime;
            if (_cdRoll > 0f) _cdRoll -= Time.deltaTime;
            if (_cdDeslize > 0f) _cdDeslize -= Time.deltaTime;
            if (_rolando)
            {
                _tempoRoll -= Time.deltaTime;
                if (_tempoRoll <= 0f) _rolando = false;
            }
            if (_deslizando)
            {
                _tempoDeslize -= Time.deltaTime;
                if (_tempoDeslize <= 0f || Mathf.Abs(_velDeslize) < GameConfig.VelocidadeAndar * 0.6f)
                    _deslizando = false;
            }
        }

        void FixedUpdate()
        {
            if (GameManager.Instance != null && GameManager.Instance.Estado != EstadoJogo.Jogando)
            {
                _rb.velocity = new Vector3(0, _rb.velocity.y, 0);
                _contatoChao = false;
                return;
            }

            ChecarChao();

            if (_controleAtivo)
            {
                if (_rolando) MovimentoRoll();
                else if (_deslizando) MovimentoDeslize();
                else if (_emEscada) MovimentoEscalada();
                else MovimentoNormal();
            }

            AtualizarEstadoVisual();
            ChecarQueda();

            _contatoChao = false;
        }

        // =========================================================
        //  Deteccao de chao (contato real + raycast acima dos pes)
        // =========================================================
        void OnCollisionStay(Collision c)  { AvaliarContato(c); }
        void OnCollisionEnter(Collision c) { AvaliarContato(c); }

        private void AvaliarContato(Collision c)
        {
            for (int i = 0; i < c.contactCount; i++)
                if (c.GetContact(i).normal.y > 0.5f) { _contatoChao = true; return; }
        }

        private void ChecarChao()
        {
            bool ray = false;
            Vector3 origem = transform.position + Vector3.up * 0.5f;
            float dist = 0.5f + AlcanceChao;
            var hits = Physics.SphereCastAll(origem, _col.radius * 0.7f, Vector3.down, dist,
                                             ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform)) continue;

                // CAUSA #2: quando a esfera do SphereCast NASCE dentro de um colisor
                // (encostado numa parede, por ex), a Unity devolve distance = 0 e
                // normal = -direcao = Vector3.up. Isso passava no teste normal.y > 0.5
                // e o soldado se achava NO CHAO grudado numa parede -> pulo infinito e
                // troca NoChao/!NoChao a cada passo = tremedeira. Descarta esses hits.
                if (h.distance <= 0.0001f) continue;

                if (h.normal.y > 0.5f) { ray = true; break; }
            }

            bool noChaoAgora = (_contatoChao || ray) && _rb.velocity.y <= 1.0f;
            if (noChaoAgora) _coyote = 0.08f;
            else _coyote -= Time.fixedDeltaTime;

            NoChao = noChaoAgora;
            _contatoReal = _contatoChao;   // guarda: havia TOQUE fisico neste passo?
            _temCoyote = _coyote > 0f;
        }
        private bool _contatoReal;

        // =========================================================
        //  Movimento
        // =========================================================
        private void MovimentoNormal()
        {
            float h = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;

            bool correndo = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _agachado = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) && NoChao;

            float vel = _agachado ? GameConfig.VelocidadeAndar * 0.45f
                       : (correndo ? GameConfig.VelocidadeCorrer : GameConfig.VelocidadeAndar);

            if (_tempoAtaque > 0f) vel *= 0.45f;
            if (_tempoMelee > 0f || _tempoGranada > 0f) vel *= 0.3f;

            Vector3 v = _rb.velocity;

            // ---- deslize: agachar durante corrida em velocidade alta ----
            if (NoChao && !_deslizando && _cdDeslize <= 0f && correndo && _agachado &&
                Mathf.Abs(v.x) > GameConfig.VelocidadeCorrer * 0.8f)
            {
                IniciarDeslize(Mathf.Sign(v.x));
                return;
            }

            // ---- aceleracao/desaceleracao naturais (inercia) em vez de velocidade instantanea ----
            float alvoX = h * vel;
            float taxa = (Mathf.Abs(alvoX) > Mathf.Abs(v.x) + 0.01f) ? AcelSolo : DesacelSolo;
            if (!NoChao) taxa *= ControleAr;                 // menos controle no ar
            v.x = Mathf.MoveTowards(v.x, alvoX, taxa * Time.fixedDeltaTime);

            if (NoChao && v.y <= 0.1f) v.y = VyNoChao();
            else v.y -= GameConfig.Gravidade * Time.fixedDeltaTime;

            if (PuloPressionado())
            {
                if (NoChao || _temCoyote)
                {
                    v.y = GameConfig.ForcaPulo;
                    _podeDuploPulo = true;
                    _coyote = 0f; _temCoyote = false;
                    _bufferPulo = 0f;              // consome: o 2o pulo exige NOVO toque
                    AudioManager.Instance?.Play("jump");
                }
                else if (_podeDuploPulo)
                {
                    // Max: se o 2o toque vier cedo (ainda subindo forte), o duplo pulo
                    // nao DERRUBA a velocidade — antes ele podia deixar o pulo MENOR.
                    v.y = Mathf.Max(v.y, GameConfig.ForcaDuploPulo);
                    _podeDuploPulo = false;
                    _bufferPulo = 0f;
                    AudioManager.Instance?.Play("double_jump");
                }
            }

            _rb.velocity = v;

            // facing: ATIRANDO encara o alvo (senao o fuzil fica apontado pras costas e a
            // bala sai do lado errado); ANDANDO encara o movimento; PARADO encara o mouse.
            if (_tempoMira > 0f && Mathf.Abs(_aimRawX) > 0.15f) VoltadoDireita = _aimRawX > 0f;
            else if (h > 0.01f) VoltadoDireita = true;
            else if (h < -0.01f) VoltadoDireita = false;
            else if (Mathf.Abs(_aimRawX) > 0.4f) VoltadoDireita = _aimRawX > 0f;

            AjustarColisor(_agachado);
        }

        private void MovimentoRoll()
        {
            // dash na direcao do rolamento, colado ao chao
            Vector3 v = _rb.velocity;
            v.x = _dirRoll * GameConfig.VelocidadeCorrer * 1.35f;
            if (NoChao && v.y <= 0.1f) v.y = VyNoChao();
            else v.y -= GameConfig.Gravidade * Time.fixedDeltaTime;
            _rb.velocity = v;
            AjustarColisor(true); // baixinho enquanto rola
        }

        // desliza no chao mantendo o embalo, desacelerando ate parar/levantar
        private void IniciarDeslize(float dir)
        {
            _deslizando = true;
            _tempoDeslize = 0.6f;
            _cdDeslize = 0.7f;
            _dirDeslize = dir == 0 ? (VoltadoDireita ? 1f : -1f) : dir;
            _velDeslize = GameConfig.VelocidadeCorrer * 1.15f; // arranca um pouco mais rapido que a corrida
            AudioManager.Instance?.Play("esquiva", 0.5f);
        }

        private void MovimentoDeslize()
        {
            _velDeslize = Mathf.MoveTowards(_velDeslize, 0f, 14f * Time.fixedDeltaTime); // atrito do deslize
            Vector3 v = _rb.velocity;
            v.x = _dirDeslize * _velDeslize;
            if (NoChao && v.y <= 0.1f) v.y = VyNoChao();
            else v.y -= GameConfig.Gravidade * Time.fixedDeltaTime;
            _rb.velocity = v;

            // cancela cedo com pulo (slide-jump)
            if (PuloPressionado() && (NoChao || _temCoyote))
            {
                _deslizando = false;
                v.y = GameConfig.ForcaPulo;
                _rb.velocity = v;
                _podeDuploPulo = true;
                _bufferPulo = 0f;
                AudioManager.Instance?.Play("jump");
            }

            AjustarColisor(true); // corpo baixo durante o deslize
        }

        private void MovimentoEscalada()
        {
            float ver = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ver += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ver -= 1f;
            float hor = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) hor -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) hor += 1f;

            Vector3 v = new Vector3(hor * GameConfig.VelocidadeAndar * 0.6f,
                                    ver * GameConfig.VelocidadeEscalar, 0);
            _rb.velocity = v;

            if (PuloPressionado())
            {
                _emEscada = false;
                _rb.velocity = new Vector3(v.x, GameConfig.ForcaPulo * 0.9f, 0);
                AudioManager.Instance?.Play("jump");
            }
        }

        // Le o toque BUFFERIZADO (capturado no Update). Nunca ler GetKeyDown aqui:
        // o FixedUpdate nao roda em todo frame e o toque se perde.
        private bool PuloPressionado()
        {
            return _bufferPulo > 0f;
        }

        // CAUSA #1 DO "BATENDO NO CHAO":
        // isto rodava em TODO FixedUpdate. Escrever height/center de um CapsuleCollider
        // reconstroi a forma no PhysX e JOGA FORA o cache de contato de repouso. Sem o
        // contato persistente, o soldado re-penetrava o chao e era empurrado de volta a
        // cada passo de fisica = vibracao vertical. Alem disso Mathf.Lerp nunca chega
        // exatamente no alvo, entao a escrita nunca parava.
        // Agora so escreve quando MUDA de verdade, e trava no alvo quando chega perto.
        private void AjustarColisor(bool agachar)
        {
            float alvo = agachar ? _alturaAgachado : _alturaNormal;
            if (Mathf.Abs(_col.height - alvo) < 0.002f)
            {
                if (_col.height != alvo)                      // encosta no alvo 1 vez e para
                {
                    _col.height = alvo;
                    _col.center = new Vector3(0, alvo / 2f, 0);
                }
                return;                                        // <- em repouso NAO toca no colisor
            }

            float novo = Mathf.Lerp(_col.height, alvo, 0.4f);
            _col.height = novo;
            _col.center = new Vector3(0, novo / 2f, 0);
        }

        private void ChecarQueda()
        {
            if (_caiu) return;
            if (transform.position.y < _quedaLimite)
            {
                _caiu = true;
                _controleAtivo = false;
                _rb.velocity = Vector3.zero;
                AudioManager.Instance?.Play("hurt");
                GameManager.Instance?.PerderVida();
            }
        }

        // =========================================================
        //  Mira (mouse -> cima/frente/baixo)
        // =========================================================
        private void AtualizarMira()
        {
            float facing = VoltadoDireita ? 1f : -1f;
            _aimRawX = facing;                            // fallback: para onde encara
            Vector3 dir = new Vector3(facing, 0f, 0f);    // fallback: reto a frente

            var cam = Camera.main;
            if (cam != null && Input.mousePresent)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                Plane plano = new Plane(Vector3.forward, Vector3.zero); // plano z=0
                if (plano.Raycast(ray, out float enter))
                {
                    Vector3 mundo = ray.GetPoint(enter);
                    // Mira a partir do OMBRO (ponto FIXO no corpo). Antes mirava a partir
                    // do cano, mas o cano depende da mira -> realimentacao: a direcao
                    // "corria atras" da propria origem e o tiro saia torto perto do corpo.
                    // Do ombro a direcao e estavel, e o cano fica SEMPRE nessa mesma linha
                    // (ver PosCano), entao a bala sai da arma e vai reto ao ponto clicado.
                    Vector3 d = mundo - PosOmbro; d.z = 0f;
                    if (d.sqrMagnitude > 0.04f)
                    {
                        _aimRawX = d.x;
                        // ============== O "TIRO TORTO" ESTAVA AQUI ==============
                        // Antes:  dir = (Mathf.Abs(nd.x) * facing, nd.y).normalized
                        // Isso ESPELHAVA a mira para o lado que o soldado encarava. E
                        // VoltadoDireita segue o MOVIMENTO, nao o mouse. Entao correr para
                        // a DIREITA e mirar para a ESQUERDA fazia a bala sair para a
                        // DIREITA -- no espelho de onde voce clicou. Todo tiro saia torto,
                        // e nunca acertava onde a mira estava.
                        // Agora a bala vai EXATAMENTE para o ponto mirado. Quem gira para
                        // encarar o alvo e o SOLDADO (ver LerAcoes e MovimentoNormal).
                        dir = d.normalized;
                    }
                }
            }

            _aimDir = dir;
            // a POSE continua em 3 niveis, mas o SPRITE gira pela direcao real da mira
            _pitchMira = dir.y > 0.33f ? 0.7f : (dir.y < -0.33f ? -0.7f : 0f);
            if (_anim != null) _anim.DefinirMira(dir);
        }

        // =========================================================
        //  Acoes (atirar, recarregar, trocar, granada, corpo a corpo, rolar)
        // =========================================================
        private void LerAcoes()
        {
            // ---- rolar / esquiva ----
            if (Input.GetKeyDown(KeyCode.LeftControl) && !_rolando && _cdRoll <= 0f && NoChao && !_emEscada)
                IniciarRoll();

            if (_rolando) return; // durante a esquiva nao atira/recarrega

            // ---- trocar de arma ----
            if (Input.GetKeyDown(KeyCode.Q)) _weapons.Trocar(1);
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f) _weapons.Trocar(1);
            else if (scroll < -0.01f) _weapons.Trocar(-1);
            if (Input.GetKeyDown(KeyCode.Alpha1)) _weapons.TrocarPara(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) _weapons.TrocarPara(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) _weapons.TrocarPara(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) _weapons.TrocarPara(3);

            // ---- recarregar ----
            if (Input.GetKeyDown(KeyCode.R)) _weapons.Recarregar();

            // ---- granada ----
            if (Input.GetKeyDown(KeyCode.G) && _grenade.PodeLancar)
            {
                _grenade.Lancar(PosCano, _aimDir);
                _tempoGranada = 0.35f;
            }

            // ---- corpo a corpo ----
            if ((Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.F)) && _combat.Pronto)
            {
                _combat.Golpear(PosCano, DirFrente());
                _tempoMelee = 0.35f;
            }

            // ---- atirar ----
            bool disparo = _weapons.Automatica
                ? (Input.GetMouseButton(0) || Input.GetKey(KeyCode.J))
                : (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J));
            if (disparo && _tempoMelee <= 0f && _tempoGranada <= 0f && !_weapons.Recarregando)
            {
                // 1) ENCARA O ALVO. Sem isto, correndo p/ um lado e mirando p/ o outro, o
                //    cano fica nas COSTAS e a bala nasce do lado errado do corpo.
                if (Mathf.Abs(_aimRawX) > 0.15f) VoltadoDireita = _aimRawX > 0f;

                // 2) ERGUE O FUZIL. PosCano devolve a ponta do cano DA POSE DESENHADA.
                //    Antes, TentarDisparar rodava ANTES da pose trocar para MIRA, entao o
                //    1o tiro de cada rajada (e TODO tiro isolado de pistola/espingarda)
                //    saia com o soldado ainda na pose parada -- fuzil nas costas -- e a
                //    bala, o flash e a capsula nasciam NO AR, na frente da cabeca dele.
                _tempoMira = 0.40f;
                Estado = EstadoPlayer.Atacando;
                if (_anim != null) _anim.Aplicar(EstadoPlayer.Atacando, VoltadoDireita);

                // 3) recalcula a mira com o LADO e a POSE ja corretos: _aimDir passa a
                //    sair exatamente da boca do fuzil ate o ponto clicado.
                AtualizarMira();

                // 4) so agora dispara.
                _weapons.TentarDisparar(PosCano, _aimDir);
                _tempoAtaque = 0.14f;   // freio de movimento (curto)
            }
        }

        private void IniciarRoll()
        {
            _rolando = true;
            _tempoRoll = 0.42f;
            _cdRoll = 0.8f;
            _dirRoll = VoltadoDireita ? 1f : -1f;
            _health?.DarInvulnerabilidade(0.42f);
            AudioManager.Instance?.Play("esquiva", 0.7f);
        }

        private Vector3 DirFrente()
        {
            return VoltadoDireita ? Vector3.right : Vector3.left;
        }

        private void LerInteracao()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                var cols = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, 1.4f);
                foreach (var c in cols)
                {
                    var i = c.GetComponentInParent<IInteragivel>();
                    if (i != null) { i.Interagir(this); break; }
                }
            }
        }

        // =========================================================
        //  Passos por superficie
        // =========================================================
        private void AtualizarPassos()
        {
            if (_rolando || !NoChao || _emEscada) { _stepTimer = 0.05f; return; }
            float velX = Mathf.Abs(_rb.velocity.x);
            if (velX < 0.6f) { _stepTimer = 0.05f; return; }

            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                bool correndo = velX > GameConfig.VelocidadeAndar + 0.5f;
                _stepTimer = correndo ? 0.22f : 0.30f;
                AudioManager.Instance?.PlayAt(SuperficieSom(), transform.position, 0.35f);
            }
        }

        private string SuperficieSom()
        {
            var hits = Physics.RaycastAll(transform.position + Vector3.up * 0.4f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider.transform == transform || h.collider.transform.IsChildOf(transform)) continue;
                string nome = h.collider.gameObject.name.ToLower();
                if (nome.Contains("movel") || nome.Contains("elev")) return "passo_metal";
                if (nome.Contains("plataforma")) return "passo_pedra";
                if (nome.Contains("solo")) return "passo_terra";
                return "passo_terra";
            }
            return "passo_terra";
        }

        // =========================================================
        //  Escada (chamado pelo trigger Climbable)
        // =========================================================
        public void EntrarEscada()
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ||
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                _emEscada = true;
        }
        public void SairEscada() { _emEscada = false; }

        // =========================================================
        //  Estado visual
        // =========================================================
        private void AtualizarEstadoVisual()
        {
            EstadoPlayer e;
            if (_health != null && _health.Morto) e = EstadoPlayer.Derrota;
            else if (_rolando || _deslizando) e = EstadoPlayer.Rolando;
            else if (_tempoMelee > 0f) e = EstadoPlayer.Corpo;
            else if (_tempoGranada > 0f) e = EstadoPlayer.Arremesso;
            else if (_weapons != null && _weapons.Recarregando) e = EstadoPlayer.Recarregando;
            else if (_tempoMira > 0f) e = EstadoPlayer.Atacando;
            else if (_emEscada) e = EstadoPlayer.Escalando;
            else if (!NoChao) e = _rb.velocity.y > 0.2f ? EstadoPlayer.Pulando : EstadoPlayer.Caindo;
            else if (_agachado) e = EstadoPlayer.Agachado;
            else if (Mathf.Abs(_rb.velocity.x) > GameConfig.VelocidadeAndar + 0.5f) e = EstadoPlayer.Correndo;
            else if (Mathf.Abs(_rb.velocity.x) > 0.3f) e = EstadoPlayer.Andando;
            else e = EstadoPlayer.Idle;

            Estado = e;
            if (_anim != null) _anim.Aplicar(e, VoltadoDireita);
        }

        public void ComemorarVitoria()
        {
            _controleAtivo = false;
            _rb.velocity = Vector3.zero;
            Estado = EstadoPlayer.Vitoria;
            if (_anim != null) _anim.Aplicar(EstadoPlayer.Vitoria, VoltadoDireita);
        }

        public void DesativarControle() { _controleAtivo = false; if (_rb != null) _rb.velocity = Vector3.zero; }

        // ---- API publica para os subsistemas e HUD ----
        public Rigidbody Corpo => _rb;
        public PlayerHealth Saude => _health;
        public WeaponSystem Armas => _weapons;
        public PlayerGrenade Granadas => _grenade;
        public Vector3 AimDir => _aimDir;
        public float PitchMira => _pitchMira;
        /// <summary>Ombro do soldado (ponto fixo de onde o fuzil e segurado).</summary>
        public Vector3 PosOmbro
        {
            get
            {
                float lado = VoltadoDireita ? 1f : -1f;
                return transform.position + new Vector3(lado * 0.26f, 1.62f, 0f);
            }
        }

        /// <summary>
        /// Boca do fuzil = OMBRO + direcao da mira x comprimento da arma.
        /// O "tiro fora da arma" acontecia porque a origem vinha de uma TABELA de pixels
        /// por pose: bastava o frame trocar (corrida, pulo, recarga) para a bala nascer
        /// na cabeca ou no ar. Agora a origem e GEOMETRICA: esteja o soldado em qualquer
        /// pose, o flash, a capsula e a bala nascem sempre no fim do cano, na MESMA linha
        /// da mira — para frente, para cima, para baixo e nas diagonais, sem falhas.
        /// </summary>
        public Vector3 PosCano
        {
            get
            {
                Vector3 dir = _aimDir.sqrMagnitude > 0.01f ? _aimDir.normalized
                              : (VoltadoDireita ? Vector3.right : Vector3.left);
                return PosOmbro + dir * 1.05f;   // ~1.05 un. = braco + fuzil
            }
        }
    }

    /// <summary>Interface para objetos que o jogador pode acionar com E.</summary>
    public interface IInteragivel { void Interagir(PlayerController player); }
}
