using System.Collections;
using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Jefe 2: "La Reina de las Polillas". Vuela por la parte alta de la arena.
    ///
    /// Ataques:
    ///  Picada   → tiembla, se lanza hacia el jugador y vuelve a subir. En la segunda fase, tres seguidas.
    ///  Lluvia   → cruza el techo soltando polvo que cae en línea; siempre deja un hueco libre.
    ///  Esporas  → anillos de orbes que se abren desde su cuerpo, cada uno girado respecto al anterior.
    ///  Danza    → (solo segunda fase) da vueltas alrededor del centro de la arena disparando al jugador.
    /// Al enfurecerse suelta un anillo grande.
    /// </summary>
    public class BossMoth : BossBase
    {
        [SerializeField] private float flySpeed = 6f;
        [SerializeField] private float diveSpeed = 15f;
        [SerializeField] private float diveWindup = 0.65f;
        [SerializeField] private float hoverBelowCeiling = 3f;
        [SerializeField] private float dustSpeed = 5f;
        [SerializeField] private float sporeSpeed = 4f;

        private static readonly Color WarnColor = new Color(1f, 0.89f, 0.54f);

        protected override string RoarClip => "cast";
        protected override string IdleClip => "fly";
        protected override Vector2 BodyCenter => rb.position;

        private float HoverY => Arena.yMax - hoverBelowCeiling;

        // Es grande: no se acerca tanto al techo ni a las paredes.
        protected override Vector2 ClampToArena(Vector2 p) =>
            new Vector2(Mathf.Clamp(p.x, Arena.xMin + 2f, Arena.xMax - 2f), Mathf.Clamp(p.y, Arena.yMin + 1.5f, Arena.yMax - 2f));

        protected override IEnumerator NextAttack()
        {
            // Se recoloca sobre el jugador, un poco desplazada.
            Play(IdleClip);
            float x = PlayerFeet.x + Random.Range(-4f, 4f);
            yield return Fly(new Vector2(x, HoverY), flySpeed);
            yield return Hover(0.5f);

            switch (PickAttack(PhaseTwo ? 4 : 3))
            {
                case 0: yield return Dive(PhaseTwo ? 3 : 1); break;
                case 1: yield return DustRain(PhaseTwo ? 2 : 1); break;
                case 2: yield return Spores(PhaseTwo ? 4 : 3); break;
                default: yield return Dance(); break;
            }
            Play(IdleClip);
            yield return Hover(0.6f);
        }

        protected override IEnumerator OnPhaseTwo()
        {
            Ring(BodyCenter, 18, sporeSpeed * 0.8f);
            yield return Hover(0.5f);
        }

        private IEnumerator Fly(Vector2 target, float speed)
        {
            FaceTowards(target.x);
            yield return MoveTo(target, speed * Speed, 3f);
        }

        /// <summary>Flota en el sitio con un vaivén suave.</summary>
        private IEnumerator Hover(float seconds)
        {
            Vector2 center = rb.position;
            for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
            {
                FacePlayer();
                rb.MovePosition(center + Vector2.up * (Mathf.Sin(t * 5f) * 0.25f));
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator Dive(int times)
        {
            for (int n = 0; n < times; n++)
            {
                FacePlayer();
                Play("windup", true);
                Warn(BodyCenter, WarnColor);
                Vector2 start = rb.position;
                for (float t = 0f; t < diveWindup / Speed; t += Time.fixedDeltaTime)
                {
                    rb.MovePosition(start + Random.insideUnitCircle * 0.08f); // tiembla para avisar
                    yield return new WaitForFixedUpdate();
                }

                // Se lanza hacia donde está el jugador y sigue un poco más allá.
                Vector2 target = PlayerPos;
                Vector2 dir = (target - start).normalized;
                Vector2 end = target + dir * 2f;
                Play("swoop");
                FaceTowards(end.x);
                yield return MoveTo(end, diveSpeed * Speed, 1.2f);
                HitFeedback.Burst(BodyCenter, new Color(0.8f, 0.75f, 1f), 8, 4f, 0.1f, -1f);

                Play(IdleClip);
                yield return Fly(new Vector2(rb.position.x, HoverY), flySpeed * 0.8f);
            }
        }

        private IEnumerator DustRain(int sweeps)
        {
            for (int s = 0; s < sweeps; s++)
            {
                // Cruza el techo de un lado a otro soltando polvo; un tramo queda libre.
                bool leftToRight = (s % 2 == 0) == (rb.position.x < Arena.center.x);
                float fromX = leftToRight ? Arena.xMin + 2f : Arena.xMax - 2f;
                float toX = leftToRight ? Arena.xMax - 2f : Arena.xMin + 2f;
                float y = Arena.yMax - 1.5f;
                yield return Fly(new Vector2(fromX, y), flySpeed * 1.3f);

                Play("cast", true);
                Warn(BodyCenter, WarnColor);
                yield return Wait(0.35f);
                Play(IdleClip);

                const int drops = 12;
                int gap = Random.Range(2, drops - 3);
                FaceTowards(toX);
                for (int i = 0; i <= drops; i++)
                {
                    float x = Mathf.Lerp(fromX, toX, i / (float)drops);
                    yield return MoveTo(new Vector2(x, y), flySpeed * 1.6f * Speed, 1f);
                    if (i < gap || i > gap + 1) Shoot(BodyCenter + Vector2.down * 0.8f, Vector2.down * dustSpeed);
                }
            }
        }

        private IEnumerator Spores(int rings)
        {
            yield return Fly(new Vector2(Mathf.Clamp(rb.position.x, Arena.xMin + 5f, Arena.xMax - 5f), HoverY - 1f), flySpeed);
            Play("cast", true);
            Warn(BodyCenter, WarnColor);
            yield return Wait(0.5f);

            int count = PhaseTwo ? 14 : 10;
            for (int r = 0; r < rings; r++)
            {
                Ring(BodyCenter, count, sporeSpeed, r * 180f / count);
                HitFeedback.Burst(BodyCenter, WarnColor, 6, 3f);
                yield return Wait(0.55f);
            }
        }

        /// <summary>Vuela en círculo alrededor del centro y dispara al jugador cada poco.</summary>
        private IEnumerator Dance()
        {
            Vector2 center = new Vector2(Arena.center.x, Arena.yMax - 4.5f);
            const float radius = 4f;
            yield return Fly(center + Vector2.right * radius, flySpeed * 1.3f);
            Play("cast");

            const float duration = 3.2f;
            float nextShot = 0.4f;
            for (float t = 0f; t < duration; t += Time.fixedDeltaTime)
            {
                float a = t / duration * Mathf.PI * 2f;
                rb.MovePosition(center + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * 0.6f));
                FacePlayer();
                if (t >= nextShot)
                {
                    nextShot += 0.45f;
                    Vector2 dir = (PlayerPos - BodyCenter).normalized;
                    Fan(BodyCenter, dir, 3, 24f, sporeSpeed * 1.4f);
                }
                yield return new WaitForFixedUpdate();
            }
        }

        private void FaceTowards(float x)
        {
            if (Mathf.Abs(x - rb.position.x) > 0.1f) SetFacing(x > rb.position.x ? 1 : -1);
        }
    }
}
