using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>Valor global de vento, lido pelos objetos que balancam (BalancoVento).</summary>
    public static class Vento
    {
        public static float Atual;
        public static void Atualizar(float baseVento)
        {
            // vento oscila suavemente em torno do valor base
            Atual = baseVento * (0.6f + 0.4f * Mathf.Sin(Time.time * 0.7f));
        }
    }

    /// <summary>
    /// Sistema de clima e ambiente da fase (roda por codigo, relativo a camera):
    ///  - CHUVA em riscos que caem e reciclam ao redor da view.
    ///  - NEVE opcional (flocos que descem flutuando).
    ///  - POEIRA: motes lentos suspensos no ar.
    ///  - VENTO global que balanca objetos do cenario.
    ///  - RAIOS/TROVAO ocasionais (clarao + som) em fases chuvosas.
    ///  - SOM AMBIENTE em loop (rumor de guerra).
    /// </summary>
    public class SistemaClima : MonoBehaviour
    {
        private Camera _cam;
        private bool _chuva, _neve;
        private float _vento = 1f;
        private float _tRaio;

        private Transform[] _gotas;
        private float[] _velGota;
        private Transform[] _motes;

        private Material _matChuva;
        private Material _matNeve;

        public void Configurar(bool chuva, float vento, bool neve = false)
        {
            _chuva = chuva; _neve = neve; _vento = vento;
            _tRaio = Random.Range(6f, 14f);

            _matChuva = MaterialUtil.Emissivo(new Color(0.6f, 0.7f, 0.95f), 0.8f);
            _matNeve  = MaterialUtil.Emissivo(new Color(0.95f, 0.97f, 1f), 0.5f);

            if (_chuva || _neve) CriarPrecipitacao(_chuva ? 70 : 50);
            CriarPoeira(14);

            // som ambiente em loop
            AudioManager.Instance?.PlayAmbient("ambiente_guerra", 0.35f);
        }

        void Start() { if (Camera.main != null) _cam = Camera.main; }

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.Estado != EstadoJogo.Jogando) return;
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

            Vento.Atualizar(_vento);
            AtualizarPrecipitacao();
            AtualizarPoeira();

            if (_chuva)
            {
                _tRaio -= Time.deltaTime;
                if (_tRaio <= 0f) { Raio(); _tRaio = Random.Range(8f, 18f); }
            }
        }

        private void CriarPrecipitacao(int qtd)
        {
            _gotas = new Transform[qtd];
            _velGota = new float[qtd];
            Vector3 c = _cam != null ? _cam.transform.position : Vector3.zero;
            for (int i = 0; i < qtd; i++)
            {
                var go = new GameObject("gota");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.material = _neve ? _matNeve : _matChuva;
                lr.positionCount = 2;
                float comp = _neve ? 0.06f : 0.5f;
                lr.startWidth = _neve ? 0.09f : 0.03f;
                lr.endWidth = lr.startWidth;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, new Vector3(0, comp, 0));
                var col = lr.material.color; col.a = _neve ? 0.9f : 0.55f; lr.material.color = col;

                go.transform.position = PosGota(c);
                _velGota[i] = _neve ? Random.Range(1.2f, 2.2f) : Random.Range(16f, 24f);
                _gotas[i] = go.transform;
            }
        }

        private Vector3 PosGota(Vector3 c)
        {
            return new Vector3(c.x + Random.Range(-18f, 18f),
                               c.y + Random.Range(6f, 14f),
                               Random.Range(-2f, 5f));
        }

        private void AtualizarPrecipitacao()
        {
            if (_gotas == null) return;
            Vector3 c = _cam.transform.position;
            float deriva = Vento.Atual * (_neve ? 1.5f : 0.8f);
            for (int i = 0; i < _gotas.Length; i++)
            {
                var g = _gotas[i];
                if (g == null) continue;
                Vector3 p = g.position;
                p.y -= _velGota[i] * Time.deltaTime;
                p.x += deriva * Time.deltaTime + (_neve ? Mathf.Sin(Time.time * 2f + i) * 0.3f * Time.deltaTime : 0f);
                if (p.y < c.y - 8f || Mathf.Abs(p.x - c.x) > 22f)
                    p = PosGota(c);
                g.position = p;
            }
        }

        private void CriarPoeira(int qtd)
        {
            _motes = new Transform[qtd];
            Vector3 c = _cam != null ? _cam.transform.position : Vector3.zero;
            var mat = MaterialUtil.Emissivo(new Color(0.9f, 0.85f, 0.7f), 0.4f);
            for (int i = 0; i < qtd; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.transform.SetParent(transform, false);
                var col = s.GetComponent<Collider>(); if (col != null) Destroy(col);
                s.transform.localScale = Vector3.one * Random.Range(0.03f, 0.07f);
                s.GetComponent<MeshRenderer>().material = mat;
                s.transform.position = new Vector3(c.x + Random.Range(-16f, 16f), c.y + Random.Range(-3f, 6f), Random.Range(-1f, 4f));
                _motes[i] = s.transform;
            }
        }

        private void AtualizarPoeira()
        {
            if (_motes == null) return;
            Vector3 c = _cam.transform.position;
            for (int i = 0; i < _motes.Length; i++)
            {
                var m = _motes[i];
                if (m == null) continue;
                Vector3 p = m.position;
                p.x += (Vento.Atual * 0.4f + Mathf.Sin(Time.time * 0.5f + i) * 0.2f) * Time.deltaTime;
                p.y += Mathf.Sin(Time.time * 0.8f + i * 1.3f) * 0.15f * Time.deltaTime;
                if (Mathf.Abs(p.x - c.x) > 18f || Mathf.Abs(p.y - c.y) > 9f)
                    p = new Vector3(c.x + Random.Range(-16f, 16f), c.y + Random.Range(-3f, 6f), Random.Range(-1f, 4f));
                m.position = p;
            }
        }

        private void Raio()
        {
            Vector3 c = _cam.transform.position;
            Vector3 pos = new Vector3(c.x + Random.Range(-10f, 10f), c.y + Random.Range(6f, 12f), 8f);
            FX.Flash(pos, new Color(0.85f, 0.9f, 1f), 30f, 5f, 0.2f);
            // segundo estouro (piscada dupla do relampago)
            FX.Flash(pos + Vector3.right * Random.Range(-2f, 2f), new Color(0.9f, 0.95f, 1f), 25f, 4f, 0.12f);
            AudioManager.Instance?.PlayAt("trovao", pos, 0.5f);
        }
    }

    /// <summary>
    /// Faz um objeto do cenario BALANCAR com o vento (oscilacao horizontal suave), lendo o
    /// valor global de Vento. Como usa deslocamento (nao rotacao), funciona junto do
    /// Billboard dos sprites. Aplicado a props altos (igreja, torres).
    /// </summary>
    public class BalancoVento : MonoBehaviour
    {
        private float _amplitude = 0.12f;
        private float _fase;
        private Vector3 _base;
        private bool _pronto;

        public void Configurar(float amplitude)
        {
            _amplitude = amplitude;
            _fase = Random.value * 6.28f;
            _base = transform.localPosition;
            _pronto = true;
        }

        void Update()
        {
            if (!_pronto) return;
            float sway = Mathf.Sin(Time.time * 1.3f + _fase) * _amplitude * (0.5f + 0.5f * Mathf.Abs(Vento.Atual));
            transform.localPosition = _base + new Vector3(sway, 0, 0);
        }
    }
}
