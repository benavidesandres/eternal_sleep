using EternalSleep.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Viñeta: un borde oscuro que cierra un poco la pantalla y centra la mirada en Kael.
    /// Es una imagen de interfaz con un degradado radial generado por código, así que no depende de URP.
    /// </summary>
    public class ScreenVignette : MonoBehaviour
    {
        private static readonly Color Shade = new Color(0.03f, 0.02f, 0.07f);
        private static readonly Color Blood = new Color(0.55f, 0.04f, 0.1f);

        private Image image;
        private float baseAlpha = 0.5f;
        private float hurt; // 1 justo al recibir daño, baja a 0
        private PlayerHealth player;

        public static ScreenVignette Create(Transform parent, float strength)
        {
            var canvasGo = new GameObject("Vineta");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 700; // por encima del HUD, por debajo de los carteles y de la pausa

            var imgGo = new GameObject("Borde", typeof(RectTransform));
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var v = canvasGo.AddComponent<ScreenVignette>();
            v.image = imgGo.AddComponent<Image>();
            v.image.sprite = GradientSprite();
            v.image.raycastTarget = false;
            v.baseAlpha = strength;
            v.image.color = new Color(Shade.r, Shade.g, Shade.b, strength);
            return v;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            if (player != null) player.Damaged += OnDamaged;
        }

        private void OnDestroy()
        {
            if (player != null) player.Damaged -= OnDamaged;
        }

        private void OnDamaged(Vector2 from) => hurt = 1f;

        private void Update()
        {
            hurt = Mathf.Max(0f, hurt - Time.unscaledDeltaTime * 1.6f);
            // Con un solo corazón (de dos o más) la pantalla late despacio, como un pulso: dos golpes y una pausa.
            float beat = 0f;
            if (player != null && !player.IsDead && player.Max >= 2 && player.Current == 1)
            {
                float phase = Mathf.Repeat(Time.unscaledTime * 0.9f, 1f);
                beat = Mathf.Max(Thump(phase, 0.05f), Thump(phase, 0.22f) * 0.6f);
            }

            Color c = Color.Lerp(Shade, Blood, Mathf.Max(hurt, beat * 0.7f));
            c.a = Mathf.Clamp01(baseAlpha + hurt * 0.45f + beat * 0.22f);
            image.color = c;
        }

        /// <summary>Un golpe del latido: sube rápido en "at" y se apaga en unos 0,15 del ciclo.</summary>
        private static float Thump(float phase, float at)
        {
            float d = phase - at;
            return d < 0f ? 0f : Mathf.Exp(-d * 22f);
        }

        private static Sprite gradient;

        /// <summary>Transparente en el centro y opaco en las esquinas (se crea una sola vez).</summary>
        private static Sprite GradientSprite()
        {
            if (gradient != null) return gradient;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.41421f; // 0 en el centro, 1 en las esquinas
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1f, d));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            gradient = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return gradient;
        }
    }
}
