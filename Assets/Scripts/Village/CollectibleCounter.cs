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

        private void Start()
        {
            // Cuenta todas las lágrimas de la escena (también las ya recogidas, que están desactivadas).
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
            label.color = CollectibleTracker.HasAll ? new Color(1f, 0.89f, 0.54f) : new Color(0.85f, 0.8f, 1f);
        }

        private void Update()
        {
            if (pulse <= 0f || icon == null) return;
            pulse = Mathf.Max(0f, pulse - Time.unscaledDeltaTime * 3f);
            icon.localScale = Vector3.one * (1f + Mathf.Sin(pulse * Mathf.PI) * 0.5f);
        }
    }
}
