using EternalSleep.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EternalSleep.UI
{
    /// <summary>
    /// Al seleccionar el botón o pasar el mouse muestra "> Texto <" y lo mueve un poco.
    /// </summary>
    public class MenuButtonSelector : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float nudge = 2f;

        private string baseText;
        private Vector2 basePosition;

        private void Awake()
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>();
            if (label == null) return;
            baseText = label.text;
            basePosition = label.rectTransform.anchoredPosition;
        }

        private void OnDisable() => SetHighlighted(false);

        /// <summary>Cambia el texto del botón (para botones creados en el juego).</summary>
        public void SetText(string text)
        {
            if (label == null) return;
            baseText = text;
            label.text = text;
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetHighlighted(true);
            if (Time.timeSinceLevelLoad > 0.1f) AudioManager.Play("menu_mover"); // no al seleccionar el primero al abrir
        }

        public void OnSubmit(BaseEventData eventData) => AudioManager.Play("menu_aceptar");

        public void OnPointerClick(PointerEventData eventData) => AudioManager.Play("menu_aceptar");

        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        // Con el mouse, pasar por encima selecciona el botón (así teclado y mouse no se pisan).
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void SetHighlighted(bool on)
        {
            if (label == null) return;
            label.text = on ? $"> {baseText} <" : baseText;
            label.rectTransform.anchoredPosition = basePosition + (on ? new Vector2(0, nudge) : Vector2.zero);
        }
    }
}
