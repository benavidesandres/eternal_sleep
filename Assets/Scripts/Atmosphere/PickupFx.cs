using UnityEngine;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Efecto de recoger algo valioso (Lágrima de Luna): un destello que se apaga rápido y dos anillos
    /// que se expanden y se desvanecen. Todo con sprites generados por código, sin assets nuevos.
    /// </summary>
    public static class PickupFx
    {
        public static void Collect(Vector2 position, Color color)
        {
            Color pale = Color.Lerp(color, Color.white, 0.6f);
            Spawn(Fireflies.GlowSprite(), position, pale, 0.4f, 3.8f, 0.35f, 0f, 0.9f);
            Spawn(RingSprite(), position, pale, 0.3f, 2.4f, 0.6f, 0f, 1f);
            Spawn(RingSprite(), position, color, 0.2f, 3.6f, 0.85f, 0.12f, 0.8f);
        }

        /// <summary>
        /// Aviso de ataque: un anillo que se cierra hacia el punto del golpe, para leer el ataque a tiempo.
        /// </summary>
        public static void Warning(Vector2 position, Color color)
        {
            Spawn(RingSprite(), position, color, 3.4f, 0.5f, 0.35f, 0f, 0.95f);
        }

        private static void Spawn(Sprite sprite, Vector2 position, Color color, float from, float to, float time, float delay, float alpha)
        {
            var go = new GameObject("EfectoRecoger");
            go.transform.position = position;
            go.transform.localScale = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 35;
            sr.color = new Color(color.r, color.g, color.b, 0f);
            var p = go.AddComponent<Pulse>();
            p.sr = sr; p.color = color; p.from = from; p.to = to; p.time = time; p.delay = delay; p.alpha = alpha;
        }

        private class Pulse : MonoBehaviour
        {
            public SpriteRenderer sr;
            public Color color;
            public float from, to, time, delay, alpha;
            private float age;

            private void Update()
            {
                age += Time.unscaledDeltaTime;
                if (age < delay) return;
                float k = (age - delay) / time;
                if (k >= 1f) { Destroy(gameObject); return; }

                float grow = 1f - (1f - k) * (1f - k); // se expande rápido y frena
                transform.localScale = Vector3.one * Mathf.Lerp(from, to, grow);
                sr.color = new Color(color.r, color.g, color.b, alpha * (1f - k) * Mathf.Clamp01(k * 12f + 0.3f));
            }
        }

        private static Sprite ring;

        /// <summary>Anillo fino con borde suave (se crea una sola vez).</summary>
        private static Sprite RingSprite()
        {
            if (ring != null) return ring;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            ring = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return ring;
        }
    }
}
