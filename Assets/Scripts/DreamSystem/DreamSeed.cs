using UnityEngine;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Semilla propia de cada jugador: se crea la primera vez que se abre el juego y se guarda en PlayerPrefs.
    /// Se mezcla con el número de partida y con el aldeano, así que cada persona, cada partida
    /// y cada aldeano tienen su propio sueño.
    /// </summary>
    public static class DreamSeed
    {
        private const string PlayerKey = "dreams.playerSeed";
        private const string GamesKey = "dreams.gamesStarted";

        /// <summary>Semilla del jugador (siempre la misma en este ordenador).</summary>
        public static int Player
        {
            get
            {
                if (!PlayerPrefs.HasKey(PlayerKey))
                {
                    PlayerPrefs.SetInt(PlayerKey, (int)(System.DateTime.Now.Ticks ^ System.Guid.NewGuid().GetHashCode()));
                    PlayerPrefs.Save();
                }
                return PlayerPrefs.GetInt(PlayerKey);
            }
        }

        /// <summary>Cuántas partidas se han empezado (cambia los sueños de una partida a otra).</summary>
        public static int Game => PlayerPrefs.GetInt(GamesKey, 0);

        /// <summary>Se llama al empezar una partida nueva.</summary>
        public static void NextGame()
        {
            PlayerPrefs.SetInt(GamesKey, Game + 1);
            PlayerPrefs.Save();
        }

        /// <summary>Semilla de un sueño concreto. attempt sube si el nivel salió sin solución y hay que repetirlo.</summary>
        public static int ForDream(string dreamerId, int attempt = 0)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + Player;
                h = h * 31 + Game;
                h = h * 31 + Stable(dreamerId);
                h = h * 31 + attempt;
                return Mix(h);
            }
        }

        /// <summary>Hash de texto que no cambia entre ejecuciones (string.GetHashCode sí puede cambiar).</summary>
        public static int Stable(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                if (s != null)
                    foreach (char c in s) h = (h ^ c) * 16777619;
                return h;
            }
        }

        private static int Mix(int x)
        {
            unchecked
            {
                uint v = (uint)x;
                v ^= v >> 16; v *= 0x7feb352d;
                v ^= v >> 15; v *= 0x846ca68b;
                v ^= v >> 16;
                return (int)v;
            }
        }
    }
}
