using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>Ficha de uma arma (data-driven, facil balancear).</summary>
    public struct ArmaDef
    {
        public string nome;
        public float dano;
        public float cadencia;      // disparos por segundo
        public int pellets;         // projeteis por disparo (espingarda > 1)
        public float espalhamento;  // graus de dispersao
        public float alcance;
        public int pente;           // capacidade do pente
        public int reservaInicial;  // municao de reserva
        public float recarga;       // segundos de recarga
        public float recuo;         // intensidade do coice
        public bool automatica;     // segurar dispara em rajada
        public Color corTracador;
        public string som;
    }

    /// <summary>
    /// Arsenal do soldado: pistola, fuzil, espingarda e metralhadora. Cada arma tem
    /// sistema de municao (pente + reserva), recarga temporizada, recuo (coice + tremor +
    /// dispersao dinamica), flash do cano, capsula ejetada, tracador e balistica por
    /// SphereCast respeitando a direcao da mira (cima/frente/baixo). Dispara o evento de
    /// combate para os inimigos "ouvirem" o tiro. Consumido pela HUD (municao/arma/mira).
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        private readonly List<ArmaDef> _armas = new List<ArmaDef>();
        private readonly List<int> _pente = new List<int>();
        private readonly List<int> _reserva = new List<int>();
        private int _idx;
        private float _cd;
        private float _recargaRestante;
        private float _espalhamentoAtual;   // dispersao acumulada (bloom)
        private Sprite _spCapsula;
        private Transform _player;

        public bool Recarregando => _recargaRestante > 0f;
        public string ArmaNome => _armas.Count > 0 ? _armas[_idx].nome : "";
        public int MunicaoPente => _pente.Count > 0 ? _pente[_idx] : 0;
        public int MunicaoReserva => _reserva.Count > 0 ? _reserva[_idx] : 0;
        public int PenteMax => _armas.Count > 0 ? _armas[_idx].pente : 0;
        public bool Automatica => _armas.Count > 0 && _armas[_idx].automatica;
        public int TotalArmas => _armas.Count;
        public int IndiceArma => _idx;
        /// <summary>Abertura visual da mira (0 parado, cresce ao atirar/correr).</summary>
        public float EspalhamentoVisual => Mathf.Clamp01((_armas.Count > 0 ? _espalhamentoAtual / 14f : 0f));

        void Awake()
        {
            _player = transform;
            _spCapsula = null; // capsula desenhada como cubo dourado

            // ---- Definicao das 4 armas ----
            Add(new ArmaDef {
                nome = "PISTOLA", dano = 22, cadencia = 4.5f, pellets = 1, espalhamento = 1.5f,
                alcance = 16f, pente = 12, reservaInicial = 96, recarga = 0.9f, recuo = 0.6f,
                automatica = false, corTracador = new Color(1f, 0.9f, 0.5f), som = "shoot"
            });
            Add(new ArmaDef {
                nome = "FUZIL", dano = 30, cadencia = 9f, pellets = 1, espalhamento = 2.5f,
                alcance = 22f, pente = 30, reservaInicial = 120, recarga = 1.4f, recuo = 1.0f,
                automatica = true, corTracador = new Color(1f, 0.85f, 0.45f), som = "shoot"
            });
            Add(new ArmaDef {
                nome = "ESPINGARDA", dano = 14, cadencia = 1.4f, pellets = 7, espalhamento = 9f,
                alcance = 12f, pente = 6, reservaInicial = 36, recarga = 1.8f, recuo = 2.4f,
                automatica = false, corTracador = new Color(1f, 0.75f, 0.35f), som = "shoot"
            });
            Add(new ArmaDef {
                nome = "METRALHADORA", dano = 16, cadencia = 13f, pellets = 1, espalhamento = 4.5f,
                alcance = 20f, pente = 60, reservaInicial = 240, recarga = 2.2f, recuo = 0.8f,
                automatica = true, corTracador = new Color(1f, 0.8f, 0.4f), som = "laser"
            });

            _idx = 1; // comeca com o fuzil
        }

        private void Add(ArmaDef a)
        {
            _armas.Add(a);
            _pente.Add(a.pente);
            _reserva.Add(a.reservaInicial);
        }

        void Update()
        {
            if (_cd > 0f) _cd -= Time.deltaTime;
            if (_recargaRestante > 0f)
            {
                _recargaRestante -= Time.deltaTime;
                if (_recargaRestante <= 0f) FinalizarRecarga();
            }
            // dispersao volta ao normal com o tempo (recuperacao da mira)
            _espalhamentoAtual = Mathf.MoveTowards(_espalhamentoAtual, 0f, 18f * Time.deltaTime);
        }

        /// <summary>Tenta disparar na direcao dada, saindo do cano em 'muzzle'.</summary>
        public void TentarDisparar(Vector3 muzzle, Vector3 dir)
        {
            if (_armas.Count == 0 || _cd > 0f || Recarregando) return;

            var a = _armas[_idx];
            if (_pente[_idx] <= 0)
            {
                AudioManager.Instance?.Play("arma_vazia", 0.6f);
                Recarregar();
                return;
            }

            _cd = 1f / Mathf.Max(0.1f, a.cadencia);
            _pente[_idx]--;

            // ---- balistica: um SphereCast por pellet, com dispersao ----
            for (int p = 0; p < Mathf.Max(1, a.pellets); p++)
            {
                float ang = (Random.value - 0.5f) * (a.espalhamento + _espalhamentoAtual) * Mathf.Deg2Rad;
                Vector3 d = Rotacionar(dir, ang);
                DispararRaio(muzzle, d, a);
            }

            // ---- feedback: flash, som, capsula, coice, dispersao, evento ----
            FX.Flash(muzzle, new Color(1f, 0.85f, 0.4f), 3.2f, 3.2f, 0.06f);
            AudioManager.Instance?.Play(a.som, 0.85f);
            EjetarCapsula(muzzle, dir);
            AplicarRecuo(a, dir);
            _espalhamentoAtual = Mathf.Min(14f, _espalhamentoAtual + a.espalhamento * 0.6f);
            CombatEvents.DispararTiro(muzzle, true);
        }

        private void DispararRaio(Vector3 origem, Vector3 dir, ArmaDef a)
        {
            float distImpacto = a.alcance;
            bool acertou = false;
            var hits = Physics.SphereCastAll(origem, 0.25f, dir, a.alcance, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform == _player || hit.collider.transform.IsChildOf(_player)) continue;

                var inimigo = hit.collider.GetComponentInParent<EnemyController>();
                if (inimigo != null && inimigo.EstaVivo) { inimigo.LevarDano(a.dano, origem); distImpacto = hit.distance; acertou = true; break; }
                var boss = hit.collider.GetComponentInParent<BossController>();
                if (boss != null && boss.EstaVivo) { boss.LevarDano(a.dano * 0.25f); distImpacto = hit.distance; acertou = true; break; }
                var heli = hit.collider.GetComponentInParent<HelicopterEnemy>();
                if (heli != null) { heli.LevarDano(a.dano, origem); distImpacto = hit.distance; acertou = true; break; }
                var aviao = hit.collider.GetComponentInParent<AviaoInimigo>();
                if (aviao != null) { aviao.LevarDano(a.dano, origem); distImpacto = hit.distance; acertou = true; break; }
                var dano = hit.collider.GetComponentInParent<IDanificavel>();
                if (dano != null) { dano.LevarDano(a.dano, origem); distImpacto = hit.distance; acertou = true; break; }

                // acertou geometria solida (parede/plataforma) -> para o tracador
                if (!hit.collider.isTrigger) { distImpacto = hit.distance; acertou = false; break; }
            }

            Vector3 fim = origem + dir * distImpacto;
            Bala(origem, fim, a.corTracador, acertou);
        }

        /// <summary>Projetil VISIVEL: uma bala que viaja do cano ate o ponto de impacto.</summary>
        private void Bala(Vector3 de, Vector3 ate, Color cor, bool acertou)
        {
            Vector3 delta = ate - de;
            float dist = delta.magnitude;
            if (dist < 0.01f) return;
            Vector3 dir = delta / dist;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "bala";
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);

            go.transform.position = de;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
            go.transform.localScale = new Vector3(0.34f, 0.11f, 0.11f);  // projetil alongado (latao)
            // brilho suave: nao estoura pra branco como o tracador antigo
            go.GetComponent<MeshRenderer>().material = MaterialUtil.Emissivo(cor, 0.55f);

            go.AddComponent<BalaVisual>().Iniciar(de, ate, 60f, acertou, cor);
        }

        private void EjetarCapsula(Vector3 muzzle, Vector3 dir)
        {
            var go = new GameObject("capsula");
            go.transform.position = muzzle - dir * 0.2f + Vector3.up * 0.1f;
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.transform.SetParent(go.transform, false);
            cubo.transform.localScale = new Vector3(0.09f, 0.05f, 0.05f);
            var col = cubo.GetComponent<Collider>(); if (col != null) Destroy(col);
            cubo.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(new Color(0.85f, 0.7f, 0.25f), 0.7f, 0.6f);
            float lado = dir.x >= 0 ? -1f : 1f;
            Vector3 v = new Vector3(lado * Random.Range(1f, 2f), Random.Range(2.5f, 4f), 0);
            go.AddComponent<ShellCasing>().Iniciar(v);
            AudioManager.Instance?.Play("capsula", 0.3f);
        }

        private void AplicarRecuo(ArmaDef a, Vector3 dir)
        {
            CameraController.Instance?.Shake(a.recuo * 0.14f, 0.1f);
            // coice fisico: empurra levemente o jogador para tras (armas pesadas empurram mais)
            var pc = GetComponent<PlayerController>();
            if (pc != null && pc.Corpo != null && a.recuo >= 2f && pc.NoChao)
            {
                var v = pc.Corpo.velocity;
                v.x -= Mathf.Sign(dir.x == 0 ? (pc.VoltadoDireita ? 1 : -1) : dir.x) * a.recuo * 0.5f;
                pc.Corpo.velocity = v;
            }
        }

        public void Recarregar()
        {
            if (_armas.Count == 0 || Recarregando) return;
            var a = _armas[_idx];
            if (_pente[_idx] >= a.pente || _reserva[_idx] <= 0) return;
            _recargaRestante = a.recarga;
            AudioManager.Instance?.Play("recarregar", 0.7f);
        }

        private void FinalizarRecarga()
        {
            var a = _armas[_idx];
            int falta = a.pente - _pente[_idx];
            int usar = Mathf.Min(falta, _reserva[_idx]);
            _pente[_idx] += usar;
            _reserva[_idx] -= usar;
        }

        /// <summary>Troca de arma (delta +1/-1). Cancela recarga em andamento.</summary>
        public void Trocar(int delta)
        {
            if (_armas.Count == 0) return;
            _idx = (_idx + delta + _armas.Count) % _armas.Count;
            _recargaRestante = 0f;
            _cd = 0.12f;
            _espalhamentoAtual = 0f;
            AudioManager.Instance?.Play("click", 0.6f);
        }

        public void TrocarPara(int i)
        {
            if (_armas.Count == 0) return;
            _idx = Mathf.Clamp(i, 0, _armas.Count - 1);
            _recargaRestante = 0f; _cd = 0.12f; _espalhamentoAtual = 0f;
            AudioManager.Instance?.Play("click", 0.6f);
        }

        /// <summary>Recarrega reserva de todas as armas (item de municao).</summary>
        public void AdicionarMunicao(float fracao)
        {
            for (int i = 0; i < _armas.Count; i++)
            {
                int add = Mathf.RoundToInt(_armas[i].reservaInicial * fracao);
                _reserva[i] = Mathf.Min(_armas[i].reservaInicial * 2, _reserva[i] + add);
            }
        }

        private static Vector3 Rotacionar(Vector3 dir, float ang)
        {
            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
            return new Vector3(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos, 0f).normalized;
        }
    }

    /// <summary>Bala visivel: viaja do cano ate o ponto de impacto e solta faisca ao chegar.</summary>
    public class BalaVisual : MonoBehaviour
    {
        private Vector3 _de, _ate;
        private float _t, _dur;
        private bool _acertou;
        private Color _cor;

        public void Iniciar(Vector3 de, Vector3 ate, float velocidade, bool acertou, Color cor)
        {
            _de = de; _ate = ate; _acertou = acertou; _cor = cor;
            _dur = Mathf.Max(0.03f, Vector3.Distance(de, ate) / Mathf.Max(1f, velocidade));
            transform.position = de;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _dur);
            transform.position = Vector3.Lerp(_de, _ate, k);
            if (k >= 1f)
            {
                if (_acertou) FX.Faiscas(_ate, _cor, 5, 4f);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Capsula ejetada: descreve um arco com gravidade, quica e some.</summary>
    public class ShellCasing : MonoBehaviour
    {
        private Vector3 _v;
        private float _vida = 1.2f;
        private float _giro;

        public void Iniciar(Vector3 v) { _v = v; _giro = Random.Range(-720f, 720f); }

        void Update()
        {
            _v.y -= 16f * Time.deltaTime;
            transform.position += _v * Time.deltaTime;
            transform.Rotate(0, 0, _giro * Time.deltaTime);
            // quica no chao (y ~ 0.05)
            if (transform.position.y < 0.06f && _v.y < 0f)
            {
                var p = transform.position; p.y = 0.06f; transform.position = p;
                _v.y = -_v.y * 0.4f; _v.x *= 0.6f;
                if (Mathf.Abs(_v.y) < 0.6f) _v = Vector3.zero;
            }
            _vida -= Time.deltaTime;
            if (_vida <= 0f) Destroy(gameObject);
        }
    }
}
