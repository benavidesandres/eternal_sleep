using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Fondo animado del menú: estrellas que parpadean y capas de niebla que se desplazan.
    /// </summary>
    public class MenuBackground : MonoBehaviour
    {
        [System.Serializable]
        public class FogLayer
        {
            public UnityEngine.UI.RawImage image;
            public float speed = 0.01f;
        }

        [SerializeField] private UnityEngine.UI.Image[] stars;
        [SerializeField] private FogLayer[] fogLayers;
        [SerializeField] private Vector2 twinkleSpeed = new Vector2(0.4f, 1.6f);

        private float[] starSpeeds;
        private float[] starOffsets;

        private void Awake()
        {
            if (stars == null) return;
            starSpeeds = new float[stars.Length];
            starOffsets = new float[stars.Length];
            for (int i = 0; i < stars.Length; i++)
            {
                starSpeeds[i] = Random.Range(twinkleSpeed.x, twinkleSpeed.y);
                starOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;

            if (stars != null)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null) continue;
                    // Escalones de alfa para que el parpadeo se vea "pixelado".
                    float a = 0.5f + 0.5f * Mathf.Sin(t * starSpeeds[i] * Mathf.PI * 2f + starOffsets[i]);
                    a = Mathf.Round(a * 4f) / 4f;
                    var c = stars[i].color;
                    c.a = Mathf.Lerp(0.15f, 1f, a);
                    stars[i].color = c;
                }
            }

            if (fogLayers != null)
            {
                foreach (var layer in fogLayers)
                {
                    if (layer?.image == null) continue;
                    var r = layer.image.uvRect;
                    r.x = Mathf.Repeat(t * layer.speed, 1f);
                    layer.image.uvRect = r;
                }
            }
        }
    }
}
