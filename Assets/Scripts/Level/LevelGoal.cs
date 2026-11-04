using EternalSleep.Player;
using UnityEngine;

namespace EternalSleep.Level
{
    /// <summary>
    /// Portal de salida: al tocarlo, el jugador pasa al siguiente nivel.
    /// El collider debe ser trigger.
    /// </summary>
    public class LevelGoal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            var level = FindAnyObjectByType<LevelManager>();
            if (level != null) level.CompleteLevel();
        }
    }
}
