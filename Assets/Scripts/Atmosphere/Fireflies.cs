using UnityEngine;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Luciérnagas: puntos de luz que flotan despacio por la zona visible de la cámara, titilan y
    /// se desvían como si respiraran. Si salen de la pantalla reaparecen por el otro lado.
    /// </summary>
    public class Fireflies : MonoBehaviour
    {
        private class Bug
        {
            public Transform t;
            public SpriteRenderer core;
            public SpriteRenderer halo;
            public Vector2 offset;   // posición relativa al centro de la cámara, de -1 a 1
            public float seed;
            public float speed;
            public float size;
        }

        private Bug[] bugs;
        private Color color;
        private Camera cam;

        public void Init(Color tint, int count)
        {
            color = tint;
            var glow = GlowSprite();
            bugs = new Bug[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Luciernaga");
                go.transform.SetParent(transform, false);
                var core = go.AddComponent<SpriteRenderer>();
                core.sprite = glow;
                core.sortingOrder = 22;

                var haloGo = new GameObject("Halo");
                haloGo.transform.SetParent(go.transform, false);
                var halo = haloGo.AddComponent<SpriteRenderer>();
                halo.sprite = glow;
                halo.sortingOrder = 21;
                haloGo.transform.localScale = Vector3.one * 4f;

                bugs[i] = new Bug
                {
                    t = go.transform, core = core, halo = halo,
                    seed = Random.Range(0f, 100f),
                    speed = Random.Range(0.25f, 0.6f),
                    size = Random.Range(0.07f, 0.13f),
                    offset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)),
                };
            }
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || bugs == null) return;

            float halfH = cam.orthographicSize + 1f;
            float halfW = halfH * cam.aspect + 1f;
            Vector3 c = cam.transform.position;
            float time = Time.time;
            Color bright = Color.Lerp(color, Color.white, 0.5f);

            foreach (var b in bugs)
            {
                // Deriva lenta y sinuosa (ruido de Perlin); al salir por un lado entra por el otro.
                b.offset.x += (Mathf.PerlinNoise(b.seed, time * 0.3f) - 0.5f) * b.speed * Time.deltaTime / halfW * 2f;
                b.offset.y += (Mathf.PerlinNoise(b.seed + 50f, time * 0.3f) - 0.45f) * b.speed * Time.deltaTime / halfH * 2f;
                b.offset.x = Wrap(b.offset.x);
                b.offset.y = Wrap(b.offset.y);

                b.t.position = new Vector3(c.x + b.offset.x * halfW, c.y + b.offset.y * halfH, 0f);
                b.t.localScale = Vector3.one * b.size;

                float blink = 0.35f + 0.65f * Mathf.Pow(Mathf.Sin(time * (1.2f + b.speed) + b.seed) * 0.5f + 0.5f, 2f);
                // Se desvanecen cerca del borde para no "aparecer" de golpe.
                float edge = Mathf.Clamp01((1f - Mathf.Abs(b.offset.x)) * 6f) * Mathf.Clamp01((1f - Mathf.Abs(b.offset.y)) * 6f);
                b.core.color = new Color(bright.r, bright.g, bright.b, blink * edge);
                b.halo.color = new Color(color.r, color.g, color.b, blink * edge * 0.22f);
            }
        }

        private static float Wrap(float v) => v > 1f ? v - 2f : v < -1f ? v + 2f : v;

        private static Sprite glow;

        /// <summary>Círculo suave con borde que se desvanece (se crea una sola vez).</summary>
        internal static Sprite GlowSprite()
        {
            if (glow != null) return glow;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return glow;
        }
    }
}
