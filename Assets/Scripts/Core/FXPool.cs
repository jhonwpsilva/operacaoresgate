using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Pool de efeitos (Object Pooling): reaproveita luzes de flash e particulas em vez
    /// de instanciar/destruir a cada disparo. Reduz garbage collection e melhora o FPS
    /// em cenas com muito combate. Singleton persistente criado sob demanda.
    /// </summary>
    public class FXPool : MonoBehaviour
    {
        private static FXPool _inst;
        public static FXPool Instance
        {
            get
            {
                if (_inst == null)
                {
                    var go = new GameObject("FXPool");
                    _inst = go.AddComponent<FXPool>();
                    DontDestroyOnLoad(go);
                }
                return _inst;
            }
        }

        private readonly Queue<PooledFlash> _flashes = new Queue<PooledFlash>();
        private readonly Queue<PooledParticle> _parts = new Queue<PooledParticle>();

        public PooledFlash PegarFlash()
        {
            PooledFlash f = _flashes.Count > 0 ? _flashes.Dequeue() : PooledFlash.Criar(transform);
            f.gameObject.SetActive(true);
            return f;
        }
        public void Devolver(PooledFlash f)
        {
            if (f == null) return;
            f.gameObject.SetActive(false);
            f.transform.SetParent(transform, false);
            _flashes.Enqueue(f);
        }

        public PooledParticle PegarParticula()
        {
            PooledParticle p = _parts.Count > 0 ? _parts.Dequeue() : PooledParticle.Criar(transform);
            p.gameObject.SetActive(true);
            return p;
        }
        public void Devolver(PooledParticle p)
        {
            if (p == null) return;
            p.gameObject.SetActive(false);
            p.transform.SetParent(transform, false);
            _parts.Enqueue(p);
        }
    }

    /// <summary>Luz de flash reutilizavel: acende e apaga em rampa, depois volta ao pool.</summary>
    public class PooledFlash : MonoBehaviour
    {
        private Light _luz;
        private float _t, _dur, _i0;
        private bool _ativo;

        public static PooledFlash Criar(Transform pai)
        {
            var go = new GameObject("flashPool");
            go.transform.SetParent(pai, false);
            var f = go.AddComponent<PooledFlash>();
            f._luz = go.AddComponent<Light>();
            f._luz.type = LightType.Point;
            go.SetActive(false);
            return f;
        }

        public void Disparar(Vector3 pos, Color cor, float range, float intensidade, float dur)
        {
            transform.SetParent(null, true);
            transform.position = pos;
            if (_luz != null) { _luz.color = cor; _luz.range = range; _luz.intensity = intensidade; }
            _i0 = intensidade; _dur = Mathf.Max(0.03f, dur); _t = 0f; _ativo = true;
        }

        void Update()
        {
            if (!_ativo) return;
            _t += Time.deltaTime;
            float k = _t / _dur;
            if (_luz != null) _luz.intensity = Mathf.Lerp(_i0, 0f, k);
            if (k >= 1f) { _ativo = false; FXPool.Instance.Devolver(this); }
        }
    }

    /// <summary>Particula reutilizavel (esfera emissiva) com gravidade, encolhe e some.</summary>
    public class PooledParticle : MonoBehaviour
    {
        private Transform _mesh;
        private MeshRenderer _mr;
        private Vector3 _vel;
        private float _t, _dur;
        private bool _ativo;

        public static PooledParticle Criar(Transform pai)
        {
            var go = new GameObject("partPool");
            go.transform.SetParent(pai, false);
            var p = go.AddComponent<PooledParticle>();
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.SetParent(go.transform, false);
            var col = s.GetComponent<Collider>(); if (col != null) Destroy(col);
            p._mesh = s.transform; p._mr = s.GetComponent<MeshRenderer>();
            go.SetActive(false);
            return p;
        }

        public void Disparar(Vector3 pos, Vector3 vel, Color cor, float tam, float dur, float emiss)
        {
            transform.SetParent(null, true);
            transform.position = pos;
            if (_mesh != null) _mesh.localScale = Vector3.one * tam;
            if (_mr != null) _mr.material = MaterialUtil.Emissivo(cor, emiss);
            _vel = vel; _dur = Mathf.Max(0.05f, dur); _t = 0f; _ativo = true;
        }

        void Update()
        {
            if (!_ativo) return;
            _t += Time.deltaTime;
            _vel.y -= 14f * Time.deltaTime;
            transform.position += _vel * Time.deltaTime;
            if (_mesh != null) _mesh.localScale *= 0.92f;
            if (_t >= _dur) { _ativo = false; FXPool.Instance.Devolver(this); }
        }
    }

    /// <summary>
    /// Fachada unica de efeitos, toda apoiada no pool. Novos sistemas (armas, granadas,
    /// robos, drones, torres) usam FX para gerar flashes, faiscas, fumaca e explosoes
    /// completas (luz + particulas + som 3D + tremor de camera).
    /// </summary>
    public static class FX
    {
        public static void Flash(Vector3 pos, Color cor, float range = 3f, float intensidade = 3f, float dur = 0.12f)
        {
            FXPool.Instance.PegarFlash().Disparar(pos, cor, range, intensidade, dur);
        }

        public static void Faiscas(Vector3 pos, Color cor, int qtd = 8, float forca = 5f)
        {
            for (int i = 0; i < qtd; i++)
            {
                Vector3 v = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1.3f), Random.Range(-0.3f, 0.3f)).normalized
                            * Random.Range(forca * 0.5f, forca);
                FXPool.Instance.PegarParticula().Disparar(pos, v, cor, Random.Range(0.06f, 0.13f), 0.5f, 1.6f);
            }
            Flash(pos, cor, 2.2f, 2.2f, 0.1f);
        }

        public static void Fumaca(Vector3 pos, int qtd = 4)
        {
            var cor = new Color(0.42f, 0.42f, 0.44f);
            for (int i = 0; i < qtd; i++)
            {
                Vector3 v = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.4f), Random.Range(-0.2f, 0.2f));
                FXPool.Instance.PegarParticula().Disparar(pos + Vector3.up * 0.2f, v, cor, Random.Range(0.3f, 0.6f), 0.9f, 0.12f);
            }
        }

        public static void Explosao(Vector3 pos, float escala = 1f, bool som = true, bool tremor = true)
        {
            Flash(pos, new Color(1f, 0.6f, 0.2f), 6f * escala, 6f, 0.35f);
            Faiscas(pos, new Color(1f, 0.75f, 0.35f), Mathf.RoundToInt(10 * escala), 6f * escala);
            Fumaca(pos, Mathf.RoundToInt(5 * escala));
            if (som) AudioManager.Instance?.PlayAt("explosion", pos, Mathf.Clamp01(0.6f * escala));
            if (tremor) CameraController.Instance?.Shake(0.5f * escala, 0.35f);
        }
    }
}
