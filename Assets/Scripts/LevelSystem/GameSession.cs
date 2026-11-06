using EternalSleep.Dream;
using EternalSleep.Effects;
using EternalSleep.Player;
using EternalSleep.UI;
using EternalSleep.Village;
using UnityEngine;

namespace EternalSleep
{
    /// <summary>
    /// Deja a cero todo lo que el juego recuerda en variables static.
    /// El proyecto tiene la recarga de dominio desactivada (Enter Play Mode Options), así que esas variables
    /// sobreviven de un Play al siguiente: sin esto, en el segundo Play los aldeanos seguían "despiertos"
    /// y la hora guardada del último diálogo quedaba en el futuro, con lo que nadie respondía.
    /// </summary>
    public static class GameSession
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnPlayStart()
        {
            NewGame();
            ScreenFade.ResetState();
        }

        /// <summary>Partida nueva: se llama al empezar Play y al pulsar Jugar en el menú.</summary>
        public static void NewGame()
        {
            DreamWorldManager.ResetProgress();
            CollectibleTracker.Reset();
            TalkingNPC.ResetTalks();
            DialogueBox.ResetTimers();
            PlayerUpgrades.Reset();
        }
    }
}
