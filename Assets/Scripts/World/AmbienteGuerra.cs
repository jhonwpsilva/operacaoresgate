using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Ambiente de guerra ao FUNDO (sem afetar a jogabilidade): cacas/avioes cruzando o ceu,
    /// explosoes distantes com clarao e fumaca, e bombardeios eventuais no horizonte.
    /// Tudo relativo a camera, so aparece perto da view. Roda em codigo, sem prefab.
    /// </summary>
    public class AmbienteGuerra : MonoBehaviour
    {
        private Camera _cam;
        private float _tAviao, _tBoom, _tAtaque;
        private float _intensidade = 1f;
        private Sprite _spAviao, _spFogo, _spMissil;
        private const float ZFundo = 14f;   // atras da acao, na frente da pintura

        public void Configurar(float intensidade)
        {
            _intensidade = Mathf.Clamp(intensidade, 0.4f, 2f);
            _spAviao = SpriteLibrary.Get("Sprites/props/aviao", 200f);
            _spFogo  = SpriteLibrary.Get("Sprites/hazards/fogo", 200f);
            _spMissil= SpriteLibrary.Get("Sprites/props/missil", 200f);
            _tAviao = Random.Range(2f, 5f);
            _tBoom  = Random.Range(1.5f, 3f);
            _tAtaque= Random.Range(7f, 12f);
        }

        void Start() { if (Camera.main != null) _cam = Camera.main; }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

            _tAviao -= Time.deltaTime;
            _tBoom  -= Time.deltaTime;

            if (_tAviao <= 0f) { SpawnAviao(); _tAviao = Random.Range(6f, 12f) / _intensidade; }
            if (_tBoom  <= 0f) { SpawnExplosao(); _tBoom = Random.Range(2.2f, 4.5f) / _intensidade; }
            if (_tAtaque<= 0f) { SpawnAviaoAtaque(); _tAtaque = Random.Range(9f, 16f) / _intensidade; }
        }

        // AVIAO INIMIGO: voa baixo o bastante pra ser abatido, solta missil no jogador
        private void SpawnAviaoAtaque()
        {
            if (_spAviao == null) return;
            Vector3 c = _cam.transform.position;
            bool paraDireita = Random.value > 0.5f;
            float xi = c.x + (paraDireita ? -1f : 1f) * 22f;
            float y  = c.y + Random.Range(4.5f, 7.5f);
            var go = new GameObject("aviaoInimigo");
            go.transform.SetParent(transform, false);
            go.AddComponent<AviaoInimigo>().Iniciar(_spAviao, _spMissil, paraDireita ? 1f : -1f,
                                                    new Vector3(xi, y, 6f), _intensidade);
        }

        // caca cruzando o ceu (de um lado ao outro), alto e ao fundo
        private void SpawnAviao()
        {
            if (_spAviao == null) return;
            Vector3 c = _cam.transform.position;
            float meiaLargura = _cam.orthographicSize > 0 ? _cam.orthographicSize * _cam.aspect : 16f;
            meiaLargura = 20f;
            bool paraDireita = Random.value > 0.5f;
            float xi = c.x + (paraDireita ? -1f : 1f) * (meiaLargura + 6f);
            float y  = c.y + Random.Range(6f, 12f);

            var go = new GameObject("aviao");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(xi, y, ZFundo);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _spAviao; sr.sortingOrder = 6; sr.color = new Color(0.85f, 0.85f, 0.9f);
            float m = Mathf.Max(_spAviao.bounds.size.x, _spAviao.bounds.size.y);
            float esc = 3.2f / Mathf.Max(0.01f, m);
            go.transform.localScale = new Vector3(esc * (paraDireita ? 1f : -1f), esc, 1f);

            // brilho do motor
            var luz = new GameObject("motor").AddComponent<Light>();
            luz.transform.SetParent(go.transform, false);
            luz.transform.localPosition = new Vector3(paraDireita ? -1.2f : 1.2f, 0, -0.5f);
            luz.color = new Color(1f, 0.7f, 0.3f); luz.range = 2.5f; luz.intensity = 1.5f;

            StartCoroutine(VoarAviao(go, paraDireita ? 1f : -1f, meiaLargura));
        }

        private IEnumerator VoarAviao(GameObject go, float dir, float meiaLargura)
        {
            float vel = Random.Range(9f, 15f);
            bool soltou = false;
            float xLimite = _cam.transform.position.x + dir * (meiaLargura + 10f);
            while (go != null)
            {
                go.transform.position += new Vector3(dir * vel * Time.deltaTime, 0, 0);
                // pode largar uma bomba distante no meio da travessia (so visual)
                if (!soltou && Random.value < 0.01f * _intensidade)
                {
                    soltou = true;
                    ExplosaoEm(go.transform.position + new Vector3(0, -Random.Range(3f, 6f), 0), 0.7f, false);
                }
                if ((dir > 0 && go.transform.position.x > xLimite) ||
                    (dir < 0 && go.transform.position.x < xLimite))
                    break;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        // explosao distante: clarao + labareda + fumaca subindo
        private void SpawnExplosao()
        {
            Vector3 c = _cam.transform.position;
            float x = c.x + Random.Range(-16f, 16f);
            float y = c.y + Random.Range(-1f, 7f);
            bool comSom = Random.value < 0.5f;
            ExplosaoEm(new Vector3(x, y, ZFundo + Random.Range(-2f, 4f)), Random.Range(0.7f, 1.4f), comSom);
        }

        private void ExplosaoEm(Vector3 pos, float escala, bool som)
        {
            if (som) AudioManager.Instance?.Play("explosion", 0.25f);

            var luz = new GameObject("boomLuz");
            luz.transform.position = pos;
            var l = luz.AddComponent<Light>();
            l.color = new Color(1f, 0.6f, 0.25f); l.range = 6f * escala; l.intensity = 4.5f;
            StartCoroutine(FadeLuz(l, luz, 0.5f));

            if (_spFogo != null)
            {
                var go = new GameObject("boomFogo");
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _spFogo; sr.sortingOrder = 7;
                float m = Mathf.Max(_spFogo.bounds.size.x, _spFogo.bounds.size.y);
                float e0 = (1.6f * escala) / Mathf.Max(0.01f, m);
                StartCoroutine(CrescerFogo(go, sr, e0));
            }
        }

        private IEnumerator FadeLuz(Light l, GameObject go, float dur)
        {
            float t = 0f, i0 = l.intensity;
            while (t < dur && l != null)
            {
                t += Time.deltaTime;
                l.intensity = Mathf.Lerp(i0, 0f, t / dur);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private IEnumerator CrescerFogo(GameObject go, SpriteRenderer sr, float e0)
        {
            float t = 0f, dur = 0.6f;
            while (t < dur && go != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(e0 * 0.6f, e0 * 1.6f, k);
                go.transform.position += new Vector3(0, 1.5f * Time.deltaTime, 0); // fumaca sobe
                var col = sr.color; col.a = Mathf.Lerp(1f, 0f, k); sr.color = col;
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }

    /// <summary>
    /// Aviao inimigo que cruza o ceu BAIXO o bastante para ser abatido. Solta missil
    /// mirando no jogador e, quando derrubado, cai girando e explode no chao.
    /// </summary>
    public class AviaoInimigo : MonoBehaviour
    {
        private float _vida = 45f;
        private float _dir, _vel, _cdMissil, _vidaVoo = 13f;
        private bool _morto;
        private SpriteRenderer _sr;
        private Sprite _spMissil;

        public void Iniciar(Sprite sp, Sprite missil, float dir, Vector3 pos, float intensidade)
        {
            _dir = dir; _vel = Random.Range(6f, 9f); _spMissil = missil;
            transform.position = pos;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sp; _sr.sortingOrder = 9;
            float m = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
            float esc = 3.4f / Mathf.Max(0.01f, m);
            transform.localScale = new Vector3(esc * dir, esc, 1f);
            _cdMissil = Random.Range(0.8f, 1.8f);

            var luz = new GameObject("motor").AddComponent<Light>();
            luz.transform.SetParent(transform, false);
            luz.transform.localPosition = new Vector3(-dir * 1.3f, 0, -0.5f);
            luz.color = new Color(1f, 0.6f, 0.25f); luz.range = 3f; luz.intensity = 2f;
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Estado != EstadoJogo.Jogando) return;

            if (_morto)
            {
                transform.position += new Vector3(_dir * 3f, -6f, 0) * Time.deltaTime;
                transform.Rotate(0, 0, 240f * Time.deltaTime);
                if (transform.position.y <= 0.4f) Explodir();
                return;
            }

            transform.position += new Vector3(_dir * _vel, 0, 0) * Time.deltaTime;
            _vidaVoo -= Time.deltaTime;

            _cdMissil -= Time.deltaTime;
            var player = GameManager.Instance.Player;
            if (_cdMissil <= 0f && player != null && _spMissil != null)
            {
                float dx = Mathf.Abs(player.transform.position.x - transform.position.x);
                if (dx < 12f) { SoltarMissil(player); _cdMissil = Random.Range(1.6f, 2.8f); }
                else _cdMissil = 0.4f;
            }

            if (_vidaVoo <= -4f) Destroy(gameObject);
        }

        private void SoltarMissil(PlayerController player)
        {
            AudioManager.Instance?.Play("shoot", 0.5f);
            var go = new GameObject("missil");
            go.transform.position = transform.position + Vector3.down * 0.5f;
            float dx = player.transform.position.x - transform.position.x;
            Vector3 vel = new Vector3(dx * 0.5f, -3f, 0);
            go.AddComponent<FallingBomb>().Iniciar(vel, 20f, 2.4f, _spMissil);
        }

        public void LevarDano(float d, Vector3 origem)
        {
            if (_morto) return;
            _vida -= d;
            ImpactFX.Faiscas(transform.position);
            if (_vida <= 0f)
            {
                _morto = true;
                AudioManager.Instance?.Play("explosion", 0.6f);
                GameManager.Instance?.AdicionarPontos(180);
            }
        }

        private void Explodir()
        {
            AudioManager.Instance?.Play("explosion", 0.7f);
            var f = new GameObject("boom"); f.transform.position = transform.position;
            var l = f.AddComponent<Light>(); l.color = new Color(1f, 0.6f, 0.25f); l.range = 7f; l.intensity = 6f;
            Destroy(f, 0.3f);
            Destroy(gameObject);
        }
    }

}
