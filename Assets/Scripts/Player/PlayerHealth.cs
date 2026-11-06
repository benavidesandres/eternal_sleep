using System;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Vida del jugador (corazones).
    /// - Golpe de enemigo: quita vida, lo empuja y queda invulnerable un momento.
    /// - Pinchos o caídas: quita vida y lo devuelve al último suelo seguro.
    /// - Sin vida: avisa con Died (el LevelManager reinicia el nivel).
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invulnerableTime = 1f;
        [SerializeField] private Vector2 knockback = new Vector2(6f, 7f);
        [SerializeField] private float knockbackDuration = 0.2f;
        [SerializeField] private float blinkInterval = 0.08f;

        private PlayerMotor motor;
        private SpriteRenderer sprite;
        private float invulnerableUntil = float.NegativeInfinity;
        private Vector2 lastSafePosition;
        private float groundedSince = float.PositiveInfinity;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead => Current <= 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        /// <summary>(vida actual, vida máxima)</summary>
        public event Action<int, int> HealthChanged;
        public event Action Died;
        /// <summary>Recibió daño (posición desde donde vino el golpe). Para efectos visuales.</summary>
        public event Action<Vector2> Damaged;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            maxHealth += PlayerUpgrades.BonusHearts;
            Current = maxHealth;
            lastSafePosition = transform.position;
        }

        private void OnEnable() => PlayerUpgrades.HeartAdded += OnHeartAdded;
        private void OnDisable() => PlayerUpgrades.HeartAdded -= OnHeartAdded;

        private void Start() => HealthChanged?.Invoke(Current, maxHealth);

        /// <summary>Corazón nuevo: sube la vida máxima y deja a Kael curado del todo.</summary>
        private void OnHeartAdded()
        {
            maxHealth++;
            Current = maxHealth;
            HealthChanged?.Invoke(Current, maxHealth);
        }

        private void FixedUpdate()
        {
            // Guarda la posición como "segura" si lleva un ratito en el suelo sin estar recibiendo daño.
            if (IsDead) return;
            if (motor.IsGrounded)
            {
                if (float.IsPositiveInfinity(groundedSince)) groundedSince = Time.time;
                if (Time.time - groundedSince > 0.15f && !IsInvulnerable) lastSafePosition = transform.position;
            }
            else groundedSince = float.PositiveInfinity;
        }

        private void Update()
        {
            if (sprite == null) return;
            bool blink = IsInvulnerable && !IsDead && Mathf.Repeat(Time.time, blinkInterval * 2f) < blinkInterval;
            var c = sprite.color;
            c.a = blink ? 0.3f : 1f;
            sprite.color = c;
        }

        /// <summary>Golpe normal (enemigos): quita vida y empuja en sentido contrario.</summary>
        public void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead || IsInvulnerable) return;
            Damaged?.Invoke(from);
            if (!Damage(amount)) return;

            float dir = Mathf.Sign(transform.position.x - from.x);
            if (dir == 0f) dir = -motor.FacingDirection;
            motor.ApplyKnockback(new Vector2(dir * knockback.x, knockback.y), knockbackDuration);
        }

        /// <summary>Pinchos o caída al vacío: quita vida y vuelve al último suelo seguro.</summary>
        public void HazardHit(int amount = 1)
        {
            if (IsDead) return;
            if (!IsInvulnerable) Damaged?.Invoke(transform.position + Vector3.down);
            bool alive = IsInvulnerable || Damage(amount);
            if (alive) motor.Teleport(lastSafePosition);
        }

        /// <summary>Resta vida. Devuelve false si el jugador murió.</summary>
        private bool Damage(int amount)
        {
            Current = Mathf.Max(0, Current - amount);
            invulnerableUntil = Time.time + invulnerableTime;
            HealthChanged?.Invoke(Current, maxHealth);

            if (Current > 0) return true;

            motor.Teleport(transform.position); // se queda quieto
            motor.enabled = false;
            Died?.Invoke();
            return false;
        }
    }
}
