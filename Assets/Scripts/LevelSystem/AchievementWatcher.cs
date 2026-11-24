using System;
using EternalSleep.Dream;
using EternalSleep.Player;
using EternalSleep.Village;
using UnityEngine;

namespace EternalSleep
{
    /// <summary>
    /// Revisa cada medio segundo las condiciones de los logros y desbloquea los que ya se cumplen.
    /// Así los logros no dependen de que cada sistema avise: basta con leer lo que el juego ya cuenta.
    /// Se crea solo al arrancar y sobrevive a los cambios de escena.
    /// </summary>
    public class AchievementWatcher : MonoBehaviour
    {
        private float next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("AchievementWatcher");
            DontDestroyOnLoad(go);
            go.AddComponent<AchievementWatcher>();
        }

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.5f;
            Evaluate();
        }

        private static void Evaluate()
        {
            if (CollectibleTracker.Count >= 1) Achievements.Unlock("primera_luz");
            if (CollectibleTracker.HasAll) Achievements.Unlock("todas_lagrimas");
            if (CollectibleTracker.SecretsFound >= 1) Achievements.Unlock("secreto");
            if (DreamWorldManager.AwakenedCount >= 1) Achievements.Unlock("primer_despertar");
            if (DreamWorldManager.EndingSeen) Achievements.Unlock("pueblo_despierto");
            if (PlayStats.BossesDefeated >= 1) Achievements.Unlock("primer_jefe");
            if (PlayStats.HitsDealt >= 100) Achievements.Unlock("cien_golpes");
            if (PlayStats.Deaths >= 1) Achievements.Unlock("primera_caida");
            if (DreamDiary.Entries.Count >= 5) Achievements.Unlock("sonador");
            if (PlayStats.Seconds >= 1800f) Achievements.Unlock("insomne");

            bool all = true;
            foreach (Upgrade u in Enum.GetValues(typeof(Upgrade)))
                if (!PlayerUpgrades.Has(u)) { all = false; break; }
            if (all) Achievements.Unlock("kael_completo");
        }
    }
}
