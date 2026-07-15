using UnityEngine;

namespace OperacaoResgate
{
    /// <summary>Mantem o sprite sempre de frente para a camera (eixo vertical travado).</summary>
    public class Billboard : MonoBehaviour
    {
        private Transform _cam;
        void Start() { if (Camera.main != null) _cam = Camera.main.transform; }
        void LateUpdate()
        {
            if (_cam == null) { if (Camera.main != null) _cam = Camera.main.transform; else return; }
            // olha para a camera apenas no plano horizontal (evita inclinar o sprite)
            Vector3 dir = transform.position - _cam.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    /// <summary>Plataforma estatica solida. Apenas marca o tipo (a geometria vem do LevelBuilder).</summary>
    public class Platform : MonoBehaviour
    {
        public bool ehSolo;
    }
}
