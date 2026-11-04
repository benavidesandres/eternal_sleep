using UnityEngine;

namespace EternalSleep.Level
{
    /// <summary>
    /// Capa de fondo que se mueve más lento que la cámara para dar profundidad.
    /// 0 = se queda pegada al mundo, 1 = va pegada a la cámara.
    /// Usa un SpriteRenderer en modo Tiled: se repite hacia los lados sin cortes.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float followX = 0.5f;
        [SerializeField, Range(0f, 1f)] private float followY = 0.8f;
        [Tooltip("Desplazamiento automático (para la niebla), en unidades por segundo.")]
        [SerializeField] private float drift;
        [Tooltip("Si se repite hacia los lados (desactivar para objetos sueltos como la luna).")]
        [SerializeField] private bool repeat = true;
        [Tooltip("Posición de la cámara para la que se colocó esta capa. Si no se usa, se toma la del primer frame.")]
        [SerializeField] private bool useCameraReference;
        [SerializeField] private Vector2 cameraReference;

        private Transform cam;
        private Vector3 startPos;
        private Vector3 camStart;
        private float tileWidth;

        private void Start()
        {
            startPos = transform.position;
            var sr = GetComponent<SpriteRenderer>();
            tileWidth = sr.sprite != null ? sr.sprite.bounds.size.x * transform.lossyScale.x : 0f;
        }

        private void LateUpdate()
        {
            // Se toma la posición de la cámara en el primer frame (cuando ya se colocó sobre el jugador).
            if (cam == null)
            {
                cam = Camera.main != null ? Camera.main.transform : null;
                if (cam == null) return;
                camStart = useCameraReference ? new Vector3(cameraReference.x, cameraReference.y, cam.position.z) : cam.position;
            }
            Vector3 d = cam.position - camStart;
            float x = startPos.x + d.x * followX + drift * Time.time;
            float y = startPos.y + d.y * followY;

            // Mantiene la capa centrada en la cámara saltando de a un "azulejo", así nunca se ve el borde.
            if (repeat && tileWidth > 0f)
            {
                float offset = cam.position.x - x;
                x += Mathf.Round(offset / tileWidth) * tileWidth;
            }

            transform.position = new Vector3(x, y, startPos.z);
        }
    }
}
