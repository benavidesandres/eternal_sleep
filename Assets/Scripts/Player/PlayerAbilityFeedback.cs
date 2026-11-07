using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Solo presentación de las mejoras: un aleteo de polvo en el doble salto,
    /// copias de sombra que se desvanecen detrás del impulso, polvo al deslizar y saltar de la pared,
    /// el impacto del golpe al suelo, el brillo del ataque cargado y el tercer golpe del combo.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerAbilityFeedback : MonoBehaviour
    {
        [SerializeField] private float ghostInterval = 0.03f;
        [SerializeField] private float ghostLife = 0.28f;
        [SerializeField] private Color ghostColor = new Color(0.55f, 0.42f, 0.95f, 0.6f);
        [SerializeField] private Color dustColor = new Color(0.8f, 0.75f, 1f);

        [Header("Movimientos nuevos")]
        [SerializeField] private float wallDustInterval = 0.08f;
        [SerializeField] private Color slamColor = new Color(0.65f, 0.55f, 1f);
        [SerializeField] private Color chargeTint = new Color(0.75f, 0.7f, 1f);
        [SerializeField] private Color chargeReadyColor = new Color(1f, 0.95f, 0.7f);

        private PlayerMotor motor;
        private PlayerAttack attack; // opcional
        private SpriteRenderer sprite;
        private float nextGhost;
        private float nextWallDust;
        private bool wasCharging;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            attack = GetComponent<PlayerAttack>();
            sprite = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            motor.AirJumped += OnAirJumped;
            motor.Dashed += OnDashed;
            motor.WallJumped += OnWallJumped;
            motor.Slammed += OnSlammed;
            if (attack == null) return;
            attack.Attacked += OnAttacked;
            attack.ChargeReady += OnChargeReady;
            attack.ChargedAttacked += OnChargedAttacked;
        }

        private void OnDisable()
        {
            motor.AirJumped -= OnAirJumped;
            motor.Dashed -= OnDashed;
            motor.WallJumped -= OnWallJumped;
            motor.Slammed -= OnSlammed;
            if (attack == null) return;
            attack.Attacked -= OnAttacked;
            attack.ChargeReady -= OnChargeReady;
            attack.ChargedAttacked -= OnChargedAttacked;
        }

        private void OnAirJumped() => HitFeedback.Burst(transform.position, dustColor, 12, 3.5f, 0.09f, 5f);

        private void OnDashed()
        {
            HitFeedback.Burst(transform.position + Vector3.up * 0.9f, ghostColor, 8, 2.5f, 0.09f, 0f);
            nextGhost = 0f;
        }

        // El motor ya mira hacia el lado contrario a la pared: el polvo sale detrás.
        private void OnWallJumped() =>
            HitFeedback.Burst(WallPoint(-motor.FacingDirection), dustColor, 10, 3f, 0.08f, 4f);

        private void OnSlammed(Vector2 feet)
        {
            HitFeedback.Burst(feet, slamColor, 22, 6f, 0.11f, 8f);
            HitFeedback.Shake(0.18f, 0.2f);
        }

        private void OnAttacked(AttackDirection direction, int comboStep)
        {
            if (comboStep == 2)
                HitFeedback.Burst(transform.position + new Vector3(motor.FacingDirection * 1.2f, 0.9f), dustColor, 8, 4f, 0.08f, 0f);
        }

        private void OnChargeReady() =>
            HitFeedback.Burst(transform.position + Vector3.up * 0.9f, chargeReadyColor, 10, 2.5f, 0.08f, 0f);

        private void OnChargedAttacked()
        {
            HitFeedback.Burst(transform.position + new Vector3(motor.FacingDirection * 1.5f, 0.9f), chargeReadyColor, 16, 6f, 0.12f, 0f);
            HitFeedback.Shake(0.12f, 0.15f);
        }

        private Vector3 WallPoint(int side) => transform.position + new Vector3(side * 0.35f, 0.9f);

        private void Update()
        {
            UpdateWallDust();
            UpdateChargeGlow();
            UpdateGhosts();
        }

        private void UpdateWallDust()
        {
            if (!motor.IsWallSliding || Time.time < nextWallDust) return;
            nextWallDust = Time.time + wallDustInterval;
            HitFeedback.Burst(WallPoint(motor.FacingDirection), dustColor, 2, 1f, 0.06f, 2f);
        }

        private void UpdateChargeGlow()
        {
            if (sprite == null || attack == null) return;
            bool charging = attack.ChargeProgress > 0.3f; // un toque rápido no llega a brillar
            if (!charging && !wasCharging) return;
            wasCharging = charging;

            // Solo cambia el color; la transparencia es del parpadeo de daño.
            Color c = Color.white;
            if (charging)
                c = attack.IsChargeReady
                    ? Color.Lerp(Color.white, chargeReadyColor, 0.5f + 0.5f * Mathf.Sin(Time.time * 30f))
                    : Color.Lerp(Color.white, chargeTint, 0.5f + 0.5f * Mathf.Sin(Time.time * 12f));
            sprite.color = new Color(c.r, c.g, c.b, sprite.color.a);
        }

        private void UpdateGhosts()
        {
            if (!motor.IsDashing || sprite == null || Time.time < nextGhost) return;
            nextGhost = Time.time + ghostInterval;

            var go = new GameObject("SombraDeKael");
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.transform.localScale = transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite.sprite;
            sr.flipX = sprite.flipX;
            sr.sortingOrder = sprite.sortingOrder - 1;
            sr.color = ghostColor;
            go.AddComponent<Ghost>().life = ghostLife;
        }

        private class Ghost : MonoBehaviour
        {
            public float life;

            private SpriteRenderer sr;
            private float startAlpha;
            private float age;

            private void Start()
            {
                sr = GetComponent<SpriteRenderer>();
                startAlpha = sr.color.a;
            }

            private void Update()
            {
                age += Time.deltaTime;
                var c = sr.color;
                c.a = startAlpha * (1f - age / life);
                sr.color = c;
                if (age >= life) Destroy(gameObject);
            }
        }
    }
}
