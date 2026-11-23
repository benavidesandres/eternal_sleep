using System.Collections;
using System.Collections.Generic;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalSleep.Effects
{
    /// <summary>
    /// Música y efectos de sonido. Se crea solo al arrancar el juego y sobrevive a los cambios de escena.
    /// - Los sonidos están en Resources/Audio (los genera Eternal Sleep > Generar sonidos). Si falta uno, no suena y ya.
    /// - Play("salto") lanza un efecto desde cualquier sitio.
    /// - La música cambia sola según la escena, con un fundido; en pausa baja a la mitad.
    /// - Volúmenes de GameSettings (el general ya lo aplica AudioListener.volume).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int Voices = 10;
        private const float MusicFade = 1f;

        private static AudioManager instance;

        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        private AudioSource[] sfx;
        private int nextVoice;
        private AudioSource musicA, musicB;
        private float fadeA, fadeB;
        private Coroutine crossfade;
        private string currentMusic;

        /// <summary>Música que se fuerza en vez de la de la escena (el final del juego). null = la de la escena.</summary>
        private string overrideMusic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            sfx = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++) sfx[i] = NewSource(false);
            musicA = NewSource(true);
            musicB = NewSource(true);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        private AudioSource NewSource(bool music)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = music;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        // ---------- Efectos ----------

        /// <param name="name">Nombre del archivo en Resources/Audio, sin extensión.</param>
        /// <param name="pitchVariation">Variación de tono al azar, para que no suene siempre igual.</param>
        /// <param name="minInterval">No repetir el mismo sonido antes de estos segundos (evita amontonarlos).</param>
        public static void Play(string name, float pitchVariation = 0.05f, float volume = 1f, float minInterval = 0.03f)
        {
            if (instance == null) return;
            instance.PlaySfx(name, pitchVariation, volume, minInterval);
        }

        private void PlaySfx(string name, float pitchVariation, float volume, float minInterval)
        {
            var clip = Clip(name);
            if (clip == null) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(name, out float last) && now - last < minInterval) return;
            lastPlayed[name] = now;

            var source = sfx[nextVoice];
            nextVoice = (nextVoice + 1) % Voices;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }

        private AudioClip Clip(string name)
        {
            if (!clips.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + name);
                clips[name] = clip; // también los null: no se vuelve a buscar
            }
            return clip;
        }

        // ---------- Música ----------

        /// <summary>Pone una música concreta hasta que se llame a ClearMusicOverride (o null para la de la escena).</summary>
        public static void OverrideMusic(string name)
        {
            if (instance == null) return;
            instance.overrideMusic = name;
            instance.PlayMusic(name ?? MusicFor(SceneManager.GetActiveScene().name));
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            overrideMusic = null;
            PlayMusic(MusicFor(scene.name));
            AttachPlayerSounds();
        }

        private static string MusicFor(string scene)
        {
            switch (scene)
            {
                case "MainMenu": return "musica_menu";
                case "Hub": return "musica_pueblo";
                case "Arena": return "musica_jefe";
                default: return "musica_sueno"; // Sueno y las Level_XX
            }
        }

        private void PlayMusic(string name)
        {
            if (name == currentMusic) return;
            currentMusic = name;
            var clip = Clip(name);
            if (crossfade != null) StopCoroutine(crossfade);
            crossfade = StartCoroutine(Crossfade(clip));
        }

        /// <summary>La música que suena baja mientras la nueva sube (en tiempo real: funciona en pausa).</summary>
        private IEnumerator Crossfade(AudioClip clip)
        {
            bool toA = fadeA <= fadeB; // entra por la fuente que suena menos
            var incoming = toA ? musicA : musicB;
            float startIn = toA ? fadeA : fadeB, startOut = toA ? fadeB : fadeA;
            if (clip != null && incoming.clip != clip)
            {
                incoming.clip = clip;
                incoming.Play();
            }

            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / MusicFade)
            {
                SetFades(toA, Mathf.Lerp(startIn, clip != null ? 1f : 0f, t), Mathf.Lerp(startOut, 0f, t));
                yield return null;
            }
            SetFades(toA, clip != null ? 1f : 0f, 0f);
            var outgoing = toA ? musicB : musicA;
            outgoing.Stop();
            outgoing.clip = null;
            crossfade = null;
        }

        private void SetFades(bool toA, float incoming, float outgoing)
        {
            if (toA) { fadeA = incoming; fadeB = outgoing; }
            else { fadeB = incoming; fadeA = outgoing; }
        }

        private void Update()
        {
            float vol = GameSettings.MusicVolume * (PauseMenu.IsPaused ? 0.5f : 1f) * 0.6f;
            musicA.volume = vol * fadeA;
            musicB.volume = vol * fadeB;
        }

        // ---------- Jugador ----------

        private static void AttachPlayerSounds()
        {
            var motor = FindAnyObjectByType<PlayerMotor>();
            if (motor != null && motor.GetComponent<PlayerSounds>() == null) motor.gameObject.AddComponent<PlayerSounds>();
        }
    }
}
