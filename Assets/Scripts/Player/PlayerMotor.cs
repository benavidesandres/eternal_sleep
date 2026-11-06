using System;
using EternalSleep.Village;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Solo física: mover el Rigidbody2D, saltar, impulsarse y detectar suelo.
    /// El doble salto y el impulso solo funcionan si PlayerUpgrades dice que ya se consiguieron.
    /// No tiene ninguna referencia al Animator ni al SpriteRenderer.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(PlayerInputReader))]
    public class PlayerMotor : MonoBehaviour, IMotorState
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;
        [Tooltip("Qué tan rápido alcanza la velocidad máxima en el suelo (unidades/s²).")]
        [SerializeField] private float groundAcceleration = 70f;
        [Tooltip("Qué tan rápido frena al soltar la dirección en el suelo.")]
        [SerializeField] private float groundDeceleration = 90f;
        [Tooltip("Aceleración en el aire (menor = menos control en el aire).")]
        [SerializeField] private float airAcceleration = 45f;

        [Header("Gravedad")]
        [Tooltip("Gravedad extra al caer (salto más pesado, estilo metroidvania).")]
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float maxFallSpeed = 20f;

        [Header("Salto")]
        [Tooltip("15 = unas 3,7 casillas de alto: llega a cualquier plataforma a 3 casillas y a ninguna a 4.")]
        [SerializeField] private float jumpForce = 15f;
        [Tooltip("Al soltar el botón mientras sube, la velocidad vertical se multiplica por esto (salto variable).")]
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [Tooltip("Tiempo extra para saltar justo después de salir de una plataforma.")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("Tiempo que se recuerda un salto pulsado justo antes de tocar el suelo.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Mejoras")]
        [Tooltip("Fuerza del doble salto. Con el salto normal suma unas 6,7 casillas.")]
        [SerializeField] private float airJumpForce = 13.5f;
        [SerializeField] private float dashSpeed = 16f;
        [SerializeField] private float dashDuration = 0.18f;
        [Tooltip("Tiempo mínimo entre impulsos. En el aire además hay que tocar suelo para recargarlo.")]
        [SerializeField] private float dashCooldown = 0.4f;

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
        private float lastDashPressedTime = float.NegativeInfinity;
        private float baseGravityScale;

        private bool airJumpReady;
        private bool dashReady;
        private int dashDirection;
        private float dashEndsAt;
        private float dashForcedEndAt;
        private float nextDashAt = float.NegativeInfinity;

        public Vector2 Velocity => rb.linearVelocity;
        public bool IsGrounded { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public bool IsDashing { get; private set; }

        public event Action Jumped;
        public event Action Landed;
        public event Action AirJumped;
        public event Action Dashed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            input = GetComponent<PlayerInputReader>();
            rb.freezeRotation = true;
            baseGravityScale = rb.gravityScale;

            groundFilter = new ContactFilter2D { useTriggers = false };
            groundFilter.SetLayerMask(groundLayer);
        }

        private void OnEnable()
        {
            input.JumpPressed += OnJumpPressed;
            input.DashPressed += OnDashPressed;
        }

        private void OnDisable()
        {
            input.JumpPressed -= OnJumpPressed;
            input.DashPressed -= OnDashPressed;
            EndDash();
        }

        private void OnJumpPressed() => lastJumpPressedTime = Time.time;
        private void OnDashPressed() => lastDashPressedTime = Time.time;

        /// <summary>Empuja al jugador (por ejemplo al recibir un golpe) y le quita el control un momento.</summary>
        public void ApplyKnockback(Vector2 velocity, float duration)
        {
            EndDash();
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
            airJumpReady = true; // rebotar en un enemigo recarga las mejoras, como tocar suelo
            dashReady = true;
        }

        /// <summary>Mueve al jugador a un punto y lo deja quieto.</summary>
        public void Teleport(Vector2 position)
        {
            EndDash();
            rb.position = position;
            transform.position = position;
            rb.linearVelocity = Vector2.zero;
            knockbackUntil = float.NegativeInfinity;
        }

        private void FixedUpdate()
        {
            UpdateGround();
            UpdateDash();
            if (!IsDashing)
            {
                Move();
                Jump();
            }
            ApplyGravity();
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

            if (IsGrounded)
            {
                lastGroundedTime = Time.time;
                airJumpReady = true;
                dashReady = true;
            }
            if (!wasGrounded && IsGrounded) Landed?.Invoke();
        }

        private void Move()
        {
            if (Time.time < knockbackUntil) return;

            float x = input.Horizontal;
            float target = x * moveSpeed;
            bool accelerating = Mathf.Abs(x) > 0.01f;
            float rate = !IsGrounded ? airAcceleration : accelerating ? groundAcceleration : groundDeceleration;
            float vx = Mathf.MoveTowards(rb.linearVelocity.x, target, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

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
            else if (wantsJump && airJumpReady && Time.time >= knockbackUntil && PlayerUpgrades.Has(Upgrade.DoubleJump))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, airJumpForce);
                lastJumpPressedTime = float.NegativeInfinity;
                airJumpReady = false;
                AirJumped?.Invoke();
            }
            else if (!input.JumpHeld && rb.linearVelocity.y > 0f && Time.time >= noJumpCutUntil)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        private void UpdateDash()
        {
            if (IsDashing)
            {
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
                bool finished = Time.time >= dashEndsAt && !DashVeil.Overlaps(col);
                if (finished || Time.time >= dashForcedEndAt)
                {
                    EndDash();
                    rb.linearVelocity = new Vector2(dashDirection * moveSpeed, 0f);
                }
                return;
            }

            bool wantsDash = Time.time - lastDashPressedTime <= jumpBufferTime;
            if (!wantsDash || !dashReady || Time.time < nextDashAt || Time.time < knockbackUntil) return;
            if (!PlayerUpgrades.Has(Upgrade.Dash)) return;

            if (Mathf.Abs(input.Horizontal) > 0.01f) FacingDirection = input.Horizontal > 0f ? 1 : -1;
            dashDirection = FacingDirection;
            dashEndsAt = Time.time + dashDuration;
            dashForcedEndAt = dashEndsAt + dashDuration * 2f;
            nextDashAt = Time.time + dashCooldown;
            lastDashPressedTime = float.NegativeInfinity;
            dashReady = false;
            IsDashing = true;
            DashVeil.SetPassable(col, true);
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            Dashed?.Invoke();
        }

        private void EndDash()
        {
            if (!IsDashing) return;
            IsDashing = false;
            DashVeil.SetPassable(col, false);
        }

        private void ApplyGravity()
        {
            if (IsDashing)
            {
                rb.gravityScale = 0f;
                return;
            }
            rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravityScale * fallGravityMultiplier : baseGravityScale;
            if (rb.linearVelocity.y < -maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
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
