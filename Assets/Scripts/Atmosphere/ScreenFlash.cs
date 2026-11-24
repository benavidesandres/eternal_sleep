using UnityEngine;
using UnityEngine.UI;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Destello de pantalla completa que se apaga solo (al vencer a un jefe, por ejemplo).
    /// Se crea la primera vez que se usa y funciona en tiempo real, aunque el juego vaya en cámara lenta.
    /// </summary>
    public class ScreenFlash : MonoBehaviour
    {
        private static ScreenFlash instance;
        private Image image;
        private float alpha;
        private float fadePerSecond;

        /// <param name="peak">Opacidad al empezar (0 a 1).</param>
        /// <param name="seconds">Lo que tarda en apagarse.</param>
        public static void Play(Color color, float peak, float seconds)
        {
            var f = Get();
            f.alpha = Mathf.Clamp01(peak);
            f.image.color = new Color(color.r, color.g, color.b, f.alpha);
            f.fadePerSecond = f.alpha / Mathf.Max(0.05f, seconds);
        }

        private static ScreenFlash Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("DestelloDePantalla");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 850; // por encima de los carteles, por debajo del fundido a negro

            var imgGo = new GameObject("Color", typeof(RectTransform));
            imgGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = imgGo.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);

            instance = go.AddComponent<ScreenFlash>();
            instance.image = img;
            return instance;
        }

        private void Update()
        {
            if (alpha <= 0f) return;
            alpha = Mathf.Max(0f, alpha - fadePerSecond * Time.unscaledDeltaTime);
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
}
