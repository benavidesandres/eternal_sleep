using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Sonidos de Kael: saltos, impulso, golpes, daño y muerte. Escucha los eventos del motor, el ataque y la vida.
    /// AudioManager lo añade solo al jugador de cada escena.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerSounds : MonoBehaviour
    {
        private PlayerMotor motor;
        private PlayerAttack attack;
        private PlayerHealth health;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            attack = GetComponent<PlayerAttack>();
            health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            motor.Jumped += OnJumped;
            motor.AirJumped += OnAirJumped;
            motor.WallJumped += OnJumped;
            motor.Dashed += OnDashed;
            motor.Slammed += OnSlammed;
            if (attack != null)
            {
                attack.Attacked += OnAttacked;
                attack.ChargeReady += OnChargeReady;
                attack.ChargedAttacked += OnChargedAttacked;
            }
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            motor.Jumped -= OnJumped;
            motor.AirJumped -= OnAirJumped;
            motor.WallJumped -= OnJumped;
            motor.Dashed -= OnDashed;
            motor.Slammed -= OnSlammed;
            if (attack != null)
            {
                attack.Attacked -= OnAttacked;
                attack.ChargeReady -= OnChargeReady;
                attack.ChargedAttacked -= OnChargedAttacked;
            }
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnJumped() => AudioManager.Play("salto", 0.06f, 0.8f);
        private void OnAirJumped() => AudioManager.Play("doble_salto");
        private void OnDashed() => AudioManager.Play("impulso");
        private void OnSlammed(Vector2 feet) => AudioManager.Play("golpe_suelo", 0.03f);
        private void OnAttacked(AttackDirection dir, int combo) => AudioManager.Play("ataque", 0.1f, combo >= 3 ? 1f : 0.75f);
        private void OnChargeReady() => AudioManager.Play("carga_lista", 0f);
        private void OnChargedAttacked() => AudioManager.Play("ataque_cargado", 0.03f);
        private void OnDamaged(Vector2 from) => AudioManager.Play("dano", 0.04f);
        private void OnDied() => AudioManager.Play("muerte", 0f);
    }
}
