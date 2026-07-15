using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Efeitos de impacto reutilizaveis: faiscas (alvos metalicos) e respingos
    /// (alvos organicos) ao receber dano. Gera fragmentos emissivos que voam com
    /// gravidade e um flash de luz — feedback visual de cada acerto.
    /// </summary>
    public static class ImpactFX
    {
        public static void Impacto(Vector3 pos, Color cor, bool metalico, int qtd = 7)
        {
            for (int i = 0; i < qtd; i++)
            {
                var go = new GameObject("frag");
                go.transform.position = pos;
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(go.transform, false);
                s.transform.localScale = Vector3.one * Random.Range(0.05f, 0.12f);
                var c = s.GetComponent<Collider>(); if (c != null) Object.Destroy(c);
                s.GetComponent<MeshRenderer>().material = MaterialUtil.Emissivo(cor, metalico ? 1.8f : 1.1f);

                Vector3 v = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1.3f), 0).normalized
                            * Random.Range(metalico ? 3f : 2f, metalico ? 6f : 4f);
                go.AddComponent<Fragmento>().Iniciar(v);
            }

            var f = new GameObject("flashImpacto");
            f.transform.position = pos;
            var l = f.AddComponent<Light>();
            l.color = cor; l.range = 2.2f; l.intensity = metalico ? 2.4f : 1.6f;
            Object.Destroy(f, 0.1f);
        }

        /// <summary>Faiscas amarelas para alvos metalicos (robos, drones, veiculos).</summary>
        public static void Faiscas(Vector3 pos)
            => Impacto(pos, new Color(1f, 0.9f, 0.4f), true, 8);

        /// <summary>Respingo para alvos organicos (mutantes, zumbis).</summary>
        public static void Sangue(Vector3 pos, Color cor)
            => Impacto(pos, cor, false, 6);
    }

    /// <summary>Fragmento de impacto: voa com gravidade, encolhe e some.</summary>
    public class Fragmento : MonoBehaviour
    {
        private Vector3 _v;
        private float _vida = 0.5f;

        public void Iniciar(Vector3 v) { _v = v; }

        void Update()
        {
            _v.y -= 14f * Time.deltaTime;
            transform.position += _v * Time.deltaTime;
            transform.localScale *= 0.9f;
            _vida -= Time.deltaTime;
            if (_vida <= 0f) Destroy(gameObject);
        }
    }
}
