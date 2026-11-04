using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Hace daño al jugador cuando lo toca. El collider del enemigo debe ser trigger.
    /// </summary>
    public class EnemyContactDamage : MonoBehaviour
    {
        [SerializeField] private int damage = 1;

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) player.TakeDamage(damage, transform.position);
        }
    }
}
