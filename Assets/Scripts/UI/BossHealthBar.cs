using EternalSleep.Enemies;
using TMPro;
using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Barra de vida del jefe, abajo en la pantalla, con su nombre.
    /// La parte clara baja al instante con cada golpe y la oscura la sigue despacio,
    /// para que se vea cuánto quitó el golpe.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private RectTransform fill;
        [SerializeField] private RectTransform trail;
        [SerializeField] private float fadeSpeed = 3f;
        [SerializeField] private float trailDelay = 0.4f;
        [SerializeField] private float trailSpeed = 0.6f;

        public static BossHealthBar Instance { get; private set; }

        private EnemyHealth health;
        private bool visible;
        private float shown;      // fracción que muestra la barra clara
        private float trailShown; // fracción de la barra oscura
        private float trailWaitUntil;

        private void Awake()
        {
            Instance = this;
            if (root != null) root.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string bossName, EnemyHealth boss)
        {
            health = boss;
            visible = true;
            if (nameText != null) nameText.text = bossName;
            shown = trailShown = 1f;
            SetWidth(fill, 1f);
            SetWidth(trail, 1f);
        }

        public void Hide() => visible = false;

        private void Update()
        {
            if (root != null) root.alpha = Mathf.MoveTowards(root.alpha, visible ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            if (health == null) return;

            float target = Mathf.Clamp01(health.Current / (float)Mathf.Max(1, health.Max));
            if (target < shown) trailWaitUntil = Time.unscaledTime + trailDelay;
            shown = target;
            if (Time.unscaledTime >= trailWaitUntil)
                trailShown = Mathf.MoveTowards(trailShown, shown, trailSpeed * Time.unscaledDeltaTime);

            SetWidth(fill, shown);
            SetWidth(trail, trailShown);
        }

        private static void SetWidth(RectTransform bar, float fraction)
        {
            if (bar == null) return;
            var max = bar.anchorMax;
            max.x = fraction;
            bar.anchorMax = max;
        }
    }
}
