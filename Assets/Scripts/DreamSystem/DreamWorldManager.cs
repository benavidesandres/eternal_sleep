using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EternalSleep.Enemies;
using EternalSleep.Player;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Puente entre la realidad (el pueblo, escena "Hub") y los sueños (escenas Level_XX).
    ///
    /// - EnterDream(): un aldeano dormido elige una pesadilla al azar (sin repetir la anterior),
    ///   guarda una semilla y carga esa escena con un fundido.
    /// - Al cargar el sueño aplica variaciones con esa semilla: quita algunos enemigos,
    ///   añade plataformas extra y apaga/enciende los objetos marcados con DreamVariant.
    ///   Si mueres, el sueño se recarga con la misma semilla (mismo diseño).
    /// - CompleteDream(): al llegar al portal, el aldeano queda despierto, vuelves al pueblo
    ///   junto a él y te regala la siguiente mejora (PlayerUpgrades).
    ///   Desde el tercer aldeano, el portal lleva antes a la arena del jefe (escena "Arena").
    ///
    /// Se crea solo la primera vez que se usa y sobrevive a los cambios de escena.
    /// </summary>
    public class DreamWorldManager : MonoBehaviour
    {
        [Header("Escenas")]
        [SerializeField] private string hubSceneName = "Hub";
        [Tooltip("Las escenas de Build Settings que empiezan así son pesadillas posibles.")]
        [SerializeField] private string dreamScenePrefix = "Level_";
        [Tooltip("Arena del jefe que espera al final de algunos sueños.")]
        [SerializeField] private string arenaSceneName = "Arena";
        [Tooltip("A partir de qué aldeano (1 = el primero que despiertas) su sueño termina con un jefe.")]
        [SerializeField] private int firstBossDream = 3;

        [Header("Variaciones")]
        [Tooltip("Probabilidad de que cada enemigo siga en el sueño.")]
        [SerializeField, Range(0f, 1f)] private float enemyKeepChance = 0.8f;
        [SerializeField] private int minEnemies = 2;
        [Tooltip("Cuántas plataformas extra (de las que se atraviesan desde abajo) intenta añadir.")]
        [SerializeField] private int extraPlatforms = 4;
        [SerializeField] private Color nightmareTint = new Color(0.85f, 0.75f, 0.95f);

        [Header("Transición")]
        [SerializeField] private float fadeTime = 0.6f;

        private static DreamWorldManager instance;
        private static readonly HashSet<string> awakened = new HashSet<string>();

        private readonly List<string> dreamPool = new List<string>();
        private string currentDreamer;
        private string currentDreamScene;
        private string lastDreamScene;
        private int currentSeed;
        private Vector2? returnPosition;
        private Upgrade? pendingReward;
        private bool transitioning;
        private Image fade;

        /// <summary>El gestor (se crea si todavía no existe).</summary>
        public static DreamWorldManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<DreamWorldManager>();
                    if (instance == null) instance = new GameObject("DreamWorldManager").AddComponent<DreamWorldManager>();
                }
                return instance;
            }
        }

        /// <summary>¿Estamos dentro de un sueño iniciado desde el pueblo?</summary>
        public static bool IsInDream => instance != null && instance.currentDreamScene != null &&
                                        SceneManager.GetActiveScene().name == instance.currentDreamScene;

        public static bool IsAwake(string dreamerId) => awakened.Contains(dreamerId);

        /// <summary>Olvida el sueño actual (al volver al menú principal).</summary>
        public static void ClearDream()
        {
            if (instance == null) return;
            instance.currentDreamer = null;
            instance.currentDreamScene = null;
            instance.returnPosition = null;
        }

        /// <summary>Partida nueva: todos vuelven a dormir.</summary>
        public static void ResetProgress()
        {
            awakened.Clear();
            ClearDream();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildPool();
            CreateFade();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        private void BuildPool()
        {
            dreamPool.Clear();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                if (name.StartsWith(dreamScenePrefix)) dreamPool.Add(name);
            }
        }

        // ---------- Entrar y salir ----------

        /// <param name="dreamerId">Quién sueña (para despertarlo al terminar).</param>
        /// <param name="fixedScene">Escena concreta; vacío = pesadilla al azar.</param>
        /// <param name="returnAt">Dónde reaparece el jugador en el pueblo al volver.</param>
        public void EnterDream(string dreamerId, string fixedScene, Vector2 returnAt)
        {
            if (transitioning) return;
            string scene = string.IsNullOrEmpty(fixedScene) ? PickRandomDream() : fixedScene;
            if (scene == null)
            {
                Debug.LogError($"[Eternal Sleep] No hay escenas que empiecen por \"{dreamScenePrefix}\" en Build Settings.");
                return;
            }

            currentDreamer = dreamerId;
            currentDreamScene = scene;
            lastDreamScene = scene;
            currentSeed = Random.Range(int.MinValue, int.MaxValue);
            returnPosition = returnAt;
            StartCoroutine(FadeAndLoad(scene));
        }

        /// <summary>
        /// El jugador llegó al portal del sueño: el aldeano despierta y volvemos al pueblo.
        /// Desde el aldeano número firstBossDream, antes hay que vencer a un jefe en la arena
        /// (mientras queden jefes); el portal de la arena vuelve a llamar aquí.
        /// </summary>
        public void CompleteDream()
        {
            if (transitioning) return;
            if (NeedsBoss())
            {
                currentDreamScene = arenaSceneName; // sigue siendo el mismo sueño
                StartCoroutine(FadeAndLoad(arenaSceneName));
                return;
            }
            if (currentDreamer != null && awakened.Add(currentDreamer) &&
                PlayerUpgrades.GrantNextWakeReward(out var reward))
                pendingReward = reward;
            currentDreamer = null;
            currentDreamScene = null;
            StartCoroutine(FadeAndLoad(hubSceneName));
        }

        /// <summary>Abandona el sueño sin despertar al aldeano.</summary>
        public void AbortDream()
        {
            if (transitioning) return;
            currentDreamer = null;
            currentDreamScene = null;
            StartCoroutine(FadeAndLoad(hubSceneName));
        }

        private bool NeedsBoss() =>
            currentDreamer != null &&
            currentDreamScene != arenaSceneName &&
            !awakened.Contains(currentDreamer) &&
            awakened.Count + 1 >= firstBossDream &&
            PlayerUpgrades.NextBossIndex() >= 0 &&
            Application.CanStreamedLevelBeLoaded(arenaSceneName);

        private string PickRandomDream()
        {
            if (dreamPool.Count == 0) BuildPool();
            if (dreamPool.Count == 0) return null;
            if (dreamPool.Count == 1) return dreamPool[0];

            string pick;
            do pick = dreamPool[Random.Range(0, dreamPool.Count)];
            while (pick == lastDreamScene);
            return pick;
        }

        private IEnumerator FadeAndLoad(string scene)
        {
            transitioning = true;
            yield return Fade(0f, 1f);
            SceneManager.LoadScene(scene);
            yield return null; // espera a que la escena nueva arranque
            yield return Fade(1f, 0f);
            transitioning = false;

            if (pendingReward.HasValue && DialogueBox.Instance != null)
                DialogueBox.Instance.Show("Nueva mejora", PlayerUpgrades.Describe(pendingReward.Value), null, null);
            pendingReward = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == currentDreamScene && scene.name != arenaSceneName) ApplyVariations(new System.Random(currentSeed));
            else if (scene.name == hubSceneName && returnPosition.HasValue) PlacePlayer(returnPosition.Value);
        }

        private static void PlacePlayer(Vector2 position)
        {
            var motor = FindAnyObjectByType<PlayerMotor>();
            if (motor != null) motor.Teleport(position);
        }

        // ---------- Variaciones del sueño ----------

        private void ApplyVariations(System.Random rng)
        {
            ThinOutEnemies(rng);
            AddExtraPlatforms(rng);
            foreach (var v in ByPosition(FindObjectsByType<DreamVariant>(FindObjectsInactive.Include)))
                v.gameObject.SetActive(rng.NextDouble() < v.Chance);

            var light = FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
            if (light != null) light.color = nightmareTint;
        }

        /// <summary>Orden fijo (por posición) para que la misma semilla dé siempre el mismo sueño.</summary>
        private static IEnumerable<T> ByPosition<T>(IEnumerable<T> items) where T : Component =>
            items.OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.y);

        private void ThinOutEnemies(System.Random rng)
        {
            var enemies = ByPosition(FindObjectsByType<EnemyHealth>()).ToArray();
            int alive = enemies.Length;
            foreach (var e in enemies)
            {
                if (alive <= minEnemies) break;
                if (rng.NextDouble() < enemyKeepChance) continue;
                Destroy(e.gameObject);
                alive--;
            }
        }

        /// <summary>
        /// Añade plataformas de un solo sentido en el aire, a 2 o 3 casillas sobre el suelo (siempre al alcance del salto).
        /// Solo añade (nunca quita), y se atraviesan desde abajo, así que no pueden bloquear el camino.
        /// </summary>
        private void AddExtraPlatforms(System.Random rng)
        {
            Tilemap platforms = null, ground = null, hazards = null;
            foreach (var map in FindObjectsByType<Tilemap>())
            {
                if (map.name == "Platforms") platforms = map;
                else if (map.name == "Ground") ground = map;
                else if (map.name == "Hazards") hazards = map;
            }
            if (platforms == null || ground == null) return;

            TileBase tile = FirstTile(platforms);
            if (tile == null) return;

            ground.CompressBounds();
            BoundsInt b = ground.cellBounds;
            if (b.size.x < 12) return;
            int placed = 0;

            for (int attempt = 0; attempt < extraPlatforms * 10 && placed < extraPlatforms; attempt++)
            {
                int x = rng.Next(b.xMin + 3, b.xMax - 5);
                int width = rng.Next(2, 4);
                int top = GroundTop(ground, x, b);
                if (top == int.MinValue) continue;
                int y = top + 2 + rng.Next(0, 2);

                bool free = true;
                for (int dx = -1; dx <= width && free; dx++)
                for (int dy = -1; dy <= 2 && free; dy++)
                {
                    var c = new Vector3Int(x + dx, y + dy, 0);
                    free = !ground.HasTile(c) && !platforms.HasTile(c) && (hazards == null || !hazards.HasTile(c));
                }
                if (!free) continue;

                for (int dx = 0; dx < width; dx++) platforms.SetTile(new Vector3Int(x + dx, y, 0), tile);
                placed++;
            }
        }

        private static int GroundTop(Tilemap ground, int x, BoundsInt b)
        {
            for (int y = b.yMax; y >= b.yMin; y--)
                if (ground.HasTile(new Vector3Int(x, y, 0))) return y;
            return int.MinValue; // hueco: no se pone nada encima del vacío
        }

        private static TileBase FirstTile(Tilemap map)
        {
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var t = map.GetTile(pos);
                if (t != null) return t;
            }
            return null;
        }

        // ---------- Fundido ----------

        private void CreateFade()
        {
            var canvasGo = new GameObject("DreamFade");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var img = new GameObject("Fade", typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(canvasGo.transform, false);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.color = new Color(0.03f, 0.02f, 0.05f, 0f);
            img.raycastTarget = false;
            fade = img;
        }

        private IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                SetFade(Mathf.Lerp(from, to, t / fadeTime));
                yield return null;
            }
            SetFade(to);
        }

        private void SetFade(float a)
        {
            var c = fade.color;
            c.a = a;
            fade.color = c;
        }
    }
}
