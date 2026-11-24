using System.Collections.Generic;
using EternalSleep.Dream;
using EternalSleep.Effects;
using EternalSleep.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EternalSleep.UI
{
    /// <summary>
    /// Menú de pausa: Escape o P (mando: Start) congela el juego y ofrece continuar, opciones,
    /// volver al pueblo (si estás en un sueño) o ir al menú principal.
    /// En Opciones se cambian los volúmenes con A/D (←/→). Escape o B vuelven atrás o cierran.
    /// Se crea solo al arrancar el juego y sobrevive a los cambios de escena,
    /// así que no hace falta añadirlo a las escenas. Funciona en tiempo real (con el juego parado).
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        private static readonly Color OptionColor = new Color(0.55f, 0.48f, 0.75f);
        private static readonly Color SelectedColor = new Color(1f, 0.89f, 0.54f);
        private const string Resume = "Continuar", Options = "Opciones", Diary = "Diario de sueños", ToHub = "Volver al pueblo", ToMenu = "Menú principal";
        private const string Master = "General", Music = "Música", Sfx = "Efectos", Back = "Volver";
        private const string MainHelp = "W/S elegir - Espacio aceptar - Escape volver";
        private const string OptionsHelp = "W/S elegir - A/D cambiar - Escape volver";
        private const int Rows = 5;

        public static bool IsPaused { get; private set; }

        private InputAction toggle, back, confirm, navigate, adjust;
        private GameObject root;
        private TMP_Text title, help;
        private readonly List<TMP_Text> optionTexts = new List<TMP_Text>();
        private readonly List<string> labels = new List<string>();
        private bool inOptions;
        private IPausePage page;     // página extra abierta (diario...), null si se ve el menú
        private string pageLabel;
        private readonly Dictionary<string, IPausePage> pages = new Dictionary<string, IPausePage>();
        private int selected;
        private float lastNav, lastAdjust;
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
            adjust = new InputAction("PauseAdjust", InputActionType.Value);
            adjust.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            adjust.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            adjust.AddBinding("<Gamepad>/leftStick/x");
            adjust.AddBinding("<Gamepad>/dpad/x");
            foreach (var a in new[] { toggle, back, confirm, navigate, adjust }) a.Enable();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            foreach (var a in new[] { toggle, back, confirm, navigate, adjust }) a?.Dispose();
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

            if (toggle.WasPressedThisFrame() || back.WasPressedThisFrame())
            {
                if (page != null) ShowMain(pageLabel);
                else if (inOptions) ShowMain(Options);
                else Close(true);
                return;
            }

            float nav = navigate.ReadValue<float>();
            if (page != null)
            {
                if (Mathf.Abs(nav) > 0.5f && Mathf.Abs(lastNav) <= 0.5f)
                {
                    page.Scroll(nav > 0f ? -1 : 1);
                    AudioManager.Play("menu_mover");
                }
                lastNav = nav;
                return;
            }
            if (Mathf.Abs(nav) > 0.5f && Mathf.Abs(lastNav) <= 0.5f)
            {
                selected = (selected + (nav > 0f ? -1 : 1) + labels.Count) % labels.Count;
                AudioManager.Play("menu_mover");
            }
            lastNav = nav;

            float adj = adjust.ReadValue<float>();
            if (inOptions && Mathf.Abs(adj) > 0.5f && Mathf.Abs(lastAdjust) <= 0.5f && ChangeVolume(labels[selected], adj > 0f ? 0.1f : -0.1f))
                AudioManager.Play("menu_mover", 0f);
            lastAdjust = adj;

            for (int i = 0; i < labels.Count; i++)
            {
                bool sel = i == selected;
                float wave = sel ? Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f : 0f;
                string text = Display(labels[i]);
                optionTexts[i].text = sel ? $"~ {text} ~" : text;
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
            if (DreamWorldManager.IsTransitioning || ScreenFade.Busy || EndingSequence.IsPlaying) return false;
            var player = FindAnyObjectByType<PlayerHealth>();
            return player != null && !player.IsDead;
        }

        private void Open()
        {
            if (root == null) Build();
            ShowMain(Resume);

            pausedInput = FindAnyObjectByType<PlayerInputReader>();
            if (pausedInput != null && pausedInput.enabled) pausedInput.enabled = false;
            else pausedInput = null;

            AudioManager.Play("menu_aceptar", 0f);
            IsPaused = true;
            Time.timeScale = 0f;
            root.SetActive(true);
        }

        private void Close(bool resumeTime)
        {
            if (inOptions) GameSettings.Save();
            inOptions = false;
            ClosePage();
            IsPaused = false;
            if (root != null) root.SetActive(false);
            if (pausedInput != null) pausedInput.enabled = true;
            pausedInput = null;
            if (resumeTime) Time.timeScale = 1f;
        }

        private void ShowMain(string select)
        {
            if (inOptions) GameSettings.Save();
            inOptions = false;
            ClosePage();
            var list = new List<string> { Resume, Options, Diary };
            if (DreamWorldManager.IsInDream) list.Add(ToHub);
            list.Add(ToMenu);
            ShowPage("PAUSA", MainHelp, list, select);
        }

        private void OpenPage(string label)
        {
            page = pages[label];
            pageLabel = label;
            labels.Clear();
            foreach (var t in optionTexts) t.gameObject.SetActive(false);
            lastNav = 0f;
            title.text = page.Heading;
            help.text = page.Help;
            page.Show();
        }

        private void ClosePage()
        {
            if (page != null) page.Hide();
            page = null;
        }

        private void ShowOptions()
        {
            inOptions = true;
            ShowPage("OPCIONES", OptionsHelp, new List<string> { Master, Music, Sfx, Back }, Master);
        }

        private void ShowPage(string heading, string helpText, List<string> page, string select)
        {
            labels.Clear();
            labels.AddRange(page);
            for (int i = 0; i < optionTexts.Count; i++) optionTexts[i].gameObject.SetActive(i < labels.Count);
            selected = Mathf.Max(0, labels.IndexOf(select));
            lastNav = lastAdjust = 0f;
            title.text = heading;
            help.text = helpText;
        }

        private void Choose(string choice)
        {
            AudioManager.Play("menu_aceptar");
            switch (choice)
            {
                case Resume: Close(true); break;
                case Options: ShowOptions(); break;
                case Back: ShowMain(Options); break;
                case ToHub: Close(true); DreamWorldManager.Instance.AbortDream(); break;
                case ToMenu:
                    Close(true);
                    DreamWorldManager.ClearDream();
                    SceneManager.LoadScene(0);
                    break;
                default:
                    if (pages.ContainsKey(choice)) OpenPage(choice);
                    break;
            }
        }

        // ---------- Volúmenes ----------

        private static string Display(string label)
        {
            float? v = Volume(label);
            return v.HasValue ? $"{label}  < {Mathf.RoundToInt(v.Value * 100f)}% >" : label;
        }

        private static float? Volume(string label)
        {
            switch (label)
            {
                case Master: return GameSettings.MasterVolume;
                case Music: return GameSettings.MusicVolume;
                case Sfx: return GameSettings.SfxVolume;
                default: return null;
            }
        }

        /// <summary>Sube o baja un volumen en pasos de 10 %. Devuelve false si esa fila no es un volumen.</summary>
        private static bool ChangeVolume(string label, float delta)
        {
            float? v = Volume(label);
            if (!v.HasValue) return false;
            float next = Mathf.Clamp01(Mathf.Round((v.Value + delta) * 10f) / 10f);
            switch (label)
            {
                case Master: GameSettings.MasterVolume = next; break;
                case Music: GameSettings.MusicVolume = next; break;
                case Sfx: GameSettings.SfxVolume = next; break;
            }
            return true;
        }

        // ---------- Interfaz (se crea la primera vez que se abre) ----------

        private void Build()
        {
            // Por encima del HUD y la muerte, por debajo del fundido de los sueños.
            var canvas = RuntimeUI.Canvas("PausaCanvas", transform, 900);
            var overlay = RuntimeUI.Panel("Contenido", canvas.transform, new Color(0.03f, 0.02f, 0.05f, 0.78f));
            root = overlay.gameObject;

            title = RuntimeUI.Text("Titulo", root.transform, 50f, 24f, new Color(0.85f, 0.8f, 1f), "PAUSA");
            title.characterSpacing = 8f;
            for (int i = 0; i < Rows; i++)
                optionTexts.Add(RuntimeUI.Text("Opcion" + i, root.transform, -2f - i * 22f, 13f, OptionColor, ""));
            help = RuntimeUI.Text("Ayuda", root.transform, -150f, 9f, new Color(0.55f, 0.48f, 0.75f, 0.8f), MainHelp);
            pages[Diary] = new DreamDiaryPanel(root.transform);
            root.SetActive(false);
        }
    }
}
