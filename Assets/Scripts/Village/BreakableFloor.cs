using EternalSleep.Effects;
using UnityEngine;

namespace EternalSleep.Village
{
    /// <summary>
    /// Suelo agrietado: un bloque sólido que solo se rompe con el Peso del Sueño (golpe al suelo).
    /// PlayerAttack llama a Break() al impactar encima. Al recargar la escena vuelve a estar entero.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BreakableFloor : MonoBehaviour
    {
        [SerializeField] private Color debrisColor = new Color(0.45f, 0.38f, 0.55f);

        public void Break()
        {
            var b = GetComponent<Collider2D>().bounds;
            HitFeedback.Burst(b.center, debrisColor, 24, 6f, 0.14f, 12f);
            HitFeedback.Shake(0.15f, 0.2f);
            gameObject.SetActive(false);
        }
    }
}
