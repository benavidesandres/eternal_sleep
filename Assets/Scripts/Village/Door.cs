using EternalSleep.Effects;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EternalSleep.Village
{
    /// <summary>
    /// Puerta: si el jugador está delante y pulsa ↑ / W / E (mando: Y), la pantalla se funde a negro
    /// y aparece en el destino (el interior de una casa, o de vuelta en la calle).
    /// Necesita un Collider2D trigger como zona de la puerta.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Door : MonoBehaviour
    {
        [SerializeField] private Transform destination;
        [SerializeField] private GameObject prompt;

        private InputAction interact;
        private bool playerInRange;

        private void Awake()
        {
            interact = new InputAction("Door", InputActionType.Button);
            interact.AddBinding("<Keyboard>/w");
            interact.AddBinding("<Keyboard>/upArrow");
            interact.AddBinding("<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonNorth");
            interact.performed += _ => Use();
            if (prompt != null) prompt.SetActive(false);
        }

        private void OnEnable() => interact.Enable();
        private void OnDisable() => interact.Disable();
        private void OnDestroy() => interact.Dispose();

        private void Update()
        {
            if (prompt != null) prompt.SetActive(playerInRange && !DialogueBox.IsOpen && !ScreenFade.Busy);
        }

        private void Use()
        {
            if (!playerInRange || destination == null || DialogueBox.IsOpen || ScreenFade.Busy) return;
            var player = FindAnyObjectByType<PlayerMotor>();
            if (player == null) return;

            var input = player.GetComponent<PlayerInputReader>();
            if (input != null) input.enabled = false; // quieto durante el fundido
            ScreenFade.Transition(() =>
            {
                player.Teleport(destination.position);
                playerInRange = false;
                if (input != null) input.enabled = true;
            });
        }

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
