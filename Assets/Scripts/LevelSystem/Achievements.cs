using System;
using System.Collections.Generic;

namespace EternalSleep
{
    /// <summary>
    /// Logros de la partida. Cada uno se desbloquea una sola vez y se guarda con la partida.
    /// La lista está aquí; las condiciones se comprueban en Evaluate (ver AchievementWatcher).
    /// </summary>
    public static class Achievements
    {
        public struct Def
        {
            public string id;
            public string nombre;
            public string descripcion;
        }

        public static readonly Def[] All =
        {
            new Def { id = "primera_luz", nombre = "Primera luz", descripcion = "Recoge una Lágrima de Luna." },
            new Def { id = "todas_lagrimas", nombre = "Coleccionista de lágrimas", descripcion = "Reúne todas las Lágrimas de Luna del pueblo." },
            new Def { id = "secreto", nombre = "Ojo de sombra", descripcion = "Descubre un secreto." },
            new Def { id = "primer_despertar", nombre = "Despertar", descripcion = "Despierta a un aldeano." },
            new Def { id = "pueblo_despierto", nombre = "Pueblo despierto", descripcion = "Despierta a todos los aldeanos." },
            new Def { id = "primer_jefe", nombre = "Vigilia rota", descripcion = "Vence a un jefe." },
            new Def { id = "jefe_limpio", nombre = "Sin un rasguño", descripcion = "Vence a un jefe sin recibir daño." },
            new Def { id = "cien_golpes", nombre = "Cien golpes", descripcion = "Golpea a los enemigos cien veces." },
            new Def { id = "primera_caida", nombre = "Caer y levantarse", descripcion = "Cae por primera vez." },
            new Def { id = "kael_completo", nombre = "Kael completo", descripcion = "Consigue todas las mejoras." },
            new Def { id = "sonador", nombre = "Soñador", descripcion = "Completa cinco sueños." },
            new Def { id = "insomne", nombre = "Insomne", descripcion = "Juega durante media hora." },
        };

        private static readonly HashSet<string> unlocked = new HashSet<string>();

        /// <summary>Se dispara la primera vez que se desbloquea un logro.</summary>
        public static event Action<Def> Unlocked;

        public static int UnlockedCount => unlocked.Count;
        public static bool IsUnlocked(string id) => unlocked.Contains(id);

        public static void Unlock(string id)
        {
            if (unlocked.Contains(id)) return;
            foreach (var def in All)
            {
                if (def.id != id) continue;
                unlocked.Add(id);
                SaveSystem.SaveOutsideDream();
                Unlocked?.Invoke(def);
                return;
            }
        }

        public static void WriteTo(SaveData data) => data.logros.AddRange(unlocked);

        public static void ReadFrom(SaveData data)
        {
            foreach (var id in data.logros) unlocked.Add(id);
        }

        /// <summary>Vacía los logros (partida nueva). Conserva a quien escucha el evento.</summary>
        public static void Reset() => unlocked.Clear();
    }
}
