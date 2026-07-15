using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    public enum EstadoK9 { Seguindo, Esperando, Alerta, Curando, Comemorando, Furtivo }

    /// <summary>
    /// K9-CYBER ALPHA — pastor alemao cibernetico, companheiro de combate do jogador.
    /// Segue o parceiro, detecta inimigos e LUTA de verdade: cospe FOGO pela boca de perto,
    /// dispara LASER da arma das costas de longe e da o BOTE no corpo a corpo. Cura o jogador
    /// ferido, late/rosna, evolui de nivel e demonstra emocao pelo LED. Menor que o soldado.
    /// IA em codigo (sem NavMesh).
    /// </summary>
    public class CompanionDog : MonoBehaviour
    {
        public string Nome = "K9-CYBER ALPHA";

        public int Nivel { get; private set; } = 1;
        public int XP { get; private set; }
        public int XPProximo => Nivel * 120;
        public int Amizade { get; private set; }
        public EstadoK9 Estado { get; private set; } = EstadoK9.Seguindo;

        public float Vida { get; private set; }
        public float VidaMax { get; private set; }
        public float Energia { get; private set; }
        public float EnergiaMax { get; private set; }

        private SpriteRenderer _sr;
        private Transform _visual;
        private Light _led;
        private GameObject _marcador;

        private float _velBase, _curaQtd, _alcanceDeteccao, _danoMordida;
        private float _cdCura, _cdLatido, _cdMordida, _cdGrunhido, _cdLaser, _cdFogo, _stuckTimer, _bob;
        private bool _direita = true;
        private float _followLado = -2.2f;
        private float _trocaLado;      // histerese: impede o cao de trocar de lado a cada tremida do mouse

        // ataques a distancia (escalam com o nivel)
        private float DanoLaser => 20f + Nivel * 3f;
        private float DanoFogo  => 14f + Nivel * 2.5f;

        // ---- animacao por quadros (walk/run cycle do MESMO cao) ----
        private Sprite[] _walk, _run;
        private Sprite _idle;
        private float _animTime;
        private float _halfH;
        private float _escala = 1f;
        private const float LIMIAR_ANDAR  = 0.12f;  // acima disso: anima caminhada
        private const float LIMIAR_CORRER = 3.6f;   // acima disso: anima corrida

        public void Configurar(Sprite sprite)
        {
            Nivel = SaveSystem.K9Nivel;
            XP = SaveSystem.K9XP;
            AtualizarAtributos();
            Vida = VidaMax; Energia = EnergiaMax;

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 14;
            // o cao e um sprite chapado; sem isto a luz projeta uma SOMBRA-QUADRADO preta no chao
            _sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _sr.receiveShadows = false;

            // carrega os quadros do walk/run cycle (mesmo cao, agora com as pernas mexendo)
            _walk = CarregarClipe("k9_walk_", 6);
            _run  = CarregarClipe("k9_run_", 6);
            _idle = SpriteLibrary.Get("Sprites/companion/k9_idle", 200f);
            if (_idle == null && _walk.Length > 0) _idle = _walk[0];

            // fallback seguro: se os quadros ainda nao foram colocados na pasta, usa o sprite antigo
            Sprite baseSp = _idle != null ? _idle : sprite;
            _sr.sprite = baseSp;

            // MENOR que o soldado (o player tem ~2.1): cao ~1.3 de altura, patas no chao
            float alvoAltura = 0.9f;   // cao companheiro: menor que o soldado (~2.1)
            if (baseSp != null && baseSp.bounds.size.y > 0.001f)
            {
                _escala = alvoAltura / baseSp.bounds.size.y;
                _visual.localScale = Vector3.one * _escala;
                _halfH = baseSp.bounds.size.y * _escala * 0.5f;
                _visual.localPosition = new Vector3(0, _halfH, 0);
            }
            // SEM Billboard. Ele girava o sprite em 3D e, junto com a escala X negativa
            // (o espelhamento), deformava/tombava o cao e ate invertia o lado que ele
            // encarava. O soldado nao usa Billboard — o cao agora tambem nao.

            _led = new GameObject("led").AddComponent<Light>();
            _led.transform.SetParent(transform, false);
            _led.transform.localPosition = new Vector3(0, 0.7f, -0.4f);
            _led.color = new Color(0.3f, 0.6f, 1f); _led.range = 2.6f; _led.intensity = 1.2f;

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true; col.height = 1.4f; col.radius = 0.42f; col.center = new Vector3(0, 0.7f, 0);

            _marcador = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _marcador.transform.localScale = Vector3.one * 0.3f;
            var mc = _marcador.GetComponent<Collider>(); if (mc != null) Destroy(mc);
            _marcador.GetComponent<MeshRenderer>().material = MaterialUtil.Emissivo(new Color(1f, 0.3f, 0.2f), 1.6f);
            _marcador.SetActive(false);

            AudioManager.Instance?.Play("bark_happy", 0.6f);
        }

        private void AtualizarAtributos()
        {
            VidaMax = 120f + Nivel * 12f;
            EnergiaMax = 100f + Nivel * 8f;
            _velBase = 4.6f + Nivel * 0.12f;
            _curaQtd = 18f + Nivel * 3f;
            _alcanceDeteccao = 9f + Nivel * 0.6f;
            _danoMordida = 18f + Nivel * 2.5f;
        }

        /// <summary>Carrega os quadros de um clipe (prefixo + 0..n-1) da pasta Resources.</summary>
        private Sprite[] CarregarClipe(string prefixo, int n)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < n; i++)
            {
                var sp = SpriteLibrary.Get("Sprites/companion/" + prefixo + i, 200f);
                if (sp != null) list.Add(sp);
            }
            return list.ToArray();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            var player = gm.Player;
            if (player == null || gm.Estado != EstadoJogo.Jogando)
            {
                if (_marcador != null) _marcador.SetActive(false);
                return;
            }

            if (_cdCura > 0) _cdCura -= Time.deltaTime;
            if (_cdLatido > 0) _cdLatido -= Time.deltaTime;
            if (_cdMordida > 0) _cdMordida -= Time.deltaTime;
            if (_cdGrunhido > 0) _cdGrunhido -= Time.deltaTime;
            if (_cdLaser > 0) _cdLaser -= Time.deltaTime;
            if (_cdFogo > 0) _cdFogo -= Time.deltaTime;
            _bob += Time.deltaTime;

            Energia = Mathf.Min(EnergiaMax, Energia + (12f + Nivel) * Time.deltaTime);
            Vida = Mathf.Min(VidaMax, Vida + 6f * Time.deltaTime);

            float dist = Vector3.Distance(transform.position, player.transform.position);
            var inimigo = InimigoMaisProximo(_alcanceDeteccao, out float distIni);

            DecidirEstado(player, dist, inimigo, distIni);

            switch (Estado)
            {
                case EstadoK9.Curando:  AtoCurar(player); break;
                case EstadoK9.Alerta:   AtoAlerta(inimigo, distIni); break;
                case EstadoK9.Comemorando: AtoComemorar(); break;
                default: if (_marcador != null) _marcador.SetActive(false); break;
            }

            MoverSeguindo(player, dist);
            AtualizarVisual(player);
        }

        private void DecidirEstado(PlayerController player, float dist, EnemyController inimigo, float distIni)
        {
            var saude = player.Saude;
            bool feridoGrave = saude != null && saude.Vida < GameConfig.VidaMaxima * 0.5f;

            if (player.Estado == EstadoPlayer.Vitoria) { Estado = EstadoK9.Comemorando; return; }
            if (feridoGrave && Energia >= 25f && _cdCura <= 0f && dist < 4.5f) { Estado = EstadoK9.Curando; return; }
            if (inimigo != null) { Estado = EstadoK9.Alerta; return; }
            if (player.Estado == EstadoPlayer.Agachado && dist < 3f) { Estado = EstadoK9.Furtivo; return; }
            Estado = dist > 2.6f ? EstadoK9.Seguindo : EstadoK9.Esperando;
        }

        private void AtoCurar(PlayerController player)
        {
            var saude = player.Saude;
            if (saude == null) return;
            saude.Curar(_curaQtd);
            Energia -= 25f;
            _cdCura = 10f;
            Amizade += 1;
            AudioManager.Instance?.Play("bark", 0.7f);

            var f = new GameObject("curaFX");
            f.transform.position = player.transform.position + Vector3.up * 1f;
            var l = f.AddComponent<Light>(); l.color = GameConfig.VerdeHUD; l.range = 4f; l.intensity = 3f;
            Destroy(f, 0.4f);
            if (_marcador != null) _marcador.SetActive(false);
        }

        // ---- combate: laser (longe), fogo (perto), bote (colado) ----
        private void AtoAlerta(EnemyController inimigo, float distIni)
        {
            if (inimigo == null) { if (_marcador != null) _marcador.SetActive(false); return; }

            // vira para o inimigo
            _direita = inimigo.transform.position.x > transform.position.x;

            if (_marcador != null)
            {
                _marcador.SetActive(true);
                _marcador.transform.position = inimigo.transform.position + Vector3.up * 2.4f;
                _marcador.transform.Rotate(0, 180f * Time.deltaTime, 0);
            }

            if (_cdGrunhido <= 0f) { AudioManager.Instance?.Play("growl_dog", 0.5f); _cdGrunhido = 3f; }

            // BOTE (bem perto)
            if (distIni < 2.0f && _cdMordida <= 0f && Energia >= 10f)
            {
                inimigo.LevarDano(_danoMordida, transform.position);
                Energia -= 10f; Vida -= 3f;
                _cdMordida = 1.8f;
                AudioManager.Instance?.Play("bark", 0.8f);
                StartCoroutine(Investida(inimigo.transform.position));
                return;
            }

            // CUSPE DE FOGO (perto/medio)
            if (distIni <= 4.2f && _cdFogo <= 0f && Energia >= 15f)
            {
                CuspirFogo(inimigo);
                Energia -= 15f; _cdFogo = 2.4f;
                return;
            }

            // LASER (longe) — arma das costas
            if (distIni <= _alcanceDeteccao && _cdLaser <= 0f && Energia >= 12f)
            {
                AtirarLaser(inimigo);
                Energia -= 12f; _cdLaser = 1.6f;
            }
        }

        // cuspe de fogo pela boca: cone curto de dano + visual, atinge alvo e vizinhos a frente
        private void CuspirFogo(EnemyController alvo)
        {
            AudioManager.Instance?.Play("flame", 0.8f);
            AudioManager.Instance?.Play("bark", 0.5f);
            float dir = _direita ? 1f : -1f;
            Vector3 boca = transform.position + new Vector3(dir * 0.7f, 0.7f, 0);

            // visual: chama laranja saindo da boca
            var fogo = new GameObject("cuspeFogo");
            fogo.transform.position = boca;
            var l = fogo.AddComponent<Light>();
            l.color = new Color(1f, 0.55f, 0.15f); l.range = 4.5f; l.intensity = 4f;
            var spr = SpriteLibrary.Get("Sprites/hazards/fogo", 200f);
            if (spr != null)
            {
                var sv = new GameObject("chama"); sv.transform.SetParent(fogo.transform, false);
                sv.transform.localPosition = new Vector3(dir * 0.8f, 0, 0);
                var sr = sv.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.sortingOrder = 16;
                float m = Mathf.Max(spr.bounds.size.x, spr.bounds.size.y);
                sv.transform.localScale = Vector3.one * (1.4f / Mathf.Max(0.01f, m));
                sv.transform.localScale = new Vector3(sv.transform.localScale.x * dir, sv.transform.localScale.y, 1f);
            }
            Destroy(fogo, 0.35f);

            // dano no alvo + inimigos proximos a frente (splash do cone)
            foreach (var e in Object.FindObjectsOfType<EnemyController>())
            {
                if (!e.EstaVivo) continue;
                Vector3 dd = e.transform.position - boca;
                if (Mathf.Sign(dd.x) == dir && dd.magnitude < 4.5f)
                    e.LevarDano(DanoFogo, transform.position);
            }
        }

        // laser da arma das costas: feixe reto instantaneo ate o alvo
        private void AtirarLaser(EnemyController alvo)
        {
            AudioManager.Instance?.Play("laser", 0.8f);
            Vector3 canhao = transform.position + new Vector3((_direita ? 0.3f : -0.3f), 1.0f, -0.05f);
            Vector3 mira = alvo.transform.position + Vector3.up * (0.9f);

            var go = new GameObject("laserK9");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, canhao); lr.SetPosition(1, mira);
            lr.startWidth = 0.10f; lr.endWidth = 0.03f;
            lr.material = MaterialUtil.Emissivo(new Color(0.4f, 0.85f, 1f), 2.2f);
            lr.numCapVertices = 2;
            var luz = new GameObject("laserLuz"); luz.transform.position = mira;
            var l = luz.AddComponent<Light>(); l.color = new Color(0.4f, 0.85f, 1f); l.range = 3f; l.intensity = 3f;
            Destroy(luz, 0.1f);
            Destroy(go, 0.08f);

            alvo.LevarDano(DanoLaser, transform.position);
            ImpactFX.Faiscas(mira);
        }

        private IEnumerator Investida(Vector3 alvo)
        {
            if (_visual == null) yield break;
            Vector3 ini = _visual.localPosition;
            float t = 0f;
            float dir = Mathf.Sign(alvo.x - transform.position.x);
            while (t < 0.18f && _visual != null)
            {
                t += Time.deltaTime;
                _visual.localPosition = ini + new Vector3(dir * 0.5f * Mathf.Sin(t / 0.18f * Mathf.PI), 0, 0);
                yield return null;
            }
            if (_visual != null) _visual.localPosition = ini;
        }

        private void AtoComemorar()
        {
            if (_cdLatido <= 0f) { AudioManager.Instance?.Play("bark_happy", 0.7f); _cdLatido = 1.2f; }
            if (_marcador != null) _marcador.SetActive(false);
        }

        private void MoverSeguindo(PlayerController player, float dist)
        {
            // ====== O CAO ATRAVESSANDO E VIRANDO SEM PARAR ESTAVA AQUI ======
            // Antes:  _followLado = player.VoltadoDireita ? -2.2f : 2.2f;
            // VoltadoDireita segue o MOUSE (o soldado encara o cursor quando esta parado,
            // e tambem quando atira). Bastava mexer o mouse por cima dele para o alvo do
            // cao SALTAR 4.4 unidades de um lado ao outro -- ATRAVESSANDO o soldado. O cao
            // ia e voltava dentro do player e espelhava o sprite a cada ida e volta: era
            // exatamente o "cao virando e batendo no soldado".
            // Agora o lado so muda quando o soldado ANDA DE VERDADE, e ainda passa por uma
            // histerese antes de trocar. Mouse e mira nao mexem mais com o cao.
            float velPlayer = player.Corpo != null ? player.Corpo.velocity.x : 0f;
            float ladoQuer = _followLado;
            if (velPlayer > 0.6f)       ladoQuer = -2.2f;   // player indo p/ direita -> cao atras (a esquerda)
            else if (velPlayer < -0.6f) ladoQuer =  2.2f;   // player indo p/ esquerda -> cao atras (a direita)

            if (Mathf.Abs(ladoQuer - _followLado) > 0.01f)
            {
                _trocaLado += Time.deltaTime;
                if (_trocaLado >= 0.4f) { _followLado = ladoQuer; _trocaLado = 0f; }
            }
            else _trocaLado = 0f;

            float alvoX = player.transform.position.x + _followLado;

            if (dist > 16f)
            {
                Vector3 p = transform.position; p.x = alvoX; p.y = player.transform.position.y + 0.2f;
                transform.position = p;
                _stuckTimer = 0f;
                return;
            }

            Vector3 pos = transform.position;
            float dx = alvoX - pos.x;
            float adx = Mathf.Abs(dx);

            bool correndo = player.Estado == EstadoPlayer.Correndo;
            float vel = _velBase * (adx > 4f ? 2.2f : correndo ? 1.5f : 1f); // alcança o player correndo
            if (Estado == EstadoK9.Curando || Estado == EstadoK9.Comemorando) vel *= 0.2f;
            if (Estado == EstadoK9.Alerta) vel *= 0.85f; // segura um pouco pra atirar

            if (adx > 0.4f)
            {
                float passo = Mathf.Sign(dx) * vel * Time.deltaTime;
                if (Mathf.Abs(passo) > adx) passo = dx;
                pos.x += passo;
                // VIRADA: so muda de lado com folga (0.9). Com 0.4 o cao chegava no alvo,
                // passava 1 cm, virava, voltava, virava de novo — o "cao virando sem parar".
                if (Estado != EstadoK9.Alerta && adx > 0.9f) _direita = dx > 0;
                _stuckTimer = 0f;
            }
            else _stuckTimer += Time.deltaTime;

            // chao: so encaixa em geometria SOLIDA do cenario (ignora player, inimigos e o proprio cao)
            float groundY = pos.y;
            bool achouChao = false;
            var raios = Physics.RaycastAll(pos + Vector3.up * 5f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
            float maisPerto = float.MaxValue;
            for (int i = 0; i < raios.Length; i++)
            {
                var col = raios[i].collider;
                if (col == null) continue;
                var t = col.transform;
                if (t == transform || t.IsChildOf(transform)) continue;
                if (col.GetComponentInParent<PlayerController>() != null) continue;
                if (col.GetComponentInParent<EnemyController>() != null) continue;
                if (col.GetComponentInParent<CompanionDog>() != null) continue;
                if (raios[i].distance < maisPerto) { maisPerto = raios[i].distance; groundY = raios[i].point.y; achouChao = true; }
            }
            if (!achouChao) groundY = player.transform.position.y;

            float alvoY = groundY;
            if (player.transform.position.y - groundY > 2.5f) alvoY = player.transform.position.y;
            pos.y = Mathf.Lerp(pos.y, alvoY, 10f * Time.deltaTime);

            transform.position = pos;

            if (_stuckTimer > 2.5f && dist > 5f)
            {
                Vector3 p = transform.position; p.x = alvoX; transform.position = p; _stuckTimer = 0f;
            }
        }

        private float _ultimaX = -9999f;
        private bool _olharAtual = true;
        private float _flipTimer;
        private void AtualizarVisual(PlayerController player)
        {
            bool olharDesejado = (Estado == EstadoK9.Esperando || Estado == EstadoK9.Curando)
                ? player.transform.position.x > transform.position.x
                : _direita;

            // DEBOUNCE do espelhamento: o lado desejado troca toda hora quando o estado
            // alterna Seguindo/Esperando na fronteira de distancia — o sprite ficava
            // "tremendo" espelhando a cada frame. So espelha se o desejo ficar estavel
            // por 0.25s.
            if (olharDesejado != _olharAtual)
            {
                _flipTimer += Time.deltaTime;
                if (_flipTimer >= 0.25f) { _olharAtual = olharDesejado; _flipTimer = 0f; }
            }
            else _flipTimer = 0f;
            bool olharDireita = _olharAtual;

            // velocidade instantanea -> escolhe idle / caminhada / corrida
            float dxFrame = _ultimaX < -9000f ? 0f : Mathf.Abs(transform.position.x - _ultimaX);
            _ultimaX = transform.position.x;
            float vel = Time.deltaTime > 0f ? dxFrame / Time.deltaTime : 0f;

            Sprite[] clipe = null; float fps = 0f;
            if (vel > LIMIAR_CORRER && _run != null && _run.Length > 0) { clipe = _run; fps = 13f; }
            else if (vel > LIMIAR_ANDAR && _walk != null && _walk.Length > 0) { clipe = _walk; fps = 9f; }

            if (_sr != null)
            {
                if (clipe != null)
                {
                    _animTime += Time.deltaTime * fps;
                    _sr.sprite = clipe[((int)_animTime) % clipe.Length];   // pernas em movimento
                }
                else
                {
                    if (_idle != null) _sr.sprite = _idle;                 // parado
                    _animTime = 0f;
                }

                // os quadros olham para a DIREITA por padrao -> espelha para a esquerda
                float sx = Mathf.Abs(_escala) * (olharDireita ? 1f : -1f);
                float bobY = clipe == null ? Mathf.Sin(_bob * 2.2f) * 0.015f : 0f; // respiracao ao parar
                // escala UNIFORME: com o Billboard girando o sprite em 3D, deixar o Z em
                // 1f (enquanto X/Y usam a escala do cao) deformava/deitava o bicho.
                _visual.localScale = new Vector3(sx, _escala, Mathf.Abs(_escala));
                _visual.localPosition = new Vector3(0f, _halfH + bobY, 0f);
            }
            if (_led != null) _led.color = CorEstado();
        }

        private Color CorEstado()
        {
            switch (Estado)
            {
                case EstadoK9.Alerta:      return new Color(1f, 0.25f, 0.2f);
                case EstadoK9.Curando:     return GameConfig.VerdeHUD;
                case EstadoK9.Comemorando: return GameConfig.Dourado;
                case EstadoK9.Furtivo:     return new Color(0.2f, 0.5f, 0.6f);
                default:                   return new Color(0.3f, 0.6f, 1f);
            }
        }

        private EnemyController InimigoMaisProximo(float alcance, out float distOut)
        {
            EnemyController melhor = null; float menor = alcance;
            foreach (var e in Object.FindObjectsOfType<EnemyController>())
            {
                if (!e.EstaVivo) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < menor) { menor = d; melhor = e; }
            }
            distOut = melhor != null ? menor : 999f;
            return melhor;
        }

        public void RegistrarAbate(Vector3 pos, int pontos)
        {
            if (Vector3.Distance(transform.position, pos) > 14f) return;
            GanharXP(Mathf.Max(8, pontos / 4));
            Amizade += 1;
        }

        private void GanharXP(int q)
        {
            XP += q;
            bool subiu = false;
            while (XP >= XPProximo && Nivel < 10) { XP -= XPProximo; Nivel++; subiu = true; }
            if (subiu) SubirNivel();
            SaveSystem.SalvarK9(Nivel, XP);
        }

        private void SubirNivel()
        {
            AtualizarAtributos();
            Vida = VidaMax; Energia = EnergiaMax;
            Amizade += 5;
            AudioManager.Instance?.Play("bark_happy", 0.9f);
            AudioManager.Instance?.Play("item", 0.7f);
            var f = new GameObject("nivelFX");
            f.transform.position = transform.position + Vector3.up * 1.5f;
            var l = f.AddComponent<Light>(); l.color = GameConfig.Dourado; l.range = 6f; l.intensity = 5f;
            Destroy(f, 0.6f);
        }

        public float VidaPct => VidaMax > 0 ? Mathf.Clamp01(Vida / VidaMax) : 0f;
        public float EnergiaPct => EnergiaMax > 0 ? Mathf.Clamp01(Energia / EnergiaMax) : 0f;
        public float XPPct => XPProximo > 0 ? Mathf.Clamp01((float)XP / XPProximo) : 0f;
        public string EstadoTexto
        {
            get
            {
                switch (Estado)
                {
                    case EstadoK9.Seguindo: return "SEGUINDO";
                    case EstadoK9.Esperando: return "EM GUARDA";
                    case EstadoK9.Alerta: return "ALERTA";
                    case EstadoK9.Curando: return "CURANDO";
                    case EstadoK9.Comemorando: return "COMEMORANDO";
                    case EstadoK9.Furtivo: return "FURTIVO";
                    default: return "";
                }
            }
        }

        void OnDestroy()
        {
            if (_marcador != null) Destroy(_marcador);
        }
    }
}
