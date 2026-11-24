using EternalSleep.UI;
using TMPro;
using UnityEngine;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Números de daño: al golpear a un enemigo sale su número con un pequeño "pop", sube y se desvanece.
    /// Con la fuente pixel del juego y borde oscuro para que se lea sobre cualquier fondo.
    /// </summary>
    public static class DamageNumbers
    {
        private static readonly Color Normal = new Color(0.9f, 0.86f, 1f);
        private static readonly Color Strong = new Color(1f, 0.82f, 0.4f);
        private static readonly Color Kill = new Color(1f, 0.55f, 0.6f);

        /// <param name="killed">El golpe lo mató: el número sale más grande y rojizo.</param>
        public static void Spawn(Vector2 position, int amount, bool killed = false)
        {
            if (amount <= 0) return;

            var go = new GameObject("NumeroDano");
            go.transform.position = position + new Vector2(Random.Range(-0.2f, 0.2f), 0f);

            var text = go.AddComponent<TextMeshPro>();
            var font = RuntimeUI.PixelFont();
            if (font != null) text.font = font;
            text.text = amount.ToString();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(4f, 2f);
            text.outlineWidth = 0.3f;
            text.outlineColor = new Color32(12, 8, 24, 255);

            bool strong = amount >= 3;
            text.color = killed ? Kill : strong ? Strong : Normal;
            text.fontSize = killed ? 5.2f : strong ? 4.6f : 3.6f;
            text.GetComponent<MeshRenderer>().sortingOrder = 40;

            go.AddComponent<Floater>();
        }

        private class Floater : MonoBehaviour
        {
            private const float Life = 0.75f;
            private TextMeshPro text;
            private Vector3 start;
            private Vector2 drift;
            private float age;

            private void Awake()
            {
                text = GetComponent<TextMeshPro>();
                start = transform.position;
                drift = new Vector2(Random.Range(-0.3f, 0.3f), 0f);
            }

            private void Update()
            {
                age += Time.unscaledDeltaTime; // sigue animándose durante el hitstop
                float k = age / Life;
                if (k >= 1f) { Destroy(gameObject); return; }

                float rise = 1f - (1f - k) * (1f - k); // sube rápido y frena
                transform.position = start + new Vector3(drift.x * rise, rise * 1.1f, 0f);

                // "Pop": aparece grande y se asienta.
                float pop = 1f + Mathf.Max(0f, 1f - k * 6f) * 0.6f;
                transform.localScale = Vector3.one * pop;

                var c = text.color;
                c.a = k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f;
                text.color = c;
            }
        }
    }
}
