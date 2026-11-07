using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace EternalSleep.UI
{
    /// <summary>
    /// Fondo animado del menú: video en bucle de fondo, estrellas que parpadean y capas de niebla que se desplazan.
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

        [Tooltip("Archivo dentro de Assets/StreamingAssets que se reproduce en bucle detrás del menú. Vacío = sin video.")]
        [SerializeField] private string loopVideoFile = "menu_loop.mp4";

        private float[] starSpeeds;
        private float[] starOffsets;
        private UnityEngine.UI.RawImage videoImage;
        private RenderTexture videoTexture;

        private void Awake()
        {
            StartLoopVideo();

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

            FitVideoToScreen();

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

        private void OnDestroy()
        {
            if (videoTexture == null) return;
            videoTexture.Release();
            Destroy(videoTexture);
        }

        private void StartLoopVideo()
        {
            if (string.IsNullOrEmpty(loopVideoFile)) return;

            string path = Path.Combine(Application.streamingAssetsPath, loopVideoFile);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Eternal Sleep] No se encontró el video del menú: " + path);
                return;
            }

            // Justo encima del cielo y detrás de estrellas, luna y niebla. Si el video aún no carga, se ve el cielo.
            var go = new GameObject("LoopVideo", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var sky = transform.Find("Sky");
            go.transform.SetSiblingIndex(sky != null ? sky.GetSiblingIndex() + 1 : 0);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            videoImage = go.AddComponent<UnityEngine.UI.RawImage>();
            videoImage.raycastTarget = false;

            var player = go.AddComponent<VideoPlayer>();
            player.source = VideoSource.Url;
            player.url = path;
            player.isLooping = true;
            player.audioOutputMode = VideoAudioOutputMode.None; // La música la controla el juego.
            player.renderMode = VideoRenderMode.RenderTexture;
            player.prepareCompleted += OnVideoPrepared;
            player.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer player)
        {
            videoTexture = new RenderTexture((int)player.width, (int)player.height, 0);
            player.targetTexture = videoTexture;
            videoImage.texture = videoTexture;
            player.Play();

            // El video sustituye al fondo dibujado: se ocultan cielo, estrellas, luna y niebla.
            foreach (Transform child in transform)
                if (child != videoImage.transform) child.gameObject.SetActive(false);
        }

        // Escala el video para cubrir todo el menú sin deformarlo (recorta los bordes sobrantes).
        private void FitVideoToScreen()
        {
            if (videoTexture == null) return;

            var r = ((RectTransform)videoImage.transform).rect;
            if (r.height <= 0f) return;

            float screenAspect = r.width / r.height;
            float videoAspect = (float)videoTexture.width / videoTexture.height;
            var uv = new Rect(0f, 0f, 1f, 1f);
            if (videoAspect > screenAspect)
            {
                uv.width = screenAspect / videoAspect;
                uv.x = (1f - uv.width) * 0.5f;
            }
            else
            {
                uv.height = videoAspect / screenAspect;
                uv.y = (1f - uv.height) * 0.5f;
            }
            videoImage.uvRect = uv;
        }
    }
}
