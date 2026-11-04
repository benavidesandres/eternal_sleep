using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// "Vigía": ojo que no se mueve y dispara orbes de sueño.
    ///
    /// Estados (IA):
    ///  Vigila  → mira a un lado y a otro buscando al jugador.
    ///  Apunta  → lo ve: brilla y muestra "!" (aviso) antes de disparar.
    ///  Dispara → lanza un orbe hacia donde estará el jugador (apunta un poco por delante).
    ///  Espera  → recarga antes de poder volver a disparar.
    /// </summary>
    [RequireComponent(typeof(EnemySenses))]
    public class ShooterEnemy : MonoBehaviour
    {
        private enum State { Scan, Aim, Cooldown }

        [SerializeField] private DreamOrb projectilePrefab;
        [SerializeField] private float projectileSpeed = 5f;
        [SerializeField] private float aimTime = 0.6f;
        [SerializeField] private float cooldown = 1.6f;
        [SerializeField] private float scanFlipTime = 2.5f;
        [Tooltip("Cuánto se adelanta al movimiento del jugador (0 = dispara a donde está).")]
        [SerializeField, Range(0f, 1f)] private float leadFactor = 0.5f;
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0f, 0.5f);
        [SerializeField] private GameObject alertIcon;

        private EnemySenses senses;
        private SpriteRenderer sprite;
        private State state = State.Scan;
        private float stateUntil;
        private int facing = -1;
        private Vector2 lastPlayerPos;
        private Vector2 playerVelocity;

        private void Awake()
        {
            senses = GetComponent<EnemySenses>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            if (alertIcon != null) alertIcon.SetActive(false);
            Enter(State.Scan, scanFlipTime);

            var health = GetComponent<EnemyHealth>();
            if (health != null) health.Damaged += _ => { if (state == State.Aim) Enter(State.Cooldown, cooldown); };
        }

        private void Enter(State next, float duration)
        {
            state = next;
            stateUntil = Time.time + duration;
            if (alertIcon != null) alertIcon.SetActive(next == State.Aim);
        }

        private void FixedUpdate()
        {
            // Estima la velocidad del jugador para apuntar por delante.
            Vector2 p = senses.PlayerPosition;
            playerVelocity = Vector2.Lerp(playerVelocity, (p - lastPlayerPos) / Time.fixedDeltaTime, 0.2f);
            lastPlayerPos = p;

            bool sees = senses.CanSeePlayer();
            if (sees) facing = (int)Mathf.Sign(p.x - transform.position.x);

            switch (state)
            {
                case State.Scan:
                    if (sees) Enter(State.Aim, aimTime);
                    else if (Time.time >= stateUntil) { facing = -facing; Enter(State.Scan, scanFlipTime); }
                    break;

                case State.Aim:
                    if (!sees && senses.TimeSinceSeen > 0.5f) { Enter(State.Scan, scanFlipTime); break; }
                    if (Time.time >= stateUntil) { Shoot(); Enter(State.Cooldown, cooldown); }
                    break;

                case State.Cooldown:
                    if (Time.time >= stateUntil) Enter(sees ? State.Aim : State.Scan, sees ? aimTime : scanFlipTime);
                    break;
            }

            if (sprite != null)
            {
                sprite.flipX = facing > 0;
                // Brilla más fuerte mientras apunta.
                float glow = state == State.Aim ? 0.75f + 0.25f * Mathf.Sin(Time.time * 30f) : 1f;
                sprite.color = new Color(glow, glow, glow, sprite.color.a);
            }
        }

        private void Shoot()
        {
            if (projectilePrefab == null) return;
            Vector2 muzzle = (Vector2)transform.position + muzzleOffset;
            Vector2 target = senses.PlayerPosition;

            // Tiempo aproximado de vuelo → dónde estará el jugador.
            float flight = Vector2.Distance(muzzle, target) / projectileSpeed;
            target += playerVelocity * (flight * leadFactor);

            var orb = Instantiate(projectilePrefab, muzzle, Quaternion.identity);
            orb.Launch((target - muzzle).normalized * projectileSpeed);
        }
    }
}
