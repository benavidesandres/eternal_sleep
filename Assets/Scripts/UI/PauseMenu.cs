using System.Collections.Generic;
using EternalSleep.Dream;
using EternalSleep.Effects;
using EternalSleep.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalSleep.UI
{
    /// <summary>
    /// Menú de pausa: Escape o P (mando: Start) congela el juego y ofrece continuar,
    /// volver al pueblo (si estás en un sueño) o ir al menú principal.
    /// Escape o B lo cierran. Se crea solo al arrancar el juego y sobrevive a los cambios de escena,
    /// así que no hace falta añadirlo a las escenas. Funciona en tiempo real (con el juego parado).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private static readonly Color OptionColor = new Color(0.55f, 0.48f, 0.75f);
        private static readonly Color SelectedColor = new Color(1f, 0.89f, 0.54f);
        private const string Resume = "Continuar", ToHub = "Volver al pueblo", ToMenu = "Menú principal";

        public static bool IsPaused { get; private set; }

        private InputAction toggle, back, confirm, navigate;
        private GameObject root;
        private TMP_Text title;
        private readonly List<TMP_Text> optionTexts = new List<TMP_Text>();
        private readonly List<string> labels = new List<string>();
        private int selected;
        private float lastNav;
        private PlayerInputReader pausedInput;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            IsPaused = false;
            var go = new GameObject("PauseMenu");
            DontDestroyOnLoad(go);
            go.AddComponent<PauseMenu>();
        }

        private void Awake()
        {
            toggle = new InputAction("PauseToggle", InputActionType.Button);
            foreach (var b in new[] { "<Keyboard>/escape", "<Keyboard>/p", "<Gamepad>/start" }) toggle.AddBinding(b);
            back = new InputAction("PauseBack", InputActionType.Button);
            back.AddBinding("<Gamepad>/buttonEast");
            confirm = new InputAction("PauseConfirm", InputActionType.Button);
            foreach (var b in new[] { "<Keyboard>/space", "<Keyboard>/enter", "<Keyboard>/j", "<Keyboard>/z", "<Keyboard>/e",
                                      "<Gamepad>/buttonSouth" })
                confirm.AddBinding(b);
            navigate = new InputAction("PauseNavigate", InputActionType.Value);
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/downArrow").With("Positive", "<Keyboard>/upArrow");
            navigate.AddBinding("<Gamepad>/leftStick/y");
            navigate.AddBinding("<Gamepad>/dpad/y");
            toggle.Enable();
            back.Enable();
            confirm.Enable();
            navigate.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            toggle?.Dispose();
            back?.Dispose();
            confirm?.Dispose();
            navigate?.Dispose();
        }

        // Por si se cambia de escena con el menú abierto.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsPaused) Close(false);
        }

        private void Update()
        {
            if (!IsPaused)
            {
                if (toggle.WasPressedThisFrame() && CanPause()) Open();
                return;
            }

            if (toggle.WasPressedThisFrame() || back.WasPressedThisFrame()) { Close(true); return; }

            float nav = navigate.ReadValue<float>();
            if (Mathf.Abs(nav) > 0.5f && Mathf.Abs(lastNav) <= 0.5f)
                selected = (selected + (nav > 0f ? -1 : 1) + labels.Count) % labels.Count;
            lastNav = nav;

            for (int i = 0; i < labels.Count; i++)
            {
                bool sel = i == selected;
                float wave = sel ? Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f : 0f;
                optionTexts[i].text = sel ? $"~ {labels[i]} ~" : labels[i];
                optionTexts[i].color = sel ? Color.Lerp(SelectedColor, Color.white, wave * 0.3f) : OptionColor;
            }

            if (confirm.WasPressedThisFrame()) Choose(labels[selected]);
        }

        /// <summary>
        /// Solo en una escena con jugador vivo y con el juego corriendo: no durante la pantalla de muerte,
        /// el congelado de un golpe (timeScale 0), un diálogo (Escape lo cierra) ni un fundido entre escenas.
        /// </summary>
        private static bool CanPause()
        {
            if (Time.timeScale <= 0f || DialogueBox.IsOpen || Time.unscaledTime - DialogueBox.LastClosedTime < 0.2f) return false;
            if (DreamWorldManager.IsTransitioning || ScreenFade.Busy) return false;
            var player = FindAnyObjectByType<PlayerHealth>();
            return player != null && !player.IsDead;
        }

        private void Open()
        {
            if (root == null) Build();
            labels.Clear();
            labels.Add(Resume);
            if (DreamWorldManager.IsInDream) labels.Add(ToHub);
            labels.Add(ToMenu);
            for (int i = 0; i < optionTexts.Count; i++) optionTexts[i].gameObject.SetActive(i < labels.Count);
            selected = 0;
            lastNav = 0f;

            pausedInput = FindAnyObjectByType<PlayerInputReader>();
            if (pausedInput != null && pausedInput.enabled) pausedInput.enabled = false;
            else pausedInput = null;

            IsPaused = true;
            Time.timeScale = 0f;
            root.SetActive(true);
        }

        private void Close(bool resumeTime)
        {
            IsPaused = false;
            if (root != null) root.SetActive(false);
            if (pausedInput != null) pausedInput.enabled = true;
            pausedInput = null;
            if (resumeTime) Time.timeScale = 1f;
        }

        private void Choose(string choice)
        {
            Close(true);
            if (choice == ToHub) DreamWorldManager.Instance.AbortDream();
            else if (choice == ToMenu)
            {
                DreamWorldManager.ClearDream();
                SceneManager.LoadScene(0);
            }
        }

        // ---------- Interfaz (se crea la primera vez que se abre) ----------

        private void Build()
        {
            var canvasGo = new GameObject("PausaCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900; // por encima del HUD y la muerte, por debajo del fundido de los sueños
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;

            root = new GameObject("Contenido", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var overlay = root.AddComponent<Image>();
            overlay.color = new Color(0.03f, 0.02f, 0.05f, 0.78f);
            overlay.raycastTarget = false;

            // La misma fuente pixel que el resto de la interfaz (se toma de cualquier texto de la escena).
            var any = FindAnyObjectByType<TMP_Text>(FindObjectsInactive.Include);
            var font = any != null ? any.font : TMP_Settings.defaultFontAsset;

            title = Text("Titulo", 50f, 24f, new Color(0.85f, 0.8f, 1f), "PAUSA", font);
            title.characterSpacing = 8f;
            for (int i = 0; i < 3; i++) optionTexts.Add(Text("Opcion" + i, -2f - i * 22f, 13f, OptionColor, "", font));
            Text("Ayuda", -150f, 9f, new Color(0.55f, 0.48f, 0.75f, 0.8f), "W/S elegir - Espacio aceptar - Escape volver", font);
            root.SetActive(false);
        }

        private TMP_Text Text(string name, float y, float size, Color color, string text, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(620f, size + 10f);
            var t = go.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }
    }
}
