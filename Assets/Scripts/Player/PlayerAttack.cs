using System;
using UnityEngine;

namespace EternalSleep.Player
{
    public enum AttackDirection { Forward, Up, Down }

    /// <summary>
    /// Decide cuándo se ataca y en qué dirección (según el eje vertical pulsado).
    /// No sabe nada de animaciones; quien quiera reaccionar se suscribe a Attacked.
    /// El daño real (hitbox) se añadirá aquí más adelante.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField] private float cooldown = 0.45f;
        [Tooltip("Cuánto hay que empujar arriba/abajo para contar como ataque direccional.")]
        [SerializeField, Range(0.1f, 1f)] private float verticalThreshold = 0.5f;

        private PlayerInputReader input;
        private float lastAttackTime = float.NegativeInfinity;

        public event Action<AttackDirection> Attacked;

        private void Awake() => input = GetComponent<PlayerInputReader>();
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
        }
    }
}
