using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Muestra la vida del jugador como corazones (llenos o vacíos).
    /// </summary>
    public class HealthHUD : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image[] hearts;
        [SerializeField] private Sprite fullHeart;
        [SerializeField] private Sprite emptyHeart;

        private PlayerHealth player;

        private void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            if (player == null) return;
            player.HealthChanged += Refresh;
            Refresh(player.Current, player.Max);
        }

        private void OnDestroy()
        {
            if (player != null) player.HealthChanged -= Refresh;
        }

        private void Refresh(int current, int max)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                if (hearts[i] == null) continue;
                hearts[i].gameObject.SetActive(i < max);
                hearts[i].sprite = i < current ? fullHeart : emptyHeart;
            }
        }
    }
}
