using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Level
{
    /// <summary>
    /// Peligro (pinchos): al tocarlo, el jugador pierde vida y vuelve al último suelo seguro.
    /// El collider debe ser trigger.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [SerializeField] private int damage = 1;

        private void OnTriggerEnter2D(Collider2D other) => Touch(other);
        private void OnTriggerStay2D(Collider2D other) => Touch(other);

        private void Touch(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) player.HazardHit(damage);
        }
    }
}
