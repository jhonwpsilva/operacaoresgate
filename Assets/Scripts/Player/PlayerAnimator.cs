using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Anima o jogador trocando sprites (sem Animator Controller, para maxima robustez).
    /// - Pivo no PE (0.5, 0): os pes ficam sempre colados no chao em qualquer pose.
    /// - Ciclos com varios frames para as pernas se moverem ao andar/correr.
    /// - NOVAS acoes: rolar (giro), corpo a corpo, recarregar e arremessar granada.
    /// - INCLINACAO DE MIRA: o soldado inclina levemente na direcao para onde mira
    ///   (cima/frente/baixo), respeitando o lado para que a arma aponte certo.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        private const float PPU = 105f;   // soldado bem maior (~3.15 un.). menor PPU = maior

        // Ponta do cano MEDIDA em cada sprite (px: x a frente do pivo, y acima da base).
        // A bala nasce no cano da POSE QUE ESTA SENDO DESENHADA — por isso a tabela.
        // Poses sem o fuzil erguido herdam o valor da pose de mira.
        private static readonly Vector2[] CANO_PX = {
            new Vector2(130f, 203f),  // 00 idle (fuzil nas costas)
            new Vector2(130f, 203f),  // 01 corrida
            new Vector2(130f, 203f),  // 02 corrida
            new Vector2(130f, 203f),  // 03 pulo
            new Vector2(130f, 203f),  // 04 corrida
            new Vector2(130f, 203f),  // 05 queda
            new Vector2(130f, 203f),  // 06 agachado
            new Vector2(130f, 203f),  // 07 MIRA  (fuzil erguido)
            new Vector2(125f, 215f),  // 08 MIRA  (fuzil erguido)
            new Vector2(130f, 203f),  // 09 escalar
            new Vector2(116f, 201f),  // 10 MIRA  (fuzil erguido)
            new Vector2(130f, 203f),  // 11 corrida
            new Vector2(130f, 203f),  // 12 soco
            new Vector2(130f, 203f),  // 13 ferido
            new Vector2(130f, 203f),  // 14 vitoria
            new Vector2(130f, 203f),  // 15 ajoelhado
            new Vector2(130f, 203f),  // 16 ajoelhado
            new Vector2(130f, 203f),  // 17 interagir
        };

        /// <summary>
        /// Ponta do cano do frame ATUAL, ja considerando o lado e o giro do sprite.
        /// Retorna o deslocamento a somar em transform.position do jogador.
        /// </summary>
        public Vector3 CanoOffset(bool direita)
        {
            int pose = 7;
            if (_framesAtuais != null && _framesAtuais.Length > 0)
                pose = _framesAtuais[Mathf.Clamp(_frameIndex, 0, _framesAtuais.Length - 1)];
            pose = Mathf.Clamp(pose, 0, CANO_PX.Length - 1);
            var p = CANO_PX[pose];

            // ponto do cano no sprite (ja espelhado pelo lado)
            Vector3 local = new Vector3(p.x / PPU * (direita ? 1f : -1f), p.y / PPU, 0f);
            // o sprite gira em torno do proprio pivo; o LIFT e' do transform (fora do giro)
            return new Vector3(0f, LIFT, 0f) + (Quaternion.Euler(0f, 0f, _tiltAtual) * local);
        }

        private const float LIFT = 0.03f;

        private SpriteRenderer _sr;
        private Sprite[] _poses;
        private EstadoPlayer _estadoAtual = EstadoPlayer.Idle;
        private int[] _framesAtuais;
        private int _frameIndex;
        private float _timer;
        private float _fps = 10f;
        private bool _direita = true;
        private float _pitchMira;      // -1 baixo, 0 frente, +1 cima
        private float _tiltAtual;      // inclinacao suavizada (graus)
        private float _elevacao;       // elevacao da mira em graus (0 = reto)
        private float _spinRoll;       // giro acumulado ao rolar

        // ---- Mapa de poses (indices da NOVA folha do soldado) ----
        //  0 idle | 1,2,4 corrida | 3 pulo(sobe) | 5 aereo(cai) | 6 agachar
        //  7,8,10 mira/tiro | 9 escalar | 11 corrida c/ rifle | 12 soco
        //  13 ferido | 14 vitoria | 15,16 ajoelhado | 17 interagir caixa
        private static readonly int[] IDLE      = { 0 };
        private static readonly int[] ANDANDO   = { 1, 2 };
        private static readonly int[] CORRENDO  = { 1, 4, 2 };
        private static readonly int[] PULANDO   = { 3 };       // subindo (impulso, braco p/ cima)
        private static readonly int[] CAINDO    = { 5 };       // aereo/caindo (pernas abertas)
        private static readonly int[] AGACHADO  = { 6 };
        private static readonly int[] ESCALANDO = { 9 };       // sobe obstaculo
        private static readonly int[] ATACANDO  = { 7, 10 };   // mira/atira com rifle
        private static readonly int[] FERIDO    = { 13 };      // mao na cabeca
        private static readonly int[] VITORIA   = { 14 };      // bracos pro alto
        private static readonly int[] DERROTA   = { 15, 16 };  // ajoelha
        // novas acoes (reaproveitam poses coerentes do sheet)
        private static readonly int[] ROLANDO      = { 6 };        // agachado + giro
        private static readonly int[] CORPO        = { 12 };       // soco/corpo-a-corpo
        private static readonly int[] RECARREGANDO = { 8 };        // manuseia a arma
        private static readonly int[] ARREMESSO    = { 3, 12 };    // arma p/ tras -> frente

        void Awake()
        {
            _poses = CarregarPoses(18);

            var child = new GameObject("Sprite");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(0, LIFT, 0);
            _sr = child.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 20;
            if (_poses != null && _poses.Length > 0 && _poses[0] != null) _sr.sprite = _poses[0];

            _framesAtuais = IDLE;
            _frameIndex = 0;
            MostrarFrameAtual();
        }

        private static Sprite[] CarregarPoses(int count)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var tex = Resources.Load<Texture2D>($"Sprites/player/pose_{i:00}");
                if (tex == null) continue;
                var rect = new Rect(0, 0, tex.width, tex.height);
                arr[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), PPU, 0, SpriteMeshType.FullRect);
                arr[i].name = $"pose_{i:00}";
            }
            return arr;
        }

        /// <summary>Define a direcao da mira (usada para o fuzil apontar certo).</summary>
        public void DefinirMira(Vector3 dir)
        {
            _pitchMira = Mathf.Clamp(dir.y, -1f, 1f);
            // elevacao em graus RELATIVA A FRENTE do soldado (0 = reto, + = para cima)
            _elevacao = Mathf.Atan2(dir.y, Mathf.Max(0.0001f, Mathf.Abs(dir.x))) * Mathf.Rad2Deg;
        }

        public void Aplicar(EstadoPlayer estado, bool direita)
        {
            _direita = direita;
            if (estado != _estadoAtual)
            {
                _estadoAtual = estado;
                _framesAtuais = MapaDe(estado);
                _frameIndex = 0;
                _timer = 0f;
                _fps = VelocidadeDe(estado);
                if (estado == EstadoPlayer.Rolando) _spinRoll = 0f;
                MostrarFrameAtual();
            }
            if (_sr != null)
            {
                var s = _sr.transform.localScale;
                s.x = Mathf.Abs(s.x) * (direita ? 1f : -1f);
                _sr.transform.localScale = s;
            }
        }

        void Update()
        {
            AtualizarRotacao();

            if (_framesAtuais == null || _framesAtuais.Length <= 1) return;
            _timer += Time.deltaTime;
            if (_timer >= 1f / _fps)
            {
                _timer = 0f;
                _frameIndex++;
                if (_frameIndex >= _framesAtuais.Length)
                {
                    bool loop = !(_estadoAtual == EstadoPlayer.Vitoria || _estadoAtual == EstadoPlayer.Derrota);
                    _frameIndex = loop ? 0 : _framesAtuais.Length - 1;
                }
                MostrarFrameAtual();
            }
        }

        // aplica giro ao rolar ou inclinacao de mira nas demais poses
        private void AtualizarRotacao()
        {
            if (_sr == null) return;
            if (_estadoAtual == EstadoPlayer.Rolando)
            {
                _spinRoll += (_direita ? -1f : 1f) * 720f * Time.deltaTime; // um giro completo rapido
                _sr.transform.localRotation = Quaternion.Euler(0, 0, _spinRoll);
                return;
            }
            // INCLINACAO DE MIRA (o fuzil aponta para onde voce atira).
            // O pivo do sprite e' (0.5, 0) = NOS PES. Girar o sprite gira em torno dos pes,
            // entao OS PES NAO SAEM DO LUGAR -- so o tronco inclina.
            // O erro antigo era usar o angulo CHEIO da mira: mirando 60 graus o soldado
            // literalmente TOMBAVA e o cano subia ate a cabeca. Com um limite curto ele so
            // "aponta" e continua em pe.
            // CanoOffset aplica ESTE MESMO _tiltAtual, entao a boca do cano acompanha o
            // desenho: a bala continua saindo exatamente da ponta do fuzil.
            // Quer o soldado 100% reto de volta? e' so por TILT_MAX = 0f.
            float alvoTilt = 0f;
            if (_estadoAtual == EstadoPlayer.Atacando)
                alvoTilt = Mathf.Clamp(_elevacao, -TILT_MAX, TILT_MAX) * (_direita ? 1f : -1f);

            _tiltAtual = Mathf.Lerp(_tiltAtual, alvoTilt, 14f * Time.deltaTime);
            _sr.transform.localRotation = Quaternion.Euler(0, 0, _tiltAtual);
        }

        private const float TILT_MAX = 24f;   // graus. 0 = soldado sempre reto.
                                              // 24: o fuzil acompanha melhor a mira
                                              // (cima/baixo) sem o soldado tombar.

        private void MostrarFrameAtual()
        {
            if (_sr == null || _poses == null || _framesAtuais == null) return;
            int idx = _framesAtuais[Mathf.Clamp(_frameIndex, 0, _framesAtuais.Length - 1)];
            if (idx >= 0 && idx < _poses.Length && _poses[idx] != null)
                _sr.sprite = _poses[idx];
        }

        private static int[] MapaDe(EstadoPlayer e)
        {
            switch (e)
            {
                case EstadoPlayer.Idle: return IDLE;
                case EstadoPlayer.Andando: return ANDANDO;
                case EstadoPlayer.Correndo: return CORRENDO;
                case EstadoPlayer.Pulando: return PULANDO;
                case EstadoPlayer.Caindo: return CAINDO;
                case EstadoPlayer.Agachado: return AGACHADO;
                case EstadoPlayer.Escalando: return ESCALANDO;
                case EstadoPlayer.Atacando: return ATACANDO;
                case EstadoPlayer.Ferido: return FERIDO;
                case EstadoPlayer.Vitoria: return VITORIA;
                case EstadoPlayer.Derrota: return DERROTA;
                case EstadoPlayer.Rolando: return ROLANDO;
                case EstadoPlayer.Corpo: return CORPO;
                case EstadoPlayer.Recarregando: return RECARREGANDO;
                case EstadoPlayer.Arremesso: return ARREMESSO;
                default: return IDLE;
            }
        }

        private static float VelocidadeDe(EstadoPlayer e)
        {
            switch (e)
            {
                case EstadoPlayer.Correndo: return 13f;
                case EstadoPlayer.Andando: return 7f;
                case EstadoPlayer.Atacando: return 14f;
                case EstadoPlayer.Escalando: return 6f;
                case EstadoPlayer.Derrota: return 4f;
                case EstadoPlayer.Corpo: return 16f;
                case EstadoPlayer.Recarregando: return 6f;
                case EstadoPlayer.Arremesso: return 12f;
                default: return 8f;
            }
        }
    }
}
