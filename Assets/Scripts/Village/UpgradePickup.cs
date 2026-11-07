using EternalSleep.Effects;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// Objeto del mapa que da una mejora (salto de pared, golpe al suelo, ataque cargado...).
    /// Solo se recoge una vez por partida. Necesita un Collider2D trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpgradePickup : MonoBehaviour
    {
        [SerializeField] private Upgrade upgrade = Upgrade.WallJump;
        [Tooltip("Identificador único en la partida. Vacío = el nombre de la mejora.")]
        [SerializeField] private string id = "";
        [SerializeField] private Color burstColor = new Color(0.7f, 0.6f, 1f);
        [SerializeField] private float bobHeight = 0.12f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 start;

        private string Id => string.IsNullOrEmpty(id) ? upgrade.ToString() : id;

        private void Awake() => start = transform.position;

        private void Start()
        {
            if (PlayerUpgrades.IsClaimed(Id)) gameObject.SetActive(false);
        }

        private void Update()
        {
            transform.position = start + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            PlayerUpgrades.Claim(Id, upgrade);
            HitFeedback.Burst(transform.position, burstColor, 18, 4f, 0.1f, -2f);
            if (DialogueBox.Instance != null)
                DialogueBox.Instance.Show("Nueva mejora", PlayerUpgrades.Describe(upgrade), null, null);
            gameObject.SetActive(false);
        }
    }
}
