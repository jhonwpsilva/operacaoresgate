using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Efeito de impacto: ao acertar um inimigo (ou o jogador levar dano), gera um flash
    /// de luz e algumas faiscas que voam e somem. Da peso e feedback aos tiros e golpes.
    /// </summary>
    public static class ImpactEffect
    {
        public static void Faiscas(Vector3 pos, Color cor, int qtd = 5)
        {
            // flash
            var f = new GameObject("flashImpacto");
            f.transform.position = pos;
            var l = f.AddComponent<Light>();
            l.color = cor; l.range = 2.4f; l.intensity = 2.2f;
            Object.Destroy(f, 0.1f);

            // faiscas (pequenas esferas emissivas voando)
            for (int i = 0; i < qtd; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.position = pos;
                s.transform.localScale = Vector3.one * Random.Range(0.06f, 0.13f);
                var col = s.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                var mr = s.GetComponent<MeshRenderer>();
                mr.material = MaterialUtil.Emissivo(cor, 1.6f);
                var fa = s.AddComponent<Faisca>();
                Vector3 dir = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1f), Random.Range(-0.3f, 0.3f)).normalized;
                fa.Iniciar(dir * Random.Range(2.5f, 5f));
            }
        }
    }

    /// <summary>Particula de faisca: voa, cai com gravidade e some.</summary>
    public class Faisca : MonoBehaviour
    {
        private Vector3 _vel;
        private float _vida = 0.45f;
        private Transform _t;

        public void Iniciar(Vector3 vel) { _vel = vel; _t = transform; }

        void Update()
        {
            _vel.y -= 14f * Time.deltaTime;
            _t.position += _vel * Time.deltaTime;
            _t.localScale *= 0.92f;
            _vida -= Time.deltaTime;
            if (_vida <= 0f) Destroy(gameObject);
        }
    }
}
