using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// "Polilla": enemigo que vuela (atraviesa paredes, es un ser de sueño).
    ///
    /// Estados (IA):
    ///  Flota     → vaivén suave en su sitio.
    ///  Persigue  → ve al jugador: se coloca encima de él.
    ///  Prepara   → tiembla y muestra "!" (aviso para que el jugador reaccione).
    ///  Se lanza  → picada rápida hacia donde estaba el jugador.
    ///  Recupera  → sube despacio después de la picada.
    ///  Vuelve    → perdió al jugador: regresa a su sitio.
    ///  Huye      → le queda 1 de vida: se aleja un rato.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemySenses))]
    public class FlyerEnemy : MonoBehaviour
    {
        private enum State { Hover, Chase, Windup, Swoop, Recover, Return, Flee }

        [SerializeField] private float chaseSpeed = 2.4f;
        [SerializeField] private float returnSpeed = 1.5f;
        [SerializeField] private float swoopSpeed = 9f;
        [SerializeField] private float fleeSpeed = 3f;
        [SerializeField] private float hoverHeight = 2.5f;
        [SerializeField] private float swoopRange = 3.5f;
        [SerializeField] private float windupTime = 0.5f;
        [SerializeField] private float swoopMaxTime = 0.6f;
        [SerializeField] private float recoverTime = 1f;
        [SerializeField] private float swoopCooldown = 1.5f;
        [SerializeField] private float memoryTime = 3f;
        [SerializeField] private float fleeTime = 1.5f;
        [SerializeField] private float bobAmplitude = 0.4f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float hurtPushSpeed = 6f;
        [SerializeField] private GameObject alertIcon;

        private Rigidbody2D rb;
        private SpriteRenderer sprite;
        private EnemySenses senses;
        private EnemyHealth health;
        private Vector2 home;
        private Vector2 push;
        private Vector2 swoopTarget;
        private float phase;
        private State state = State.Hover;
        private float stateUntil;
        private float lastSwoopTime = float.NegativeInfinity;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            senses = GetComponent<EnemySenses>();
            health = GetComponent<EnemyHealth>();
            home = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);

            if (health != null) health.Damaged += OnDamaged;
            if (alertIcon != null) alertIcon.SetActive(false);
        }

        private void OnDamaged(Vector2 from)
        {
            push = ((Vector2)transform.position - from).normalized * hurtPushSpeed;
            if (health != null && health.IsWeak) Enter(State.Flee, fleeTime);
            else if (state == State.Swoop || state == State.Windup) Enter(State.Recover, recoverTime);
        }

        private void Enter(State next, float duration = 0f)
        {
            state = next;
            stateUntil = Time.time + duration;
            if (alertIcon != null) alertIcon.SetActive(next == State.Windup);
        }

        private bool TimeUp => Time.time >= stateUntil;

        private void FixedUpdate()
        {
            Vector2 pos = rb.position;
            Vector2 bob = Vector2.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude);
            Vector2 target = pos;
            float speed = 0f;

            switch (state)
            {
                case State.Hover:
                    target = home + bob;
                    speed = returnSpeed;
                    if (senses.CanSeePlayer()) Enter(State.Chase);
                    break;

                case State.Chase:
                    bool sees = senses.CanSeePlayer(0, 1.3f);
                    if (!sees && senses.TimeSinceSeen > memoryTime) { Enter(State.Return); break; }
                    target = senses.LastSeenPosition + Vector2.up * hoverHeight + bob;
                    speed = chaseSpeed;
                    if (sees && senses.DistanceToPlayer < swoopRange && Time.time - lastSwoopTime > swoopCooldown)
                        Enter(State.Windup, windupTime);
                    break;

                case State.Windup:
                    // Tiembla en el sitio para avisar del ataque.
                    target = pos + Random.insideUnitCircle * 0.05f;
                    speed = 5f;
                    if (TimeUp)
                    {
                        swoopTarget = senses.PlayerPosition;
                        lastSwoopTime = Time.time;
                        Enter(State.Swoop, swoopMaxTime);
                    }
                    break;

                case State.Swoop:
                    // Sigue de largo un poco más allá del objetivo.
                    Vector2 dir = (swoopTarget - pos).normalized;
                    target = swoopTarget + dir * 1.5f;
                    speed = swoopSpeed;
                    if (TimeUp || Vector2.Distance(pos, target) < 0.1f) Enter(State.Recover, recoverTime);
                    break;

                case State.Recover:
                    target = pos + Vector2.up * 2f;
                    speed = 1.2f;
                    if (TimeUp) Enter(State.Chase);
                    break;

                case State.Return:
                    target = home + bob;
                    speed = returnSpeed;
                    if (senses.CanSeePlayer()) Enter(State.Chase);
                    else if (Vector2.Distance(pos, home) < 0.3f) Enter(State.Hover);
                    break;

                case State.Flee:
                    Vector2 away = (pos - senses.PlayerPosition).normalized;
                    target = pos + away * 3f + Vector2.up * 0.5f;
                    speed = fleeSpeed;
                    if (TimeUp) Enter(State.Chase);
                    break;
            }

            Vector2 step = Vector2.MoveTowards(pos, target, speed * Time.fixedDeltaTime) - pos;
            push = Vector2.MoveTowards(push, Vector2.zero, 20f * Time.fixedDeltaTime);
            rb.MovePosition(pos + step + push * Time.fixedDeltaTime);

            if (sprite != null && Mathf.Abs(step.x) > 0.001f) sprite.flipX = step.x > 0f;
        }
    }
}
