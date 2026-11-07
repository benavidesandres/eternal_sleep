using System.Collections;
using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Enemies
{
    /// <summary>
    /// Jefe 1: "Insomnio, la Bestia sin Párpados". Camina por el suelo de la arena.
    ///
    /// Ataques:
    ///  Embestida → se agacha y cruza la arena; al chocar con la pared queda aturdido (momento para golpearla).
    ///              En la segunda fase rebota y vuelve a embestir.
    ///  Salto     → salta hacia el jugador y al caer lanza dos ondas por el suelo (hay que saltarlas).
    ///              En la segunda fase salta dos veces seguidas.
    ///  Rugido    → lanza púas de sombra hacia arriba que caen en arco. En la segunda fase, dos tandas.
    ///  Furia     → (solo segunda fase) embiste y, sin parar, salta de vuelta.
    /// </summary>
    public class BossBeast : BossBase
    {
        [SerializeField] private float walkSpeed = 2.4f;
        [SerializeField] private float chargeSpeed = 12f;
        [SerializeField] private float chargeWindup = 0.7f;
        [SerializeField] private float stunTime = 1.1f;
        [SerializeField] private float leapTime = 0.85f;
        [SerializeField] private float leapHeight = 5.5f;
        [SerializeField] private float waveSpeed = 7f;
        [SerializeField] private float shardSpeed = 9f;
        [Tooltip("Medio ancho del cuerpo (para no meterse en las paredes).")]
        [SerializeField] private float halfWidth = 1.6f;

        private static readonly Color WarnColor = new Color(0.91f, 0.42f, 0.57f);
        private float groundY;

        protected override Vector2 BodyCenter => rb.position + Vector2.up * 1.4f;

        protected override void Awake()
        {
            base.Awake();
            groundY = transform.position.y;
        }

        protected override IEnumerator NextAttack()
        {
            yield return Stalk(Random.Range(0.7f, 1.3f) / Speed);
            switch (PickAttack(PhaseTwo ? 4 : 3))
            {
                case 0: yield return Charge(PhaseTwo ? 2 : 1); break;
                case 1: yield return Leap(PhaseTwo ? 2 : 1); break;
                case 2: yield return Howl(PhaseTwo ? 2 : 1); break;
                default:
                    yield return Charge(1);
                    yield return Leap(1);
                    break;
            }
            Play(IdleClip);
            yield return Wait(0.6f);
        }

        /// <summary>Se acerca al jugador caminando (se para si ya está cerca).</summary>
        private IEnumerator Stalk(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
            {
                FacePlayer();
                float dx = PlayerFeet.x - rb.position.x;
                bool close = Mathf.Abs(dx) < 3f;
                Play(close ? IdleClip : "walk");
                if (!close) rb.MovePosition(new Vector2(ClampX(rb.position.x + Facing * walkSpeed * Speed * Time.fixedDeltaTime), groundY));
                yield return new WaitForFixedUpdate();
            }
        }

        private IEnumerator Charge(int times)
        {
            FacePlayer();
            for (int n = 0; n < times; n++)
            {
                Play("windup", true);
                Warn(BodyCenter + Vector2.right * (Facing * 1.5f), WarnColor);
                yield return Wait(chargeWindup);

                Play("charge");
                float wall = Facing > 0 ? Arena.xMax - halfWidth : Arena.xMin + halfWidth;
                while (Mathf.Abs(rb.position.x - wall) > 0.05f)
                {
                    float x = Mathf.MoveTowards(rb.position.x, wall, chargeSpeed * Speed * Time.fixedDeltaTime);
                    rb.MovePosition(new Vector2(x, groundY));
                    if (Random.value < 0.4f) HitFeedback.Burst(rb.position, new Color(0.1f, 0.07f, 0.16f), 1, 2f, 0.15f, -1f);
                    yield return new WaitForFixedUpdate();
                }

                // Choca con la pared.
                HitFeedback.Shake(0.35f, 0.3f);
                HitFeedback.Burst(rb.position + new Vector2(Facing * halfWidth, 1.2f), new Color(0.75f, 0.7f, 0.9f), 16, 6f);
                Shoot(new Vector2(rb.position.x, Arena.yMax - 0.5f), Vector2.down * 3f, 0.5f); // cae algo del techo
                SetFacing(-Facing);

                bool last = n == times - 1;
                Play("hurt"); // aturdido
                yield return new WaitForSeconds(last ? stunTime : stunTime * 0.35f);
            }
        }

        private IEnumerator Leap(int times)
        {
            for (int n = 0; n < times; n++)
            {
                FacePlayer();
                Play("windup", true);
                Warn(rb.position, WarnColor);
                yield return Wait(0.45f);

                float fromX = rb.position.x;
                float toX = ClampX(PlayerFeet.x);
                float time = leapTime / Mathf.Sqrt(Speed);
                Play("leap");
                for (float t = 0f; t < time; t += Time.fixedDeltaTime)
                {
                    float k = t / time;
                    float y = groundY + leapHeight * 4f * k * (1f - k);
                    rb.MovePosition(new Vector2(Mathf.Lerp(fromX, toX, k), y));
                    yield return new WaitForFixedUpdate();
                }
                rb.MovePosition(new Vector2(toX, groundY));

                // Aterriza: ondas por el suelo hacia los dos lados.
                HitFeedback.Shake(0.4f, 0.3f);
                HitFeedback.Burst(new Vector2(toX, groundY + 0.2f), new Color(0.55f, 0.45f, 0.8f), 20, 7f);
                foreach (int side in new[] { -1, 1 })
                    Shoot(new Vector2(toX + side * halfWidth, groundY + 0.45f), Vector2.right * (side * waveSpeed * Speed), 0f, 1.8f);
                Play(IdleClip);
                yield return Wait(n == times - 1 ? 0.7f : 0.35f);
            }
        }

        private IEnumerator Howl(int volleys)
        {
            FacePlayer();
            for (int v = 0; v < volleys; v++)
            {
                Play("roar", true);
                Vector2 head = rb.position + new Vector2(Facing * 1.2f, 2.4f);
                Warn(head, WarnColor);
                yield return Wait(0.5f);

                // Púas en abanico hacia arriba; caen en arco por toda la arena.
                int count = PhaseTwo ? 8 : 6;
                float offset = v % 2 == 0 ? 0f : 0.5f;
                HitFeedback.Shake(0.2f, 0.4f);
                for (int i = 0; i < count; i++)
                {
                    float k = (i + offset) / count;               // 0..1 de un lado al otro
                    float angle = Mathf.Lerp(150f, 30f, k) * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Shoot(head, dir * shardSpeed * Random.Range(0.85f, 1.1f), 1f);
                }
                yield return Wait(0.6f);
            }
        }

        private float ClampX(float x) => Mathf.Clamp(x, Arena.xMin + halfWidth, Arena.xMax - halfWidth);
    }
}
