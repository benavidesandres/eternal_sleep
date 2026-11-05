using System.Collections;
using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Solo presentación: cómo se ve Kael al recibir un golpe.
    /// Destello rojo, el juego se congela un instante, la cámara tiembla,
    /// la animación se detiene en seco y salen fragmentos de su máscara/capa.
    /// El parpadeo de invulnerabilidad lo hace PlayerHealth.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHurtFeedback : MonoBehaviour
    {
        [SerializeField] private Color hurtTint = new Color(1f, 0.25f, 0.4f);
        [SerializeField] private float tintTime = 0.25f;
        [SerializeField] private float hitStop = 0.1f;
        [SerializeField] private float shake = 0.3f;
        [SerializeField] private float freezeAnimation = 0.2f;

        private PlayerHealth health;
        private SpriteRenderer sprite;
        private Animator animator;
        private Coroutine routine;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void OnDamaged(Vector2 from)
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * 0.9f;
            HitFeedback.HitStop(hitStop);
            HitFeedback.Shake(shake, 0.3f);
            HitFeedback.Burst(center, new Color(0.91f, 0.42f, 0.57f), 10, 5f);
            HitFeedback.Burst(center, new Color(0.95f, 0.93f, 1f), 5, 4f, 0.1f);
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Hurt());
        }

        private void OnDied()
        {
            Vector2 center = (Vector2)transform.position + Vector2.up * 0.9f;
            HitFeedback.Shake(0.5f, 0.5f);
            HitFeedback.Burst(center, new Color(0.1f, 0.08f, 0.16f), 24, 6f, 0.16f, -1f);
            HitFeedback.Burst(center, new Color(0.95f, 0.93f, 1f), 12, 5f, 0.12f, -1f);
            StartCoroutine(Vanish());
        }

        /// <summary>Kael se deshace en sombra: parpadea rápido y desaparece.</summary>
        private IEnumerator Vanish()
        {
            for (int i = 0; i < 6 && sprite != null; i++)
            {
                SetTint(i % 2 == 0 ? hurtTint : Color.white);
                yield return new WaitForSecondsRealtime(0.07f);
            }
            if (sprite != null) sprite.enabled = false;
        }

        private IEnumerator Hurt()
        {
            if (animator != null) animator.speed = 0f; // se queda "congelado" en la pose del golpe
            float end = Time.time + tintTime;
            while (Time.time < end)
            {
                float k = 1f - (end - Time.time) / tintTime;
                SetTint(Color.Lerp(hurtTint, Color.white, k * k));
                if (animator != null && Time.time > end - tintTime + freezeAnimation) animator.speed = 1f;
                yield return null;
            }
            SetTint(Color.white);
            if (animator != null) animator.speed = 1f;
            routine = null;
        }

        private void SetTint(Color c)
        {
            if (sprite == null) return;
            sprite.color = new Color(c.r, c.g, c.b, sprite.color.a); // la transparencia es del parpadeo
        }
    }
}
