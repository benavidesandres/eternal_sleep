using UnityEngine;

namespace EternalSleep
{
    /// <summary>
    /// Cualquier cosa que el ataque del jugador pueda golpear (enemigos, cajas, etc.).
    /// </summary>
    public interface IDamageable
    {
        /// <param name="amount">Cuánto daño hace el golpe.</param>
        /// <param name="from">Desde dónde viene el golpe (para empujar en sentido contrario).</param>
        void TakeDamage(int amount, Vector2 from);
    }
}
