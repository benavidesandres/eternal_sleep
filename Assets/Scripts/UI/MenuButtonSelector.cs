using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EternalSleep.UI
{
    /// <summary>
    /// Al seleccionar el botón o pasar el mouse muestra "> Texto <" y lo mueve un poco.
    /// </summary>
    public class MenuButtonSelector : MonoBehaviour,
        ISelectHandler, IDeselectHandler, IPointerEnterHandler
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

        public void OnSelect(BaseEventData eventData) => SetHighlighted(true);

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
