using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Gera por codigo os efeitos sonoros que nao existem como arquivo WAV (passos por
    /// superficie, recarga, ejecao de capsula, quique de granada, escudo, coleta, esquiva,
    /// clique de arma vazia, ambiente de guerra e trovao). Cada clip e sintetizado com
    /// AudioClip.Create e registrado no AudioManager, para o resto do jogo tocar por nome.
    /// </summary>
    public static class ProceduralAudio
    {
        private const int FREQ = 22050;

        public static void Registrar(AudioManager am)
        {
            if (am == null) return;

            am.RegisterClip("passo_terra",   Passo(0.9f, 0.10f));
            am.RegisterClip("passo_metal",   Passo(1.6f, 0.09f, true));
            am.RegisterClip("passo_pedra",   Passo(1.2f, 0.09f));
            am.RegisterClip("passo_grama",   Passo(0.7f, 0.11f));
            am.RegisterClip("recarregar",    Recarga());
            am.RegisterClip("capsula",       Capsula());
            am.RegisterClip("granada_quique", Quique());
            am.RegisterClip("escudo",        Sweep(320f, 1200f, 0.4f, 0.28f));
            am.RegisterClip("powerup",       Arpejo());
            am.RegisterClip("esquiva",       Whoosh(0.25f));
            am.RegisterClip("arma_vazia",    Click(0.045f, 0.5f));
            am.RegisterClip("ambiente_guerra", AmbienteGuerra());
            am.RegisterClip("trovao",        Trovao());
        }

        // ---- passo: ruido curto filtrado com envelope rapido ----
        private static AudioClip Passo(float tint, float dur, bool metal = false)
        {
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Exp(-t * 22f);
                float ruido = Random.Range(-1f, 1f);
                // filtro passa-baixa simples (media com anterior) -> som mais "terroso"
                prev = Mathf.Lerp(prev, ruido, metal ? 0.85f : 0.35f);
                float s = prev * env * 0.5f;
                if (metal) s += Mathf.Sin(t * dur * 3600f * tint) * env * 0.15f;
                buf[i] = Mathf.Clamp(s * tint, -1f, 1f);
            }
            return Criar("passo", buf);
        }

        // ---- recarga: dois cliques metalicos com silencio entre eles ----
        private static AudioClip Recarga()
        {
            float dur = 0.42f;
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            Click(buf, 0, 0.05f, 0.6f, 700f);
            Click(buf, (int)(n * 0.45f), 0.06f, 0.7f, 520f);
            return Criar("recarga", buf);
        }

        // ---- capsula ejetada: tink metalico agudo ----
        private static AudioClip Capsula()
        {
            float dur = 0.14f;
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Exp(-t * 30f);
                float s = (Mathf.Sin(t * dur * 2f * Mathf.PI * 4200f) * 0.6f
                         + Mathf.Sin(t * dur * 2f * Mathf.PI * 6100f) * 0.4f
                         + Random.Range(-1f, 1f) * 0.2f) * env;
                buf[i] = Mathf.Clamp(s * 0.6f, -1f, 1f);
            }
            return Criar("capsula", buf);
        }

        private static AudioClip Quique()
        {
            float dur = 0.11f;
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Exp(-t * 26f);
                float s = (Mathf.Sin(t * dur * 2f * Mathf.PI * 520f) * 0.7f + Random.Range(-1f, 1f) * 0.3f) * env;
                buf[i] = Mathf.Clamp(s * 0.6f, -1f, 1f);
            }
            return Criar("quique", buf);
        }

        // ---- varredura de frequencia (escudo/energia) ----
        private static AudioClip Sweep(float f0, float f1, float dur, float amp)
        {
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Lerp(f0, f1, t);
                fase += 2f * Mathf.PI * f / FREQ;
                float env = Mathf.Sin(t * Mathf.PI); // sobe e desce
                buf[i] = Mathf.Sin(fase) * env * amp;
            }
            return Criar("sweep", buf);
        }

        // ---- arpejo agradavel (coleta especial) ----
        private static AudioClip Arpejo()
        {
            float[] notas = { 523f, 659f, 784f, 1046f }; // C E G C
            float passo = 0.09f;
            float dur = passo * notas.Length;
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            for (int k = 0; k < notas.Length; k++)
            {
                int ini = (int)(k * passo * FREQ);
                int len = (int)(passo * FREQ);
                for (int i = 0; i < len && ini + i < n; i++)
                {
                    float t = i / (float)len;
                    float env = Mathf.Exp(-t * 6f);
                    buf[ini + i] += Mathf.Sin(2f * Mathf.PI * notas[k] * (i / (float)FREQ)) * env * 0.28f;
                }
            }
            return Criar("arpejo", buf);
        }

        private static AudioClip Whoosh(float dur)
        {
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Sin(t * Mathf.PI);
                prev = Mathf.Lerp(prev, Random.Range(-1f, 1f), 0.2f); // passa-baixa -> vento
                buf[i] = prev * env * 0.5f;
            }
            return Criar("whoosh", buf);
        }

        private static AudioClip Click(float dur, float amp)
        {
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            Click(buf, 0, dur, amp, 900f);
            return Criar("click", buf);
        }

        // escreve um clique dentro de um buffer existente
        private static void Click(float[] buf, int inicio, float dur, float amp, float freq)
        {
            int len = (int)(FREQ * dur);
            for (int i = 0; i < len && inicio + i < buf.Length; i++)
            {
                float t = i / (float)len;
                float env = Mathf.Exp(-t * 40f);
                float s = (Mathf.Sin(2f * Mathf.PI * freq * (i / (float)FREQ)) * 0.5f + Random.Range(-1f, 1f) * 0.5f) * env;
                buf[inicio + i] += Mathf.Clamp(s * amp, -1f, 1f);
            }
        }

        // ---- ambiente de guerra: rumor grave + estalos distantes (loopavel) ----
        private static AudioClip AmbienteGuerra()
        {
            float dur = 2.0f;
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                prev = Mathf.Lerp(prev, Random.Range(-1f, 1f), 0.02f); // ruido grave
                float rumor = prev * 0.18f;
                float grave = Mathf.Sin(2f * Mathf.PI * 55f * (i / (float)FREQ)) * 0.05f
                            + Mathf.Sin(2f * Mathf.PI * 70f * (i / (float)FREQ)) * 0.04f;
                buf[i] = rumor + grave;
            }
            // fade nas pontas para loop sem clique
            int fade = (int)(FREQ * 0.15f);
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                buf[i] *= k;
                buf[n - 1 - i] *= k;
            }
            return Criar("ambiente", buf);
        }

        private static AudioClip Trovao()
        {
            float dur = 1.2f;
            int n = (int)(FREQ * dur);
            var buf = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = t < 0.05f ? (t / 0.05f) : Mathf.Exp(-(t - 0.05f) * 4f);
                prev = Mathf.Lerp(prev, Random.Range(-1f, 1f), 0.08f);
                float grave = Mathf.Sin(2f * Mathf.PI * 48f * (i / (float)FREQ)) * 0.4f;
                buf[i] = Mathf.Clamp((prev * 0.6f + grave) * env, -1f, 1f);
            }
            return Criar("trovao", buf);
        }

        private static AudioClip Criar(string nome, float[] dados)
        {
            var clip = AudioClip.Create(nome, dados.Length, 1, FREQ, false);
            clip.SetData(dados, 0);
            return clip;
        }
    }
}
