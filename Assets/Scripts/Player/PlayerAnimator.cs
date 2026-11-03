using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Solo presentación: lee el estado del Motor y lo traduce a parámetros del Animator.
    /// Nunca mueve nada ni modifica la física.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Márcalo si los sprites originales miran hacia la derecha.")]
        [SerializeField] private bool spriteFacesRight = true;

        private static readonly int SpeedHash    = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int AttackForwardHash = Animator.StringToHash("AttackForward");
        private static readonly int AttackUpHash      = Animator.StringToHash("AttackUp");
        private static readonly int AttackDownHash    = Animator.StringToHash("AttackDown");

        private IMotorState motor;
        private PlayerAttack attack; // opcional

        private void Reset()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            attack = GetComponent<PlayerAttack>();
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (attack != null) attack.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            if (attack != null) attack.Attacked -= OnAttacked;
        }

        private void Update()
        {
            animator.SetFloat(SpeedHash, Mathf.Abs(motor.Velocity.x));
            animator.SetFloat(VerticalHash, motor.Velocity.y);
            animator.SetBool(GroundedHash, motor.IsGrounded);

            bool lookLeft = motor.FacingDirection < 0;
            spriteRenderer.flipX = spriteFacesRight ? lookLeft : !lookLeft;
        }

        private void OnAttacked(AttackDirection direction)
        {
            switch (direction)
            {
                case AttackDirection.Up:   animator.SetTrigger(AttackUpHash);      break;
                case AttackDirection.Down: animator.SetTrigger(AttackDownHash);    break;
                default:                   animator.SetTrigger(AttackForwardHash); break;
            }
        }
    }
}
