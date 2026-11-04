using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Enemigo que flota en el aire con un vaivén suave.
    /// Si el jugador se acerca, lo persigue; si se aleja, vuelve a su sitio.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class FlyerEnemy : MonoBehaviour
    {
        [SerializeField] private float detectRange = 6f;
        [SerializeField] private float chaseSpeed = 2.2f;
        [SerializeField] private float returnSpeed = 1.2f;
        [SerializeField] private float bobAmplitude = 0.4f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float hurtPushSpeed = 6f;

        private Rigidbody2D rb;
        private SpriteRenderer sprite;
        private Transform player;
        private Vector2 home;
        private Vector2 push;
        private float phase;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            home = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);

            var health = GetComponent<EnemyHealth>();
            if (health != null) health.Damaged += from => push = ((Vector2)transform.position - from).normalized * hurtPushSpeed;
        }

        private void Start()
        {
            var p = FindAnyObjectByType<PlayerHealth>();
            if (p != null) player = p.transform;
        }

        private void FixedUpdate()
        {
            Vector2 pos = rb.position;
            Vector2 bob = Vector2.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude);

            Vector2 target = home + bob;
            float speed = returnSpeed;
            if (player != null && Vector2.Distance(player.position, pos) < detectRange)
            {
                target = (Vector2)player.position + Vector2.up * 1f; // apunta al cuerpo, no a los pies
                speed = chaseSpeed;
            }

            Vector2 step = Vector2.MoveTowards(pos, target, speed * Time.fixedDeltaTime) - pos;
            push = Vector2.MoveTowards(push, Vector2.zero, 20f * Time.fixedDeltaTime);
            rb.MovePosition(pos + step + push * Time.fixedDeltaTime);

            if (sprite != null && Mathf.Abs(step.x) > 0.001f) sprite.flipX = step.x > 0f;
        }
    }
}
