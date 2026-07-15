using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Sistema de save simples e robusto via PlayerPrefs. Guarda o progresso entre
    /// sessoes: maior fase desbloqueada, recorde de pontos e total de medalhas coletadas.
    /// Nao depende do editor nem de arquivos externos — funciona em qualquer build.
    /// </summary>
    public static class SaveSystem
    {
        private const string K_FASE   = "OR_fase_desbloqueada";
        private const string K_RECORDE= "OR_recorde";
        private const string K_MEDALHAS="OR_medalhas";
        private const string K_K9NIVEL= "OR_k9_nivel";
        private const string K_K9XP   = "OR_k9_xp";

        /// <summary>Maior indice de fase desbloqueada (0 = so a primeira).</summary>
        public static int FaseDesbloqueada
        {
            get => PlayerPrefs.GetInt(K_FASE, 0);
            private set { PlayerPrefs.SetInt(K_FASE, value); PlayerPrefs.Save(); }
        }

        public static int Recorde => PlayerPrefs.GetInt(K_RECORDE, 0);
        public static int MedalhasTotais => PlayerPrefs.GetInt(K_MEDALHAS, 0);

        /// <summary>Marca uma fase como concluida, desbloqueando a proxima.</summary>
        public static void ConcluirFase(int indiceConcluido)
        {
            int proxima = indiceConcluido + 1;
            if (proxima > FaseDesbloqueada && proxima < LevelData.TotalNiveis)
                FaseDesbloqueada = proxima;
        }

        /// <summary>Desbloqueia explicitamente ate determinada fase (nunca regride).</summary>
        public static void DesbloquearAte(int indice)
        {
            if (indice > FaseDesbloqueada)
                FaseDesbloqueada = Mathf.Clamp(indice, 0, LevelData.TotalNiveis - 1);
        }

        public static bool FaseLiberada(int indice) => indice <= FaseDesbloqueada;

        /// <summary>Salva o recorde se a pontuacao for maior. Retorna true se for novo recorde.</summary>
        public static bool SalvarRecorde(int pontos)
        {
            if (pontos > Recorde)
            {
                PlayerPrefs.SetInt(K_RECORDE, pontos); PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public static void AdicionarMedalhas(int qtd)
        {
            if (qtd <= 0) return;
            PlayerPrefs.SetInt(K_MEDALHAS, MedalhasTotais + qtd); PlayerPrefs.Save();
        }

        // ---- progresso do companheiro K9 ----
        public static int K9Nivel => Mathf.Max(1, PlayerPrefs.GetInt(K_K9NIVEL, 1));
        public static int K9XP => PlayerPrefs.GetInt(K_K9XP, 0);

        public static void SalvarK9(int nivel, int xp)
        {
            PlayerPrefs.SetInt(K_K9NIVEL, Mathf.Max(1, nivel));
            PlayerPrefs.SetInt(K_K9XP, Mathf.Max(0, xp));
            PlayerPrefs.Save();
        }

        /// <summary>Apaga todo o progresso (usado por um botao de reset, se desejado).</summary>
        public static void Resetar()
        {
            PlayerPrefs.DeleteKey(K_FASE);
            PlayerPrefs.DeleteKey(K_RECORDE);
            PlayerPrefs.DeleteKey(K_MEDALHAS);
            PlayerPrefs.DeleteKey(K_K9NIVEL);
            PlayerPrefs.DeleteKey(K_K9XP);
            PlayerPrefs.Save();
        }
    }
}
