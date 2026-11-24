using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalSleep
{
    /// <summary>
    /// Suma el tiempo de juego a PlayStats. Cuenta tiempo "de juego" (Time.deltaTime), así que no avanza
    /// en la pausa ni durante un golpe congelado, y no cuenta en el menú principal.
    /// Se crea solo al arrancar y sobrevive a los cambios de escena.
    /// </summary>
    public class PlayTimer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("PlayTimer");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayTimer>();
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().buildIndex == 0) return; // el menú principal es la escena 0
            PlayStats.AddTime(Time.deltaTime);
        }
    }
}
