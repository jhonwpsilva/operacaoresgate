using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Item coletavel. Flutua e gira levemente; ao tocar o jogador, aplica seu efeito
    /// (cura, energia, pontos) com feedback sonoro e visual, e se destroi.
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        public string tipo = "medalha";
        public int pontos = 100;

        private Vector3 _base;
        private float _fase;
        private Transform _visual;
        private bool _ehLoot;
        private float _vidaLoot = -1f; // loot some apos um tempo (-1 = nunca)

        public void Configurar(string t, Sprite sprite, bool ehLoot = false)
        {
            tipo = t;
            pontos = PontosDe(t);
            _ehLoot = ehLoot;
            if (ehLoot) _vidaLoot = 12f; // loot dropado expira em 12s

            _visual = new GameObject("icone").transform;
            _visual.SetParent(transform, false);
            var sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            // escala do sprite para caber ~0.8 unidade
            if (sprite != null)
            {
                float alvo = 0.95f;
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
            }
            _visual.gameObject.AddComponent<Billboard>();

            // brilho + particulas orbitando (todo item brilha e solta faiscas)
            var brilho = new GameObject("brilho");
            brilho.transform.SetParent(transform, false);
            brilho.transform.localPosition = new Vector3(0, 0.1f, 0);
            brilho.AddComponent<BrilhoItem>().Configurar(CorBrilho(t));

            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.6f;

            _base = transform.position;
            _fase = Random.value * 6.28f;
        }

        void Update()
        {
            _fase += Time.deltaTime * 2f;
            if (_visual != null)
            {
                transform.position = _base + Vector3.up * Mathf.Sin(_fase) * 0.18f;
                _visual.Rotate(0, 60f * Time.deltaTime, 0, Space.World);
            }
            // loot dropado pisca perto de expirar e some
            if (_vidaLoot > 0f)
            {
                _vidaLoot -= Time.deltaTime;
                if (_vidaLoot <= 0f) { Destroy(gameObject); return; }
                if (_vidaLoot < 3f && _visual != null)
                {
                    var sr = _visual.GetComponent<SpriteRenderer>();
                    if (sr != null) { var c = sr.color; c.a = Mathf.PingPong(Time.time * 6f, 1f); sr.color = c; }
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc == null) return;

            AplicarEfeito(pc);
            if (tipo == "medalha") SaveSystem.AdicionarMedalhas(1);
            if (_ehLoot) GameManager.Instance?.AdicionarPontos(pontos);
            else GameManager.Instance?.RegistrarColeta(pontos);
            EfeitoColeta();
            Destroy(gameObject);
        }

        private void AplicarEfeito(PlayerController pc)
        {
            var saude = pc.Saude;
            if (saude == null) return;
            switch (tipo)
            {
                case "medkit": saude.Curar(35f); break;
                case "vida": saude.Curar(50f); break;
                case "energia":
                case "bateria":
                case "celula_energia": saude.RecarregarEnergia(40f); break;
                case "combustivel": saude.RecarregarEnergia(25f); break;
                case "municao":
                    pc.Armas?.AdicionarMunicao(0.5f);
                    pc.Granadas?.Reabastecer(1);
                    AudioManager.Instance?.Play("recarregar", 0.6f);
                    break;
                case "escudo": saude.AdicionarEscudo(50f); break;
                case "xp":
                    GameManager.Instance?.Companheiro?.RegistrarAbate(transform.position, 80);
                    AudioManager.Instance?.Play("powerup", 0.6f);
                    break;
                case "dinheiro":
                    AudioManager.Instance?.Play("coin", 0.7f);
                    break;
                default: break; // medalha, chave, cartao, ferramentas => pontos
            }
        }

        private static Color CorBrilho(string t)
        {
            switch (t)
            {
                case "medkit": case "vida": return new Color(1f, 0.4f, 0.4f);
                case "energia": case "bateria": case "celula_energia": case "combustivel": return new Color(0.4f, 0.7f, 1f);
                case "escudo": return new Color(0.4f, 0.7f, 1f);
                case "municao": return new Color(0.8f, 0.75f, 0.4f);
                case "xp": return new Color(0.4f, 0.9f, 1f);
                case "dinheiro": return GameConfig.Dourado;
                default: return GameConfig.Dourado;
            }
        }

        private void EfeitoColeta()
        {
            // pequeno "flash" de luz na coleta
            var f = new GameObject("flash");
            f.transform.position = transform.position;
            var l = f.AddComponent<Light>();
            l.color = GameConfig.Dourado; l.range = 3f; l.intensity = 2.5f;
            Destroy(f, 0.18f);
        }

        public static int PontosDe(string t)
        {
            switch (t)
            {
                case "medalha": return 150;
                case "medkit": return 80;
                case "vida": return 60;
                case "chave": return 200;
                case "cartao": return 250;
                case "combustivel": return 120;
                case "ferramentas": return 100;
                case "municao": return 60;
                case "escudo": return 90;
                case "xp": return 120;
                case "dinheiro": return 200;
                default: return 100;
            }
        }
    }

    /// <summary>
    /// Brilho de item: uma luz pulsante mais alguns pontos emissivos que orbitam o item,
    /// dando o efeito de "flutuar e brilhar com particulas". Cor definida pelo tipo.
    /// </summary>
    public class BrilhoItem : MonoBehaviour
    {
        private Light _luz;
        private Transform[] _pontos;
        private float _fase;

        public void Configurar(Color cor)
        {
            _luz = gameObject.AddComponent<Light>();
            _luz.color = cor; _luz.range = 2.4f; _luz.intensity = 1.4f;

            _pontos = new Transform[3];
            var mat = MaterialUtil.Emissivo(cor, 1.6f);
            for (int i = 0; i < _pontos.Length; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(transform, false);
                var col = s.GetComponent<Collider>(); if (col != null) Destroy(col);
                s.transform.localScale = Vector3.one * 0.07f;
                s.GetComponent<MeshRenderer>().material = mat;
                _pontos[i] = s.transform;
            }
            _fase = Random.value * 6.28f;
        }

        void Update()
        {
            _fase += Time.deltaTime * 2.4f;
            if (_luz != null) _luz.intensity = 1.2f + Mathf.Sin(_fase * 2f) * 0.4f;
            if (_pontos == null) return;
            for (int i = 0; i < _pontos.Length; i++)
            {
                if (_pontos[i] == null) continue;
                float a = _fase + i * (6.28f / _pontos.Length);
                _pontos[i].localPosition = new Vector3(Mathf.Cos(a) * 0.45f, Mathf.Sin(a * 1.3f) * 0.25f + 0.1f, Mathf.Sin(a) * 0.2f);
            }
        }
    }
}
