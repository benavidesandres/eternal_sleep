using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// "Ojos" de un enemigo: sabe dónde está el jugador y si lo puede ver
    /// (dentro del alcance, mirando hacia él y sin paredes en medio).
    /// También recuerda dónde lo vio por última vez.
    /// </summary>
    public class EnemySenses : MonoBehaviour
    {
        [SerializeField] private float viewRange = 6f;
        [Tooltip("Si es mayor que 0, solo ve hacia donde mira (1 = solo de frente, 0 = 180°).")]
        [SerializeField, Range(-1f, 1f)] private float minFacingDot = -1f;
        [SerializeField] private Vector2 eyeOffset = new Vector2(0f, 0.4f);

        private PlayerHealth player;
        private int groundMask;

        public Vector2 LastSeenPosition { get; private set; }
        public float LastSeenTime { get; private set; } = float.NegativeInfinity;
        public float ViewRange => viewRange;

        /// <summary>Centro del cuerpo del jugador (no los pies).</summary>
        public Vector2 PlayerPosition => player != null ? (Vector2)player.transform.position + Vector2.up * 0.9f : (Vector2)transform.position;
        public bool PlayerAvailable => player != null && !player.IsDead;
        public Vector2 Eye => (Vector2)transform.position + eyeOffset;

        private void Awake()
        {
            groundMask = LayerMask.GetMask("Ground");
            player = FindAnyObjectByType<PlayerHealth>();
        }

        /// <summary>¿Ve al jugador ahora? facing: 1 derecha, -1 izquierda, 0 en todas direcciones.</summary>
        public bool CanSeePlayer(int facing = 0, float rangeMultiplier = 1f)
        {
            if (!PlayerAvailable) return false;

            Vector2 eye = Eye, target = PlayerPosition, toPlayer = target - eye;
            if (toPlayer.magnitude > viewRange * rangeMultiplier) return false;
            if (facing != 0 && Vector2.Dot(toPlayer.normalized, Vector2.right * facing) < minFacingDot) return false;
            if (Physics2D.Linecast(eye, target, groundMask)) return false; // hay una pared en medio

            LastSeenPosition = target;
            LastSeenTime = Time.time;
            return true;
        }

        /// <summary>Segundos desde que lo vio por última vez.</summary>
        public float TimeSinceSeen => Time.time - LastSeenTime;

        public float DistanceToPlayer => PlayerAvailable ? Vector2.Distance(Eye, PlayerPosition) : float.PositiveInfinity;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere((Vector2)transform.position + eyeOffset, viewRange);
        }
#endif
    }
}
