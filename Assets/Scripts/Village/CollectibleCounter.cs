using TMPro;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// HUD: icono de Lágrima de Luna con "recogidas / total". Late al recoger una.
    /// </summary>
    public class CollectibleCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform icon;

        private float pulse;
        private Vector2 iconBase;
        private RectTransform labelRect;

        private void Start()
        {
            // Cuenta todas las lágrimas de la escena (también las ya recogidas, que están desactivadas).
            if (icon != null) iconBase = icon.anchoredPosition;
            if (label != null) labelRect = label.rectTransform;
            CollectibleTracker.Total = FindObjectsByType<Collectible>(FindObjectsInactive.Include).Length;
            CollectibleTracker.Changed += OnChanged;
            Refresh();
        }

        private void OnDestroy() => CollectibleTracker.Changed -= OnChanged;

        private void OnChanged()
        {
            pulse = 1f;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            label.text = $"{CollectibleTracker.Count}/{CollectibleTracker.Total}";
            label.color = RestColor;
        }

        private static Color RestColor => CollectibleTracker.HasAll ? new Color(1f, 0.89f, 0.54f) : new Color(0.85f, 0.8f, 1f);

        private void Update()
        {
            if (pulse <= 0f) return;
            pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime * 2f);

            // Salto: el icono sube y se agranda, el número se infla y destella en blanco, y todo vuelve a su sitio.
            float hop = Mathf.Sin(pulse * Mathf.PI);
            if (icon != null)
            {
                icon.localScale = Vector3.one * (1f + hop * 0.7f);
                icon.anchoredPosition = iconBase + Vector2.up * (hop * 7f);
            }
            if (labelRect != null) labelRect.localScale = Vector3.one * (1f + hop * 0.45f);
            if (label != null) label.color = Color.Lerp(RestColor, Color.white, pulse);
        }
    }
}
