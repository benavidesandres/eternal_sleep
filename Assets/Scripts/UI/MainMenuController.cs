using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EternalSleep.UI
{
    /// <summary>
    /// Botones del menú principal: Continuar (si hay partida guardada), Jugar, Opciones y Salir.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Tooltip("Posición en Build Settings del primer nivel (0 es este menú).")]
        [SerializeField] private int firstLevelBuildIndex = 1;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject mainFirstSelected;
        [SerializeField] private GameObject optionsFirstSelected;

        private void Start()
        {
            if (SaveSystem.HasSave) AddContinueButton();
            ShowMain();
        }

        /// <summary>
        /// Con una partida guardada, añade "Continuar" encima de Jugar (que pasa a llamarse "Nueva partida").
        /// Se crea en el juego copiando el botón Jugar, así no hace falta volver a generar la escena del menú.
        /// </summary>
        private void AddContinueButton()
        {
            var play = mainFirstSelected != null ? mainFirstSelected.GetComponent<Button>() : null;
            if (play == null) return;

            var copy = Instantiate(play.gameObject, play.transform.parent);
            copy.name = "ContinuarButton";
            copy.transform.SetSiblingIndex(play.transform.GetSiblingIndex());
            var button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent(); // sin la llamada a Play() copiada
            button.onClick.AddListener(Continue);

            SetLabel(copy, "Continuar");
            SetLabel(play.gameObject, "Nueva partida");

            // El grupo de botones tiene alto fijo: se agranda para que quepa uno más.
            var group = play.transform.parent as RectTransform;
            var layout = group != null ? group.GetComponent<VerticalLayoutGroup>() : null;
            if (layout != null)
                group.sizeDelta += new Vector2(0f, ((RectTransform)play.transform).sizeDelta.y + layout.spacing);

            mainFirstSelected = copy;
        }

        private static void SetLabel(GameObject button, string text)
        {
            var selector = button.GetComponent<MenuButtonSelector>();
            if (selector != null) selector.SetText(text);
            else
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = text;
            }
        }

        public void Continue()
        {
            GameSettings.Save();
            if (!GameSession.Continue()) GameSession.NewGame();
            SceneManager.LoadScene(firstLevelBuildIndex);
        }

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
