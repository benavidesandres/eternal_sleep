using System;
using System.Collections.Generic;
using EternalSleep.Village;
using UnityEngine;

namespace EternalSleep.Player
{
    public enum AttackDirection { Forward, Up, Down, UpForward, DownForward }

    /// <summary>
    /// Decide cuándo se ataca y en qué dirección (según lo que se empuje: delante, arriba, abajo
    /// o en diagonal) y golpea todo lo que tenga IDamageable dentro del área del ataque.
    /// Todas las direcciones sirven en el suelo, saltando y cayendo.
    /// - Hacia delante se encadenan hasta 3 golpes (combo); el tercero pega más fuerte.
    /// - Con Aliento Contenido, mantener atacar carga un golpe que se suelta al levantar el botón.
    /// - En el suelo, el golpe hacia abajo barre bajo y por delante.
    /// - El golpe hacia abajo (recto o en diagonal) que acierta en el aire hace rebotar al jugador.
    /// - El golpe al suelo (Peso del Sueño) lo empieza PlayerMotor; aquí solo se aplica su daño al aterrizar.
    /// No sabe nada de animaciones; quien quiera reaccionar se suscribe a los eventos.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float cooldown = 0.45f;
        [Tooltip("Cuánto hay que empujar arriba/abajo para contar como ataque direccional.")]
        [SerializeField, Range(0.1f, 1f)] private float verticalThreshold = 0.5f;
        [Tooltip("Cuánto hay que empujar a un lado, además de arriba/abajo, para que el golpe sea en diagonal.")]
        [SerializeField, Range(0.1f, 1f)] private float diagonalThreshold = 0.4f;

        [Header("Golpe")]
        [SerializeField] private int damage = 1;
        [SerializeField] private Vector2 forwardSize = new Vector2(1.6f, 1.3f);
        [SerializeField] private Vector2 verticalSize = new Vector2(1.4f, 1.5f);
        [SerializeField] private Vector2 diagonalSize = new Vector2(1.5f, 1.5f);
        [SerializeField] private float downBounceForce = 12f;

        [Header("Combo")]
        [Tooltip("Espera mínima entre golpes encadenados.")]
        [SerializeField] private float comboCooldown = 0.2f;
        [Tooltip("Tiempo máximo tras un golpe para encadenar el siguiente.")]
        [SerializeField] private float comboWindow = 0.5f;
        [SerializeField] private int finisherBonusDamage = 1;
        [SerializeField] private float finisherSizeMultiplier = 1.25f;
        [SerializeField] private float finisherLungeSpeed = 5f;
        [SerializeField] private float finisherLungeTime = 0.1f;

        [Header("Ataque cargado (mejora)")]
        [SerializeField] private float chargeTime = 0.6f;
        [SerializeField] private int chargedDamageMultiplier = 3;
        [SerializeField] private float chargedSizeMultiplier = 1.5f;

        [Header("Golpe al suelo (mejora)")]
        [SerializeField] private int slamDamage = 2;
        [SerializeField] private Vector2 slamSize = new Vector2(3f, 1f);

        private PlayerInputReader input;
        private PlayerMotor motor;
        private Collider2D body;
        private float lastAttackTime = float.NegativeInfinity;
        private int comboStep;           // 0, 1 o 2: golpe del combo que se hizo el último
        private bool lastWasForward;
        private float chargeStartedAt;
        private readonly Collider2D[] hits = new Collider2D[16];
        private readonly HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        public bool IsCharging { get; private set; }
        public bool IsChargeReady { get; private set; }
        /// <summary>0 al pulsar, 1 cuando el golpe cargado está listo.</summary>
        public float ChargeProgress => IsCharging ? Mathf.Clamp01((Time.time - chargeStartedAt) / chargeTime) : 0f;

