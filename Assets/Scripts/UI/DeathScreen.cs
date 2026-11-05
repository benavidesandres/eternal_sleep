using System.Collections;
using System.Collections.Generic;
using EternalSleep.Dream;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalSleep.UI
{
    /// <summary>
    /// Pantalla de muerte: el mundo se ralentiza y oscurece, la máscara de Kael aparece y se parte en dos,
    /// suben motas de sueño y se ofrece volver a intentarlo, volver al pueblo (si era un sueño) o ir al menú.
    /// LevelManager la abre cuando el jugador muere. Funciona con el juego en pausa (tiempo real).
    /// </summary>
    public class DeathScreen : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image overlay;
        [SerializeField] private Image vignette;
        [SerializeField] private RectTransform maskLeft;
        [SerializeField] private RectTransform maskRight;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text quote;
        [SerializeField] private TMP_Text[] options;
        [SerializeField] private Sprite moteSprite;
        [SerializeField] private Color optionColor = new Color(0.55f, 0.48f, 0.75f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.89f, 0.54f);

        private static readonly string[] Quotes =
        {
            "\"La pesadilla aún respira...\"",
            "\"Incluso los caballeros se quedan dormidos.\"",
            "\"El sueño no perdona, pero tampoco olvida.\"",
            "\"Despierta, Kael. Todavía te esperan.\"",
            "\"Cada caída es solo otro sueño.\"",
        };

        public static DeathScreen Instance { get; private set; }

        private InputAction confirm, navigate;
        private readonly List<(RectTransform rt, Image img, float speed, float drift)> motes = new List<(RectTransform, Image, float, float)>();

        private void Awake()
        {
            Instance = this;
            confirm = new InputAction("DeathConfirm", InputActionType.Button);
            foreach (var b in new[] { "<Keyboard>/space", "<Keyboard>/enter", "<Keyboard>/j", "<Keyboard>/z", "<Keyboard>/e",
                                      "<Gamepad>/buttonSouth", "<Mouse>/leftButton" })
                confirm.AddBinding(b);
            navigate = new InputAction("DeathNavigate", InputActionType.Value);
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/downArrow").With("Positive", "<Keyboard>/upArrow");
            navigate.AddBinding("<Gamepad>/leftStick/y");
            navigate.AddBinding("<Gamepad>/dpad/y");
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            confirm?.Dispose();
            navigate?.Dispose();
        }

        public void Show() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            bool inDream = DreamWorldManager.IsInDream;
            var dialogue = FindAnyObjectByType<DialogueBox>();
            if (dialogue != null) dialogue.gameObject.SetActive(false); // que no quede una conversación detrás
            var labels = new List<string> { "Intentarlo de nuevo" };
            if (inDream) labels.Add("Volver al pueblo");
            labels.Add("Menú principal");

            // 1. Cámara lenta mientras Kael se deshace.
            // (se fija en cada cuadro: el congelado del golpe intenta devolverlo a 1)
            for (float t = 0f; t < 0.9f; t += Time.unscaledDeltaTime)
            {
                Time.timeScale = 0.3f;
                yield return null;
            }

            root.SetActive(true);
            SetAlpha(overlay, 0f);
            SetAlpha(vignette, 0f);
            title.alpha = 0f;
            quote.alpha = 0f;
            quote.text = Quotes[Random.Range(0, Quotes.Length)];
            foreach (var o in options) o.gameObject.SetActive(false);
            SetMask(0f, 0f, 0f);
            SpawnMotes();

            // 2. Oscurecer y aparecer la máscara entera.
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.1f)
            {
                SetAlpha(overlay, t * 0.88f);
                SetAlpha(vignette, t);
                SetMask(t, 0f, 0f);
                Time.timeScale = Mathf.Lerp(0.3f, 0f, t);
                yield return null;
            }
            Time.timeScale = 0f;

            // 3. Temblor y se parte en dos.
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                SetMask(1f, 0f, Random.Range(-2f, 2f));
                yield return null;
            }
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                float k = 1f - (1f - t) * (1f - t); // frena al final
                SetMask(1f, k, 0f);
                title.alpha = t;
                yield return null;
            }
            SetMask(1f, 1f, 0f);
            title.alpha = 1f;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f) { quote.alpha = t; yield return null; }
            quote.alpha = 1f;

            // 4. Opciones.
            for (int i = 0; i < options.Length; i++)
            {
                options[i].gameObject.SetActive(i < labels.Count);
                if (i < labels.Count) options[i].text = labels[i];
            }

            confirm.Enable();
            navigate.Enable();
            yield return null;

            int selected = 0;
            float lastNav = 0f;
            while (true)
            {
                float nav = navigate.ReadValue<float>();
                if (Mathf.Abs(nav) > 0.5f && Mathf.Abs(lastNav) <= 0.5f)
                    selected = (selected + (nav > 0f ? -1 : 1) + labels.Count) % labels.Count;
                lastNav = nav;

                for (int i = 0; i < labels.Count; i++)
                {
                    bool sel = i == selected;
                    float wave = sel ? Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f : 0f;
                    options[i].text = sel ? $"~ {labels[i]} ~" : labels[i];
                    options[i].color = sel ? Color.Lerp(selectedColor, Color.white, wave * 0.3f) : optionColor;
                }
                if (confirm.WasPressedThisFrame()) break;
                yield return null;
            }

            confirm.Disable();
            navigate.Disable();
            Time.timeScale = 1f;

            string choice = labels[selected];
            if (choice == "Volver al pueblo") DreamWorldManager.Instance.AbortDream();
            else if (choice == "Menú principal")
            {
                DreamWorldManager.ClearDream();
                SceneManager.LoadScene(0);
            }
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // el sueño se repite igual
        }

        /// <param name="appear">0..1 opacidad y escala de entrada.</param>
        /// <param name="split">0..1 cuánto se separan las dos mitades.</param>
        private void SetMask(float appear, float split, float shake)
        {
            foreach (var (half, side) in new[] { (maskLeft, -1f), (maskRight, 1f) })
            {
                if (half == null) continue;
                half.anchoredPosition = new Vector2(side * (split * 16f) + shake, 60f - split * 8f + (1f - appear) * 10f);
                half.localRotation = Quaternion.Euler(0f, 0f, -side * split * 9f);
                half.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, appear);
                var img = half.GetComponent<Image>();
                if (img != null) SetAlpha(img, appear);
            }
        }

        private static void SetAlpha(Graphic g, float a)
        {
            if (g == null) return;
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        /// <summary>Motas de luz que suben lentamente por la pantalla.</summary>
        private void SpawnMotes()
        {
            if (moteSprite == null || motes.Count > 0) return;
            var parent = (RectTransform)root.transform;
            for (int i = 0; i < 26; i++)
            {
                var go = new GameObject("Mota", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                go.transform.SetSiblingIndex(2); // detrás de la máscara y el texto
                var rt = (RectTransform)go.transform;
                float s = Random.Range(2f, 5f);
                rt.sizeDelta = new Vector2(s, s);
                rt.anchoredPosition = new Vector2(Random.Range(-330f, 330f), Random.Range(-190f, 190f));
                var img = go.AddComponent<Image>();
                img.sprite = moteSprite;
                img.raycastTarget = false;
                img.color = Color.Lerp(new Color(0.73f, 0.66f, 0.94f, 0f), new Color(1f, 0.89f, 0.54f, 0f), Random.value);
                motes.Add((rt, img, Random.Range(8f, 22f), Random.Range(0f, 10f)));
            }
        }

        private void Update()
        {
            if (root == null || !root.activeSelf) return;
            float dt = Time.unscaledDeltaTime;
            foreach (var (rt, img, speed, drift) in motes)
            {
                var p = rt.anchoredPosition;
                p.y += speed * dt;
                p.x += Mathf.Sin(Time.unscaledTime * 0.7f + drift) * 6f * dt;
                if (p.y > 200f) p.y = -200f;
                rt.anchoredPosition = p;
                var c = img.color;
                c.a = Mathf.Min(overlay.color.a, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 1.5f + drift));
                img.color = c;
            }
        }
    }
}
