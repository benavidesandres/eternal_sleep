using System;
using System.Collections.Generic;
using UnityEngine;

namespace EternalSleep.Player
{
    public enum AttackDirection { Forward, Up, Down }

    /// <summary>
    /// Decide cuándo se ataca y en qué dirección (según el eje vertical pulsado)
    /// y golpea todo lo que tenga IDamageable dentro del área del ataque.
    /// Si el golpe hacia abajo acierta en el aire, el jugador rebota.
    /// No sabe nada de animaciones; quien quiera reaccionar se suscribe a Attacked.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float cooldown = 0.45f;
        [Tooltip("Cuánto hay que empujar arriba/abajo para contar como ataque direccional.")]
        [SerializeField, Range(0.1f, 1f)] private float verticalThreshold = 0.5f;

        [Header("Golpe")]
        [SerializeField] private int damage = 1;
        [SerializeField] private Vector2 forwardSize = new Vector2(1.6f, 1.3f);
        [SerializeField] private Vector2 verticalSize = new Vector2(1.4f, 1.5f);
        [SerializeField] private float downBounceForce = 12f;

        private PlayerInputReader input;
        private PlayerMotor motor;
        private Collider2D body;
        private float lastAttackTime = float.NegativeInfinity;
        private readonly Collider2D[] hits = new Collider2D[16];
        private readonly HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        public event Action<AttackDirection> Attacked;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>();
            body = GetComponent<Collider2D>();
        }

        private void OnEnable() => input.AttackPressed += TryAttack;
        private void OnDisable() => input.AttackPressed -= TryAttack;

        private void TryAttack()
        {
            if (Time.time - lastAttackTime < cooldown) return;
            lastAttackTime = Time.time;

            AttackDirection dir = AttackDirection.Forward;
            if (input.Vertical > verticalThreshold) dir = AttackDirection.Up;
            else if (input.Vertical < -verticalThreshold) dir = AttackDirection.Down;

            Attacked?.Invoke(dir);
            Hit(dir);
        }

        private void Hit(AttackDirection dir)
        {
            GetArea(dir, out Vector2 center, out Vector2 size);

            var filter = new ContactFilter2D { useTriggers = true };
            filter.NoFilter();
            int count = Physics2D.OverlapBox(center, size, 0f, filter, hits);

            damaged.Clear();
            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null || hits[i] == body) continue;
                var target = hits[i].GetComponentInParent<IDamageable>();
                if (target == null || !damaged.Add(target)) continue;
                target.TakeDamage(damage, transform.position);
            }

            if (dir == AttackDirection.Down && damaged.Count > 0 && motor != null && !motor.IsGrounded)
                motor.Bounce(downBounceForce);
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
        }
#endif
    }
}
