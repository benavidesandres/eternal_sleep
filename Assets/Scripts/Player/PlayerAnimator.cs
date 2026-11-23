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
        private static readonly int ComboStepHash     = Animator.StringToHash("ComboStep");
        private static readonly int WallSlidingHash   = Animator.StringToHash("IsWallSliding");
        private static readonly int SlammingHash      = Animator.StringToHash("IsSlamming");
        private static readonly int DashingHash       = Animator.StringToHash("IsDashing");
        private static readonly int HurtHash          = Animator.StringToHash("Hurt");

        private IMotorState motor;
        private PlayerAttack attack; // opcional
        private PlayerHealth health; // opcional
        private bool hasMoveParams;  // false con un Animator antiguo, generado antes de los movimientos nuevos
        private bool hasReaperParams; // false con un Animator anterior al reaper (sin dash ni daño)

        private void Reset()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            attack = GetComponent<PlayerAttack>();
            health = GetComponent<PlayerHealth>();
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == WallSlidingHash) hasMoveParams = true;
                if (p.nameHash == HurtHash) hasReaperParams = true;
            }
        }

        private void OnEnable()
        {
            if (health != null) health.Damaged += OnDamaged;
            if (attack == null) return;
            attack.Attacked += OnAttacked;
            attack.ChargedAttacked += OnChargedAttacked;
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            if (attack == null) return;
            attack.Attacked -= OnAttacked;
            attack.ChargedAttacked -= OnChargedAttacked;
        }

        private void OnDamaged(Vector2 from)
        {
            if (hasReaperParams) animator.SetTrigger(HurtHash);
        }

        private void Update()
        {
            animator.SetFloat(SpeedHash, Mathf.Abs(motor.Velocity.x));
            animator.SetFloat(VerticalHash, motor.Velocity.y);
            animator.SetBool(GroundedHash, motor.IsGrounded);
            if (hasMoveParams)
            {
                animator.SetBool(WallSlidingHash, motor.IsWallSliding);
                animator.SetBool(SlammingHash, motor.IsSlamming);
            }
            if (hasReaperParams) animator.SetBool(DashingHash, motor.IsDashing);

            bool lookLeft = motor.FacingDirection < 0;
            spriteRenderer.flipX = spriteFacesRight ? lookLeft : !lookLeft;
        }

        private void OnChargedAttacked() => OnAttacked(AttackDirection.Forward, 2); // usa el golpe fuerte del combo

        private void OnAttacked(AttackDirection direction, int comboStep)
        {
            if (hasMoveParams) animator.SetInteger(ComboStepHash, comboStep);
            // Las diagonales aún no tienen clip propio: usan el de arriba / abajo
            // (en el suelo, el barrido bajo usa el golpe hacia delante).
            switch (direction)
            {
                case AttackDirection.Up:
                case AttackDirection.UpForward:
                    animator.SetTrigger(AttackUpHash);
                    break;
                case AttackDirection.Down:
                    animator.SetTrigger(AttackDownHash);
                    break;
                case AttackDirection.DownForward:
                    animator.SetTrigger(motor.IsGrounded ? AttackForwardHash : AttackDownHash);
                    break;
                default:
                    animator.SetTrigger(AttackForwardHash);
                    break;
            }
        }
    }
}
