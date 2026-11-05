using EternalSleep.Effects;
using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// Lágrima de Luna: flota, brilla y se recoge al tocarla.
    /// Si ya se recogió en esta partida, no vuelve a aparecer.
    /// Necesita un Collider2D trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private string id = "lagrima";
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2.2f;
        [SerializeField] private Color burstColor = new Color(0.75f, 0.9f, 1f);

        private Vector3 start;
        private float phase;

        public string Id => id;

        private void Awake()
        {
            start = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Start()
        {
            if (CollectibleTracker.IsCollected(id)) gameObject.SetActive(false);
        }

        private void Update()
        {
            transform.position = start + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            CollectibleTracker.Collect(id);
            HitFeedback.Burst(transform.position, burstColor, 14, 4f, 0.1f, -2f);
            HitFeedback.Burst(transform.position, Color.white, 6, 2.5f, 0.08f, -3f);
            gameObject.SetActive(false);
        }
    }
}
