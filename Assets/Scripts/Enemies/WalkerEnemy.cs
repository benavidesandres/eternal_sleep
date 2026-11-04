using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Enemigo que camina de un lado a otro sobre el suelo.
    /// Da la vuelta al llegar a una pared o al borde de una plataforma.
    /// Se mueve sin gravedad (Rigidbody2D kinematic): se coloca sobre el suelo al empezar.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class WalkerEnemy : MonoBehaviour
    {
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float hurtStunTime = 0.3f;
        [SerializeField] private float hurtPushSpeed = 4f;

        private Rigidbody2D rb;
        private Collider2D col;
        private SpriteRenderer sprite;
        private int groundMask;
        private int direction = -1;
        private float stunUntil;
        private float pushDirection;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            groundMask = LayerMask.GetMask("Ground");

            var health = GetComponent<EnemyHealth>();
            if (health != null) health.Damaged += OnDamaged;
        }

        private void OnDamaged(Vector2 from)
        {
            stunUntil = Time.time + hurtStunTime;
            pushDirection = Mathf.Sign(transform.position.x - from.x);
        }

        private void FixedUpdate()
        {
            Bounds b = col.bounds;

            if (Time.time < stunUntil)
            {
                // Retrocede por el golpe, sin caerse por el borde ni atravesar paredes.
                if (!Blocked(b, (int)pushDirection))
                    rb.MovePosition(rb.position + Vector2.right * (pushDirection * hurtPushSpeed * Time.fixedDeltaTime));
                return;
            }

            if (Blocked(b, direction)) direction = -direction;
            rb.MovePosition(rb.position + Vector2.right * (direction * speed * Time.fixedDeltaTime));
            if (sprite != null) sprite.flipX = direction > 0;
        }

        /// <summary>¿Hay pared delante o falta suelo delante?</summary>
        private bool Blocked(Bounds b, int dir)
        {
            if (dir == 0) return true;
            Vector2 side = new Vector2(b.center.x + dir * b.extents.x, b.center.y);
            bool wall = Physics2D.Raycast(side, Vector2.right * dir, 0.1f, groundMask);

            Vector2 foot = new Vector2(b.center.x + dir * (b.extents.x + 0.05f), b.min.y + 0.05f);
            bool ground = Physics2D.Raycast(foot, Vector2.down, 0.3f, groundMask);
            return wall || !ground;
        }
    }
}
