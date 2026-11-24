using UnityEngine;

namespace EternalSleep
{
    /// <summary>
    /// Estadísticas de la partida: muertes, golpes dados y recibidos, jefes vencidos y tiempo jugado.
    /// Se guardan con la partida (SaveData) y se muestran en la pausa y en los créditos.
    /// </summary>
    public static class PlayStats
    {
        public static int Deaths { get; private set; }
        public static int HitsDealt { get; private set; }
        public static int HitsTaken { get; private set; }
        public static int BossesDefeated { get; private set; }
        /// <summary>Segundos de juego (sin contar pausas ni el menú).</summary>
        public static float Seconds { get; private set; }

        public static void AddDeath() => Deaths++;
        public static void AddHitDealt() => HitsDealt++;
        public static void AddHitTaken() => HitsTaken++;
        public static void AddBossDefeated() => BossesDefeated++;
        public static void AddTime(float seconds) => Seconds += seconds;

        /// <summary>"2 h 05 min" o "14 min" o "35 s".</summary>
        public static string FormatTime()
        {
            int total = Mathf.FloorToInt(Seconds);
            int h = total / 3600, m = total % 3600 / 60;
            if (h > 0) return $"{h} h {m:00} min";
            if (m > 0) return $"{m} min";
            return $"{total} s";
        }

        public static void WriteTo(SaveData data)
        {
            data.estMuertes = Deaths;
            data.estGolpes = HitsDealt;
            data.estRecibidos = HitsTaken;
            data.estJefes = BossesDefeated;
            data.estSegundos = Seconds;
        }

        public static void ReadFrom(SaveData data)
        {
            Deaths = data.estMuertes;
            HitsDealt = data.estGolpes;
            HitsTaken = data.estRecibidos;
            BossesDefeated = data.estJefes;
            Seconds = data.estSegundos;
        }

        public static void Reset()
        {
            Deaths = HitsDealt = HitsTaken = BossesDefeated = 0;
            Seconds = 0f;
        }
    }
}
