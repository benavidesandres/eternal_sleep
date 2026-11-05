using System;
using UnityEngine;

namespace EternalSleep
{
    /// <summary>
    /// Animación por cuadros sin Animator.
    /// - Sin clips: reproduce "frames" en bucle (portal, orbe...).
    /// - Con clips: la IA llama a Play("walk"), Play("hurt"), etc.
    /// - Flash(): durante un momento muestra la silueta blanca del cuadro actual (golpe recibido).
    /// </summary>
    public class SpriteFlipbook : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public string name;
            public Sprite[] frames;
            [Tooltip("Siluetas blancas, una por cuadro (opcional).")]
            public Sprite[] flash;
            public float fps = 8f;
            public bool loop = true;
        }

        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private Clip[] clips;

        private SpriteRenderer sprite;
        private float offset;
        private Clip current;
        private float clipStart;
        private float flashUntil;

        public string CurrentClip => current != null ? current.name : null;

        private void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            offset = UnityEngine.Random.Range(0f, 1f); // que no se muevan todos a la vez
            if (clips != null && clips.Length > 0) Play(clips[0].name);
        }

        /// <summary>Cambia de animación (si ya está sonando, no la reinicia).</summary>
        public void Play(string clipName, bool restart = false)
        {
            if (!restart && current != null && current.name == clipName) return;
            if (clips == null) return;
            foreach (var c in clips)
            {
                if (c.name != clipName) continue;
                current = c;
                clipStart = Time.time;
                return;
            }
        }

        /// <summary>Muestra la silueta blanca durante unos segundos.</summary>
        public void Flash(float seconds) => flashUntil = Time.time + seconds;

        public bool HasFlash => current != null && current.flash != null && current.flash.Length == current.frames.Length;

        private void Update()
        {
            if (sprite == null) return;

            if (current == null)
            {
                if (frames == null || frames.Length == 0) return;
                sprite.sprite = frames[(int)((Time.time + offset) * framesPerSecond) % frames.Length];
                return;
            }

            int n = current.frames.Length;
            if (n == 0) return;
            int i = current.loop
                ? (int)((Time.time + offset) * current.fps) % n
                : Mathf.Min((int)((Time.time - clipStart) * current.fps), n - 1);

            bool flashing = Time.time < flashUntil && HasFlash;
            sprite.sprite = flashing ? current.flash[i] : current.frames[i];
        }
    }
}
