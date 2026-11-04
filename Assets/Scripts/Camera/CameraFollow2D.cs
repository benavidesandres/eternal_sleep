using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.CameraSystem
{
    /// <summary>
    /// Hace que la cámara siga al jugador con un movimiento suave.
    /// Si no tiene objetivo asignado, busca al Player en la escena.
    /// Opcionalmente no deja ver fuera de los límites del nivel (SetBounds).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private bool useBounds;
        [SerializeField] private Rect bounds;

        private Camera cam;
        private Vector3 velocity;

        private void Awake() => cam = GetComponent<Camera>();

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerMotor>();
                if (player != null) target = player.transform;
            }
            if (target != null) transform.position = Desired();
        }

        /// <summary>Limita la cámara a este rectángulo del mundo.</summary>
        public void SetBounds(Rect area)
        {
            bounds = area;
            useBounds = true;
            if (target != null) transform.position = Desired();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }

        private Vector3 Desired()
        {
            float x = target.position.x + offset.x;
            float y = target.position.y + offset.y;

            if (useBounds && cam != null && cam.orthographic)
            {
                float halfH = cam.orthographicSize;
                float halfW = halfH * cam.aspect;
                x = bounds.width > halfW * 2f ? Mathf.Clamp(x, bounds.xMin + halfW, bounds.xMax - halfW) : bounds.center.x;
                y = bounds.height > halfH * 2f ? Mathf.Clamp(y, bounds.yMin + halfH, bounds.yMax - halfH) : bounds.center.y;
            }

            return new Vector3(x, y, transform.position.z);
        }
    }
}
