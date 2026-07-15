using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Ponto de extracao no fim da fase. Ao alcancar, completa o nivel.
    /// Em fases normais e um sinalizador luminoso; o jogador chega andando.
    /// </summary>
    public class LevelGoal : MonoBehaviour
    {
        private float _f;
        private Transform _pilar;

        public void Configurar()
        {
            // feixe de luz / marcador visual
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "feixe";
            beam.transform.SetParent(transform, false);
            beam.transform.localScale = new Vector3(0.8f, 6f, 0.8f);
            beam.transform.localPosition = new Vector3(0, 6f, 0);
            var mr = beam.GetComponent<MeshRenderer>();
            mr.material = MaterialUtil.Emissivo(new Color(0.45f, 0.85f, 1f), 0.6f);
            mr.material.SetFloat("_Mode", 3f);
            var bc = beam.GetComponent<Collider>(); if (bc != null) Destroy(bc);
            _pilar = beam.transform;

            var trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.6f, 5f, 2f);
            trigger.center = new Vector3(0, 2.5f, 0);

            var l = new GameObject("luzGoal").AddComponent<Light>();
            l.transform.SetParent(transform, false);
            l.transform.localPosition = new Vector3(0, 2f, 0);
            l.color = new Color(0.5f, 0.85f, 1f); l.range = 8f; l.intensity = 2.2f;
        }

        void Update()
        {
            _f += Time.deltaTime;
            if (_pilar != null) _pilar.Rotate(0, 40f * Time.deltaTime, 0);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerController>() != null)
            {
                AudioManager.Instance?.Play("checkpoint");
                GameManager.Instance?.CompletarFase();
            }
        }
    }

    /// <summary>Prop decorativo (carro, sacos, caixas). Sem dano; pode ser solido.</summary>
    public class Decoration : MonoBehaviour
    {
        public void Configurar(Sprite sprite, float escala, bool solido)
        {
            var v = new GameObject("visual");
            v.transform.SetParent(transform, false);
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = 5;
            if (sprite != null)
            {
                float alvo = escala;
                float maior = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                if (maior > 0) v.transform.localScale = Vector3.one * (alvo / Mathf.Max(0.01f, sprite.bounds.size.y) * 1.2f);
                v.transform.localPosition = new Vector3(0, sprite.bounds.size.y * v.transform.localScale.y * 0.5f, 0.5f);
            }
            v.AddComponent<Billboard>();

            if (solido)
            {
                var bc = gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(escala * 0.8f, escala * 0.5f, 0.8f);
                bc.center = new Vector3(0, escala * 0.25f, 0);
            }
        }
    }

    /// <summary>Utilitario para materiais (Standard shader configurado por codigo).</summary>
    public static class MaterialUtil
    {
        public static Material Cor(Color c, float metallic = 0.0f, float smooth = 0.25f)
        {
            var sh = Shader.Find("Standard");
            var m = new Material(sh);
            m.color = c;
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smooth);
            return m;
        }
        public static Material Emissivo(Color c, float intensidade)
        {
            var m = Cor(c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensidade);
            return m;
        }
    }
}
