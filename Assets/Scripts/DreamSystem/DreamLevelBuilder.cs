using EternalSleep.Level;
using EternalSleep.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Construye en pleno juego un sueño generado (escena "Sueno"): pinta los tilemaps con el mapa de texto,
    /// coloca enemigos y portal, mueve al jugador al inicio y pone el fondo, la luz y la niebla del tema.
    /// Lo llama DreamWorldManager al cargar la escena. Si la escena se abre sola (pruebas en el editor),
    /// se genera un sueño al azar sin IA.
    /// </summary>
    public class DreamLevelBuilder : MonoBehaviour
    {
        [Header("Tilemaps")]
        [SerializeField] private Tilemap ground;
        [SerializeField] private Tilemap platforms;
        [SerializeField] private Tilemap hazards;
        [SerializeField] private TileBase groundTile;
        [SerializeField] private TileBase platformTile;
        [SerializeField] private TileBase spikeTile;

        [Header("Objetos")]
        [SerializeField] private GameObject walkerPrefab;
        [SerializeField] private GameObject flyerPrefab;
        [SerializeField] private GameObject shooterPrefab;
        [SerializeField] private GameObject portalPrefab;
        [SerializeField] private Transform objectsRoot;

        [Header("Ambiente")]
        [Tooltip("Un fondo por tema, en el orden de DreamRecipe.Themes.")]
        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer fog;

        public static DreamLevelBuilder Instance { get; private set; }
        public bool Built { get; private set; }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (Built) return;
            // Escena abierta sola: sueño al azar, sin IA.
            int seed = Random.Range(int.MinValue, int.MaxValue);
            var recipe = DreamRecipe.Procedural(seed, 0);
            Build(DreamGenerator.Generate(recipe, seed, DreamLibrary.Chunks, PlayerUpgrades.Has(Upgrade.DoubleJump)), recipe);
        }

        public void Build(string[] map, DreamRecipe recipe)
        {
            Built = true;
            Clear();

            int height = map.Length, width = 0;
            foreach (var line in map) width = Mathf.Max(width, line.Length);
            Vector2 start = new Vector2(1.5f, 3f);

            for (int row = 0; row < height; row++)
            for (int x = 0; x < map[row].Length; x++)
            {
                int y = height - 1 - row;
                var cell = new Vector3Int(x, y, 0);
                switch (map[row][x])
                {
                    case '#': ground.SetTile(cell, groundTile); break;
                    case '=': platforms.SetTile(cell, platformTile); break;
                    case '^': hazards.SetTile(cell, spikeTile); break;
                    case 'P': start = new Vector2(x + 0.5f, y); break;
                    case 'E': Spawn(walkerPrefab, new Vector2(x + 0.5f, y)); break;
                    case 'S': Spawn(shooterPrefab, new Vector2(x + 0.5f, y)); break;
                    case 'F': Spawn(flyerPrefab, new Vector2(x + 0.5f, y + 0.5f)); break;
                    case 'G': Spawn(portalPrefab, new Vector2(x + 0.5f, y)); break;
                }
            }
            ExtendGroundPastEdges(map);

            var motor = FindAnyObjectByType<PlayerMotor>();
            if (motor != null) motor.Teleport(new Vector2(start.x, start.y + 0.01f));

            var bounds = new Rect(0, 0, width, height);
            var level = FindAnyObjectByType<LevelManager>();
            if (level != null) level.SetBounds(bounds);

            ApplyMood(recipe);
        }

        /// <summary>Quita lo que trae la escena guardada (el sueño de ejemplo con el que se construyó).</summary>
        private void Clear()
        {
            ground.ClearAllTiles();
            platforms.ClearAllTiles();
            hazards.ClearAllTiles();
            if (objectsRoot != null)
                for (int i = objectsRoot.childCount - 1; i >= 0; i--) Destroy(objectsRoot.GetChild(i).gameObject);
            foreach (var goal in FindObjectsByType<LevelGoal>(FindObjectsInactive.Include))
                if (objectsRoot == null || goal.transform.parent != objectsRoot) Destroy(goal.gameObject);
        }

        private void Spawn(GameObject prefab, Vector2 position)
        {
            if (prefab != null) Instantiate(prefab, position, Quaternion.identity, objectsRoot);
        }

        /// <summary>Prolonga el suelo fuera del mapa para que el borde no se vea cortado (como el editor).</summary>
        private void ExtendGroundPastEdges(string[] map)
        {
            const int margin = 2;
            int height = map.Length;
            for (int row = 0; row < height; row++)
            {
                int y = height - 1 - row;
                string line = map[row];
                for (int i = 1; i <= margin; i++)
                {
                    if (line[0] == '#') ground.SetTile(new Vector3Int(-i, y, 0), groundTile);
                    if (line[line.Length - 1] == '#') ground.SetTile(new Vector3Int(line.Length - 1 + i, y, 0), groundTile);
                }
            }
            string bottom = map[height - 1];
            for (int x = -margin; x < bottom.Length + margin; x++)
            {
                if (bottom[Mathf.Clamp(x, 0, bottom.Length - 1)] != '#') continue;
                for (int i = 1; i <= margin; i++) ground.SetTile(new Vector3Int(x, -i, 0), groundTile);
            }
        }

        private void ApplyMood(DreamRecipe recipe)
        {
            if (background != null && backgrounds != null && backgrounds.Length > 0)
            {
                var sprite = backgrounds[Mathf.Clamp(recipe.ThemeIndex, 0, backgrounds.Length - 1)];
                if (sprite != null)
                {
                    background.sprite = sprite;
                    // Cubre toda la vista de la cámara (va pegado a ella).
                    var cam = Camera.main;
                    float viewH = cam != null ? cam.orthographicSize * 2f : 11f;
                    float viewW = viewH * 16f / 9f * 1.35f; // margen para pantallas más anchas
                    float s = Mathf.Max(viewW / sprite.bounds.size.x, viewH * 1.1f / sprite.bounds.size.y);
                    background.transform.localScale = new Vector3(s, s, 1f);
                    background.color = Color.Lerp(new Color(0.72f, 0.72f, 0.8f), recipe.LightColor, 0.35f);
                }
            }

            if (fog != null)
            {
                var c = recipe.LightColor;
                c.a = recipe.niebla;
                fog.color = c;
            }

            foreach (var light in FindObjectsByType<Light2D>())
                if (light.lightType == Light2D.LightType.Global) light.color = recipe.LightColor;
        }
    }
}
