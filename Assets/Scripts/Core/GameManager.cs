using System;
using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    public enum EstadoJogo { Menu, Jogando, Pausado, FaseCompleta, Vitoria, Derrota, Transicao }

    /// <summary>
    /// Nucleo do jogo: maquina de estados, pontuacao, vidas, progresso de fases e
    /// coordenacao entre mundo (LevelBuilder), jogador, camera e telas de UI.
    /// Singleton persistente. Nao depende de cenas — tudo e construido por codigo.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ---- Estado ----
        public EstadoJogo Estado { get; private set; } = EstadoJogo.Menu;
        public int NivelAtual { get; private set; }
        public int Pontos { get; private set; }
        public int Vidas { get; private set; }
        public Vector3 PontoRespawn { get; private set; }

        /// <summary>Atualiza o checkpoint de respawn (so avanca, nunca volta atras).</summary>
        public void DefinirCheckpoint(Vector3 pos)
        {
            if (pos.x > PontoRespawn.x) PontoRespawn = pos;
        }
        public int ItensColetados { get; private set; }
        public int ItensNoNivel { get; private set; }

        // ---- Referencias de runtime ----
        public PlayerController Player { get; private set; }
        public CompanionDog Companheiro { get; private set; }
        public CameraController Camera2D { get; private set; }
        public Nivel NivelData { get; private set; }
        private LevelBuilder _builder;
        private HUDController _hud;
        private Transform _telas; // raiz das telas de UI

        // ---- Eventos (para a HUD reagir) ----
        public event Action OnEstadoMudou;
        public event Action OnHUDMudou;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            AbrirMenu();
        }

        void Update()
        {
            if (Estado == EstadoJogo.Jogando && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
                Pausar();
            else if (Estado == EstadoJogo.Pausado && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
                Retomar();
        }

        // =========================================================
        //  Transicoes de estado
        // =========================================================
        private void MudarEstado(EstadoJogo novo)
        {
            Estado = novo;
            OnEstadoMudou?.Invoke();
        }

        public void AbrirMenu()
        {
            Time.timeScale = 1f;
            LimparMundo();
            LimparTelas();
            MudarEstado(EstadoJogo.Menu);
            AudioManager.Instance?.PlayMusic("Capa_de_Menu");
            var go = new GameObject("MainMenu");
            go.transform.SetParent(TelasRoot(), false);
            go.AddComponent<MainMenuController>();
        }

        public void NovoJogo()
        {
            Pontos = 0; Vidas = GameConfig.VidasIniciais; NivelAtual = 0;
            CarregarNivel(0);
        }

        /// <summary>Inicia o jogo diretamente numa fase escolhida (selecao de fase do menu).</summary>
        public void NivelAtualInicial(int index)
        {
            index = Mathf.Clamp(index, 0, LevelData.TotalNiveis - 1);
            Pontos = 0; Vidas = GameConfig.VidasIniciais; NivelAtual = index;
            CarregarNivel(index);
        }

        public void CarregarNivel(int index)
        {
            StartCoroutine(RotinaCarregarNivel(index));
        }

        private IEnumerator RotinaCarregarNivel(int index)
        {
            MudarEstado(EstadoJogo.Transicao);
            yield return FadeController.FadeOut(0.35f);

            LimparTelas();
            LimparMundo();
            CombatEvents.Limpar();
            NivelAtual = index;
            NivelData = LevelData.Get(index);
            ItensColetados = 0;
            PontoRespawn = NivelData.spawnPlayer;

            // construir mundo
            var worldGO = new GameObject("Mundo");
            _builder = worldGO.AddComponent<LevelBuilder>();
            _builder.Construir(NivelData);
            ItensNoNivel = _builder.TotalColetaveis;

            // camera
            EnsureCamera();
            Camera2D.DefinirLimites(NivelData.spawnPlayer.x - 6f, NivelData.comprimento + 10f);

            // player
            SpawnarPlayer(NivelData.spawnPlayer);
            Camera2D.AlvoImediato(Player.transform.position);

            // companheiro K9 (acompanha toda a campanha)
            SpawnarCompanheiro(NivelData.spawnPlayer);

            // HUD
            CriarHUD();

            AudioManager.Instance?.PlayMusic(NivelData.musica);
            MudarEstado(EstadoJogo.Jogando);
            NotificarHUD();

            // banner de missao (objetivo da fase)
            var bannerGO = new GameObject("Banner");
            bannerGO.transform.SetParent(TelasRoot(), false);
            MissionBanner.Mostrar(bannerGO.transform, index + 1, NivelData.nome, NivelData.objetivo);

            yield return FadeController.FadeIn(0.35f);
        }

        public void Pausar()
        {
            if (Estado != EstadoJogo.Jogando) return;
            Time.timeScale = 0f;
            MudarEstado(EstadoJogo.Pausado);
            var go = new GameObject("PauseMenu");
            go.transform.SetParent(TelasRoot(), false);
            go.AddComponent<PauseMenuController>();
            AudioManager.Instance?.Play("click");
        }

        public void Retomar()
        {
            Time.timeScale = 1f;
            LimparTelas();
            MudarEstado(EstadoJogo.Jogando);
            AudioManager.Instance?.Play("click");
        }

        public void CompletarFase()
        {
            if (Estado != EstadoJogo.Jogando) return;
            Time.timeScale = 0f;

            // salvar progresso: desbloqueia a proxima fase e atualiza recorde
            SaveSystem.ConcluirFase(NivelAtual);
            SaveSystem.SalvarRecorde(Pontos);

            if (NivelAtual >= LevelData.TotalNiveis - 1)
            {
                Vencer();
                return;
            }
            MudarEstado(EstadoJogo.FaseCompleta);
            AudioManager.Instance?.Play("victory");
            var go = new GameObject("FaseCompleta");
            go.transform.SetParent(TelasRoot(), false);
            go.AddComponent<LevelCompleteController>();
        }

        public void ProximaFase()
        {
            Time.timeScale = 1f;
            CarregarNivel(NivelAtual + 1);
        }

        public void Vencer()
        {
            Time.timeScale = 0f;
            SaveSystem.ConcluirFase(NivelAtual);
            SaveSystem.SalvarRecorde(Pontos);
            MudarEstado(EstadoJogo.Vitoria);
            AudioManager.Instance?.PlayMusic("Capa_de_Menu");
            AudioManager.Instance?.Play("victory");
            var go = new GameObject("Vitoria");
            go.transform.SetParent(TelasRoot(), false);
            go.AddComponent<VictoryController>();
        }

        public void PerderVida()
        {
            Vidas--;
            NotificarHUD();
            AudioManager.Instance?.Play("hurt");
            if (Vidas <= 0)
            {
                GameOver();
            }
            else
            {
                // respawn no inicio da fase
                StartCoroutine(RespawnPlayer());
            }
        }

        private IEnumerator RespawnPlayer()
        {
            MudarEstado(EstadoJogo.Transicao);
            yield return FadeController.FadeOut(0.3f);
            if (Player != null) Destroy(Player.gameObject);
            SpawnarPlayer(PontoRespawn);
            Camera2D.AlvoImediato(Player.transform.position);
            MudarEstado(EstadoJogo.Jogando);
            yield return FadeController.FadeIn(0.3f);
        }

        public void GameOver()
        {
            Time.timeScale = 0f;
            SaveSystem.SalvarRecorde(Pontos);
            MudarEstado(EstadoJogo.Derrota);
            AudioManager.Instance?.StopMusic();
            AudioManager.Instance?.Play("gameover");
            var go = new GameObject("GameOver");
            go.transform.SetParent(TelasRoot(), false);
            go.AddComponent<GameOverController>();
        }

        public void ReiniciarFase()
        {
            Vidas = Mathf.Max(Vidas, 1);
            Time.timeScale = 1f;
            CarregarNivel(NivelAtual);
        }

        public void ReiniciarJogo()
        {
            Time.timeScale = 1f;
            NovoJogo();
        }

        // =========================================================
        //  Pontuacao e coleta
        // =========================================================
        public void AdicionarPontos(int p)
        {
            Pontos += p;
            NotificarHUD();
        }

        public void RegistrarColeta(int pontos)
        {
            ItensColetados++;
            Pontos += pontos;
            AudioManager.Instance?.Play("coin");
            NotificarHUD();
        }

        public void NotificarHUD() => OnHUDMudou?.Invoke();

        // =========================================================
        //  Criacao de objetos
        // =========================================================
        private void SpawnarPlayer(Vector3 pos)
        {
            var go = new GameObject("Player");
            go.transform.position = pos;
            Player = go.AddComponent<PlayerController>();
        }

        private void SpawnarCompanheiro(Vector3 pos)
        {
            var sprite = SpriteLibrary.Get("Sprites/companion/k9_alpha", 200f);
            if (sprite == null) return;
            var go = new GameObject("K9");
            go.transform.position = pos + new Vector3(-2f, 0, 0);
            Companheiro = go.AddComponent<CompanionDog>();
            Companheiro.Configurar(sprite);
        }

        private void EnsureCamera()
        {
            if (Camera2D != null) return;
            var camGO = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            if (camGO.GetComponent<Camera>() == null) camGO.AddComponent<Camera>();
            camGO.tag = "MainCamera";
            Camera2D = camGO.GetComponent<CameraController>();
            if (Camera2D == null) Camera2D = camGO.AddComponent<CameraController>();
        }

        private void CriarHUD()
        {
            var go = new GameObject("HUD");
            go.transform.SetParent(TelasRoot(), false);
            _hud = go.AddComponent<HUDController>();
        }

        // =========================================================
        //  Limpeza
        // =========================================================
        private void LimparMundo()
        {
            if (_builder != null) { Destroy(_builder.gameObject); _builder = null; }
            if (Player != null) { Destroy(Player.gameObject); Player = null; }
            if (Companheiro != null) { Destroy(Companheiro.gameObject); Companheiro = null; }
            var mundo = GameObject.Find("Mundo");
            if (mundo != null) Destroy(mundo);
        }

        private Transform TelasRoot()
        {
            if (_telas == null)
            {
                var go = GameObject.Find("UIRoot");
                if (go == null) go = new GameObject("UIRoot");
                _telas = go.transform;
            }
            return _telas;
        }

        private void LimparTelas()
        {
            var root = TelasRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
            _hud = null;
        }

        public Transform UIParent() => TelasRoot();
    }
}
