using EternalSleep.Effects;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// Corazón de Luna: aumenta la vida máxima en un corazón. Solo se recoge una vez por partida.
    /// Necesita un Collider2D trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartPickup : MonoBehaviour
    {
        [SerializeField] private string id = "corazon";
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 start;

        private void Awake() => start = transform.position;

        private void Start()
        {
            if (PlayerUpgrades.IsHeartClaimed(id)) gameObject.SetActive(false);
        }

        private void Update()
        {
            transform.position = start + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            PlayerUpgrades.ClaimHeart(id);
            HitFeedback.Burst(transform.position, new Color(1f, 0.55f, 0.7f), 18, 4f, 0.1f, -2f);
            if (DialogueBox.Instance != null)
                DialogueBox.Instance.Show("Nueva mejora", PlayerUpgrades.Describe(Upgrade.ExtraHeart), null, null);
            gameObject.SetActive(false);
        }
    }
}
