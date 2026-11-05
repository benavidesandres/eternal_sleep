using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EternalSleep.Player
{
    /// <summary>
    /// Traduce dispositivos (teclado / mando) a intenciones del jugador.
    /// No contiene lógica de juego. Usa el Input System nuevo, creado por código,
    /// así no depende de ningún asset ni de componentes extra en la escena.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool JumpHeld => jump.IsPressed();

        public event Action JumpPressed;
        public event Action AttackPressed;

        private InputAction move;
        private InputAction aim;
        private InputAction jump;
        private InputAction attack;

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x");

            // Eje vertical: solo sirve para elegir la dirección del ataque (arriba / abajo)
            aim = new InputAction("Aim", InputActionType.Value);
            aim.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/s")
                .With("Positive", "<Keyboard>/w");
            aim.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/downArrow")
                .With("Positive", "<Keyboard>/upArrow");
            aim.AddBinding("<Gamepad>/leftStick/y");

            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            attack = new InputAction("Attack", InputActionType.Button);
            attack.AddBinding("<Keyboard>/j");
            attack.AddBinding("<Keyboard>/z");
            attack.AddBinding("<Mouse>/leftButton");
            attack.AddBinding("<Gamepad>/buttonWest");

            jump.performed += _ => JumpPressed?.Invoke();
            attack.performed += _ => AttackPressed?.Invoke();
        }

        private void OnEnable()
        {
            move.Enable();
            aim.Enable();
            jump.Enable();
            attack.Enable();
        }

        private void OnDisable()
        {
            move.Disable();
            aim.Disable();
            jump.Disable();
            attack.Disable();
            Horizontal = 0f;
            Vertical = 0f;
        }

        private void OnDestroy()
        {
            move?.Dispose();
            aim?.Dispose();
            jump?.Dispose();
            attack?.Dispose();
        }

        private void Update()
        {
            Horizontal = move.ReadValue<float>();
            Vertical = aim.ReadValue<float>();
        }
    }
}
