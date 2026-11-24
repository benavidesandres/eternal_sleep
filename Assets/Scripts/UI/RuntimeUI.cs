using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalSleep.UI
{
    /// <summary>
    /// Piezas para las pantallas que se crean por código (pausa, final): un lienzo a 640x360 como el resto
    /// de la interfaz, rectángulos que llenan la pantalla y textos con la fuente pixel del juego.
    /// </summary>
    public static class RuntimeUI
    {
        public static Canvas Canvas(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent, false);
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var img = Stretch(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Texto centrado en (0, y) respecto al centro de la pantalla.</summary>
        public static TMP_Text Text(string name, Transform parent, float y, float size, Color color, string text)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(620f, size + 10f);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            var font = PixelFont();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        /// <summary>La fuente pixel del juego: se toma de cualquier texto de la escena (no está en Resources).</summary>
        public static TMP_FontAsset PixelFont()
        {
            if (font != null) return font;
            var any = Object.FindAnyObjectByType<TMP_Text>(FindObjectsInactive.Include);
            font = any != null ? any.font : TMP_Settings.defaultFontAsset;
            return font;
        }

        private static TMP_FontAsset font;
    }
}
