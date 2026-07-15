using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Helicoptero inimigo que sobrevoa a fase no alto, acompanha o jogador e solta
    /// bombas que caem em arco e explodem no chao. Tem rotor sonoro, holofote e pode ser
    /// abatido a tiros (cai e explode). Da um ar de zona de guerra a fase.
    /// </summary>
    public class HelicopterEnemy : MonoBehaviour
    {
        public float vida = 90f;
        public float altura = 8.5f;
        private Transform _visual;
        private SpriteRenderer _sr;
        private bool _morto;
        private float _cooldownBomba;
        private float _faseRotor;
        private bool _direita = true;
        private Sprite _spriteBomba;
        private float _somRotorTimer;

        public void Configurar(Sprite sprite)
        {
            _spriteBomba = SpriteLibrary.Get("Sprites/props/bomba", 200f);

            _visual = new GameObject("visual").transform;
            _visual.SetParent(transform, false);
            _sr = _visual.gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite; _sr.sortingOrder = 18;
            if (sprite != null)
            {
                float alvo = 3.2f, maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) _visual.localScale = Vector3.one * (alvo / maior);
            }
            _visual.gameObject.AddComponent<Billboard>();

            // holofote
            var holo = new GameObject("holofote").AddComponent<Light>();
            holo.transform.SetParent(transform, false);
            holo.transform.localPosition = new Vector3(0, -0.5f, 0);
            holo.type = LightType.Spot;
            holo.color = new Color(1f, 0.97f, 0.85f);
            holo.range = 16f; holo.spotAngle = 38f; holo.intensity = 3.5f;
            holo.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            var col = gameObject.AddComponent<SphereCollider>();
            col.radius = 1.4f; col.isTrigger = true;

            transform.position = new Vector3(transform.position.x, altura, 0);
            _cooldownBomba = Random.Range(1.5f, 3f);
        }

        void Update()
        {
            if (_morto) return;
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            var player = GameManager.Instance.Player;
            if (player == null) return;

            // acompanha o jogador no alto, com leve atraso e flutuacao
            Vector3 pos = transform.position;
            float alvoX = player.transform.position.x;
            pos.x = Mathf.Lerp(pos.x, alvoX, 0.9f * Time.deltaTime);
            pos.y = altura + Mathf.Sin(Time.time * 1.5f) * 0.4f;
            _direita = (alvoX - transform.position.x) >= 0;
            transform.position = pos;

            if (_visual != null)
            {
                var s = _visual.localScale; s.x = Mathf.Abs(s.x) * (_direita ? 1f : -1f); _visual.localScale = s;
            }

            // som de rotor em loop curto
            _somRotorTimer -= Time.deltaTime;
            if (_somRotorTimer <= 0f)
            {
                AudioManager.Instance?.Play("helicoptero", 0.35f);
                _somRotorTimer = 0.9f;
            }

            // soltar bombas quando estiver razoavelmente acima do jogador
            _cooldownBomba -= Time.deltaTime;
            if (_cooldownBomba <= 0f && Mathf.Abs(transform.position.x - player.transform.position.x) < 4f)
            {
                SoltarBomba();
                _cooldownBomba = Random.Range(2.2f, 3.6f);
            }
        }

        private void SoltarBomba()
        {
            var b = new GameObject("bombaHeli");
            b.transform.position = transform.position + Vector3.down * 1f;
            // leve velocidade horizontal na direcao do movimento + queda
            var vel = new Vector3((_direita ? 1f : -1f) * 1.2f, -1f, 0);
            b.AddComponent<FallingBomb>().Iniciar(vel, 30f, 2.8f, _spriteBomba);
        }

        public void LevarDano(float d, Vector3 origem)
        {
            if (_morto) return;
            vida -= d;
            StartCoroutine(Flash());
            ImpactFX.Faiscas(transform.position + Vector3.down * 0.3f);
            if (vida <= 0f) Abater();
        }

        private IEnumerator Flash()
        {
            if (_sr == null) yield break;
            Color o = _sr.color;
            _sr.color = new Color(1f, 0.5f, 0.5f, 1f);
            yield return new WaitForSeconds(0.06f);
            if (_sr != null) _sr.color = o;
        }

        private void Abater()
        {
            _morto = true;
            AudioManager.Instance?.Play("explosion");
            GameManager.Instance?.AdicionarPontos(400);
            Loot.DoHelicoptero(transform.position + Vector3.down * 7f);
            StartCoroutine(Queda());
        }

        private IEnumerator Queda()
        {
            float vy = 0f, giro = 0f;
            while (transform.position.y > 0.6f)
            {
                vy += 14f * Time.deltaTime;
                giro += 180f * Time.deltaTime;
                transform.position += Vector3.down * vy * Time.deltaTime
                                    + Vector3.right * (_direita ? 1f : -1f) * 2f * Time.deltaTime;
                if (_visual != null) _visual.localRotation = Quaternion.Euler(0, 0, giro);
                // fumaca/faiscas
                if (Random.value < 0.3f)
                {
                    var sp = new GameObject("faisca"); sp.transform.position = transform.position;
                    var l = sp.AddComponent<Light>(); l.color = new Color(1f,0.6f,0.2f); l.range=2f; l.intensity=2f;
                    Destroy(sp, 0.2f);
                }
                yield return null;
            }
            // explosao final
            for (int i = 0; i < 4; i++)
            {
                var f = new GameObject("boom");
                f.transform.position = transform.position + new Vector3(Random.Range(-1.5f,1.5f), Random.Range(0f,1.5f), 0);
                var l = f.AddComponent<Light>(); l.color = new Color(1f,0.6f,0.2f); l.range=6f; l.intensity=6f;
                Destroy(f, 0.3f);
                AudioManager.Instance?.Play("explosion", 0.6f);
                yield return new WaitForSeconds(0.12f);
            }
            Destroy(gameObject);
        }
    }
}
