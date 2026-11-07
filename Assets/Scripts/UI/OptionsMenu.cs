using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Conecta los controles del panel de opciones con GameSettings.
    /// </summary>
    public class OptionsMenu : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Slider masterSlider;
        [SerializeField] private UnityEngine.UI.Slider musicSlider;
        [SerializeField] private UnityEngine.UI.Slider sfxSlider;
        [SerializeField] private UnityEngine.UI.Toggle fullscreenToggle;
        [SerializeField] private UnityEngine.UI.Toggle aiDreamsToggle;

        private void Awake()
        {
            if (masterSlider != null)
            {
                masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
                masterSlider.onValueChanged.AddListener(v => GameSettings.MasterVolume = v);
            }
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
                musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
                sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
                fullscreenToggle.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);
            }
            if (aiDreamsToggle != null)
            {
                aiDreamsToggle.SetIsOnWithoutNotify(GameSettings.AIDreams);
                aiDreamsToggle.onValueChanged.AddListener(v => GameSettings.AIDreams = v);
            }
        }
    }
}
