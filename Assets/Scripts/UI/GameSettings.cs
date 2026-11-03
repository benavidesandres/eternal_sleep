using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Ajustes del jugador guardados en PlayerPrefs.
    /// Se aplican solos al iniciar el juego, en cualquier escena.
    /// </summary>
    public static class GameSettings
    {
        private const string MasterKey = "settings.masterVolume";
        private const string MusicKey = "settings.musicVolume";
        private const string SfxKey = "settings.sfxVolume";
        private const string FullscreenKey = "settings.fullscreen";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); Apply(); }
        }

        // Guardados para cuando el juego tenga música y efectos de sonido.
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
            set => PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
            set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); Apply(); }
        }

        public static void Save() => PlayerPrefs.Save();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
#if !UNITY_EDITOR
            Screen.fullScreen = Fullscreen; // en el editor no se puede cambiar
#endif
        }
    }
}
