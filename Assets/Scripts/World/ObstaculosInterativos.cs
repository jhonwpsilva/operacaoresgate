using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Caixa de madeira EMPURRAVEL. Usa Rigidbody real: o jogador encosta e empurra pelo
    /// cenario (fisica). Pode ser DESTRUIDA a tiros/explosao (IDanificavel), soltando
    /// estilhacos e, as vezes, um item. Serve de obstaculo movivel e cobertura improvisada.
    /// </summary>
    public class CaixaEmpurravel : MonoBehaviour, IDanificavel
    {
        public float vida = 30f;
        private Rigidbody _rb;

        public void Configurar(Sprite sprite, float tamanho = 1.1f)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            _rb.mass = 2.2f;
            _rb.drag = 3.5f;              // para nao deslizar eternamente
            _rb.useGravity = true;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(tamanho, tamanho, tamanho);
            col.center = new Vector3(0, tamanho * 0.5f, 0);
            var mat = new PhysicMaterial("caixa");
            mat.dynamicFriction = 0.6f; mat.staticFriction = 0.7f;
            col.material = mat;

            var v = new GameObject("visual");
            v.transform.SetParent(transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 9;
            if (sprite != null)
            {
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) v.transform.localScale = Vector3.one * (tamanho / maior);
                v.transform.localPosition = new Vector3(0, tamanho * 0.5f, -0.2f);
            }
            v.AddComponent<Billboard>();
        }

        public void LevarDano(float d, Vector3 origem)
        {
            vida -= d;
            FX.Faiscas(transform.position + Vector3.up * 0.6f, new Color(0.7f, 0.5f, 0.3f), 5, 3f);
            if (vida <= 0f) Quebrar();
        }

        private void Quebrar()
        {
            AudioManager.Instance?.PlayAt("explosion", transform.position, 0.3f);
            FX.Faiscas(transform.position + Vector3.up * 0.5f, new Color(0.7f, 0.5f, 0.3f), 10, 4f);
            FX.Fumaca(transform.position, 3);
            // chance de soltar item
            if (Random.value < 0.5f) Loot.DeInimigo(transform.position, false);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Porta/portao blindado. Fica FECHADO bloqueando a passagem (colisor solido). O jogador
    /// aciona com E (IInteragivel): a porta DESLIZA para cima com som e luz, liberando o
    /// caminho. Depois de aberta some o bloqueio. Usada em pontos-chave da fase.
    /// </summary>
    public class Porta : MonoBehaviour, IInteragivel
    {
        private Transform _folha;
        private BoxCollider _bloqueio;
        private bool _aberta;
        private bool _abrindo;
        private float _altura = 3.2f;

        public void Configurar(float largura = 1.4f, float altura = 3.2f, bool portao = false)
        {
            _altura = altura;
            var cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = "folha";
            cubo.transform.SetParent(transform, false);
            cubo.transform.localScale = new Vector3(largura, altura, 0.6f);
            cubo.transform.localPosition = new Vector3(0, altura * 0.5f, 0);
            var mr = cubo.GetComponent<MeshRenderer>();
            mr.material = MaterialUtil.Cor(portao ? new Color(0.28f, 0.30f, 0.34f) : new Color(0.35f, 0.24f, 0.15f), 0.6f, 0.4f);
            var cc = cubo.GetComponent<Collider>(); if (cc != null) Destroy(cc);
            _folha = cubo.transform;

            _bloqueio = gameObject.AddComponent<BoxCollider>();
            _bloqueio.size = new Vector3(largura, altura, 1.2f);
            _bloqueio.center = new Vector3(0, altura * 0.5f, 0);

            // gatilho de interacao
            var trig = new GameObject("trigPorta");
            trig.transform.SetParent(transform, false);
            var tc = trig.AddComponent<BoxCollider>();
            tc.isTrigger = true; tc.size = new Vector3(largura + 2f, altura, 2f); tc.center = new Vector3(0, altura * 0.5f, 0);

            // luz indicadora (vermelha = fechada)
            var luz = new GameObject("luzPorta").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = new Vector3(0, altura + 0.3f, -0.5f);
            luz.color = new Color(1f, 0.3f, 0.2f); luz.range = 4f; luz.intensity = 1.5f;
            _luz = luz;
        }
        private Light _luz;

        public void Interagir(PlayerController player)
        {
            if (_aberta || _abrindo) return;
            StartCoroutine(Abrir());
        }

        private IEnumerator Abrir()
        {
            _abrindo = true;
            AudioManager.Instance?.PlayAt("checkpoint", transform.position, 0.6f);
            if (_luz != null) _luz.color = GameConfig.VerdeHUD;
            if (_bloqueio != null) _bloqueio.enabled = false;

            float t = 0f;
            Vector3 ini = _folha.localPosition;
            Vector3 fim = ini + Vector3.up * (_altura + 0.4f);
            while (t < 1f)
            {
                t += Time.deltaTime * 1.2f;
                _folha.localPosition = Vector3.Lerp(ini, fim, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            _aberta = true; _abrindo = false;
        }
    }

    /// <summary>
    /// Elevador vertical. Quando o jogador SOBE nele, ele se eleva suavemente ate o ponto
    /// alto (curva de aceleracao), espera e retorna. Carrega o jogador junto (carry). Da
    /// acesso a areas superiores. Usa som e luz nas laterais.
    /// </summary>
    public class Elevador : MonoBehaviour
    {
        private float _subida = 5f;
        private float _velocidade = 2.2f;
        private Vector3 _base;
        private Transform _passageiro;
        private Vector3 _ultima;
        private bool _subindo;
        private float _tParado;
        private AnimationCurve _curva;

        public void Configurar(float largura, float subida, float velocidade)
        {
            _subida = subida; _velocidade = velocidade;
            _base = transform.position;
            _ultima = _base;
            _curva = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // luzes indicadoras nas pontas (o proprio GameObject ja e a plataforma/colisor)
            for (int i = -1; i <= 1; i += 2)
            {
                var l = new GameObject("luzElev").AddComponent<Light>();
                l.transform.SetParent(transform, false);
                l.transform.localPosition = new Vector3(i * largura * 0.5f, 0.4f, 0);
                l.color = new Color(0.5f, 0.8f, 1f); l.range = 2.5f; l.intensity = 1.2f;
            }
        }

        private float _t;
        void FixedUpdate()
        {
            // sobe quando ha passageiro; senao desce devagar
            if (_passageiro != null) { _subindo = true; _tParado = 1.5f; }
            else if (_tParado > 0f) _tParado -= Time.fixedDeltaTime;
            else _subindo = false;

            float alvo = _subindo ? 1f : 0f;
            _t = Mathf.MoveTowards(_t, alvo, _velocidade * 0.25f * Time.fixedDeltaTime);
            float k = _curva.Evaluate(_t);
            Vector3 nova = _base + Vector3.up * (_subida * k);
            Vector3 delta = nova - _ultima;
            transform.position = nova;

            if (_passageiro != null)
            {
                var rb = _passageiro.GetComponent<Rigidbody>();
                if (rb != null) rb.position += new Vector3(0, delta.y, 0);
            }
            _ultima = nova;
        }

        void OnCollisionStay(Collision c)
        {
            if (c.collider.GetComponent<PlayerController>() != null &&
                c.transform.position.y > transform.position.y + 0.1f)
                _passageiro = c.transform;
        }
        void OnCollisionExit(Collision c)
        {
            if (c.collider.GetComponent<PlayerController>() != null && c.transform == _passageiro)
                _passageiro = null;
        }
    }

    /// <summary>
    /// Cobertura solida (sacos de areia / concreto / cerca). Bloco com colisor solido e
    /// sprite, que barra o jogador e os tiros. Serve de PROTECAO — inclusive alvo do
    /// comportamento "procurar cobertura" dos inimigos.
    /// </summary>
    public class CoberturaSolida : MonoBehaviour
    {
        public static string Tag = "cobertura";

        public void Configurar(Sprite sprite, float largura, float altura)
        {
            var v = new GameObject("visual");
            v.transform.SetParent(transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 7;
            float largVisual = largura;
            if (sprite != null)
            {
                // BARREIRA GIGANTE ESTAVA AQUI: o visual era escalado pela LARGURA do
                // sprite. Um sprite alto/estreito virava um paredao de 2-3 unidades,
                // enquanto o colisor tinha so 1.0 — impossivel de "ler" e de pular.
                // Agora o visual e escalado pela ALTURA pedida e o colisor tem exatamente
                // o tamanho do desenho: o que voce ve e o que bloqueia, e da pra pular.
                float k = altura / Mathf.Max(0.01f, sprite.bounds.size.y);
                v.transform.localScale = Vector3.one * k;
                largVisual = Mathf.Min(largura * 1.4f, sprite.bounds.size.x * k);
                v.transform.localPosition = new Vector3(0, altura * 0.5f, -0.1f);
            }
            v.AddComponent<Billboard>();

            var col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(largVisual, altura, 1f);
            col.center = new Vector3(0, altura * 0.5f, 0);
        }
    }
}