        /// <summary>Dirección del golpe y paso del combo (0, 1 o 2; los ataques arriba/abajo siempre son 0).</summary>
        public event Action<AttackDirection, int> Attacked;
        public event Action ChargeReady;
        public event Action ChargedAttacked;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>();
            body = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            input.AttackPressed += TryAttack;
            input.AttackReleased += OnAttackReleased;
            if (motor != null) motor.Slammed += OnSlammed;
        }

        private void OnDisable()
        {
            input.AttackPressed -= TryAttack;
            input.AttackReleased -= OnAttackReleased;
            if (motor != null) motor.Slammed -= OnSlammed;
            CancelCharge();
        }

        private void Update()
        {
            if (!IsCharging) return;
            if (!input.AttackHeld || (motor != null && (motor.IsSlamming || motor.IsDashing)))
            {
                CancelCharge();
                return;
            }
            if (!IsChargeReady && Time.time - chargeStartedAt >= chargeTime)
            {
                IsChargeReady = true;
                ChargeReady?.Invoke();
            }
        }

        private void TryAttack()
        {
            if (motor != null && motor.IsSlamming) return;

            AttackDirection dir = ReadDirection();

            // La carga empieza con cada pulsación, aunque el golpe rápido aún esté en espera.
            if (PlayerUpgrades.Has(Upgrade.ChargedAttack))
            {
                IsCharging = true;
                IsChargeReady = false;
                chargeStartedAt = Time.time;
            }

            float since = Time.time - lastAttackTime;
            bool chaining = dir == AttackDirection.Forward && lastWasForward && comboStep < 2 && since <= comboWindow;
            if (since < (chaining ? comboCooldown : cooldown)) return;

            lastAttackTime = Time.time;
            comboStep = chaining ? comboStep + 1 : 0;
            lastWasForward = dir == AttackDirection.Forward;

            Attacked?.Invoke(dir, comboStep);

            bool finisher = comboStep == 2;
            Hit(dir, finisher ? damage + finisherBonusDamage : damage, finisher ? finisherSizeMultiplier : 1f);
            if (finisher && motor != null) motor.Lunge(finisherLungeSpeed, finisherLungeTime);
        }

        private AttackDirection ReadDirection()
        {
            bool up = input.Vertical > verticalThreshold;
            bool down = input.Vertical < -verticalThreshold;
            bool side = Mathf.Abs(input.Horizontal) > diagonalThreshold;
            bool grounded = motor != null && motor.IsGrounded;

            if (up) return side ? AttackDirection.UpForward : AttackDirection.Up;
            // En el suelo no hay nada debajo: el golpe hacia abajo barre bajo y por delante.
            if (down) return side || grounded ? AttackDirection.DownForward : AttackDirection.Down;
            return AttackDirection.Forward;
        }

        private void OnAttackReleased()
        {
            bool ready = IsChargeReady;
            CancelCharge();
            if (!ready) return;

            lastAttackTime = Time.time;
            comboStep = 0;
            lastWasForward = false;
            ChargedAttacked?.Invoke();
            Hit(AttackDirection.Forward, damage * chargedDamageMultiplier, chargedSizeMultiplier);
        }

        private void CancelCharge()
        {
            IsCharging = false;
            IsChargeReady = false;
        }

        private void Hit(AttackDirection dir, int amount, float sizeMultiplier)
        {
            GetArea(dir, out Vector2 center, out Vector2 size);
            size *= sizeMultiplier;
            if (dir == AttackDirection.Forward)
                center.x += (motor != null ? motor.FacingDirection : 1) * (size.x - size.x / sizeMultiplier) * 0.5f;

            int count = DamageArea(center, size, amount);

            bool downward = dir == AttackDirection.Down || dir == AttackDirection.DownForward;
            if (downward && count > 0 && motor != null && !motor.IsGrounded)
                motor.Bounce(downBounceForce);
        }

        private void OnSlammed(Vector2 feet)
        {
            // El área se hunde un poco en el suelo para alcanzar los suelos agrietados que hay debajo.
            Vector2 center = feet + Vector2.up * (slamSize.y * 0.5f - 0.25f);
            DamageArea(center, slamSize, slamDamage);

            int count = Physics2D.OverlapBox(center, slamSize, 0f, ContactFilter2D.noFilter, hits);
            for (int i = 0; i < count; i++)
            {
                var floor = hits[i] != null ? hits[i].GetComponentInParent<BreakableFloor>() : null;
                if (floor != null) floor.Break();
            }
        }

        /// <summary>Daña una vez a cada IDamageable dentro del área. Devuelve a cuántos golpeó.</summary>
        private int DamageArea(Vector2 center, Vector2 size, int amount)
        {
            var filter = ContactFilter2D.noFilter; // incluye triggers (enemigos, orbes)
            int count = Physics2D.OverlapBox(center, size, 0f, filter, hits);

            damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null || hits[i] == body) continue;
                var target = hits[i].GetComponentInParent<IDamageable>();
                if (target == null || !damaged.Add(target)) continue;
                target.TakeDamage(amount, transform.position);
            }
            return damaged.Count;
        }

        private void GetArea(AttackDirection dir, out Vector2 center, out Vector2 size)
        {
            Bounds b = body != null ? body.bounds : new Bounds(transform.position, Vector3.one);
            int facing = motor != null ? motor.FacingDirection : 1;

            switch (dir)
            {
                case AttackDirection.Up:
                    size = verticalSize;
                    center = new Vector2(b.center.x, b.max.y + size.y * 0.5f);
                    break;
                case AttackDirection.Down:
                    size = verticalSize;
                    center = new Vector2(b.center.x, b.min.y - size.y * 0.5f);
                    break;
                case AttackDirection.UpForward:
                    size = diagonalSize;
                    center = new Vector2(b.center.x + facing * (b.extents.x + size.x * 0.3f), b.max.y + size.y * 0.1f);
                    break;
                case AttackDirection.DownForward:
                    size = diagonalSize;
                    center = new Vector2(b.center.x + facing * (b.extents.x + size.x * 0.3f), b.min.y + size.y * 0.1f);
                    break;
                default:
                    size = forwardSize;
                    center = new Vector2(b.center.x + facing * (b.extents.x + size.x * 0.5f), b.center.y);
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (body == null) body = GetComponent<Collider2D>();
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.8f);
            foreach (AttackDirection d in Enum.GetValues(typeof(AttackDirection)))
            {
                GetArea(d, out var c, out var s);
                Gizmos.DrawWireCube(c, s);
            }
            if (body == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.8f);
            var feet = new Vector2(body.bounds.center.x, body.bounds.min.y);
            Gizmos.DrawWireCube(feet + Vector2.up * (slamSize.y * 0.5f - 0.25f), slamSize);
        }
#endif
    }
}
