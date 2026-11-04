using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// "Sombra": enemigo que camina sobre el suelo. Nunca se cae por los bordes.
    ///
    /// Estados (IA):
    ///  Patrulla → camina de lado a lado.
    ///  Alerta   → ve al jugador delante: se detiene un instante y muestra "!".
    ///  Persigue → corre hacia el jugador (se queda en el borde si no puede seguirlo).
    ///  Embiste  → cerca del jugador: se agacha y se lanza en una carrera corta.
    ///  Busca    → perdió de vista al jugador: va hasta donde lo vio por última vez.
    ///  Aturdido → recibió un golpe: retrocede un momento.
    ///  Huye     → le queda 1 de vida: escapa un rato antes de volver a atacar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(EnemySenses))]
    public class WalkerEnemy : MonoBehaviour
    {
        private enum State { Patrol, Alert, Chase, LungeWindup, Lunge, Search, Hurt, Flee }

        [Header("Velocidades")]
        [SerializeField] private float patrolSpeed = 1.5f;
        [SerializeField] private float chaseSpeed = 3f;
        [SerializeField] private float lungeSpeed = 7f;
        [SerializeField] private float fleeSpeed = 3.2f;

        [Header("Tiempos")]
        [SerializeField] private float alertTime = 0.4f;
        [SerializeField] private float lungeWindup = 0.3f;
        [SerializeField] private float lungeDuration = 0.25f;
        [SerializeField] private float lungeCooldown = 1.2f;
        [SerializeField] private float lungeRange = 2f;
        [SerializeField] private float memoryTime = 2f;
        [SerializeField] private float searchTime = 2.5f;
        [SerializeField] private float hurtTime = 0.3f;
        [SerializeField] private float hurtPushSpeed = 4f;
        [SerializeField] private float fleeTime = 1.5f;

        [Header("Opcional")]
        [Tooltip("Icono \"!\" que aparece al ver al jugador.")]
        [SerializeField] private GameObject alertIcon;

        private Rigidbody2D rb;
        private Collider2D col;
        private SpriteRenderer sprite;
        private EnemySenses senses;
        private EnemyHealth health;
        private int groundMask;

        private State state = State.Patrol;
        private float stateUntil;
        private int facing = -1;
        private int moveDir;
        private float lastLungeTime = float.NegativeInfinity;
        private Vector3 baseScale;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            senses = GetComponent<EnemySenses>();
            health = GetComponent<EnemyHealth>();
            groundMask = LayerMask.GetMask("Ground");
            baseScale = transform.localScale;

            if (health != null) health.Damaged += OnDamaged;
            if (alertIcon != null) alertIcon.SetActive(false);
        }

        private void OnDamaged(Vector2 from)
        {
            moveDir = (int)Mathf.Sign(transform.position.x - from.x);
            Enter(State.Hurt, hurtTime);
        }

        private void Enter(State next, float duration = 0f)
        {
            state = next;
            stateUntil = Time.time + duration;
            if (alertIcon != null) alertIcon.SetActive(next == State.Alert || next == State.LungeWindup);
            transform.localScale = next == State.LungeWindup
                ? new Vector3(baseScale.x * 1.15f, baseScale.y * 0.8f, baseScale.z) // se agacha antes de lanzarse
                : baseScale;
        }

        private bool TimeUp => Time.time >= stateUntil;
        private int DirToPlayer => (int)Mathf.Sign(senses.PlayerPosition.x - transform.position.x);

        private void FixedUpdate()
        {
            float speed = 0f;

            switch (state)
            {
                case State.Patrol:
                    if (Blocked(facing)) facing = -facing;
                    speed = patrolSpeed;
                    if (senses.CanSeePlayer(facing)) Enter(State.Alert, alertTime);
                    break;

                case State.Alert:
                    facing = DirToPlayer;
                    if (TimeUp) Enter(State.Chase);
                    break;

                case State.Chase:
                    bool sees = senses.CanSeePlayer();
                    if (!sees && senses.TimeSinceSeen > memoryTime) { Enter(State.Search, searchTime); break; }

                    facing = DirToPlayer;
                    if (!Blocked(facing)) speed = chaseSpeed; // si hay borde, espera ahí

                    bool closeEnough = Mathf.Abs(senses.PlayerPosition.x - transform.position.x) < lungeRange
                                       && Mathf.Abs(senses.PlayerPosition.y - transform.position.y) < 1.8f;
                    if (sees && closeEnough && Time.time - lastLungeTime > lungeCooldown)
                        Enter(State.LungeWindup, lungeWindup);
                    break;

                case State.LungeWindup:
                    facing = DirToPlayer;
                    if (TimeUp) { lastLungeTime = Time.time; Enter(State.Lunge, lungeDuration); }
                    break;

                case State.Lunge:
                    speed = lungeSpeed;
                    if (TimeUp || Blocked(facing)) Enter(State.Chase);
                    break;

                case State.Search:
                    if (senses.CanSeePlayer(facing)) { Enter(State.Alert, alertTime * 0.5f); break; }
                    float dx = senses.LastSeenPosition.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.2f) facing = (int)Mathf.Sign(dx);
                    if (Mathf.Abs(dx) > 0.2f && !Blocked(facing)) speed = patrolSpeed;
                    if (TimeUp) Enter(State.Patrol);
                    break;

                case State.Hurt:
                    if (!Blocked(moveDir))
                        rb.MovePosition(rb.position + Vector2.right * (moveDir * hurtPushSpeed * Time.fixedDeltaTime));
                    if (TimeUp)
                    {
                        if (health != null && health.IsWeak) Enter(State.Flee, fleeTime);
                        else Enter(State.Chase);
                    }
                    return;

                case State.Flee:
                    facing = -DirToPlayer;
                    if (!Blocked(facing)) speed = fleeSpeed;
                    if (TimeUp) Enter(State.Chase);
                    break;
            }

            if (speed > 0f) rb.MovePosition(rb.position + Vector2.right * (facing * speed * Time.fixedDeltaTime));
            if (sprite != null) sprite.flipX = facing > 0;
        }

        /// <summary>¿Hay pared delante o falta suelo delante?</summary>
        private bool Blocked(int dir)
        {
            if (dir == 0) return true;
            Bounds b = col.bounds;
            Vector2 side = new Vector2(b.center.x + dir * b.extents.x, b.center.y);
            bool wall = Physics2D.Raycast(side, Vector2.right * dir, 0.1f, groundMask);

            Vector2 foot = new Vector2(b.center.x + dir * (b.extents.x + 0.05f), b.min.y + 0.05f);
            bool ground = Physics2D.Raycast(foot, Vector2.down, 0.3f, groundMask);
            return wall || !ground;
        }
    }
}
