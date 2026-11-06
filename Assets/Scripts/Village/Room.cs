using System.Collections.Generic;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// Una "habitación" para la cámara, como en Hollow Knight: mientras el jugador está dentro de su área,
    /// la cámara no sale de ella. Las casas tienen su interior como una habitación aparte.
    /// Si en la escena no hay ninguna, la cámara usa los límites del nivel.
    /// </summary>
    public class Room : MonoBehaviour
    {
        [SerializeField] private Rect area = new Rect(0, 0, 20, 11);

        private static readonly List<Room> all = new List<Room>();

        public Rect Area => area;
        public static bool Any => all.Count > 0;

        private void OnEnable() => all.Add(this);
        private void OnDisable() => all.Remove(this);

        /// <summary>La habitación que contiene ese punto (la más pequeña si hay varias), o null.</summary>
        public static Room At(Vector2 point)
        {
            Room best = null;
            foreach (var r in all)
            {
                if (!r.area.Contains(point)) continue;
                if (best == null || r.area.width * r.area.height < best.area.width * best.area.height) best = r;
            }
            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.4f, 0.5f);
            Gizmos.DrawWireCube(area.center, area.size);
        }
#endif
    }
}
