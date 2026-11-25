using System.Collections;
using System.IO;
using EternalSleep.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EternalSleep.UI
{
    /// <summary>
    /// Intro en video al abrir el juego (Assets/StreamingAssets/intro.mp4), encima del menú principal.
    /// Se salta con Escape (mando: B o Start) o haciendo clic en "ESC  Saltar".
    /// Solo sale al arrancar: volver al menú desde una partida no la repite.
    /// Si el video falta o no se puede reproducir, se pasa directo al menú.
    /// </summary>
    public class IntroVideo : MonoBehaviour
    {
        private const string FileName = "intro.mp4";
        private const float PrepareTimeout = 8f;
        private const float FadeSeconds = 0.5f;

        private VideoPlayer player;
        private AudioSource audioSource;
        private RenderTexture texture;
        private CanvasGroup group;
        private RawImage screen;
        private AspectRatioFitter fitter;
        private EventSystem blockedEvents;
        private bool hadNavigation;
        private bool ending;
        private float startedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (SceneManager.GetActiveScene().buildIndex != 0) return; // solo arrancando desde el menú
            if (!File.Exists(Path.Combine(Application.streamingAssetsPath, FileName))) return;
            new GameObject("Intro").AddComponent<IntroVideo>();
        }

        private void Awake()
        {
            BuildScreen();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = Path.Combine(Application.streamingAssetsPath, FileName);
            player.isLooping = false;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource; // así respeta el volumen general
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, audioSource);
            player.prepareCompleted += OnPrepared;
            player.loopPointReached += OnFinished;
            player.errorReceived += OnError;
        }

        private void Start()
        {
            startedAt = Time.unscaledTime;
            AudioManager.OverrideMusic("silencio"); // no hay clip con ese nombre: la música del menú se apaga

            // El menú está debajo: que Enter o Espacio no pulsen sus botones mientras dura la intro.
            blockedEvents = EventSystem.current;
            if (blockedEvents != null)
            {
                hadNavigation = blockedEvents.sendNavigationEvents;
                blockedEvents.sendNavigationEvents = false;
            }

            player.Prepare();
        }

        private void Update()
        {
            if (ending) return;

            bool skip = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame ||
                                                     Gamepad.current.startButton.wasPressedThisFrame));
            if (skip) { Finish(); return; }

            if (!player.isPrepared && Time.unscaledTime - startedAt > PrepareTimeout) Finish();
        }

        private void OnPrepared(VideoPlayer p)
        {
            if (ending) return;
            texture = new RenderTexture((int)p.width, (int)p.height, 0);
            p.targetTexture = texture;
            screen.texture = texture;
            screen.color = Color.white;
            fitter.aspectRatio = (float)p.width / p.height;
            p.Play();
        }

        private void OnFinished(VideoPlayer p) => Finish();

        private void OnError(VideoPlayer p, string message)
        {
            Debug.LogWarning("[Eternal Sleep] No se pudo reproducir la intro: " + message);
            Finish();
        }

        private void Finish()
        {
            if (ending) return;
            ending = true;
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            AudioManager.OverrideMusic(null); // vuelve la música del menú
            float volume = audioSource.volume;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                float k = 1f - t / FadeSeconds;
                group.alpha = k;
                audioSource.volume = volume * k;
                yield return null;
            }

            if (blockedEvents != null) blockedEvents.sendNavigationEvents = hadNavigation;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (texture == null) return;
            texture.Release();
            Destroy(texture);
        }

        private void BuildScreen()
        {
            var canvas = RuntimeUI.Canvas("IntroCanvas", transform, 2000);
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            group = canvas.gameObject.AddComponent<CanvasGroup>();

            // Fondo negro que tapa el menú (y recibe los clics para que no lleguen a sus botones).
            var black = RuntimeUI.Panel("Fondo", canvas.transform, Color.black);
            black.raycastTarget = true;

            // El video entero, sin recortar ni deformar.
            var video = RuntimeUI.Stretch("Video", canvas.transform);
            screen = video.gameObject.AddComponent<RawImage>();
            screen.color = Color.black;
            screen.raycastTarget = false;
            fitter = video.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;

            BuildSkipButton(canvas.transform);
        }

        private void BuildSkipButton(Transform parent)
        {
            var go = new GameObject("BotonSaltar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-10f, 10f);
            rt.sizeDelta = new Vector2(96f, 22f);

            var back = go.AddComponent<Image>();
            back.color = new Color(0.03f, 0.02f, 0.07f, 0.7f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = back;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Finish);

            var label = RuntimeUI.Text("Texto", go.transform, 0f, 10f, new Color(0.85f, 0.8f, 1f),
                                       "<color=#FFE38A>ESC</color>  Saltar");
            label.rectTransform.sizeDelta = rt.sizeDelta;
            label.alignment = TextAlignmentOptions.Center;
        }
    }
}
