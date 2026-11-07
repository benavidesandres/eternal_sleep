using System.Collections;
using EternalSleep.CameraSystem;
using EternalSleep.Dream;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalSleep.Level
{
    /// <summary>
    /// Controla un nivel:
    /// - Si el jugador cae por debajo del nivel, cuenta como peligro (quita vida y lo devuelve).
    /// - Si el jugador muere, reinicia el nivel.
    /// - CompleteLevel() carga el siguiente nivel según el orden de Build Settings;
    ///   después del último vuelve al menú (índice 0).
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Tooltip("Límites del nivel en unidades del mundo (la cámara no muestra fuera de aquí).")]
        [SerializeField] private Rect bounds = new Rect(0, 0, 40, 20);
        [Tooltip("Cuánto por debajo del borde inferior se considera que el jugador cayó al vacío.")]
        [SerializeField] private float fallMargin = 2f;
        [SerializeField] private float restartDelay = 1.2f;
        [SerializeField] private float nextLevelDelay = 0.6f;

        private PlayerHealth player;
        private bool changingScene;

        public Rect Bounds => bounds;

        /// <summary>Cambia los límites (sueños generados al cargar la escena) y mueve las paredes invisibles.</summary>
        public void SetBounds(Rect area)
        {
            bounds = area;
            foreach (Transform wall in transform)
            {
                float x = wall.name == "LeftWall" ? area.xMin - 0.5f : wall.name == "RightWall" ? area.xMax + 0.5f : wall.position.x;
                wall.position = new Vector3(x, area.center.y + 5f, 0f);
                var box = wall.GetComponent<BoxCollider2D>();
                if (box != null) box.size = new Vector2(1f, area.height + 30f);
            }
            var cam = FindAnyObjectByType<CameraFollow2D>();
            if (cam != null) cam.SetBounds(area);
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>();
            if (player != null) player.Died += OnPlayerDied;

            var cam = FindAnyObjectByType<CameraFollow2D>();
            if (cam != null) cam.SetBounds(bounds);
        }

        private void OnDestroy()
        {
            if (player != null) player.Died -= OnPlayerDied;
        }

        private void Update()
        {
            if (player == null || player.IsDead || changingScene) return;
            if (player.transform.position.y < bounds.yMin - fallMargin) player.HazardHit();
        }

        private void OnPlayerDied()
        {
            if (changingScene) return;
            changingScene = true;
            if (DeathScreen.Instance != null) { DeathScreen.Instance.Show(); return; } // la pantalla decide qué hacer
            StartCoroutine(LoadAfter(SceneManager.GetActiveScene().buildIndex, restartDelay));
        }

        public void CompleteLevel()
        {
            if (changingScene) return;
            if (DreamWorldManager.IsInDream)
            {
                // Sueño iniciado desde el pueblo: el aldeano despierta y volvemos al Hub.
                changingScene = true;
                DreamWorldManager.Instance.CompleteDream();
                return;
            }
            int next =SceneManager.GetActiveScene().buildIndex + 1;
            if (next >= SceneManager.sceneCountInBuildSettings) next = 0; // fin del juego: vuelve al menú
            StartCoroutine(LoadAfter(next, nextLevelDelay));
        }

        private IEnumerator LoadAfter(int buildIndex, float delay)
        {
            changingScene = true;
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(buildIndex);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.7f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            float y = bounds.yMin - fallMargin;
            Gizmos.DrawLine(new Vector3(bounds.xMin, y), new Vector3(bounds.xMax, y));
        }
#endif
    }
}
