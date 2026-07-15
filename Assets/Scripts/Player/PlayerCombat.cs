using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Ataque corpo a corpo (faca de combate). Golpe rapido em arco na frente do soldado:
    /// causa dano alto de curta distancia, empurra o alvo e gera rastro luminoso, faisca e
    /// som. Serve para economizar municao e para inimigos colados. Cooldown proprio.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        public float dano = 45f;
        public float alcance = 1.8f;
        private float _cd;

        public bool Pronto => _cd <= 0f;

        void Update()
        {
            if (_cd > 0f) _cd -= Time.deltaTime;
        }

        /// <summary>Executa o golpe na direcao informada. Retorna true se golpeou.</summary>
        public bool Golpear(Vector3 origem, Vector3 dir)
        {
            if (_cd > 0f) return false;
            _cd = 0.45f;

            AudioManager.Instance?.Play("hurt", 0.4f);
            RastroCorte(origem, dir);

            bool acertou = false;
            var cols = Physics.OverlapSphere(origem + dir * (alcance * 0.5f), alcance * 0.7f);
            foreach (var c in cols)
            {
                if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
                var inimigo = c.GetComponentInParent<EnemyController>();
                if (inimigo != null && inimigo.EstaVivo) { inimigo.LevarDano(dano, origem); acertou = true; continue; }
                var boss = c.GetComponentInParent<BossController>();
                if (boss != null && boss.EstaVivo) { boss.LevarDano(dano * 0.3f); acertou = true; continue; }
                var d = c.GetComponentInParent<IDanificavel>();
                if (d != null) { d.LevarDano(dano, origem); acertou = true; }
            }

            if (acertou) FX.Faiscas(origem + dir * alcance * 0.6f, new Color(1f, 0.95f, 0.7f), 6, 4f);
            return acertou;
        }

        private void RastroCorte(Vector3 origem, Vector3 dir)
        {
            var go = new GameObject("corte");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 3;
            Vector3 perp = new Vector3(-dir.y, dir.x, 0);
            Vector3 c = origem + dir * alcance * 0.6f;
            lr.SetPosition(0, c - perp * 0.5f);
            lr.SetPosition(1, c + dir * 0.25f);
            lr.SetPosition(2, c + perp * 0.5f);
            lr.startWidth = 0.14f; lr.endWidth = 0.02f;
            lr.material = MaterialUtil.Emissivo(new Color(0.9f, 0.95f, 1f), 1.8f);
            lr.numCapVertices = 3;
            FX.Flash(c, new Color(0.8f, 0.9f, 1f), 2f, 1.6f, 0.08f);
            Destroy(go, 0.08f);
        }
    }
}
