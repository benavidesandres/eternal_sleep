using System;
using System.Collections;
using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Vida de un enemigo y cómo reacciona a los golpes:
    /// - Golpe: silueta blanca, se aplasta y rebota, suelta fragmentos y el juego se congela un instante.
    /// - Muerte: deja de actuar, se ilumina, se hincha y se deshace en humo de sueño.
    /// Los scripts de IA escuchan Damaged para retroceder.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private int maxHealth = 2;
        [SerializeField] private float invulnerableTime = 0.2f;

        [Header("Efectos")]
        [SerializeField] private float flashTime = 0.1f;
        [SerializeField] private Color hitColor = new Color(0.73f, 0.66f, 0.94f);
        [SerializeField] private float hitStop = 0.05f;
        [SerializeField] private float deathTime = 0.45f;
        [Tooltip("Centro del cuerpo respecto al pivote (para los fragmentos).")]
        [SerializeField] private Vector2 centerOffset = new Vector2(0f, 0.5f);

        private SpriteRenderer sprite;
        private SpriteFlipbook flipbook;
        private Vector3 baseScale;
        private float invulnerableUntil;
        private Coroutine punch;

        public int Current { get; private set; }
        public int Max => maxHealth;
        /// <summary>Mientras es true los golpes no hacen daño (jefes al teletransportarse o cambiar de fase).</summary>
        public bool Invulnerable { get; set; }
        public bool IsDead => Current <= 0;
        /// <summary>Le queda solo 1 de vida (y tenía más): momento de huir.</summary>
        public bool IsWeak => maxHealth > 1 && Current == 1;
        private Vector2 Center => (Vector2)transform.position + centerOffset;

        /// <summary>Se dispara con la posición desde donde vino el golpe.</summary>
        public event Action<Vector2> Damaged;
        public event Action Died;

        private void Awake()
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            flipbook = GetComponentInChildren<SpriteFlipbook>();
            baseScale = transform.localScale;
            Current = maxHealth;
        }

        public void TakeDamage(int amount, Vector2 from)
        {
            if (IsDead || Time.time < invulnerableUntil) return;
            if (Invulnerable)
            {
                HitFeedback.Burst(Center, new Color(0.6f, 0.6f, 0.7f), 3, 3f); // el golpe rebota
                invulnerableUntil = Time.time + invulnerableTime;
                return;
            }

            Current -= amount;
            invulnerableUntil = Time.time + invulnerableTime;
            Flash(flashTime);
            Vector2 away = ((Vector2)transform.position - from).normalized;
            HitFeedback.Burst(Center, hitColor, 7, 5f);
            HitFeedback.HitStop(hitStop);
            HitFeedback.Shake(0.08f, 0.12f);
            Damaged?.Invoke(from);

            if (IsDead)
            {
                Died?.Invoke();
                StartCoroutine(Die(away));
            }
            else
            {
                if (punch != null) StopCoroutine(punch);
                punch = StartCoroutine(Punch());
            }
        }

        private void Flash(float seconds)
        {
            if (flipbook != null && flipbook.HasFlash) flipbook.Flash(seconds);
            else if (sprite != null) StartCoroutine(AlphaFlash(seconds));
        }

        private IEnumerator AlphaFlash(float seconds)
        {
            var c = sprite.color;
            sprite.color = new Color(c.r, c.g, c.b, 0.35f);
            yield return new WaitForSeconds(seconds);
            if (sprite != null) sprite.color = c;
        }

        /// <summary>Se aplasta y vuelve a su forma con un pequeño rebote.</summary>
        private IEnumerator Punch()
        {
            const float time = 0.18f;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                float k = t / time;
                float squash = Mathf.Sin(k * Mathf.PI) * (1f - k) * 0.6f;
                transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
                yield return null;
            }
            transform.localScale = baseScale;
            punch = null;
        }

        private IEnumerator Die(Vector2 away)
        {
            if (punch != null) StopCoroutine(punch);

            // Deja de actuar y de hacer daño (los triggers avisan incluso a scripts desactivados).
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            foreach (var b in GetComponents<MonoBehaviour>())
                if (b != this && !(b is SpriteFlipbook)) b.enabled = false;
            foreach (Transform child in transform)
                if (child.GetComponent<SpriteRenderer>() != sprite) child.gameObject.SetActive(false); // "!" etc.

            if (flipbook != null)
            {
                flipbook.Play("hurt");
                flipbook.Flash(deathTime);
            }
            HitFeedback.HitStop(0.09f);
            HitFeedback.Shake(0.18f, 0.2f);

            Vector3 start = transform.position;
            for (float t = 0f; t < deathTime; t += Time.deltaTime)
            {
                float k = t / deathTime;
                transform.position = start + (Vector3)(away * (k * 0.5f)) + Vector3.up * (k * 0.3f);
                transform.localScale = baseScale * (1f + k * 0.35f);
                if (sprite != null) sprite.color = new Color(1f, 1f, 1f, 1f - k * k);
                if (UnityEngine.Random.value < 0.5f) HitFeedback.Burst(Center, hitColor, 1, 2f, 0.1f, -2f);
                yield return null;
            }

            HitFeedback.Burst(Center, hitColor, 18, 6f, 0.14f, -1f);
            HitFeedback.Burst(Center, new Color(0.04f, 0.03f, 0.08f), 8, 4f, 0.18f, -1f);
            Destroy(gameObject);
        }
    }
}
