using UnityEngine;

namespace EternalSleep
{
    /// <summary>
    /// Animación sencilla por cuadros: va cambiando el sprite cada cierto tiempo.
    /// Sirve para enemigos, el portal y cualquier objeto con pocos cuadros.
    /// </summary>
    public class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;

        private SpriteRenderer sprite;
        private float offset;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            offset = Random.Range(0f, 1f); // que no se muevan todos a la vez
        }

        private void Update()
        {
            if (sprite == null || frames == null || frames.Length == 0) return;
            int i = (int)((Time.time + offset) * framesPerSecond) % frames.Length;
            sprite.sprite = frames[i];
        }
    }
}
