using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Sistema de loot: ao morrer, os vilões tem chance de soltar itens (suprimentos,
    /// energia, medalhas). Mini-chefes e o helicoptero soltam loot garantido e melhor.
    /// O item dropado flutua, brilha e expira apos alguns segundos.
    /// </summary>
    public static class Loot
    {
        /// <summary>Solta loot de um inimigo comum, conforme chance.</summary>
        public static void DeInimigo(Vector3 pos, bool miniChefe)
        {
            if (miniChefe)
            {
                // garantido: kit + municao + escudo + medalha
                Soltar("medkit", pos + new Vector3(-0.9f, 0.6f, 0));
                Soltar("municao", pos + new Vector3(-0.3f, 0.6f, 0));
                Soltar("escudo", pos + new Vector3(0.3f, 0.6f, 0));
                Soltar("medalha", pos + new Vector3(0.9f, 0.6f, 0));
                return;
            }

            float r = Random.value;
            if (r < 0.15f) Soltar("medkit", pos + Vector3.up * 0.6f);
            else if (r < 0.35f) Soltar("bateria", pos + Vector3.up * 0.6f);
            else if (r < 0.50f) Soltar("municao", pos + Vector3.up * 0.6f);
            else if (r < 0.62f) Soltar("medalha", pos + Vector3.up * 0.6f);
            else if (r < 0.70f) Soltar("dinheiro", pos + Vector3.up * 0.6f);
            else if (r < 0.76f) Soltar("escudo", pos + Vector3.up * 0.6f);
            // ~24% nao solta nada
        }

        /// <summary>Loot garantido do helicoptero abatido.</summary>
        public static void DoHelicoptero(Vector3 pos)
        {
            Soltar("medalha", pos + new Vector3(-0.7f, 0.5f, 0));
            Soltar("celula_energia", pos + new Vector3(0.7f, 0.5f, 0));
        }

        private static void Soltar(string tipo, Vector3 pos)
        {
            var sp = ProceduralSprites.ItemIcon(tipo);
            if (sp == null) return;
            var go = new GameObject("loot_" + tipo);
            go.transform.position = pos;
            go.AddComponent<Collectible>().Configurar(tipo, sp, true);

            // brilho de surgimento
            var f = new GameObject("flashLoot");
            f.transform.position = pos;
            var l = f.AddComponent<Light>();
            l.color = GameConfig.Dourado; l.range = 2.2f; l.intensity = 1.8f;
            Object.Destroy(f, 0.2f);
        }
    }
}
