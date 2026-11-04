using System;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Solo física: mover el Rigidbody2D, saltar y detectar suelo.
    /// No tiene ninguna referencia al Animator ni al SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(PlayerInputReader))]
    public class PlayerMotor : MonoBehaviour, IMotorState
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Salto")]
        [SerializeField] private float jumpForce = 13f;
        [Tooltip("Al soltar el botón mientras sube, la velocidad vertical se multiplica por esto (salto variable).")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [Tooltip("Tiempo extra para saltar justo después de salir de una plataforma.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Tiempo que se recuerda un salto pulsado justo antes de tocar el suelo.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Suelo")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private float groundCheckDistance = 0.08f;

        private Rigidbody2D rb;
        private Collider2D col;
        private PlayerInputReader input;

        private readonly Collider2D[] groundHits = new Collider2D[4];
        private ContactFilter2D groundFilter;

        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;
        private float knockbackUntil = float.NegativeInfinity; // mientras dure, no se controla el movimiento
        private float noJumpCutUntil = float.NegativeInfinity;  // impulsos hacia arriba que no se cortan al soltar salto

        public Vector2 Velocity => rb.linearVelocity;
        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;

        public event Action Jumped;
        public event Action Landed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            input = GetComponent<PlayerInputReader>();
            rb.freezeRotation = true;

            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(groundLayer);
        }

        private void OnEnable() => input.JumpPressed += OnJumpPressed;
        private void OnDisable() => input.JumpPressed -= OnJumpPressed;

        private void OnJumpPressed() => lastJumpPressedTime = Time.time;

        /// <summary>Empuja al jugador (por ejemplo al recibir un golpe) y le quita el control un momento.</summary>
        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            rb.linearVelocity = velocity;
            knockbackUntil = Time.time + duration;
            noJumpCutUntil = Time.time + duration;
        }

        /// <summary>Rebote hacia arriba (golpe hacia abajo sobre un enemigo).</summary>
        public void Bounce(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            lastGroundedTime = float.NegativeInfinity;
            noJumpCutUntil = Time.time + 0.2f;
        }

        /// <summary>Mueve al jugador a un punto y lo deja quieto.</summary>
        public void Teleport(Vector2 position)
        {
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            knockbackUntil = float.NegativeInfinity;
        }

        private void FixedUpdate()
        {
            UpdateGround();
            Move();
            Jump();
        }

        private void UpdateGround()
        {
            bool wasGrounded = IsGrounded;
            Bounds b = col.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckDistance * 0.5f);
            Vector2 size = new Vector2(b.size.x * 0.9f, groundCheckDistance);

            // Cuenta cualquier collider de groundLayer que no sea el propio jugador ni un trigger.
            // Mientras sube (velocidad Y > 0) nunca se considera "en suelo": evita falsos aterrizajes tras saltar.
            bool touching = false;
            int count = Physics2D.OverlapBox(center, size, 0f, groundFilter, groundHits);
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i] != col) { touching = true; break; }
            }
            IsGrounded = touching && rb.linearVelocity.y <= 0.01f;

            if (IsGrounded) lastGroundedTime = Time.time;
            if (!wasGrounded && IsGrounded) Landed?.Invoke();
        }

        private void Move()
        {
            if (Time.time < knockbackUntil) return;

            float x = input.Horizontal;
            rb.linearVelocity = new Vector2(x * moveSpeed, rb.linearVelocity.y);

            if (Mathf.Abs(x) > 0.01f) FacingDirection = x > 0f ? 1 : -1;
        }

        private void Jump()
        {
            bool canJump = Time.time - lastGroundedTime <= coyoteTime;
            bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;

            if (canJump && wantsJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                lastJumpPressedTime = float.NegativeInfinity;
                lastGroundedTime = float.NegativeInfinity;
                IsGrounded = false;
                Jumped?.Invoke();
            }
            else if (!input.JumpHeld && rb.linearVelocity.y > 0f && Time.time >= noJumpCutUntil)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var c = GetComponent<Collider2D>();
            if (c == null) return;
            Bounds b = c.bounds;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                new Vector3(b.center.x, b.min.y - groundCheckDistance * 0.5f, 0f),
                new Vector3(b.size.x * 0.9f, groundCheckDistance, 0f));
        }
#endif
    }
}
