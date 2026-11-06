using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EternalSleep.UI
{
    /// <summary>
    /// Botones del menú principal: Jugar, Opciones y Salir.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Posición en Build Settings del primer nivel (0 es este menú).")]
        [SerializeField] private int firstLevelBuildIndex = 1;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject mainFirstSelected;
        [SerializeField] private GameObject optionsFirstSelected;

        private void Start() => ShowMain();

        private void Update()
        {
            if (optionsPanel == null || !optionsPanel.activeSelf) return;

            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (back) CloseOptions();
        }

        public void Play()
        {
            GameSettings.Save();
            GameSession.NewGame();
            SceneManager.LoadScene(firstLevelBuildIndex);
        }

        public void OpenOptions()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            Select(optionsFirstSelected);
        }

        public void CloseOptions()
        {
            GameSettings.Save();
            ShowMain();
        }

        public void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowMain()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            Select(mainFirstSelected);
        }

        private static void Select(GameObject target)
        {
            if (EventSystem.current == null || target == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
