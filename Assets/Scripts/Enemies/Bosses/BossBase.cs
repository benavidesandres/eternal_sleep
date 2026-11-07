using System;
using System.Collections;
using EternalSleep.Effects;
using EternalSleep.Level;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Lo que comparten los jefes:
    /// - Presentación: espera un momento, ruge y aparece su barra de vida con el nombre.
    /// - Pelea: repite NextAttack() (cada jefe elige un ataque, lo avisa con una pose y lo ejecuta).
    /// - Segunda fase: a media vida se enfurece (un instante invulnerable), se vuelve más rápido
    ///   y usa ataques nuevos.
    /// - No retroceden al recibir golpes: destellan (EnemyHealth) y siguen.
    /// - Utilidades: moverse, mirar al jugador, disparar orbes y avisar de un ataque.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public abstract class BossBase : MonoBehaviour
    {
        [SerializeField] private string displayName = "Jefe";
        [SerializeField] protected DreamOrb projectilePrefab;
        [SerializeField] private Color projectileTint = Color.white;

        [Header("Presentación")]
        [SerializeField] private float introDelay = 1f;
        [SerializeField] private float roarTime = 1.3f;

        [Header("Segunda fase")]
        [Tooltip("Fracción de vida en la que se enfurece.")]
        [SerializeField, Range(0f, 1f)] private float phaseTwoAt = 0.5f;
        [Tooltip("Cuánto más rápido va todo en la segunda fase.")]
        [SerializeField] private float phaseTwoSpeed = 1.3f;
        [SerializeField] private Color phaseTwoTint = new Color(1f, 0.82f, 0.9f);

        protected Rigidbody2D rb;
        protected EnemyHealth health;
        protected SpriteFlipbook anim;
        protected SpriteRenderer sprite;
        private PlayerHealth player;
        private int lastAttack = -1;

        public string DisplayName => displayName;
        public EnemyHealth Health => health;
        public bool PhaseTwo { get; private set; }

        /// <summary>Se dispara al morir (la arena suelta la recompensa).</summary>
        public event Action<BossBase> Defeated;

        /// <summary>Multiplicador de velocidad: 1 en la primera fase.</summary>
        protected float Speed => PhaseTwo ? phaseTwoSpeed : 1f;
        protected bool PlayerAlive => player != null && !player.IsDead;
        /// <summary>Centro del cuerpo del jugador.</summary>
        protected Vector2 PlayerPos => player != null ? (Vector2)player.transform.position + Vector2.up * 0.9f : rb.position;
        protected Vector2 PlayerFeet => player != null ? (Vector2)player.transform.position : rb.position;
        /// <summary>Interior de la arena (sin las paredes).</summary>
        protected Rect Arena { get; private set; }
        /// <summary>1 derecha, -1 izquierda.</summary>
        protected int Facing { get; private set; } = -1;

        /// <summary>Animación de la presentación y del enfado.</summary>
        protected virtual string RoarClip => "roar";
        protected virtual string IdleClip => "idle";

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
            anim = GetComponent<SpriteFlipbook>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            player = FindAnyObjectByType<PlayerHealth>();
            health.Died += OnDied;

            var level = FindAnyObjectByType<LevelManager>();
            Rect b = level != null ? level.Bounds : new Rect(transform.position.x - 15f, transform.position.y - 2f, 30f, 14f);
            Arena = new Rect(b.xMin + 1f, b.yMin + 2f, b.width - 2f, b.height - 3f); // paredes, suelo y techo de 1 casilla
        }

        private void Start() => StartCoroutine(Run());

        private void OnDied()
        {
            StopAllCoroutines(); // desactivar el script no para las corrutinas
            if (BossHealthBar.Instance != null) BossHealthBar.Instance.Hide();
            Defeated?.Invoke(this);
        }

        private IEnumerator Run()
        {
            Play(IdleClip);
            FacePlayer();
            yield return new WaitForSeconds(introDelay);

            yield return Roar(roarTime);
            if (BossHealthBar.Instance != null) BossHealthBar.Instance.Show(displayName, health);

            while (true)
            {
                if (!PlayerAlive)
                {
                    Play(IdleClip);
                    yield return null;
                    continue;
                }
                if (!PhaseTwo && health.Current <= health.Max * phaseTwoAt) yield return EnterPhaseTwo();
                yield return NextAttack();
            }
        }

        /// <summary>Un ataque completo (aviso, ataque y descanso).</summary>
        protected abstract IEnumerator NextAttack();

        /// <summary>Al enfurecerse: además del rugido, cada jefe puede hacer algo propio.</summary>
        protected virtual IEnumerator OnPhaseTwo() { yield break; }

        private IEnumerator EnterPhaseTwo()
        {
            PhaseTwo = true;
            if (sprite != null) sprite.color = phaseTwoTint;
            yield return Roar(roarTime * 0.8f);
            yield return OnPhaseTwo();
        }

        private IEnumerator Roar(float seconds)
        {
            health.Invulnerable = true;
            Play(RoarClip, true);
            HitFeedback.Shake(0.25f, seconds * 0.7f);
            for (float t = 0f; t < seconds; t += 0.25f)
            {
                HitFeedback.Burst(BodyCenter, new Color(0.08f, 0.05f, 0.14f), 6, 5f, 0.18f, -1.5f);
                yield return new WaitForSeconds(0.25f);
            }
            health.Invulnerable = false;
            Play(IdleClip);
        }

        // ---------- Utilidades para los ataques ----------

        protected virtual Vector2 BodyCenter => rb.position + Vector2.up * 1f;

        protected void Play(string clip, bool restart = false)
        {
            if (anim != null) anim.Play(clip, restart);
        }

        /// <summary>Mira hacia el jugador (el arte mira a la izquierda).</summary>
        protected void FacePlayer()
        {
            if (player == null) return;
            SetFacing(PlayerFeet.x > rb.position.x ? 1 : -1);
        }

        protected void SetFacing(int dir)
        {
            Facing = dir >= 0 ? 1 : -1;
            if (sprite != null) sprite.flipX = Facing > 0;
        }

        /// <summary>Espera, más corto en la segunda fase.</summary>
        protected WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds / Speed);

        /// <summary>Aviso de ataque: chispas de color y un pequeño temblor.</summary>
        protected void Warn(Vector2 at, Color color)
        {
            HitFeedback.Burst(at, color, 10, 3.5f, 0.12f, -2f);
            HitFeedback.Shake(0.06f, 0.15f);
        }

        /// <summary>Va hacia un punto (sin física: los jefes son seres de sueño). Termina al llegar o al acabarse el tiempo.</summary>
        protected IEnumerator MoveTo(Vector2 target, float speed, float maxTime = 5f)
        {
            target = ClampToArena(target);
            for (float t = 0f; t < maxTime; t += Time.fixedDeltaTime)
            {
                Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
                rb.MovePosition(next);
                if ((next - target).sqrMagnitude < 0.0025f) yield break;
                yield return new WaitForFixedUpdate();
            }
        }

        protected virtual Vector2 ClampToArena(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, Arena.xMin + 1.5f, Arena.xMax - 1.5f), Mathf.Clamp(p.y, Arena.yMin, Arena.yMax - 1f));

        /// <summary>Dispara un orbe de sueño. gravity 0 = recto; 1 = cae en arco.</summary>
        protected DreamOrb Shoot(Vector2 from, Vector2 velocity, float gravity = 0f, float scale = 1.3f)
        {
            if (projectilePrefab == null) return null;
            var orb = Instantiate(projectilePrefab, from, Quaternion.identity);
            orb.transform.localScale = Vector3.one * scale;
            var sr = orb.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = projectileTint;
            orb.Launch(velocity, gravity);
            return orb;
        }

        /// <summary>Anillo de orbes alrededor de un punto. offset en grados.</summary>
        protected void Ring(Vector2 center, int count, float speed, float offset = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (offset + i * 360f / count) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Shoot(center + dir * 0.6f, dir * speed);
            }
        }

        /// <summary>Abanico de orbes hacia una dirección.</summary>
        protected void Fan(Vector2 from, Vector2 dir, int count, float spreadDegrees, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                float k = count == 1 ? 0f : i / (count - 1f) - 0.5f;
                Shoot(from, (Vector2)(Quaternion.Euler(0f, 0f, k * spreadDegrees) * dir.normalized) * speed);
            }
        }

        /// <summary>Elige un ataque al azar sin repetir el anterior.</summary>
        protected int PickAttack(int count)
        {
            if (count <= 1) return 0;
            int pick;
            do pick = Random.Range(0, count);
            while (pick == lastAttack);
            lastAttack = pick;
            return pick;
        }
    }
}
