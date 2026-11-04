using System;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Vida de un enemigo. Parpadea al recibir un golpe y desaparece al morir.
    /// Los scripts de movimiento escuchan Damaged para retroceder.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private float flashTime = 0.12f;
        [SerializeField] private float invulnerableTime = 0.2f;

        private SpriteRenderer sprite;
        private Color baseColor;
        private float flashUntil;
        private float invulnerableUntil;

        public int Current { get; private set; }
        public int Max => maxHealth;
        /// <summary>Le queda solo 1 de vida (y tenía más): momento de huir.</summary>
        public bool IsWeak => maxHealth > 1 && Current == 1;

        /// <summary>Se dispara con la posición desde donde vino el golpe.</summary>
        public event Action<Vector2> Damaged;
        public event Action Died;

        private void Awake()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite != null) baseColor = sprite.color;
            Current = maxHealth;
        }

        private void Update()
        {
            if (sprite == null) return;
            sprite.color = Time.time < flashUntil ? new Color(1f, 1f, 1f, 0.35f) : baseColor;
        }

        public void TakeDamage(int amount, Vector2 from)
        {
            if (Current <= 0 || Time.time < invulnerableUntil) return;

            Current -= amount;
            flashUntil = Time.time + flashTime;
            invulnerableUntil = Time.time + invulnerableTime;
            Damaged?.Invoke(from);

            if (Current <= 0)
            {
                Died?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
