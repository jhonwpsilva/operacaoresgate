using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>Perigo continuo (fogo): causa dano enquanto o jogador o toca.</summary>
    public class Hazard : MonoBehaviour
    {
        public float dano = 18f;
        private float _cooldown;

        public void Configurar(Sprite sprite, float alturaAlvo = 1.4f)
        {
            var v = new GameObject("visual");
            v.transform.SetParent(transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 8;
            if (sprite != null)
            {
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) v.transform.localScale = Vector3.one * (alturaAlvo / maior);
            }
            v.AddComponent<Billboard>();
            // animacao de "tremular" do fogo
            v.AddComponent<Flicker>();

            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(alturaAlvo * 0.6f, alturaAlvo * 0.7f, 1f);
            col.center = new Vector3(0, alturaAlvo * 0.35f, 0);
        }

        void OnTriggerStay(Collider other)
        {
            if (_cooldown > 0f) { _cooldown -= Time.deltaTime; return; }
            var saude = other.GetComponent<PlayerHealth>();
            if (saude != null)
            {
                saude.LevarDano(dano, transform.position);
                _cooldown = 0.7f;
            }
        }
    }

    /// <summary>Faz o sprite pulsar de escala e brilho (fogo/luz).</summary>
    public class Flicker : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Vector3 _baseScale;
        private float _f;
        void Start() { _sr = GetComponent<SpriteRenderer>(); _baseScale = transform.localScale; _f = Random.value * 6f; }
        void Update()
        {
            _f += Time.deltaTime * 12f;
            float s = 1f + Mathf.Sin(_f) * 0.06f;
            transform.localScale = new Vector3(_baseScale.x * (2f - s), _baseScale.y * s, _baseScale.z);
            if (_sr != null)
            {
                Color c = _sr.color; c.a = 0.85f + Mathf.Sin(_f * 1.3f) * 0.15f; _sr.color = c;
            }
        }
    }

    /// <summary>
    /// Barril explosivo: pode ser detonado por tiro (recebe dano) e explode causando
    /// dano em area + empurrao. Tambem explode ao contato se o jogador encostar muito.
    /// </summary>
    public class ExplosiveBarrel : MonoBehaviour, IDanificavel
    {
        public float raioExplosao = 3f;
        public float dano = 40f;
        private bool _explodiu;
        private SpriteRenderer _sr;

        public void Configurar(Sprite sprite)
        {
            var v = new GameObject("visual");
            v.transform.SetParent(transform, false);
            _sr = v.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 9;
            if (sprite != null)
            {
                float alvo = 1.3f;
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) v.transform.localScale = Vector3.one * (alvo / maior);
                v.transform.localPosition = new Vector3(0, alvo * 0.5f, 0);
            }
            v.AddComponent<Billboard>();

            // colisor solido (serve de obstaculo) + trigger de proximidade
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.7f, 1.2f, 0.7f);
            bc.center = new Vector3(0, 0.6f, 0);
        }

        public void LevarDano(float d, Vector3 origem) { Explodir(); }
        public void LevarDano(float d) { Explodir(); }

        private void Explodir()
        {
            if (_explodiu) return;
            _explodiu = true;
            AudioManager.Instance?.Play("explosion");

            // luz de explosao
            var f = new GameObject("boom");
            f.transform.position = transform.position + Vector3.up * 0.6f;
            var l = f.AddComponent<Light>();
            l.color = new Color(1f, 0.6f, 0.2f); l.range = raioExplosao * 2f; l.intensity = 6f;
            Destroy(f, 0.35f);

            // dano em area
            var cols = Physics.OverlapSphere(transform.position, raioExplosao);
            foreach (var c in cols)
            {
                var saude = c.GetComponent<PlayerHealth>();
                if (saude != null) saude.LevarDano(dano, transform.position);
                var inimigo = c.GetComponentInParent<EnemyController>();
                if (inimigo != null) inimigo.LevarDano(80, transform.position);
                var outro = c.GetComponentInParent<ExplosiveBarrel>();
                if (outro != null && outro != this) outro.LevarDano(10, transform.position);
            }
            Destroy(gameObject, 0.05f);
        }
    }

    /// <summary>Zona escalavel (escada / parede). Ativa o modo de escalada do jogador.</summary>
    public class Climbable : MonoBehaviour
    {
        public void Configurar(float largura, float altura)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(largura, altura, 1f);
            col.center = new Vector3(0, altura / 2f, 0);
        }

        void OnTriggerStay(Collider other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null) pc.EntrarEscada();
        }
        void OnTriggerExit(Collider other)
        {
            var pc = other.GetComponent<PlayerController>();
            if (pc != null) pc.SairEscada();
        }
    }

    /// <summary>Interface para coisas que recebem dano (barril, etc).</summary>
    public interface IDanificavel { void LevarDano(float dano, Vector3 origem); }
}
