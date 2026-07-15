using System.Collections.Generic;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>Como o vilao ataca o jogador.</summary>
    public enum TipoAtaque { Corpo, Tiro, Fogo, Granada, Investida }

    /// <summary>
    /// Ficha de configuracao de um vilao: atributos, tipo de ataque, "super poder"
    /// (cor do efeito) e sons. Centraliza o balanceamento de todos os inimigos do jogo
    /// num so lugar, facilitando manutencao e expansao (padrao data-driven).
    /// </summary>
    public struct EnemyConfig
    {
        public float vida;
        public float velocidade;
        public float dano;          // dano de contato
        public float altura;        // altura visual (mundo)
        public bool voador;
        public TipoAtaque ataque;
        public float alcanceAtaque; // distancia para iniciar ataque a distancia/especial
        public float recargaAtaque; // tempo entre ataques especiais
        public float danoAtaque;    // dano do ataque a distancia/especial
        public Color corEfeito;     // cor do super poder / efeito visual
        public string somAtaque;    // som ao atacar
        public string somSpawn;     // som ao surgir (rugido etc.) — vazio = nenhum
        public int pontos;          // pontos ao derrotar
        public bool chefeMenor;     // mini-chefe (barra maior, mais resistente)
    }

    /// <summary>Tabela central de vilões. Acesso por nome de tipo.</summary>
    public static class EnemyData
    {
        private static readonly Color Verde   = new Color(0.45f, 0.95f, 0.30f);
        private static readonly Color Laranja = new Color(1f, 0.55f, 0.15f);
        private static readonly Color Vermelho= new Color(1f, 0.25f, 0.20f);
        private static readonly Color Amarelo = new Color(1f, 0.85f, 0.30f);
        private static readonly Color Roxo    = new Color(0.7f, 0.35f, 0.95f);
        private static readonly Color Ciano   = new Color(0.35f, 0.8f, 1f);

        private static readonly Dictionary<string, EnemyConfig> _tabela = new Dictionary<string, EnemyConfig>
        {
            // ---- inimigos base ----
            ["soldado_inimigo"] = new EnemyConfig { vida=60, velocidade=2.6f, dano=14, altura=1.9f, ataque=TipoAtaque.Tiro, alcanceAtaque=8f, recargaAtaque=1.8f, danoAtaque=10, corEfeito=Amarelo, somAtaque="shoot", pontos=120 },
            ["drone"]           = new EnemyConfig { vida=40, velocidade=3.2f, dano=12, altura=1.2f, voador=true, ataque=TipoAtaque.Tiro, alcanceAtaque=9f, recargaAtaque=1.5f, danoAtaque=8, corEfeito=Ciano, somAtaque="laser", pontos=100 },
            ["mech"]            = new EnemyConfig { vida=120, velocidade=1.8f, dano=22, altura=2.4f, ataque=TipoAtaque.Tiro, alcanceAtaque=10f, recargaAtaque=2.2f, danoAtaque=16, corEfeito=Vermelho, somAtaque="shoot", pontos=200 },
            ["tanque"]          = new EnemyConfig { vida=150, velocidade=2.4f, dano=26, altura=1.8f, ataque=TipoAtaque.Tiro, alcanceAtaque=13f, recargaAtaque=2.2f, danoAtaque=22, corEfeito=Laranja, somAtaque="explosion", pontos=240 },

            // ---- novos vilões mutantes ----
            ["policial_mutante"]= new EnemyConfig { vida=75, velocidade=2.8f, dano=16, altura=1.95f, ataque=TipoAtaque.Corpo, corEfeito=Amarelo, somSpawn="roar", pontos=140 },
            ["soldado_mutante"] = new EnemyConfig { vida=80, velocidade=2.6f, dano=14, altura=1.95f, ataque=TipoAtaque.Tiro, alcanceAtaque=9f, recargaAtaque=1.6f, danoAtaque=12, corEfeito=Amarelo, somAtaque="shoot", pontos=150 },
            ["mutante_serra"]   = new EnemyConfig { vida=95, velocidade=4.2f, dano=24, altura=1.95f, ataque=TipoAtaque.Investida, alcanceAtaque=5f, recargaAtaque=2.0f, danoAtaque=22, corEfeito=Vermelho, somAtaque="hurt", somSpawn="roar", pontos=180 },
            ["lanca_chamas"]    = new EnemyConfig { vida=85, velocidade=2.2f, dano=18, altura=1.95f, ataque=TipoAtaque.Fogo, alcanceAtaque=4.5f, recargaAtaque=0.25f, danoAtaque=8, corEfeito=Laranja, somAtaque="flame", pontos=170 },
            ["zumbi_radioativo"]= new EnemyConfig { vida=55, velocidade=1.7f, dano=14, altura=1.9f, ataque=TipoAtaque.Corpo, corEfeito=Verde, somSpawn="roar", pontos=110 },
            ["mutante_garras"]  = new EnemyConfig { vida=60, velocidade=3.6f, dano=16, altura=1.9f, ataque=TipoAtaque.Investida, alcanceAtaque=4f, recargaAtaque=1.6f, danoAtaque=14, corEfeito=Verde, somSpawn="roar", pontos=130 },
            ["soldado_granada"] = new EnemyConfig { vida=75, velocidade=2.2f, dano=14, altura=1.95f, ataque=TipoAtaque.Granada, alcanceAtaque=10f, recargaAtaque=2.4f, danoAtaque=26, corEfeito=Verde, somAtaque="shoot", pontos=160 },
            ["brutamonte"]      = new EnemyConfig { vida=240, velocidade=1.5f, dano=30, altura=2.5f, ataque=TipoAtaque.Investida, alcanceAtaque=3.2f, recargaAtaque=3.0f, danoAtaque=28, corEfeito=Verde, somAtaque="explosion", somSpawn="roar", pontos=350, chefeMenor=true },
            ["robo_exterminador"]=new EnemyConfig { vida=110, velocidade=2.0f, dano=20, altura=2.1f, ataque=TipoAtaque.Tiro, alcanceAtaque=11f, recargaAtaque=1.4f, danoAtaque=14, corEfeito=Vermelho, somAtaque="laser", pontos=210 },
            ["drone_militar"]   = new EnemyConfig { vida=50, velocidade=3.4f, dano=12, altura=1.2f, voador=true, ataque=TipoAtaque.Tiro, alcanceAtaque=9f, recargaAtaque=1.3f, danoAtaque=9, corEfeito=Ciano, somAtaque="laser", pontos=120 },

            // ---- animais cibernéticos ----
            ["cobra_cyber"]     = new EnemyConfig { vida=70, velocidade=3.8f, dano=18, altura=2.0f, ataque=TipoAtaque.Investida, alcanceAtaque=4.5f, recargaAtaque=1.8f, danoAtaque=18, corEfeito=Verde, somSpawn="roar", pontos=150 },
            ["pantera_cyber"]   = new EnemyConfig { vida=80, velocidade=4.6f, dano=20, altura=1.7f, ataque=TipoAtaque.Investida, alcanceAtaque=5f, recargaAtaque=1.6f, danoAtaque=20, corEfeito=Vermelho, somSpawn="roar", pontos=170 },
            ["cachorro_cyber"]  = new EnemyConfig { vida=55, velocidade=4.4f, dano=15, altura=1.5f, ataque=TipoAtaque.Investida, alcanceAtaque=4f, recargaAtaque=1.4f, danoAtaque=15, corEfeito=Amarelo, somSpawn="roar", pontos=130 },
            ["macaco_mutante"]  = new EnemyConfig { vida=65, velocidade=3.6f, dano=16, altura=1.8f, ataque=TipoAtaque.Investida, alcanceAtaque=4f, recargaAtaque=1.5f, danoAtaque=16, corEfeito=Verde, somSpawn="roar", pontos=140 },
            // mini-chefes
            ["macaco_minigun"]  = new EnemyConfig { vida=220, velocidade=2.0f, dano=22, altura=2.2f, ataque=TipoAtaque.Tiro, alcanceAtaque=12f, recargaAtaque=0.35f, danoAtaque=9, corEfeito=Laranja, somAtaque="shoot", somSpawn="roar", pontos=400, chefeMenor=true },
            ["elefante_cyber"]  = new EnemyConfig { vida=300, velocidade=1.4f, dano=30, altura=2.6f, ataque=TipoAtaque.Tiro, alcanceAtaque=13f, recargaAtaque=1.8f, danoAtaque=20, corEfeito=Vermelho, somAtaque="explosion", somSpawn="roar", pontos=500, chefeMenor=true },
        };

        public static EnemyConfig Get(string tipo)
        {
            if (_tabela.TryGetValue(tipo, out var c)) return c;
            // padrao seguro
            return new EnemyConfig { vida=60, velocidade=2.4f, dano=14, altura=1.9f, ataque=TipoAtaque.Corpo, corEfeito=Amarelo, pontos=120 };
        }

        /// <summary>Nome amigavel do vilão (usado na barra de mini-chefe).</summary>
        public static string NomeExibicao(string tipo)
        {
            switch (tipo)
            {
                case "macaco_minigun": return "PRIMATA PESADO";
                case "elefante_cyber": return "COLOSSO BLINDADO";
                case "brutamonte":     return "BRUTAMONTE TOXICO";
                case "robo_exterminador": return "EXTERMINADOR";
                case "pantera_cyber":  return "PANTERA CIBERNETICA";
                case "cobra_cyber":    return "NAJA MECANICA";
                default: return tipo.Replace("_", " ").ToUpper();
            }
        }
    }
}
