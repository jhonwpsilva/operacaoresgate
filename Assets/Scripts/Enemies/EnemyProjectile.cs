using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Projetil generico disparado pelos vilões (tiro reto). Tem cor configuravel,
    /// luz dinamica e some ao atingir o jogador ou expirar.
    /// </summary>
    public class EnemyProjectile : MonoBehaviour
    {
        private Vector3 _dir;
        private float _vel, _dano, _vida = 3.5f;

        public void Iniciar(Vector3 dir, float vel, float dano, Color cor)
        {
            _dir = dir.normalized; _vel = vel; _dano = dano;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 0.35f;
            var mr = sphere.GetComponent<MeshRenderer>();
            mr.material = MaterialUtil.Emissivo(cor, 1.6f);
            var c = sphere.GetComponent<Collider>(); if (c != null) Destroy(c);

            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.28f;

            var luz = gameObject.AddComponent<Light>();
            luz.color = cor; luz.range = 2.2f; luz.intensity = 1.4f;
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

    /// <summary>
    /// Bomba/granada que descreve um arco (gravidade) e explode ao tocar o chao ou o
    /// jogador, causando dano em area com efeito de luz e som. Usada por granadeiros e
    /// pelo helicoptero.
    /// </summary>
    public class FallingBomb : MonoBehaviour
    {
        private Vector3 _vel;
        private float _dano, _raio;
        private bool _explodiu;
        private float _vida = 6f;

        public void Iniciar(Vector3 velInicial, float dano, float raio, Sprite sprite)
        {
            _vel = velInicial; _dano = dano; _raio = raio;
            AudioManager.Instance?.Play("bomb_whistle", 0.7f);

            var v = new GameObject("visual");
            v.transform.SetParent(transform, false);
            if (sprite != null)
            {
                var sr = v.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.sortingOrder = 14;
                float alvo = 0.7f, maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) v.transform.localScale = Vector3.one * (alvo / maior);
                v.AddComponent<Billboard>();
            }
            else
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(v.transform, false); s.transform.localScale = Vector3.one * 0.4f;
                var cc = s.GetComponent<Collider>(); if (cc != null) Destroy(cc);
                s.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(new Color(0.1f,0.1f,0.1f));
            }

            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 0.3f;
        }

        void Update()
        {
            if (_explodiu) return;
            _vel.y -= GameConfig.Gravidade * 0.7f * Time.deltaTime;
            transform.position += _vel * Time.deltaTime;
            transform.Rotate(0, 0, 240f * Time.deltaTime);

            // explode ao chegar perto do chao (y ~ 0)
            if (transform.position.y <= 0.4f) Explodir();
            _vida -= Time.deltaTime;
            if (_vida <= 0f) Explodir();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerHealth>() != null) Explodir();
        }

        private void Explodir()
        {
            if (_explodiu) return;
            _explodiu = true;
            AudioManager.Instance?.Play("explosion");

            var f = new GameObject("boom");
            f.transform.position = transform.position;
            var l = f.AddComponent<Light>();
            l.color = new Color(1f, 0.6f, 0.2f); l.range = _raio * 2.2f; l.intensity = 6f;
            Destroy(f, 0.35f);

            var cols = Physics.OverlapSphere(transform.position, _raio);
            foreach (var c in cols)
            {
                var saude = c.GetComponent<PlayerHealth>();
                if (saude != null) saude.LevarDano(_dano, transform.position);
                var inimigo = c.GetComponentInParent<EnemyController>();
                if (inimigo != null) inimigo.LevarDano(40, transform.position);
            }
            Destroy(gameObject, 0.05f);
        }
    }
}
