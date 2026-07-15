using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>
    /// Ajustes de performance aplicados automaticamente ao iniciar (antes de qualquer cena).
    /// Fixa o alvo de 60 FPS, desliga o vSync para nao travar em 30, deixa a fisica estavel
    /// e aquece o pool de efeitos. Junto com o Object Pooling (FXPool) e o cache de sprites,
    /// mantem o jogo fluido mesmo com muito combate na tela.
    /// </summary>
    public static class PerformanceConfig
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Aplicar()
        {
            // 60 FPS ou mais (vSync desligado para nao limitar em 30 em telas de 30Hz)
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // solver de fisica um pouco mais preciso (colisoes suaves)
            Physics.defaultSolverIterations = 8;
            Physics.defaultSolverVelocityIterations = 2;

            // aquece o pool de efeitos (evita hitch no primeiro tiro/explosao)
            var pool = FXPool.Instance;
            var flashes = new PooledFlash[8];
            for (int i = 0; i < flashes.Length; i++) flashes[i] = pool.PegarFlash();
            for (int i = 0; i < flashes.Length; i++) pool.Devolver(flashes[i]);
            var parts = new PooledParticle[24];
            for (int i = 0; i < parts.Length; i++) parts[i] = pool.PegarParticula();
            for (int i = 0; i < parts.Length; i++) pool.Devolver(parts[i]);
        }
    }
}
