using System.Collections;
using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Vida e energia do soldado. Recebe dano de hazards/inimigos, cura por itens e
    /// regenera energia com o tempo. Ao zerar a vida, avisa o GameManager (perde uma vida
    /// e renasce no checkpoint). Tem breve invulnerabilidade + piscada apos levar dano.
    /// API consumida por HUDController, Hazard, EnemyController, Collectible e CompanionDog.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public float Vida    { get; private set; }
        public float Energia { get; private set; }
        public float Escudo  { get; private set; }   // absorve dano antes da vida
        public bool  Morto   { get; private set; }

        public const float EscudoMaximo = 100f;
        public float EscudoPct => Mathf.Clamp01(Escudo / EscudoMaximo);

        private float _invuln;                 // tempo de invulnerabilidade restante
        private SpriteRenderer _sr;            // para a piscada de dano
        private Color _corBase = Color.white;

        void Awake()
        {
            Vida = GameConfig.VidaMaxima;
            Energia = GameConfig.EnergiaMaxima;
            Escudo = 0f;
            Morto = false;
        }

        void Start()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            if (_sr != null) _corBase = _sr.color;
        }

        void Update()
        {
            if (Morto) return;
            if (_invuln > 0f) _invuln -= Time.deltaTime;

            // regen lento de energia (combustivel pro tiro/corrida/HUD)
            if (Energia < GameConfig.EnergiaMaxima)
                Energia = Mathf.Min(GameConfig.EnergiaMaxima, Energia + 8f * Time.deltaTime);
        }

        /// <summary>Dano recebido (assinatura usada por Hazard/EnemyController/EnemyProjectile).</summary>
        public void LevarDano(float dano, Vector3 origem)
        {
            if (Morto || _invuln > 0f || dano <= 0f) return;

            // o escudo absorve o dano primeiro
            if (Escudo > 0f)
            {
                float absorvido = Mathf.Min(Escudo, dano);
                Escudo -= absorvido;
                dano -= absorvido;
                FX.Flash(transform.position + Vector3.up, new Color(0.4f, 0.7f, 1f), 3f, 2f, 0.15f);
            }
            if (dano > 0f) Vida = Mathf.Max(0f, Vida - dano);
            _invuln = 0.8f;
            AudioManager.Instance?.Play("hurt");
            CameraController.Instance?.Shake(0.35f, 0.18f);
            GameManager.Instance?.NotificarHUD();

            // leve empurrao pra longe da origem do dano
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                float dir = Mathf.Sign(transform.position.x - origem.x);
                if (dir == 0) dir = 1;
                rb.velocity = new Vector3(dir * 3.5f, Mathf.Max(rb.velocity.y, 3.0f), 0);
            }

            if (_sr != null) StartCoroutine(Piscar());

            if (Vida <= 0f) Morrer();
        }

        public void Curar(float qtd)
        {
            if (Morto || qtd <= 0f) return;
            Vida = Mathf.Min(GameConfig.VidaMaxima, Vida + qtd);
            GameManager.Instance?.NotificarHUD();
        }

        public void RecarregarEnergia(float qtd)
        {
            if (qtd <= 0f) return;
            Energia = Mathf.Min(GameConfig.EnergiaMaxima, Energia + qtd);
            GameManager.Instance?.NotificarHUD();
        }

        /// <summary>Adiciona escudo (item de escudo). Fica ate ser gasto no dano.</summary>
        public void AdicionarEscudo(float qtd)
        {
            if (qtd <= 0f) return;
            Escudo = Mathf.Min(EscudoMaximo, Escudo + qtd);
            AudioManager.Instance?.Play("escudo", 0.7f);
            GameManager.Instance?.NotificarHUD();
        }

        /// <summary>Gasta energia (tiro/corrida). Retorna false se nao houver o bastante.</summary>
        public bool GastarEnergia(float qtd)
        {
            if (Energia < qtd) return false;
            Energia -= qtd;
            return true;
        }

        /// <summary>Concede invulnerabilidade temporaria (usado pela esquiva/rolamento).</summary>
        public void DarInvulnerabilidade(float t)
        {
            _invuln = Mathf.Max(_invuln, t);
        }

        private void Morrer()
        {
            if (Morto) return;
            Morto = true;
            var pc = GetComponent<PlayerController>();
            if (pc != null) pc.DesativarControle();
            AudioManager.Instance?.Play("hurt");
            // perde uma vida e renasce (ou Game Over) — o GameManager destroi este player
            GameManager.Instance?.PerderVida();
        }

        private IEnumerator Piscar()
        {
            for (int i = 0; i < 3 && _sr != null; i++)
            {
                _sr.color = new Color(1f, 0.4f, 0.4f, 1f);
                yield return new WaitForSeconds(0.08f);
                _sr.color = _corBase;
                yield return new WaitForSeconds(0.08f);
            }
            if (_sr != null) _sr.color = _corBase;
        }
    }
}
