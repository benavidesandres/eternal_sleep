using System;
using System.Collections.Generic;

namespace EternalSleep.Village
{
    /// <summary>
    /// Recuerda qué coleccionables (Lágrimas de Luna) y secretos se encontraron durante la partida.
    /// </summary>
    public static class CollectibleTracker
    {
        private static readonly HashSet<string> collected = new HashSet<string>();
        private static readonly HashSet<string> secrets = new HashSet<string>();

        /// <summary>Se dispara al recoger algo nuevo.</summary>
        public static event Action Changed;

        public static int Count => collected.Count;
        public static int SecretsFound => secrets.Count;
        /// <summary>Cuántos hay en total en el pueblo (lo calcula el contador del HUD al cargar la escena).</summary>
        public static int Total { get; set; }
        public static bool HasAll => Total > 0 && Count >= Total;

        public static bool IsCollected(string id) => collected.Contains(id);

        public static void Collect(string id)
        {
            if (collected.Add(id)) Changed?.Invoke();
        }

        /// <summary>Devuelve true la primera vez que se descubre ese secreto.</summary>
        public static bool DiscoverSecret(string id) => secrets.Add(id);
    }
}
