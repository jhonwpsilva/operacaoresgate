using System;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Central de eventos de combate. Permite que inimigos "ouçam" tiros, recebam
    /// pedidos de reforço e reajam a alertas (explosões, granadas) sem acoplamento
    /// direto entre as classes. Tudo estatico e leve; assinantes se registram e
    /// cancelam no ciclo de vida. O GameManager chama Limpar() ao trocar de fase.
    /// </summary>
    public static class CombatEvents
    {
        /// <summary>Um tiro foi disparado. pos = origem; doJogador = true se foi o jogador.</summary>
        public static event Action<Vector3, bool> TiroDisparado;

        /// <summary>Um inimigo pediu reforço a partir de uma posicao.</summary>
        public static event Action<Vector3> PedidoReforco;

        /// <summary>Alerta em area (explosao/granada). pos = centro; raio = alcance do ruido.</summary>
        public static event Action<Vector3, float> Alerta;

        public static void DispararTiro(Vector3 pos, bool doJogador)
        {
            if (TiroDisparado != null)
            {
                try { TiroDisparado.Invoke(pos, doJogador); } catch { }
            }
        }

        public static void PedirReforco(Vector3 pos)
        {
            if (PedidoReforco != null)
            {
                try { PedidoReforco.Invoke(pos); } catch { }
            }
        }

        public static void EmitirAlerta(Vector3 pos, float raio)
        {
            if (Alerta != null)
            {
                try { Alerta.Invoke(pos, raio); } catch { }
            }
        }

        /// <summary>Zera todos os assinantes (evita referencias mortas entre fases).</summary>
        public static void Limpar()
        {
            TiroDisparado = null;
            PedidoReforco = null;
            Alerta = null;
        }
    }
}
