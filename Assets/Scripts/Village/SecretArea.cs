using EternalSleep.Effects;
using EternalSleep.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace EternalSleep.Village
{
    /// <summary>
    /// Pared falsa: sus casillas son suelo sin colisión, idénticas al suelo de verdad.
    /// Cuando el jugador entra, se vuelven casi transparentes y dejan ver la sala secreta.
    /// La primera vez suelta destellos dorados. Necesita un Collider2D trigger que cubra las casillas.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SecretArea : MonoBehaviour
    {
        [SerializeField] private Tilemap wall;
        [SerializeField] private Vector3Int[] cells;
        [SerializeField] private string secretId = "secreto";
        [SerializeField, Range(0f, 1f)] private float revealedAlpha = 0.2f;

        private bool inside;
        private float alpha = 1f;

        private void Start()
        {
            if (wall == null || cells == null) return;
            foreach (var c in cells) wall.SetTileFlags(c, TileFlags.None); // permite cambiar el color de cada casilla
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            inside = true;
            if (CollectibleTracker.DiscoverSecret(secretId))
            {
                var center = GetComponent<Collider2D>().bounds.center;
                HitFeedback.Burst(center, new Color(1f, 0.89f, 0.54f), 24, 5f, 0.1f, -2f);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() != null) inside = false;
        }

        private void Update()
        {
            if (wall == null || cells == null) return;
            float target = inside ? revealedAlpha : 1f;
            if (Mathf.Approximately(alpha, target)) return;
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * 3f);
            var color = new Color(1f, 1f, 1f, alpha);
            foreach (var c in cells) wall.SetColor(c, color);
        }
    }
}
