using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Camera de acompanhamento suave (estilo plataforma 2.5D). Segue o jogador em X/Y com
    /// SmoothDamp, recuo em Z e leve angulo para dar profundidade. Recursos adicionados:
    ///  - SHAKE (tremor) por explosoes e ao receber dano (ruido decaindo).
    ///  - ZOOM AUTOMATICO: recua um pouco quando ha muito combate/chefe e aproxima quando
    ///    a area esta calma, dando enquadramento cinematografico.
    ///  - HDR habilitado (para brilhos/emissivos ficarem mais vivos).
    ///  - Limites horizontais do nivel.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }
        public Camera Camera2D { get; private set; }

        private const float DistanciaBase = 10.5f;
        private const float Altura        = 2.4f;

        private Vector3 _velocidade;
        private float _limiteEsq = -999f;
        private float _limiteDir = 999f;

        // shake
        private float _shakeIntensidade;
        private float _shakeRestante;
        private Vector3 _offsetShake;

        // zoom automatico
        private float _distancia = DistanciaBase;
        private float _distanciaAlvo = DistanciaBase;

        void Awake()
        {
            Instance = this;
            Camera2D = GetComponent<Camera>();
            Camera2D.orthographic = false;
            Camera2D.fieldOfView = 50f;
            Camera2D.nearClipPlane = 0.1f;
            Camera2D.farClipPlane = 220f;
            Camera2D.clearFlags = CameraClearFlags.SolidColor;
            Camera2D.backgroundColor = new Color(0.05f, 0.07f, 0.09f);
            Camera2D.allowHDR = true;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void DefinirLimites(float esq, float dir) { _limiteEsq = esq; _limiteDir = dir; }

        public void AlvoImediato(Vector3 posAlvo)
        {
            _distancia = _distanciaAlvo;
            transform.position = CalcularPosicao(posAlvo);
            transform.rotation = Quaternion.Euler(6f, 0f, 0f);
        }

        /// <summary>Aplica um tremor de camera. intensidade em unidades, duracao em segundos.</summary>
        public void Shake(float intensidade, float duracao)
        {
            // acumula: um tremor maior sobrepoe um menor
            _shakeIntensidade = Mathf.Max(_shakeIntensidade, intensidade);
            _shakeRestante = Mathf.Max(_shakeRestante, duracao);
        }

        /// <summary>Define o alvo de zoom (0 = normal). Positivo recua, negativo aproxima.</summary>
        public void DefinirZoom(float extra)
        {
            _distanciaAlvo = Mathf.Clamp(DistanciaBase + extra, 8f, 16f);
        }

        void LateUpdate()
        {
            if (GameManager.Instance == null) return;
            var player = GameManager.Instance.Player;
            if (player == null) return;

            // zoom automatico conforme o contexto
            AjustarZoomAutomatico();
            _distancia = Mathf.Lerp(_distancia, _distanciaAlvo, 2f * Time.deltaTime);

            Vector3 desejado = CalcularPosicao(player.transform.position);

            // shake
            if (_shakeRestante > 0f)
            {
                _shakeRestante -= Time.deltaTime;
                float amt = _shakeIntensidade * Mathf.Clamp01(_shakeRestante / 0.35f);
                _offsetShake = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * 2f, 0f) * amt;
                if (_shakeRestante <= 0f) { _shakeIntensidade = 0f; _offsetShake = Vector3.zero; }
            }

            transform.position = Vector3.SmoothDamp(transform.position, desejado + _offsetShake, ref _velocidade,
                                                    GameConfig.CameraSuavidade);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(6f, 0f, 0f), 0.1f);
        }

        private void AjustarZoomAutomatico()
        {
            var gm = GameManager.Instance;
            float extra = 0f;
            if (gm.NivelData != null && gm.NivelData.ehChefe) extra = 2.5f;          // chefe: enquadra a arena
            else
            {
                var player = gm.Player;
                if (player != null && player.Corpo != null && Mathf.Abs(player.Corpo.velocity.x) > 7f)
                    extra = 1.2f;   // correndo: recua um pouco
            }
            _distanciaAlvo = Mathf.Lerp(_distanciaAlvo, DistanciaBase + extra, 2f * Time.deltaTime);
        }

        private Vector3 CalcularPosicao(Vector3 posAlvo)
        {
            float x = Mathf.Clamp(posAlvo.x, _limiteEsq, _limiteDir);
            float y = posAlvo.y + Altura;
            return new Vector3(x, Mathf.Max(y, 2.2f), -_distancia);
        }
    }
}
