using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EternalSleep.Village
{
    /// <summary>Hace que una luz 2D (farol, vela) titile suavemente.</summary>
    [RequireComponent(typeof(Light2D))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] private float amount = 0.15f;
        [SerializeField] private float speed = 6f;

        private Light2D light2D;
        private float baseIntensity;
        private float seed;

        private void Awake()
        {
            light2D = GetComponent<Light2D>();
            baseIntensity = light2D.intensity;
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            light2D.intensity = baseIntensity * (1f + n * 2f * amount);
        }
    }
}
