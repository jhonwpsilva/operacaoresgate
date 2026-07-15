using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Configuracoes globais e paleta visual do jogo OPERACAO RESGATE.
    /// Centraliza constantes para manter consistencia visual e facilitar ajustes.
    /// </summary>
    public static class GameConfig
    {
        // ---- Identidade ----
        public const string GameName = "OPERACAO RESGATE";
        public const string Tagline  = "Atravesse a zona de guerra. Complete o resgate.";

        // ---- Paleta (tema militar / comando) ----
        public static readonly Color AzulExecutivo = new Color32(0x0F, 0x2D, 0x52, 0xFF);
        public static readonly Color AzulPremium   = new Color32(0x1E, 0x5A, 0xA8, 0xFF);
        public static readonly Color VerdeMilitar  = new Color32(0x4B, 0x5A, 0x3A, 0xFF);
        public static readonly Color VerdeHUD      = new Color32(0x6E, 0xE0, 0x7A, 0xFF);
        public static readonly Color Dourado       = new Color32(0xD4, 0xA4, 0x37, 0xFF);
        public static readonly Color VermelhoAlerta= new Color32(0xD9, 0x3A, 0x2B, 0xFF);
        public static readonly Color Carvao        = new Color32(0x14, 0x16, 0x19, 0xFF);
        public static readonly Color Areia         = new Color32(0xB9, 0xA8, 0x7A, 0xFF);
        public static readonly Color Creme         = new Color32(0xEC, 0xE2, 0xCC, 0xFF);
        public static readonly Color OuroSuave     = new Color32(0xE6, 0xC6, 0x7A, 0xFF);

        // ---- Jogabilidade ----
        public const int   VidasIniciais   = 3;
        public const float VidaMaxima      = 100f;
        public const float EnergiaMaxima   = 100f;

        // ---- Fisica / movimento ----
        public const float VelocidadeAndar  = 5.0f;
        public const float VelocidadeCorrer = 8.5f;
        public const float ForcaPulo        = 13.0f;  // pulo mais alto: alcanca plataformas/brindes
        public const float ForcaDuploPulo   = 13.0f;   // igual ao 1o pulo: alcanca as plataformas altas mesmo sem timing perfeito
        public const float VelocidadeEscalar= 4.0f;
        public const float Gravidade        = 26f;

        // ---- Camera ----
        public const float CameraDistancia = 14f;
        public const float CameraAltura    = 2.5f;
        public const float CameraSuavidade = 0.14f;
    }
}
