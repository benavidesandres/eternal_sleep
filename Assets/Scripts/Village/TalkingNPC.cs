using System.Collections.Generic;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EternalSleep.Village
{
    /// <summary>
    /// Personaje u objeto con el que se puede hablar (aldeanos despiertos, el gato, lápidas, el pozo...).
    /// - Normal: dice sus líneas.
    /// - Secreto: a partir de cierta cantidad de conversaciones dice otra cosa (easter egg).
    /// - Recompensa: si ya recogiste todas las Lágrimas de Luna, dice sus líneas de recompensa.
    /// Opcionalmente se gira hacia el jugador.
    /// Interactuar: W / ↑ / E (mando: Y). Necesita un Collider2D trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TalkingNPC : MonoBehaviour
    {
        [SerializeField] private string npcId = "npc";
        [SerializeField] private string displayName = "Aldeano";
        [SerializeField, TextArea] private string[] lines = { "..." };
        [Tooltip("Después de hablar tantas veces, dice las líneas secretas (0 = nunca).")]
        [SerializeField] private int secretAfterTalks;
        [SerializeField, TextArea] private string[] secretLines;
        [Tooltip("Si tienes todas las Lágrimas de Luna, dice esto.")]
        [SerializeField, TextArea] private string[] rewardLines;

        [Header("Visual")]
        [SerializeField] private GameObject prompt;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Gira el sprite para mirar al jugador (el arte mira a la izquierda).")]
        [SerializeField] private bool facePlayer = true;

        private static readonly Dictionary<string, int> talkCount = new Dictionary<string, int>();

        private InputAction interact;
        private Transform player;
        private bool playerInRange;

        private void Awake()
        {
            interact = new InputAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/w");
            interact.AddBinding("<Keyboard>/upArrow");
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonNorth");
            interact.performed += _ => Interact();
            if (prompt != null) prompt.SetActive(false);
        }

        private void OnEnable() => interact.Enable();
        private void OnDisable() => interact.Disable();
        private void OnDestroy() => interact.Dispose();

        private void Update()
        {
            if (prompt != null) prompt.SetActive(playerInRange && !DialogueBox.IsOpen);
            if (facePlayer && body != null && player != null && playerInRange)
                body.flipX = player.position.x > transform.position.x;
        }

        private void Interact()
        {
            if (!playerInRange || DialogueBox.IsOpen || DialogueBox.Instance == null) return;
            if (Time.unscaledTime - DialogueBox.LastClosedTime < 0.25f) return;

            talkCount.TryGetValue(npcId, out int n);
            talkCount[npcId] = ++n;

            string[] say = lines;
            if (rewardLines != null && rewardLines.Length > 0 && CollectibleTracker.HasAll) say = rewardLines;
            else if (secretAfterTalks > 0 && n >= secretAfterTalks && secretLines != null && secretLines.Length > 0) say = secretLines;

            DialogueBox.Instance.Show(displayName, say, null, null);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlayerHealth>();
            if (p == null) return;
            player = p.transform;
            playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null) playerInRange = false;
        }
    }
}
