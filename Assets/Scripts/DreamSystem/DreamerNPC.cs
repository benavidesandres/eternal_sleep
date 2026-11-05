using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Aldeano del pueblo atrapado en una pesadilla.
    /// - Dormido: respira, le salen "Z" y habla en sueños. Al terminar de escucharlo,
    ///   puedes elegir entrar en su sueño.
    /// - Despierto (completaste su sueño): está sentado y te da las gracias.
    /// Interactuar: W / ↑ / E (mando: Y). Necesita un Collider2D trigger como zona de interacción.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DreamerNPC : MonoBehaviour
    {
        [Tooltip("Identificador único del aldeano (se usa para recordar si ya despertó).")]
        [SerializeField] private string dreamerId = "Aldeano";
        [SerializeField] private string displayName = "Aldeano";
        [Tooltip("Escena del sueño. Vacío = una pesadilla al azar.")]
        [SerializeField] private string dreamScene = "";

        [Header("Diálogo")]
        [SerializeField, TextArea] private string[] sleepingLines = { "...zzz..." };
        [SerializeField, TextArea] private string[] awakeLines = { "Gracias, Kael." };
        [SerializeField] private string enterChoice = "Entrar en su sueño";
        [SerializeField] private string leaveChoice = "Dejarlo descansar";

        [Header("Visual")]
        [SerializeField] private SpriteFlipbook body;
        [Tooltip("Icono que aparece cuando el jugador puede hablarle.")]
        [SerializeField] private GameObject prompt;
        [Tooltip("Las \"Z\" que flotan mientras duerme.")]
        [SerializeField] private Transform[] sleepZ;

        private InputAction interact;
        private bool playerInRange;
        private Vector3[] zBase;

        public bool IsAwake => DreamWorldManager.IsAwake(dreamerId);

        private void Awake()
        {
            interact = new InputAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/w");
            interact.AddBinding("<Keyboard>/upArrow");
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonNorth");
            interact.performed += _ => Interact();

            zBase = new Vector3[sleepZ != null ? sleepZ.Length : 0];
            for (int i = 0; i < zBase.Length; i++) zBase[i] = sleepZ[i].localPosition;
            if (prompt != null) prompt.SetActive(false);
        }

        private void Start()
        {
            if (body != null) body.Play(IsAwake ? "awake" : "sleep");
            if (IsAwake && sleepZ != null)
                foreach (var z in sleepZ) z.gameObject.SetActive(false);
        }

        private void OnEnable() => interact.Enable();
        private void OnDisable() => interact.Disable();
        private void OnDestroy() => interact.Dispose();

        private void Update()
        {
            if (prompt != null) prompt.SetActive(playerInRange && !DialogueBox.IsOpen);
            if (IsAwake || sleepZ == null) return;

            // Tres "Z" que suben en espiral, una detrás de otra, y se desvanecen.
            for (int i = 0; i < sleepZ.Length; i++)
            {
                float t = Mathf.Repeat(Time.time * 0.4f + i / (float)sleepZ.Length, 1f);
                sleepZ[i].localPosition = zBase[i] + new Vector3(Mathf.Sin(t * Mathf.PI * 2f) * 0.12f + t * 0.3f, t * 0.8f, 0f);
                sleepZ[i].localScale = Vector3.one * Mathf.Lerp(0.6f, 1.2f, t);
                var sr = sleepZ[i].GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Sin(t * Mathf.PI));
            }
        }

        private void Interact()
        {
            if (!playerInRange || DialogueBox.IsOpen) return;
            if (Time.unscaledTime - DialogueBox.LastClosedTime < 0.25f) return; // la misma tecla que cerró el diálogo

            var box = DialogueBox.Instance;
            if (IsAwake)
            {
                if (box != null) box.Show(displayName, awakeLines, null, null);
                return;
            }

            if (box == null) { EnterDream(); return; } // escena sin caja de diálogo

            var lines = new string[sleepingLines.Length + 1];
            sleepingLines.CopyTo(lines, 0);
            lines[lines.Length - 1] = $"(La pesadilla de {displayName} se agita bajo sus párpados. ¿Entras?)";
            box.Show(displayName, lines, new[] { enterChoice, leaveChoice }, choice =>
            {
                if (choice == 0) EnterDream();
            });
        }

        private void EnterDream() => DreamWorldManager.Instance.EnterDream(dreamerId, dreamScene, transform.position);

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null) playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null) playerInRange = false;
        }
    }
}
