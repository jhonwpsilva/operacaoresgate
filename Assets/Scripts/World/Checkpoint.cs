using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Ponto de controle (checkpoint). Ao ser tocado pelo jogador, vira o novo ponto de
    /// renascimento da fase — assim, ao perder uma vida, o jogador volta daqui em vez do
    /// inicio. Acende (vermelho -> verde) com luz, brilho e som ao ativar.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        private bool _ativo;
        private Light _luz;
        private MeshRenderer _orbe;

        public void Configurar()
        {
            // poste
            var poste = GameObject.CreatePrimitive(PrimitiveType.Cube);
            poste.transform.SetParent(transform, false);
            poste.transform.localScale = new Vector3(0.16f, 2.4f, 0.16f);
            poste.transform.localPosition = new Vector3(0, 1.2f, 0);
            var pc = poste.GetComponent<Collider>(); if (pc != null) Destroy(pc);
            poste.GetComponent<MeshRenderer>().material = MaterialUtil.Cor(new Color(0.25f, 0.27f, 0.32f), 0.6f, 0.4f);

            // orbe no topo
            var orbe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbe.transform.SetParent(transform, false);
            orbe.transform.localScale = Vector3.one * 0.5f;
            orbe.transform.localPosition = new Vector3(0, 2.5f, 0);
            var oc = orbe.GetComponent<Collider>(); if (oc != null) Destroy(oc);
            _orbe = orbe.GetComponent<MeshRenderer>();
            _orbe.material = MaterialUtil.Emissivo(new Color(1f, 0.25f, 0.2f), 0.8f);

            // luz (vermelha enquanto inativo)
            _luz = new GameObject("luzCP").AddComponent<Light>();
            _luz.transform.SetParent(transform, false);
            _luz.transform.localPosition = new Vector3(0, 2.5f, 0);
            _luz.color = new Color(1f, 0.3f, 0.2f); _luz.range = 4f; _luz.intensity = 1.2f;

            // gatilho
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(1.4f, 3f, 2f); col.center = new Vector3(0, 1.5f, 0);
        }

        void OnTriggerEnter(Collider other)
        {
            if (_ativo) return;
            if (other.GetComponent<PlayerController>() == null) return;
            _ativo = true;

            GameManager.Instance?.DefinirCheckpoint(transform.position + Vector3.up * 1.2f);
            AudioManager.Instance?.Play("checkpoint");

            // muda para verde
            if (_orbe != null) _orbe.material = MaterialUtil.Emissivo(GameConfig.VerdeHUD, 1.4f);
            if (_luz != null) { _luz.color = GameConfig.VerdeHUD; _luz.intensity = 2.4f; }

            StartCoroutine(Pulso());
        }

        private IEnumerator Pulso()
        {
            // brilho de ativacao
            var f = new GameObject("flashCP");
            f.transform.position = transform.position + Vector3.up * 2.5f;
            var l = f.AddComponent<Light>();
            l.color = GameConfig.VerdeHUD; l.range = 7f; l.intensity = 5f;
            float t = 0f;
            while (t < 0.4f) { t += Time.deltaTime; l.intensity = Mathf.Lerp(5f, 0f, t / 0.4f); yield return null; }
            Destroy(f);
        }
    }
}
