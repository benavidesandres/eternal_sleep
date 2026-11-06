using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Player
{
    /// <summary>
    /// Solo presentación de las mejoras: un aleteo de polvo en el doble salto
    /// y copias de sombra que se desvanecen detrás del impulso.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerAbilityFeedback : MonoBehaviour
    {
        [SerializeField] private float ghostInterval = 0.03f;
        [SerializeField] private float ghostLife = 0.28f;
        [SerializeField] private Color ghostColor = new Color(0.55f, 0.42f, 0.95f, 0.6f);
        [SerializeField] private Color dustColor = new Color(0.8f, 0.75f, 1f);

        private PlayerMotor motor;
        private SpriteRenderer sprite;
        private float nextGhost;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            sprite = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            motor.AirJumped += OnAirJumped;
            motor.Dashed += OnDashed;
        }

        private void OnDisable()
        {
            motor.AirJumped -= OnAirJumped;
            motor.Dashed -= OnDashed;
        }

        private void OnAirJumped() => HitFeedback.Burst(transform.position, dustColor, 12, 3.5f, 0.09f, 5f);

        private void OnDashed()
        {
            HitFeedback.Burst(transform.position + Vector3.up * 0.9f, ghostColor, 8, 2.5f, 0.09f, 0f);
            nextGhost = 0f;
        }

        private void Update()
        {
            if (!motor.IsDashing || sprite == null || Time.time < nextGhost) return;
            nextGhost = Time.time + ghostInterval;

            var go = new GameObject("SombraDeKael");
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.transform.localScale = transform.lossyScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite.sprite;
            sr.flipX = sprite.flipX;
            sr.sortingOrder = sprite.sortingOrder - 1;
            sr.color = ghostColor;
            go.AddComponent<Ghost>().life = ghostLife;
        }

        private class Ghost : MonoBehaviour
        {
            public float life;

            private SpriteRenderer sr;
            private float startAlpha;
            private float age;

            private void Start()
            {
                sr = GetComponent<SpriteRenderer>();
                startAlpha = sr.color.a;
            }

            private void Update()
            {
                age += Time.deltaTime;
                var c = sr.color;
                c.a = startAlpha * (1f - age / life);
                sr.color = c;
                if (age >= life) Destroy(gameObject);
            }
        }
    }
}
