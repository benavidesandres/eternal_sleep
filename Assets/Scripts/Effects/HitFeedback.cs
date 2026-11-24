using EternalSleep.CameraSystem;
using UnityEngine;

namespace EternalSleep.Effects
{
    /// <summary>
    /// "Jugo" de los golpes, usable desde cualquier script:
    /// - HitStop: congela el juego unas centésimas para que el golpe se sienta.
    /// - Shake: sacude la cámara.
    /// - Burst: lanza fragmentos de luz/sombra que caen y se desvanecen.
    /// </summary>
    public static class HitFeedback
    {
        private static Sprite pixel;

        public static void HitStop(float seconds) => Runner.Get().Stop(seconds);

        /// <summary>Cámara lenta: el juego va a "scale" de su velocidad durante "seconds" segundos reales.</summary>
        public static void SlowMo(float scale, float seconds) => Runner.Get().Slow(scale, seconds);

        public static void Shake(float amount, float seconds)
        {
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (cam != null) cam.Shake(amount, seconds);
        }

        /// <param name="count">Cuántos fragmentos.</param>
        /// <param name="speed">Velocidad inicial.</param>
        /// <param name="size">Tamaño de cada fragmento en unidades.</param>
        /// <param name="gravity">Negativo = suben (humo de sueño).</param>
        public static void Burst(Vector2 position, Color color, int count, float speed = 4f, float size = 0.12f, float gravity = 6f)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Random.insideUnitCircle.normalized * (speed * Random.Range(0.4f, 1f)) + Vector2.up * speed * 0.3f;
                SpawnShard(position + Random.insideUnitCircle * 0.2f, color, size, velocity, gravity, Random.Range(0.35f, 0.7f));
            }
        }

        /// <summary>
        /// Polvo del suelo: bocanadas bajas que salen hacia los lados y se elevan un poco.
        /// direction 0 = para los dos lados; 1 o -1 = hacia ese lado.
        /// </summary>
        public static void Dust(Vector2 feet, int direction, int count, Color color, float speed = 2.5f, float size = 0.09f)
        {
            for (int i = 0; i < count; i++)
            {
                float side = direction != 0 ? direction : (Random.value < 0.5f ? -1f : 1f);
                var velocity = new Vector2(side * speed * Random.Range(0.25f, 1f), Random.Range(0.3f, 1.1f));
                SpawnShard(feet + new Vector2(Random.Range(-0.15f, 0.15f), 0.05f), color, size, velocity, -0.5f, Random.Range(0.25f, 0.5f));
            }
        }

        private static void SpawnShard(Vector2 position, Color color, float size, Vector2 velocity, float gravity, float life)
        {
            if (pixel == null)
            {
                var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            var go = new GameObject("Fragmento");
            go.transform.position = position;
            float s = size * Random.Range(0.6f, 1.3f);
            go.transform.localScale = new Vector3(s, s, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
            sr.sortingOrder = 30;

            var shard = go.AddComponent<Shard>();
            shard.velocity = velocity;
            shard.gravity = gravity;
            shard.life = life;
        }

        private class Shard : MonoBehaviour
        {
            public Vector2 velocity;
            public float gravity;
            public float life;

            private float age;
            private SpriteRenderer sr;
            private Vector3 baseScale;

            private void Awake()
            {
                sr = GetComponent<SpriteRenderer>();
                baseScale = transform.localScale;
            }

            private void Update()
            {
                age += Time.deltaTime;
                velocity.y -= gravity * Time.deltaTime;
                velocity *= 1f - 2f * Time.deltaTime; // rozamiento del aire
                transform.position += (Vector3)(velocity * Time.deltaTime);

                float t = age / life;
                var c = sr.color;
                c.a = 1f - t;
                sr.color = c;
                transform.localScale = baseScale * (1f - t * 0.5f);
                if (t >= 1f) Destroy(gameObject);
            }
        }

        /// <summary>Objeto invisible que controla el tiempo congelado.</summary>
        private class Runner : MonoBehaviour
        {
            private static Runner instance;
            private float stopUntil;
            private bool stopped;
            private float slowUntil;
            private float slowScale = 1f;
            private bool slowing;

            public static Runner Get()
            {
                if (instance == null)
                {
                    var go = new GameObject("HitFeedback");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<Runner>();
                }
                return instance;
            }

            public void Stop(float seconds)
            {
                if (Time.timeScale == 0f && !stopped) return; // el juego está en pausa por otra razón
                stopUntil = Mathf.Max(stopUntil, Time.unscaledTime + seconds);
                stopped = true;
                Time.timeScale = 0f;
            }

            public void Slow(float scale, float seconds)
            {
                if (stopped || Time.timeScale == 0f) return; // en pausa o congelado: no se mezcla
                slowScale = Mathf.Clamp(scale, 0.05f, 1f);
                slowUntil = Time.unscaledTime + seconds;
                slowing = true;
                Time.timeScale = slowScale;
            }

            private void Update()
            {
                if (stopped && Time.unscaledTime >= stopUntil)
                {
                    stopped = false;
                    Time.timeScale = slowing ? slowScale : 1f; // si había cámara lenta, vuelve a ella
                }
                if (slowing && !stopped && Time.unscaledTime >= slowUntil)
                {
                    slowing = false;
                    if (Mathf.Approximately(Time.timeScale, slowScale)) Time.timeScale = 1f; // si el juego está en pausa, no lo toca
                }
            }
        }
    }
}
