using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Orbe de sueño disparado por el Vigía. Daña al jugador al tocarlo,
    /// desaparece al chocar con el suelo o una pared, y el jugador puede destruirlo con su ataque.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class DreamOrb : MonoBehaviour, IDamageable
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 5f;

        private Rigidbody2D rb;
        private int groundLayer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            // Dinámico sin gravedad: así detecta paredes (estáticas) y al jugador.
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            groundLayer = LayerMask.NameToLayer("Ground");
            Destroy(gameObject, lifetime);
        }

        public void Launch(Vector2 velocity) => rb.linearVelocity = velocity;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage, transform.position);
                Destroy(gameObject);
                return;
            }
            if (other.gameObject.layer == groundLayer && !other.usedByEffector) Destroy(gameObject); // las plataformas finas no lo paran
        }

        // El ataque del jugador lo deshace.
        public void TakeDamage(int amount, Vector2 from) => Destroy(gameObject);
    }
}
