using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Torre/guarita fixa. Detecta o jogador dentro do alcance com linha de visada, GIRA o
    /// cano na direcao do alvo (inclinacao visual), DISPARA projeteis com recarga e brilho,
    /// e EXPLODE quando destruida. Serve de obstaculo defensivo. Abativel (IDanificavel).
    /// </summary>
    public class TorreVigia : MonoBehaviour, IDanificavel
    {
        public float vida = 90f;
        public float alcance = 13f;
        private bool _morto;
        private float _cdTiro;
        private Transform _visual;
        private Transform _cano;
        private SpriteRenderer _sr;
        private Light _luz;

        public void Configurar(Sprite sprite)
        {
            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 12;
            if (sprite != null)
            {
                float alvo = 2.4f, maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
                _visual.localPosition = new Vector3(0, sprite.bounds.size.y * _visual.localScale.y * 0.5f, 0);
            }
            _visual.gameObject.AddComponent<Billboard>();

            // cano (cilindro) que gira mirando no alvo
            var cano = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cano.name = "cano";
            cano.transform.SetParent(transform, false);
            cano.transform.localScale = new Vector3(0.18f, 0.7f, 0.18f);
            cano.transform.localPosition = new Vector3(0, 1.7f, -0.3f);
            cano.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            var cc = cano.GetComponent<Collider>(); if (cc != null) Destroy(cc);
            cano.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(new Color(0.2f, 0.22f, 0.26f), 0.7f, 0.5f);
            _cano = cano.transform;

            _luz = new GameObject("luzTorre").AddComponent<Light>();
            _luz.transform.SetParent(transform, false);
            _luz.transform.localPosition = new Vector3(0, 1.7f, -0.4f);
            _luz.color = new Color(1f, 0.4f, 0.2f); _luz.range = 3f; _luz.intensity = 1.2f;

            var col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2.4f; col.radius = 0.5f; col.center = new Vector3(0, 1.2f, 0);

            _cdTiro = Random.Range(0.6f, 1.6f);
        }

        void Update()
        {
            if (_morto) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            if (_cdTiro > 0f) _cdTiro -= Time.deltaTime;

            var player = GameManager.Instance.Player;
            if (player == null) return;

            Vector3 canoPos = transform.position + Vector3.up * 1.7f;
            Vector3 d = player.transform.position + Vector3.up * 0.8f - canoPos;
            float dist = d.magnitude;

            bool vendo = dist < alcance &&
                         !Physics.Raycast(canoPos, d.normalized, dist - 0.5f, ~0, QueryTriggerInteraction.Ignore);

            if (vendo)
            {
                // gira o cano na direcao do alvo
                float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                if (_cano != null)
                    _cano.localRotation = Quaternion.Slerp(_cano.localRotation, Quaternion.Euler(0, 0, ang - 90f), 8f * Time.deltaTime);
                if (_luz != null) _luz.intensity = 1.8f + Mathf.Sin(Time.time * 10f) * 0.5f;

                if (_cdTiro <= 0f)
                {
                    Disparar(canoPos, d.normalized);
                    _cdTiro = 1.3f;
                }
            }
            else if (_luz != null) _luz.intensity = 1.2f;
        }

        private void Disparar(Vector3 origem, Vector3 dir)
        {
            var proj = new GameObject("tiroTorre");
            proj.transform.position = origem + dir * 0.7f;
            proj.AddComponent<EnemyProjectile>().Iniciar(dir, 13f, 16f, new Color(1f, 0.5f, 0.2f));
            FX.Flash(origem + dir * 0.7f, new Color(1f, 0.6f, 0.2f), 3f, 3f, 0.08f);
            AudioManager.Instance?.PlayAt("shoot", origem, 0.7f);
            CombatEvents.DispararTiro(origem, false);
        }

        public void LevarDano(float d, Vector3 origem)
        {
            if (_morto) return;
            vida -= d;
            FX.Faiscas(transform.position + Vector3.up * 1.5f, new Color(1f, 0.9f, 0.4f), 6, 4f);
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
            GameManager.Instance?.AdicionarPontos(180);
            Loot.DeInimigo(transform.position, false);
            if (_luz != null) Destroy(_luz.gameObject);
            if (_cano != null) Destroy(_cano.gameObject);
            FX.Explosao(transform.position + Vector3.up, 1.1f, true, true);
            StartCoroutine(Desmoronar());
        }

        private IEnumerator Desmoronar()
        {
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                if (_visual != null) { _visual.localScale *= 0.9f; _visual.Translate(0, -1f * Time.deltaTime, 0); }
                yield return null;
            }
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Diretor de reforcos: escuta os pedidos de reforco (CombatEvents.PedidoReforco) e faz
    /// surgir 1-2 inimigos leves perto de quem chamou, respeitando um orcamento por fase e
    /// uma recarga, para nao lotar a tela. Cada fase cria um destes pelo LevelBuilder.
    /// </summary>
    public class ReinforcementDirector : MonoBehaviour
    {
        private int _orcamento = 6;   // total de reforcos permitidos na fase
        private float _cd;
        private string[] _tipos = { "soldado_mutante", "cachorro_cyber", "zumbi_radioativo", "policial_mutante" };

        public void Configurar(int orcamento, string[] tipos = null)
        {
            _orcamento = orcamento;
            if (tipos != null && tipos.Length > 0) _tipos = tipos;
        }

        void OnEnable()  { CombatEvents.PedidoReforco += AoPedirReforco; }
        void OnDisable() { CombatEvents.PedidoReforco -= AoPedirReforco; }

        void Update() { if (_cd > 0f) _cd -= Time.deltaTime; }

        private void AoPedirReforco(Vector3 pos)
        {
            if (_orcamento <= 0 || _cd > 0f) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;

            _cd = 8f;
            int qtd = Mathf.Min(_orcamento, Random.Range(1, 3));
            for (int i = 0; i < qtd; i++)
            {
                string tipo = _tipos[Random.Range(0, _tipos.Length)];
                var sp = SpriteLibrary.Get("Sprites/enemies/" + tipo, 200f);
                if (sp == null) continue;
                float lado = Random.value > 0.5f ? 1f : -1f;
                Vector3 spawn = pos + new Vector3(lado * Random.Range(6f, 10f), 0.6f, 0);
                var go = new GameObject("reforco_" + tipo);
                go.transform.position = spawn;
                go.AddComponent<EnemyController>().Configurar(tipo, sp, 4f, spawn);
                FX.Flash(spawn + Vector3.up, new Color(1f, 0.5f, 0.2f), 3f, 2.5f, 0.3f);
                _orcamento--;
            }
            AudioManager.Instance?.Play("alarm", 0.4f);
        }
    }
}
