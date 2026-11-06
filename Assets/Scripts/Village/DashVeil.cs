using System.Collections.Generic;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// Velo de sombra: una pared sólida que solo se cruza con el Paso de Sombra.
    /// PlayerMotor lo vuelve atravesable mientras dura el impulso.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DashVeil : MonoBehaviour
    {
        private static readonly List<DashVeil> all = new List<DashVeil>();

        private Collider2D wall;

        private void Awake() => wall = GetComponent<Collider2D>();
        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        public static void SetPassable(Collider2D player, bool passable)
        {
            foreach (var veil in all) Physics2D.IgnoreCollision(player, veil.wall, passable);
        }

        /// <summary>¿El jugador sigue dentro de algún velo? (el impulso no termina a medio cruzar)</summary>
        public static bool Overlaps(Collider2D player)
        {
            foreach (var veil in all)
                if (veil.wall.bounds.Intersects(player.bounds)) return true;
            return false;
        }
    }
}
