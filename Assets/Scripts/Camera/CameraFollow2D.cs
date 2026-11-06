using EternalSleep.Player;
using EternalSleep.Village;
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
        private Vector3 smoothed;   // posición sin la sacudida
        private float shakeAmount;
        private float shakeUntil;
        private float shakeDuration;
        private Room currentRoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            smoothed = transform.position;
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerMotor>();
                if (player != null) target = player.transform;
            }
            if (target != null) smoothed = transform.position = Desired();
        }

        /// <summary>Limita la cámara a este rectángulo del mundo.</summary>
        public void SetBounds(Rect area)
        {
            bounds = area;
            useBounds = true;
            if (target != null) smoothed = transform.position = Desired();
        }

        /// <summary>Sacude la cámara (golpes). Se va apagando sola.</summary>
        public void Shake(float amount, float seconds)
        {
            if (amount < CurrentShake) return;
            shakeAmount = amount;
            shakeDuration = Mathf.Max(0.01f, seconds);
            shakeUntil = Time.unscaledTime + seconds;
        }

        private float CurrentShake => Time.unscaledTime < shakeUntil
            ? shakeAmount * (shakeUntil - Time.unscaledTime) / shakeDuration
            : 0f;

        private void LateUpdate()
        {
            if (target != null && Room.Any)
            {
                // Al cambiar de habitación (entrar a una casa) la cámara corta directamente, sin deslizarse.
                var room = Room.At(target.position);
                if (room != null && room != currentRoom)
                {
                    currentRoom = room;
                    bounds = room.Area;
                    useBounds = true;
                    smoothed = Desired();
                    velocity = Vector3.zero;
                }
            }
            if (target != null)
                smoothed = Vector3.SmoothDamp(smoothed, Desired(), ref velocity, smoothTime);
            transform.position = smoothed + (Vector3)(Random.insideUnitCircle * CurrentShake);
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
