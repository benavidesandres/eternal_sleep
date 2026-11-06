using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EternalSleep.Effects
{
    /// <summary>
    /// Fundido a negro de pantalla completa (puertas, cambios de habitación).
    /// Se crea solo y funciona en tiempo real (aunque el juego esté en pausa).
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        private static ScreenFade instance;
        private Image image;

        public static bool Busy { get; private set; }

        /// <summary>Si se paró el juego a mitad de un fundido, Busy quedaría en true y las puertas dejarían de funcionar.</summary>
        public static void ResetState() => Busy = false;

        private static ScreenFade Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("ScreenFade");
            DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var img = new GameObject("Negro", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            img.rectTransform.anchorMin = Vector2.zero;
            img.rectTransform.anchorMax = Vector2.one;
            img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
            img.color = new Color(0.02f, 0.015f, 0.04f, 0f);
            img.raycastTarget = false;
            instance = go.AddComponent<ScreenFade>();
            instance.image = img;
            return instance;
        }

        /// <summary>Oscurece, ejecuta la acción (por ejemplo, mover al jugador) y vuelve a aclarar.</summary>
        public static void Transition(System.Action middle, float seconds = 0.35f)
        {
            if (Busy) return;
            var f = Get();
            f.StartCoroutine(f.Run(middle, seconds));
        }

        private IEnumerator Run(System.Action middle, float seconds)
        {
            Busy = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds) { SetAlpha(t); yield return null; }
            SetAlpha(1f);
            middle?.Invoke();
            yield return new WaitForSecondsRealtime(0.12f);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / seconds) { SetAlpha(t); yield return null; }
            SetAlpha(0f);
            Busy = false;
        }

        private void SetAlpha(float a)
        {
            var c = image.color;
            c.a = a;
            image.color = c;
        }
    }
}
