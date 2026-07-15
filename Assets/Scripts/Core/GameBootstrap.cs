using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OperacaoResgate
{
    /// <summary>
    /// Ponto de entrada do jogo. Roda automaticamente assim que a cena carrega,
    /// sem precisar de objetos pre-configurados no editor. Cria toda a estrutura
    /// persistente (gerenciadores, camera, luz, EventSystem) por codigo.
    ///
    /// Isso torna o projeto "a prova de erros": basta abrir e dar Play em uma cena
    /// vazia que o jogo se monta sozinho.
    /// </summary>
    public static class GameBootstrap
    {
        private static bool _iniciado;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Boot()
        {
            if (_iniciado) return;
            _iniciado = true;

            // --- Raiz persistente do jogo ---
            var game = new GameObject("Game");
            Object.DontDestroyOnLoad(game);
            game.AddComponent<AudioManager>();
            game.AddComponent<GameManager>();

            // --- Camera principal (perspectiva, visao 2.5D) ---
            if (Camera.main == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                var cam = camGO.AddComponent<Camera>();
                cam.orthographic = false;
                cam.fieldOfView = 50f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 200f;
                cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
                camGO.AddComponent<AudioListener>();
            }
            else if (Camera.main.GetComponent<AudioListener>() == null)
            {
                Camera.main.gameObject.AddComponent<AudioListener>();
            }

            // --- Luz direcional (sol) para iluminar o relevo 3D ---
            if (Object.FindObjectOfType<Light>() == null)
            {
                var lightGO = new GameObject("Sol");
                var light = lightGO.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.96f, 0.86f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                lightGO.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
                Object.DontDestroyOnLoad(lightGO);
            }

            // luz ambiente quente
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);

            // --- EventSystem (necessario para botoes de UI) ---
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Object.DontDestroyOnLoad(es);
            }

            // --- Raiz das telas de UI ---
            var uiRoot = new GameObject("UIRoot");
            Object.DontDestroyOnLoad(uiRoot);
        }
    }
}
