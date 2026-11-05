using System;
using System.Collections;
using System.Collections.Generic;
using EternalSleep.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EternalSleep.UI
{
    /// <summary>
    /// Caja de diálogo en la parte de abajo de la pantalla.
    /// - El texto aparece letra a letra; Confirmar lo completa o pasa a la siguiente línea.
    /// - Al final puede ofrecer opciones (se eligen con ←/→ o ↑/↓).
    /// - Mientras está abierta, Kael no se mueve.
    /// Confirmar: Espacio, Enter, J, Z, E (mando: A/X). Cancelar: Escape (mando: B).
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private GameObject nextIcon;
        [SerializeField] private TMP_Text[] choiceTexts;
        [SerializeField] private float charsPerSecond = 40f;
        [SerializeField] private Color choiceColor = new Color(0.73f, 0.66f, 0.94f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.89f, 0.54f);

        public static DialogueBox Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;
        /// <summary>Momento (tiempo real) en que se cerró por última vez; evita reabrirla con la misma tecla.</summary>
        public static float LastClosedTime { get; private set; } = float.NegativeInfinity;

        private InputAction confirm, cancel, navigate;
        private bool open;
        private float lastNav;

        private void Awake()
        {
            Instance = this;

            confirm = new InputAction("DialogueConfirm", InputActionType.Button);
            foreach (var b in new[] { "<Keyboard>/space", "<Keyboard>/enter", "<Keyboard>/j", "<Keyboard>/z", "<Keyboard>/e",
                                      "<Gamepad>/buttonSouth", "<Gamepad>/buttonWest", "<Mouse>/leftButton" })
                confirm.AddBinding(b);

            cancel = new InputAction("DialogueCancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape");
            cancel.AddBinding("<Gamepad>/buttonEast");

            navigate = new InputAction("DialogueNavigate", InputActionType.Value);
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/w").With("Positive", "<Keyboard>/s");
            navigate.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/upArrow").With("Positive", "<Keyboard>/downArrow");
            navigate.AddBinding("<Gamepad>/leftStick/x");
            navigate.AddBinding("<Gamepad>/dpad/x");

            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            confirm?.Dispose();
            cancel?.Dispose();
            navigate?.Dispose();
        }

        /// <summary>
        /// Muestra las líneas y, si hay opciones, deja elegir una.
        /// done recibe el índice elegido (-1 si no había opciones; la última opción si se cancela).
        /// </summary>
        public void Show(string speaker, IList<string> lines, string[] choices, Action<int> done)
        {
            if (open) return;
            StartCoroutine(Run(speaker, lines, choices, done));
        }

        private IEnumerator Run(string speaker, IList<string> lines, string[] choices, Action<int> done)
        {
            open = true;
            var player = FindAnyObjectByType<PlayerInputReader>();
            if (player != null) player.enabled = false;

            root.SetActive(true);
            nameText.text = speaker;
            SetChoicesVisible(false);
            confirm.Enable();
            cancel.Enable();
            navigate.Enable();
            yield return null; // que la tecla que abrió el diálogo no cuente

            for (int i = 0; i < lines.Count; i++)
            {
                bool last = i == lines.Count - 1;
                yield return TypeLine(lines[i]);

                if (last && choices != null && choices.Length > 0) break;
                nextIcon.SetActive(true);
                while (!confirm.WasPressedThisFrame() && !cancel.WasPressedThisFrame()) yield return null;
                nextIcon.SetActive(false);
                yield return null;
            }

            int result = -1;
            if (choices != null && choices.Length > 0)
            {
                int selected = 0;
                SetChoicesVisible(true);
                for (int i = 0; i < choiceTexts.Length; i++)
                    choiceTexts[i].gameObject.SetActive(i < choices.Length);

                while (true)
                {
                    float nav = navigate.ReadValue<float>();
                    if (Mathf.Abs(nav) > 0.5f && Mathf.Abs(lastNav) <= 0.5f)
                        selected = (selected + (nav > 0f ? 1 : -1) + choices.Length) % choices.Length;
                    lastNav = nav;

                    for (int i = 0; i < choices.Length && i < choiceTexts.Length; i++)
                    {
                        choiceTexts[i].text = (i == selected ? "> " : "  ") + choices[i];
                        choiceTexts[i].color = i == selected ? selectedColor : choiceColor;
                    }

                    if (confirm.WasPressedThisFrame()) { result = selected; break; }
                    if (cancel.WasPressedThisFrame()) { result = choices.Length - 1; break; }
                    yield return null;
                }
            }

            confirm.Disable();
            cancel.Disable();
            navigate.Disable();
            root.SetActive(false);
            open = false;
            LastClosedTime = Time.unscaledTime;
            if (player != null) player.enabled = true;
            done?.Invoke(result);
        }

        private IEnumerator TypeLine(string line)
        {
            nextIcon.SetActive(false);
            bodyText.text = line;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;

            float shown = 0f;
            while (shown < total)
            {
                if (confirm.WasPressedThisFrame()) { yield return null; break; } // completa la línea
                shown += charsPerSecond * Time.unscaledDeltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(total, (int)shown);
                yield return null;
            }
            bodyText.maxVisibleCharacters = total;
        }

        private void SetChoicesVisible(bool visible)
        {
            foreach (var t in choiceTexts) t.gameObject.SetActive(visible);
        }
    }
}
