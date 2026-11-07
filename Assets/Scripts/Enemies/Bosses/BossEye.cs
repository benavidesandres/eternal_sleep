using System.Collections;
using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Jefe 3: "El Ojo que no Duerme". Flota y salta de un punto a otro de la arena
    /// (se cierra y desaparece; mientras está cerrado no se le puede dañar).
    ///
    /// Ataques:
    ///  Ráfaga   → tres disparos rápidos apuntando por delante del jugador (cinco en la segunda fase).
    ///  Abanico  → un abanico ancho de orbes; en la segunda fase, dos abanicos cruzados.
    ///  Anillo   → un anillo de orbes con un hueco hacia el jugador por donde escapar.
    ///  Espiral  → (solo segunda fase) gira disparando una espiral durante unos segundos.
    /// </summary>
    public class BossEye : BossBase
    {
        [SerializeField] private float shotSpeed = 7f;
        [SerializeField] private float fadeTime = 0.3f;
        [SerializeField] private float aimTime = 0.6f;
        [Tooltip("Cuánto se adelanta al movimiento del jugador.")]
        [SerializeField, Range(0f, 1f)] private float leadFactor = 0.6f;

        private static readonly Color WarnColor = new Color(0.91f, 0.42f, 0.57f);
        private Vector2 lastPlayerPos;
        private Vector2 playerVelocity;
        private int spot = -1;

        protected override string RoarClip => "aim";
        protected override Vector2 BodyCenter => rb.position;

        protected override Vector2 ClampToArena(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, Arena.xMin + 2f, Arena.xMax - 2f), Mathf.Clamp(p.y, Arena.yMin + 2f, Arena.yMax - 2f));

        private void FixedUpdate()
        {
            // Estima la velocidad del jugador para apuntar por delante.
            Vector2 p = PlayerPos;
            playerVelocity = Vector2.Lerp(playerVelocity, (p - lastPlayerPos) / Time.fixedDeltaTime, 0.2f);
            lastPlayerPos = p;
        }

        protected override IEnumerator NextAttack()
        {
            yield return Teleport();
            yield return Float(0.4f);

            switch (PickAttack(PhaseTwo ? 4 : 3))
            {
                case 0: yield return Volley(PhaseTwo ? 5 : 3); break;
                case 1: yield return Fans(PhaseTwo ? 2 : 1); break;
                case 2: yield return GapRing(); break;
                default: yield return Spiral(); break;
            }
            Play(IdleClip);
            yield return Float(0.7f);
        }

        protected override IEnumerator OnPhaseTwo()
        {
            Ring(BodyCenter, 20, shotSpeed * 0.6f);
            yield return Float(0.5f);
        }

        /// <summary>Puntos a los que salta: arriba a los lados, arriba en el centro y abajo a los lados.</summary>
        private Vector2 Spot(int i)
        {
            Rect a = Arena;
            switch (i)
            {
                case 0: return new Vector2(a.xMin + 4f, a.yMax - 3f);
                case 1: return new Vector2(a.center.x, a.yMax - 2.5f);
                case 2: return new Vector2(a.xMax - 4f, a.yMax - 3f);
                case 3: return new Vector2(a.xMin + 5f, a.yMin + 4f);
                default: return new Vector2(a.xMax - 5f, a.yMin + 4f);
            }
        }

        private IEnumerator Teleport()
        {
            health.Invulnerable = true;
            Play("close", true);
            yield return Fade(1f, 0.15f);

            // Nunca aparece encima del jugador.
            int next = (spot + 1) % 5;
            for (int tries = 0; tries < 20; tries++)
            {
                int pick = Random.Range(0, 5);
                if (pick == spot || Vector2.Distance(Spot(pick), PlayerPos) < 4f) continue;
                next = pick;
                break;
            }
            spot = next;
            rb.position = ClampToArena(Spot(spot));
            HitFeedback.Burst(rb.position, WarnColor, 10, 3f, 0.12f, -2f);

            yield return Fade(0.15f, 1f);
            Play(IdleClip);
            health.Invulnerable = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (sprite == null) yield break;
            float time = fadeTime / Speed;
            for (float t = 0f; t < time; t += Time.deltaTime)
            {
                SetAlpha(Mathf.Lerp(from, to, t / time));
                yield return null;
            }
            SetAlpha(to);
        }

        private void SetAlpha(float a)
        {
            var c = sprite.color;
            c.a = a;
            sprite.color = c;
        }

        /// <summary>Flota en el sitio mirando al jugador.</summary>
        private IEnumerator Float(float seconds)
        {
            Vector2 center = rb.position;
            for (float t = 0f; t < seconds / Speed; t += Time.fixedDeltaTime)
            {
                FacePlayer();
                rb.MovePosition(center + Vector2.up * (Mathf.Sin(Time.time * 3f) * 0.2f));
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator Aim()
        {
            FacePlayer();
            Play("aim", true);
            Warn(BodyCenter, WarnColor);
            yield return Wait(aimTime);
        }

        private Vector2 Lead(Vector2 from, float speed)
        {
            Vector2 target = PlayerPos;
            float flight = Vector2.Distance(from, target) / speed;
            return (target + playerVelocity * (flight * leadFactor) - from).normalized;
        }

        private IEnumerator Volley(int shots)
        {
            yield return Aim();
            for (int i = 0; i < shots; i++)
            {
                Play("shoot", true);
                Shoot(BodyCenter, Lead(BodyCenter, shotSpeed) * shotSpeed, 0f, 1.5f);
                yield return Wait(0.22f);
                Play("aim");
            }
        }

        private IEnumerator Fans(int count)
        {
            yield return Aim();
            for (int i = 0; i < count; i++)
            {
                Play("shoot", true);
                Vector2 dir = (PlayerPos - BodyCenter).normalized;
                if (i % 2 == 1) dir = Quaternion.Euler(0f, 0f, 7f) * dir; // el segundo, cruzado con el primero
                Fan(BodyCenter, dir, PhaseTwo ? 9 : 7, 100f, shotSpeed * 0.75f);
                HitFeedback.Shake(0.1f, 0.15f);
                yield return Wait(0.5f);
            }
        }

        /// <summary>Anillo con un hueco de 3 orbes en dirección al jugador.</summary>
        private IEnumerator GapRing()
        {
            yield return Aim();
            Play("shoot", true);
            const int count = 18;
            Vector2 toPlayer = PlayerPos - BodyCenter;
            float playerAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float angle = playerAngle + i * 360f / count;
                if (i == 0 || i == 1 || i == count - 1) continue; // el hueco
                float a = angle * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Shoot(BodyCenter + dir * 0.8f, dir * shotSpeed * 0.6f);
            }
            HitFeedback.Shake(0.15f, 0.2f);
            yield return Wait(0.6f);
        }

        private IEnumerator Spiral()
        {
            yield return Aim();
            Play("shoot");
            const float duration = 2.6f;
            float angle = Random.Range(0f, 360f);
            for (float t = 0f; t < duration; t += 0.09f)
            {
                foreach (float arm in new[] { 0f, 180f })
                {
                    float a = (angle + arm) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Shoot(BodyCenter + dir * 0.8f, dir * shotSpeed * 0.55f);
                }
                angle += 17f;
                yield return new WaitForSeconds(0.09f);
            }
        }
    }
}
