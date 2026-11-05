using UnityEngine;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Marca un objeto del sueño (plataforma, enemigo, decoración...) como opcional:
    /// cada vez que se entra al sueño aparece solo con esta probabilidad.
    /// Lo aplica DreamWorldManager; fuera de un sueño el objeto se queda como está.
    /// </summary>
    public class DreamVariant : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.5f;

        public float Chance => chance;
    }
}
