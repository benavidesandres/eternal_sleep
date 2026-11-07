#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EternalSleep.CameraSystem;
using EternalSleep.Dream;
using EternalSleep.Enemies;
using EternalSleep.Level;
using EternalSleep.Player;
using EternalSleep.UI;
using EternalSleep.Village;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Menú: Eternal Sleep > Construir niveles.
    /// Lee los mapas de texto de Assets/Levels (Level_01.txt, Level_02.txt...) y crea una escena por cada uno
    /// en Assets/Scenes/Levels, con el arte pixel art generado por código.
    ///
    /// Leyenda de los mapas (cada carácter es una casilla de 1×1):
    ///   #  suelo/pared        =  plataforma que se atraviesa desde abajo
    ///   ^  pinchos            P  inicio del jugador
    ///   E  enemigo que camina F  enemigo que vuela
    ///   S  vigía que dispara  G  portal de salida   .  vacío
    ///   N  aldeano dormido (solo en Hub.txt): al hablarle entras en su sueño
    ///   B  velo de sombra (solo se cruza con el Paso de Sombra)   H  Corazón de Luna
    ///   J  sitio del jefe (Arena.txt): se crean los tres jefes, su barra de vida y el orbe de recompensa
    ///
    /// Hub.txt (el pueblo) se construye primero y queda justo después del menú en Build Settings.
    /// </summary>
    public static class LevelBuilderEditor
    {
        private const string LevelsFolder = "Assets/Levels";
        private const string ArtFolder = "Assets/Levels/Art";
        private const string TilesFolder = "Assets/Levels/Art/Tiles";
        private const string GroundArtFolder = "Assets/Levels/Art/Suelo";
        private const string ScenesFolder = "Assets/Scenes/Levels";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string HubMapPath = LevelsFolder + "/Hub.txt";
        private const string HubBackgroundFallback = "Level_06"; // el pueblo de los sueños
        private const string ArenaMapPath = LevelsFolder + "/Arena.txt";
        private const string ArenaBackgroundFallback = "Level_04"; // la catedral
        private const string GeneratedDreamScene = "Sueno";
        private const string DreamChunksPath = "Assets/Resources/Dreams/Tramos.txt";
        private const string PrefabsFolder = ArtFolder + "/Prefabs";
        private const float TilePPU = 16f; // 16 píxeles = 1 casilla
        private const string FontAssetPath = "Assets/UI/Fonts/PixelifySans SDF.asset";

        /// <summary>Rutas de los cuadros de cada animación (se recargan en cada escena nueva).</summary>
        private static readonly Dictionary<string, string[]> clipPaths = new Dictionary<string, string[]>();

        private class Art
        {
            public RuleTile Ground, GroundSecret, Platform; // GroundSecret: pared falsa (sin colisión)
            public Tile Spike, BackWall;
            public Sprite[] Orb, Portal, Veil;
            public Sprite SleepZ, Prompt, DialoguePanel, DialogueNext;
            public Sprite Vignette, MaskLeft, MaskRight, Mote, DoorArrow, WhitePixel;
            public Dictionary<string, Sprite[]> Clips; // animaciones de enemigos y aldeanos (con su silueta "_flash")
            public Sprite Alert, HeartFull, HeartEmpty, MoonHeart, Sky, Stars, Moon, HillsFar, HillsNear, Fog;
            public DreamOrb OrbPrefab;
        }

        [MenuItem("Eternal Sleep/Construir niveles")]
        public static void BuildLevels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Eternal Sleep", "Detén el juego (botón Play) antes de usar esta opción.", "OK");
                return;
            }

            var files = Directory.Exists(LevelsFolder)
                ? Directory.GetFiles(LevelsFolder, "Level_*.txt").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray()
                : new string[0];
            if (File.Exists(HubMapPath)) files = new[] { HubMapPath }.Concat(files).ToArray();
            if (File.Exists(ArenaMapPath)) files = files.Concat(new[] { ArenaMapPath }).ToArray();
            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("Eternal Sleep", "No hay mapas en " + LevelsFolder + " (Level_01.txt, ...).", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            int groundLayer = PlayerSetupEditor.EnsureLayer("Ground");
            if (groundLayer < 0) return;
            var controller = PlayerSetupEditor.BuildController();
            if (controller == null) return;

            var scenes = new List<string>();
            try
            {
                EditorUtility.DisplayProgressBar("Eternal Sleep", "Dibujando el arte de los niveles...", 0f);
                var artPaths = ArtPaths(CreateArt());
                string controllerPath = AssetDatabase.GetAssetPath(controller);

                for (int i = 0; i < files.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Eternal Sleep",
                        "Construyendo " + Path.GetFileNameWithoutExtension(files[i]), (i + 1f) / (files.Length + 2f));
                    scenes.Add(BuildLevel(files[i], artPaths, controllerPath, groundLayer));
                }

                // Escena de los sueños generados (únicos para cada jugador): se guarda con un sueño de ejemplo
                // y en el juego DreamLevelBuilder la vuelve a pintar.
                EditorUtility.DisplayProgressBar("Eternal Sleep", "Construyendo la escena de los sueños generados", 1f);
                var sample = DreamGenerator.Fallback(DreamGenerator.Parse(File.ReadAllText(DreamChunksPath)));
                scenes.Add(BuildLevel(sample, GeneratedDreamScene, artPaths, controllerPath, groundLayer));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            UpdateBuildSettings(scenes);
            EditorSceneManager.OpenScene(scenes[0]);
            Debug.Log($"[Eternal Sleep] {scenes.Count} niveles construidos en {ScenesFolder}. Orden del juego: menú → " +
                      string.Join(" → ", scenes.Select(Path.GetFileNameWithoutExtension)));
        }

        // ---------- Escena de un nivel ----------

        private static string BuildLevel(string mapPath, Dictionary<string, string[]> paths, string controllerPath, int groundLayer)
        {
            var lines = File.ReadAllLines(mapPath).Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
            return BuildLevel(lines, Path.GetFileNameWithoutExtension(mapPath), paths, controllerPath, groundLayer);
        }

        private static string BuildLevel(string[] lines, string levelName, Dictionary<string, string[]> paths, string controllerPath, int groundLayer)
        {
            int height = lines.Length;
            int width = lines.Max(l => l.Length);
            bool generated = levelName == GeneratedDreamScene;

            // Abrir una escena nueva descarga de memoria el arte creado antes; se vuelve a cargar desde disco.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var art = ReloadArt(paths);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            // Tilemaps
            var grid = new GameObject("Grid").AddComponent<Grid>();
            var ground = NewTilemap("Ground", grid.transform, 0);
            var platforms = NewTilemap("Platforms", grid.transform, 0);
            var hazards = NewTilemap("Hazards", grid.transform, 1);
            ground.gameObject.layer = groundLayer;
            platforms.gameObject.layer = groundLayer;

            var start = new Vector2(1.5f, 3f);
            var enemies = new GameObject("Enemies").transform;
            Transform villagers = null, village = null;
            int villagerCount = 0, talkers = 0, collectibles = 0, graves = 0;
            var secretCells = new List<Vector3Int>();
            var crackedCells = new List<Vector3Int>();
            GameObject portal = null;
            Vector2? bossSpot = null;
            Transform Village() => village != null ? village : (village = new GameObject("Pueblo").transform);

            for (int row = 0; row < height; row++)
            for (int x = 0; x < lines[row].Length; x++)
            {
                int y = height - 1 - row; // la primera línea del texto es la de arriba
                var cell = new Vector3Int(x, y, 0);
                char ch = lines[row][x];
                var feet = new Vector2(x + 0.5f, y);

                // Pared falsa: suelo sin colisión (también bajo los objetos escondidos 'c' y 'm').
                if (ch == 'X' || ch == 'c' || ch == 'm')
                {
                    ground.SetTile(cell, art.GroundSecret);
                    secretCells.Add(cell);
                }
                if (IsTalker(ch))
                {
                    CreateTalker(Village(), art, feet, ch);
                    talkers++;
                    continue;
                }

                switch (ch)
                {
                    case '#': ground.SetTile(cell, art.Ground); break;
                    case '=': platforms.SetTile(cell, art.Platform); break;
                    case '^': hazards.SetTile(cell, art.Spike); break;
                    case 'P': start = new Vector2(x + 0.5f, y); break;
                    case 'E': CreateWalker(enemies, art, new Vector2(x + 0.5f, y)); break;
                    case 'F': CreateFlyer(enemies, art, new Vector2(x + 0.5f, y + 0.5f)); break;
                    case 'S': CreateShooter(enemies, art, new Vector2(x + 0.5f, y)); break;
                    case 'G': portal = CreatePortal(art, new Vector2(x + 0.5f, y)); break;
                    case 'J': bossSpot = feet; break;
                    case 'N':
                        if (villagers == null) villagers = new GameObject("Villagers").transform;
                        CreateDreamer(villagers, art, new Vector2(x + 0.5f, y), ++villagerCount, height);
                        break;
                    case 'C':
                    case 'c':
                        CreateCollectible(Village(), art, new Vector2(x + 0.5f, y + 0.5f), $"lagrima_{x}_{y}", ch == 'c');
                        collectibles++;
                        break;
                    case 'L': CreateLamp(Village(), art, feet); break;
                    case 'g': CreateGrave(Village(), art, feet, graves++ % 3); break;
                    case 'B': CreateVeil(Village(), art, new Vector2(x + 0.5f, y + 0.5f)); break;
                    case 'H': CreateMoonHeart(Village(), art, new Vector2(x + 0.5f, y + 0.5f), $"corazon_{x}_{y}"); break;
                    case '1': CreateUpgradePickup(Village(), art, new Vector2(x + 0.5f, y + 0.5f), Upgrade.WallJump); break;
                    case '2': CreateUpgradePickup(Village(), art, new Vector2(x + 0.5f, y + 0.5f), Upgrade.GroundSlam); break;
                    case '3': CreateUpgradePickup(Village(), art, new Vector2(x + 0.5f, y + 0.5f), Upgrade.ChargedAttack); break;
                    case '%': crackedCells.Add(cell); break;
                }
            }

            ExtendGroundPastEdges(ground, art.Ground, lines);
            PaintBackWalls(grid.transform, art.BackWall, lines);

            // Suelo sólido: un solo collider unido para todo el tilemap.
            ground.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var groundCol = ground.gameObject.AddComponent<TilemapCollider2D>();
            groundCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            ground.gameObject.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;

            // Plataformas: se atraviesan desde abajo.
            var platCol = platforms.gameObject.AddComponent<TilemapCollider2D>();
            platCol.usedByEffector = true;
            var effector = platforms.gameObject.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            // Pinchos.
            hazards.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
            hazards.gameObject.AddComponent<Hazard>();

            if (secretCells.Count > 0) CreateSecrets(ground, secretCells);
            if (crackedCells.Count > 0) CreateCrackedFloors(grid.transform, art, crackedCells, groundLayer);
            if (villagerCount > 0) CreateRoom(null, "Calle", new Rect(0, 0, width, height));

            // Jugador
            var player = PlayerSetupEditor.CreatePlayer(controller, groundLayer);
            player.transform.position = new Vector3(start.x, start.y + 0.01f, 0f);
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerHurtFeedback>();

            var bounds = new Rect(0, 0, width, height);
            CreateLevelManager(bounds);
            // El pueblo un poco más oscuro para que luzcan los faroles; la arena, en penumbra.
            CreateLight(villagerCount > 0 ? 0.8f : bossSpot.HasValue ? 0.85f : 1f);
            var cam = CreateCamera(player.transform, start, bounds, bossSpot.HasValue ? 7f : 5.5f); // en la arena se ve entera
            if (generated) CreateDreamBuilder(art, cam, ground, platforms, hazards, enemies);
            else CreateBackground(art, cam, bounds, levelName);
            CreateHud(art, collectibles > 0);
            if (villagerCount > 0 || talkers > 0 || bossSpot.HasValue || generated) CreateDialogueBox(art);
            if (bossSpot.HasValue)
            {
                CreateBossArena(enemies, art, bossSpot.Value, portal);
                CreateBossBar(art);
            }
            CreateDeathScreen(art);

            if (ground.GetUsedTilesCount() == 0)
                Debug.LogError($"[Eternal Sleep] {levelName}: el suelo quedó vacío (¿falta el tile de suelo?).");

            Directory.CreateDirectory(ScenesFolder);
            string scenePath = $"{ScenesFolder}/{levelName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            return scenePath;
        }

        /// <summary>
        /// Prolonga el suelo fuera del mapa (no se ve), para que las casillas del borde
        /// no dibujen el canto de roca como si el mundo se acabara ahí.
        /// </summary>
        private static void ExtendGroundPastEdges(Tilemap ground, TileBase tile, string[] lines)
        {
            const int margin = 2;
            int height = lines.Length;
            for (int row = 0; row < height; row++)
            {
                int y = height - 1 - row;
                string line = lines[row];
                if (line.Length == 0) continue;
                for (int i = 1; i <= margin; i++)
                {
                    if (line[0] == '#') ground.SetTile(new Vector3Int(-i, y, 0), tile);
                    if (line[line.Length - 1] == '#') ground.SetTile(new Vector3Int(line.Length - 1 + i, y, 0), tile);
                }
            }

            string bottom = lines[height - 1];
            for (int x = -margin; x < bottom.Length + margin; x++)
            {
                if (bottom[Mathf.Clamp(x, 0, bottom.Length - 1)] != '#') continue;
                for (int i = 1; i <= margin; i++) ground.SetTile(new Vector3Int(x, -i, 0), tile);
            }
        }

        /// <summary>
        /// Muro de fondo en las salas cerradas por un velo de sombra (el mausoleo): rellena el hueco
        /// que hay detrás de cada velo si está rodeado de suelo. El lado que da al aire libre se descarta.
        /// </summary>
        private static void PaintBackWalls(Transform grid, TileBase tile, string[] lines)
        {
            int height = lines.Length;
            char At(int x, int row) => row >= 0 && row < height && x >= 0 && x < lines[row].Length ? lines[row][x] : '.';
            bool Wall(int x, int row) { char c = At(x, row); return c == '#' || c == 'B'; }

            var enclosed = new HashSet<Vector2Int>();
            for (int row = 0; row < height; row++)
            for (int x = 0; x < lines[row].Length; x++)
            {
                if (lines[row][x] != 'B') continue;
                foreach (int side in new[] { x - 1, x + 1 })
                {
                    var start = new Vector2Int(side, row);
                    if (Wall(side, row) || enclosed.Contains(start)) continue;

                    var seen = new HashSet<Vector2Int> { start };
                    var open = new Stack<Vector2Int>(seen);
                    bool escapes = false;
                    while (open.Count > 0 && !escapes)
                    {
                        var c = open.Pop();
                        escapes = c.y <= 0 || c.x <= 0 || c.x >= lines[Mathf.Clamp(c.y, 0, height - 1)].Length - 1;
                        foreach (var n in new[] { c + Vector2Int.left, c + Vector2Int.right, c + Vector2Int.up, c + Vector2Int.down })
                            if (!Wall(n.x, n.y) && seen.Add(n)) open.Push(n);
                    }
                    if (!escapes) enclosed.UnionWith(seen);
                }
            }
            if (enclosed.Count == 0) return;

            var map = NewTilemap("BackWall", grid, -10);
            map.color = new Color(0.62f, 0.56f, 0.78f);
            foreach (var c in enclosed) map.SetTile(new Vector3Int(c.x, height - 1 - c.y, 0), tile);
        }

        /// <summary>Guarda la ruta en disco de cada asset del arte (campo → rutas).</summary>
        private static Dictionary<string, string[]> ArtPaths(Art art)
        {
            var result = new Dictionary<string, string[]>();
            foreach (var field in typeof(Art).GetFields())
            {
                object value = field.GetValue(art);
                if (value is Object[] array)
                    result[field.Name] = array.Select(AssetDatabase.GetAssetPath).ToArray();
                else if (value is Object obj && obj != null)
                    result[field.Name] = new[] { AssetDatabase.GetAssetPath(obj) };
            }
            return result;
        }

        /// <summary>Vuelve a cargar el arte desde disco usando las rutas guardadas.</summary>
        private static Art ReloadArt(Dictionary<string, string[]> paths)
        {
            var art = new Art();
            foreach (var field in typeof(Art).GetFields())
            {
                if (!paths.TryGetValue(field.Name, out var p)) continue;
                var type = field.FieldType;

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    var array = System.Array.CreateInstance(elementType, p.Length);
                    for (int i = 0; i < p.Length; i++) array.SetValue(Load(p[i], elementType), i);
                    field.SetValue(art, array);
                }
                else field.SetValue(art, Load(p[0], type));
            }
            art.Clips = clipPaths.ToDictionary(kv => kv.Key,
                kv => kv.Value.Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray());
            return art;
        }

        private static Object Load(string path, System.Type type)
        {
            if (typeof(Component).IsAssignableFrom(type)) // prefab: se carga el GameObject y se toma el componente
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                return go != null ? go.GetComponent(type) : null;
            }
            return AssetDatabase.LoadAssetAtPath(path, type);
        }

        private static Tilemap NewTilemap(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return map;
        }

        private static GameObject CreateWalker(Transform parent, Art art, Vector2 feet)
        {
            var go = new GameObject("Sombra");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            AddEnemyBase(go, art, "sombra", 5, 3, PixelArt.Hex("#8C7FB5"), new Vector2(0f, 0.75f),
                ("idle", 3f, true), ("walk", 8f, true), ("run", 13f, true), ("windup", 14f, true), ("lunge", 1f, true), ("hurt", 1f, true));

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.7f, 1.3f);
            col.offset = new Vector2(0f, 0.65f);

            AddSenses(go, 6.5f, 0.2f, new Vector2(-0.5f, 0.95f)); // solo ve hacia delante
            var walker = go.AddComponent<WalkerEnemy>();
            SetRefs(walker, ("alertIcon", AddAlertIcon(go, art, 2.0f)));
            return go;
        }

        private static GameObject CreateFlyer(Transform parent, Art art, Vector2 center)
        {
            var go = new GameObject("Polilla");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            AddEnemyBase(go, art, "polilla", 6, 2, PixelArt.Hex("#CDBFF5"), Vector2.zero,
                ("fly", 10f, true), ("flyfast", 15f, true), ("windup", 16f, true), ("swoop", 1f, true), ("hurt", 1f, true));

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.72f;

            AddSenses(go, 6.5f, -1f, Vector2.zero); // ve en todas direcciones
            var flyer = go.AddComponent<FlyerEnemy>();
            SetRefs(flyer, ("alertIcon", AddAlertIcon(go, art, 1.25f)));
            return go;
        }

        private static GameObject CreateShooter(Transform parent, Art art, Vector2 feet)
        {
            var go = new GameObject("Vigia");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            AddEnemyBase(go, art, "vigia", 5, 3, PixelArt.Hex("#E86A92"), new Vector2(0f, 1.5f),
                ("idle", 3f, true), ("aim", 10f, true), ("shoot", 1f, true), ("hurt", 1f, true));

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.3f, 2.1f);
            col.offset = new Vector2(0f, 1.05f);

            AddSenses(go, 8.5f, -1f, new Vector2(0f, 1.5f));
            var shooter = go.AddComponent<ShooterEnemy>();
            SetRefs(shooter, ("projectilePrefab", art.OrbPrefab), ("alertIcon", AddAlertIcon(go, art, 2.6f)));
            var so = new SerializedObject(shooter);
            so.FindProperty("muzzleOffset").vector2Value = new Vector2(-0.4f, 1.5f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        // ---------- Sueños generados ----------

        /// <summary>
        /// Prepara la escena "Sueno": prefabs de enemigos y portal, fondo y niebla pegados a la cámara,
        /// y el DreamLevelBuilder que pinta el sueño de cada jugador al cargarla.
        /// </summary>
        private static void CreateDreamBuilder(Art art, Camera cam, Tilemap ground, Tilemap platforms, Tilemap hazards, Transform objects)
        {
            Directory.CreateDirectory(PrefabsFolder);
            GameObject SavePrefab(GameObject go, string name)
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsFolder}/{name}.prefab");
                Object.DestroyImmediate(go);
                return prefab;
            }
            var walker = SavePrefab(CreateWalker(null, art, Vector2.zero), "Sombra");
            var flyer = SavePrefab(CreateFlyer(null, art, Vector2.zero), "Polilla");
            var shooter = SavePrefab(CreateShooter(null, art, Vector2.zero), "Vigia");
            var portal = SavePrefab(CreatePortal(art, Vector2.zero), "Portal");

            // Un fondo por tema (en el orden de DreamRecipe.Themes): los de los niveles hechos a mano.
            var backgrounds = new Object[DreamRecipe.Themes.Length];
            for (int i = 0; i < backgrounds.Length; i++) backgrounds[i] = LoadLevelBackground($"Level_{i + 1:00}");

            var bg = new GameObject("FondoDelSueno");
            bg.transform.SetParent(cam.transform, false);
            bg.transform.localPosition = new Vector3(0f, 0f, 10f);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = backgrounds.OfType<Sprite>().FirstOrDefault() ?? art.Sky;
            bgSr.sortingOrder = -100;

            var fog = new GameObject("Niebla");
            fog.transform.SetParent(cam.transform, false);
            fog.transform.localPosition = new Vector3(0f, -cam.orthographicSize + 1.5f, 10f);
            var fogSr = fog.AddComponent<SpriteRenderer>();
            fogSr.sprite = art.Fog;
            fogSr.sortingOrder = -60;
            Tiled(fogSr, cam.orthographicSize * 2f * 16f / 9f * 1.4f, 4f);
            fogSr.color = PixelArt.WithAlpha(Color.white, 0.3f);

            var builder = new GameObject("ConstructorDelSueno").AddComponent<DreamLevelBuilder>();
            SetRefs(builder, ("ground", ground), ("platforms", platforms), ("hazards", hazards),
                ("groundTile", art.Ground), ("platformTile", art.Platform), ("spikeTile", art.Spike),
                ("walkerPrefab", walker), ("flyerPrefab", flyer), ("shooterPrefab", shooter), ("portalPrefab", portal),
                ("objectsRoot", objects), ("backgrounds", backgrounds), ("background", bgSr), ("fog", fogSr));
        }

        private static void AddSenses(GameObject go, float range, float minFacingDot, Vector2 eye)
        {
            var senses = go.AddComponent<EnemySenses>();
            var so = new SerializedObject(senses);
            so.FindProperty("viewRange").floatValue = range;
            so.FindProperty("minFacingDot").floatValue = minFacingDot;
            so.FindProperty("eyeOffset").vector2Value = eye;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Icono "!" sobre la cabeza del enemigo (oculto hasta que ve al jugador).</summary>
        private static GameObject AddAlertIcon(GameObject enemy, Art art, float height)
        {
            var icon = new GameObject("Alerta");
            icon.transform.SetParent(enemy.transform, false);
            icon.transform.localPosition = new Vector3(0f, height, 0f);
            var sr = icon.AddComponent<SpriteRenderer>();
            sr.sprite = art.Alert;
            sr.sortingOrder = 20;
            icon.SetActive(false);
            return icon;
        }

        /// <summary>Lo que comparten todos los enemigos: animaciones, física sin gravedad, vida, efectos de golpe y daño al tocar.</summary>
        private static void AddEnemyBase(GameObject go, Art art, string prefix, int sortingOrder, int health,
            Color hitColor, Vector2 center, params (string name, float fps, bool loop)[] clips)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Clips[$"{prefix}_{clips[0].name}"][0];
            sr.sortingOrder = sortingOrder;
            SetClips(go.AddComponent<SpriteFlipbook>(), art, prefix, true, clips);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var hp = new SerializedObject(go.AddComponent<EnemyHealth>());
            hp.FindProperty("maxHealth").intValue = health;
            hp.FindProperty("hitColor").colorValue = hitColor;
            hp.FindProperty("centerOffset").vector2Value = center;
            hp.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<EnemyContactDamage>();
        }

        /// <summary>Rellena la lista de clips de un SpriteFlipbook.</summary>
        private static void SetClips(SpriteFlipbook book, Art art, string prefix, bool withFlash,
            params (string name, float fps, bool loop)[] clips)
        {
            var so = new SerializedObject(book);
            var list = so.FindProperty("clips");
            list.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                var (name, fps, loop) = clips[i];
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("name").stringValue = name;
                e.FindPropertyRelative("fps").floatValue = fps;
                e.FindPropertyRelative("loop").boolValue = loop;
                FillArray(e.FindPropertyRelative("frames"), art.Clips[$"{prefix}_{name}"]);
                FillArray(e.FindPropertyRelative("flash"),
                    withFlash && art.Clips.TryGetValue($"{prefix}_{name}_flash", out var f) ? f : new Sprite[0]);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void FillArray(SerializedProperty prop, Object[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static GameObject CreatePortal(Art art, Vector2 feet)
        {
            var go = new GameObject("Portal");
            go.transform.position = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Portal[0];
            sr.sortingOrder = 3;
            SetRefs(go.AddComponent<SpriteFlipbook>(), ("frames", art.Portal));

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 1.8f);
            col.offset = new Vector2(0f, 0.9f);
            go.AddComponent<LevelGoal>();
            return go;
        }

        // ---------- Arena del jefe ----------

        /// <summary>
        /// Los tres jefes (desactivados: BossArena despierta el que toca), el orbe de recompensa oculto
        /// y el gestor de la arena. El portal queda cerrado hasta recoger el orbe.
        /// </summary>
        private static void CreateBossArena(Transform enemies, Art art, Vector2 feet, GameObject portal)
        {
            var bosses = new Object[]
            {
                CreateBoss<BossBeast>(enemies, art, "Jefe_Bestia", "jefe_bestia", feet, 22, PixelArt.Hex("#E86A92"), new Vector2(0f, 1.4f),
                    "INSOMNIO, LA BESTIA SIN PÁRPADOS", PixelArt.Hex("#E86A92"),
                    ("idle", 3f, true), ("walk", 7f, true), ("windup", 12f, true), ("charge", 12f, true),
                    ("leap", 1f, true), ("roar", 10f, true), ("hurt", 1f, true)),
                CreateBoss<BossMoth>(enemies, art, "Jefe_Reina", "jefe_reina", feet + new Vector2(0f, 6f), 26, PixelArt.Hex("#FFE38A"), new Vector2(0f, 0.2f),
                    "LA REINA DE LAS POLILLAS", PixelArt.Hex("#FFE9A8"),
                    ("fly", 10f, true), ("windup", 16f, true), ("swoop", 1f, true), ("cast", 6f, true), ("hurt", 1f, true)),
                CreateBoss<BossEye>(enemies, art, "Jefe_Ojo", "jefe_ojo", feet + new Vector2(0f, 5f), 30, PixelArt.Hex("#E86A92"), new Vector2(0f, 0.25f),
                    "EL OJO QUE NO DUERME", Color.white,
                    ("idle", 4f, true), ("aim", 10f, true), ("shoot", 1f, true), ("close", 1f, true), ("hurt", 1f, true)),
            };

            // Colliders a la medida de cada dibujo.
            var beast = ((Component)bosses[0]).gameObject.AddComponent<BoxCollider2D>();
            beast.isTrigger = true;
            beast.size = new Vector2(3.4f, 2.4f);
            beast.offset = new Vector2(0f, 1.3f);
            var queen = ((Component)bosses[1]).gameObject.AddComponent<CircleCollider2D>();
            queen.isTrigger = true;
            queen.radius = 1.4f;
            queen.offset = new Vector2(0f, 0.2f);
            var eye = ((Component)bosses[2]).gameObject.AddComponent<CircleCollider2D>();
            eye.isTrigger = true;
            eye.radius = 1.2f;
            eye.offset = new Vector2(0f, 0.25f);
            foreach (Component b in bosses) b.gameObject.SetActive(false);

            var reward = CreateUpgradePickup(null, art, feet + Vector2.up * 1.5f, Upgrade.WallJump);
            reward.name = "RecompensaDelJefe";
            reward.SetActive(false);

            var arena = new GameObject("ArenaDelJefe").AddComponent<BossArena>();
            SetRefs(arena, ("bosses", bosses), ("portal", portal), ("reward", reward.GetComponent<UpgradePickup>()));
        }

        private static T CreateBoss<T>(Transform parent, Art art, string name, string prefix, Vector2 position, int health,
            Color hitColor, Vector2 center, string displayName, Color projectileTint,
            params (string name, float fps, bool loop)[] clips) where T : BossBase
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            AddEnemyBase(go, art, prefix, 5, health, hitColor, center, clips);

            var hp = new SerializedObject(go.GetComponent<EnemyHealth>());
            hp.FindProperty("deathTime").floatValue = 1.4f;       // se deshace despacio
            hp.FindProperty("hitStop").floatValue = 0.03f;
            hp.FindProperty("invulnerableTime").floatValue = 0.12f; // deja encadenar el combo
            hp.ApplyModifiedPropertiesWithoutUndo();

            var boss = go.AddComponent<T>();
            var so = new SerializedObject(boss);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("projectilePrefab").objectReferenceValue = art.OrbPrefab;
            so.FindProperty("projectileTint").colorValue = projectileTint;
            so.ApplyModifiedPropertiesWithoutUndo();
            return boss;
        }

        /// <summary>Barra de vida del jefe abajo en la pantalla (oculta hasta que empieza la pelea).</summary>
        private static void CreateBossBar(Art art)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var canvasGo = new GameObject("BarraDelJefe", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;

            var root = NewUI("Contenido", canvasGo.transform);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 14f);
            root.sizeDelta = new Vector2(380f, 32f);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var label = NewUI("Nombre", root);
            label.anchorMin = new Vector2(0f, 1f);
            label.anchorMax = new Vector2(1f, 1f);
            label.pivot = new Vector2(0.5f, 1f);
            label.anchoredPosition = Vector2.zero;
            label.sizeDelta = new Vector2(0f, 16f);
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = 12f;
            text.color = PixelArt.Pale;
            text.characterSpacing = 4f;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            var frame = NewUI("Marco", root);
            frame.anchorMin = new Vector2(0f, 0f);
            frame.anchorMax = new Vector2(1f, 0f);
            frame.pivot = new Vector2(0.5f, 0f);
            frame.anchoredPosition = Vector2.zero;
            frame.sizeDelta = new Vector2(0f, 14f);
            var frameImg = frame.gameObject.AddComponent<UnityEngine.UI.Image>();
            frameImg.sprite = art.DialoguePanel;
            frameImg.type = UnityEngine.UI.Image.Type.Sliced;
            frameImg.raycastTarget = false;

            var inner = NewUI("Interior", frame);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);

            RectTransform Bar(string name, Color color)
            {
                var r = NewUI(name, inner);
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
                var img = r.gameObject.AddComponent<UnityEngine.UI.Image>();
                img.sprite = art.WhitePixel;
                img.color = color;
                img.raycastTarget = false;
                return r;
            }
            var trail = Bar("Rastro", PixelArt.Hex("#6E1F3D"));
            var fill = Bar("Vida", PixelArt.Rose);

            var bar = canvasGo.AddComponent<BossHealthBar>();
            SetRefs(bar, ("root", group), ("nameText", text), ("fill", fill), ("trail", trail));
        }

        /// <summary>Aldeano dormido: respira bajo su manta, le salen "Z", tiene bocadillo para hablar y su diálogo.</summary>
        /// <summary>
        /// Casa de un aldeano, como en Hollow Knight: en la calle solo se ve la fachada con su puerta;
        /// al entrar, la pantalla se funde y apareces en el interior (una habitación aparte, encima del mapa),
        /// donde el aldeano duerme en su cama.
        /// </summary>
        private static void CreateDreamer(Transform parent, Art art, Vector2 feet, int number, int mapHeight)
        {
            var info = Villagers[(number - 1) % Villagers.Length];
            int variant = (number - 1) % VillagerVariants;
            int groundLayer = LayerMask.NameToLayer("Ground");

            // --- Fachada en la calle.
            var house = new GameObject("Casa_" + number);
            house.transform.SetParent(parent, false);
            house.transform.position = feet;
            var hsr = house.AddComponent<SpriteRenderer>();
            hsr.sprite = art.Clips[$"fachada{variant}_idle"][0];
            hsr.sortingOrder = -20;
            AddLight(house.transform, new Vector2(-2.25f, 2.8f), new Color(1f, 0.72f, 0.38f), 0.7f, 2.2f, true);
            AddLight(house.transform, new Vector2(2.25f, 2.8f), new Color(1f, 0.72f, 0.38f), 0.7f, 2.2f, true);
            AddLight(house.transform, new Vector2(1.06f, 1.95f), new Color(1f, 0.65f, 0.3f), 0.9f, 2.6f, true);

            // --- Interior (habitación aparte).
            var origin = new Vector2(6f + (number - 1) * 22f, mapHeight + 12f); // suelo del interior
            var inside = new GameObject("Interior_" + number).transform;
            inside.SetParent(parent, false);
            inside.position = origin;
            CreateRoom(inside, "Habitacion", new Rect(origin.x - 0.5f, origin.y - 1f, 15f, 9f));

            var backdrop = new GameObject("Oscuridad");
            backdrop.transform.SetParent(inside, false);
            backdrop.transform.localPosition = new Vector3(7f, 3.5f, 0f);
            backdrop.transform.localScale = new Vector3(44f, 30f, 1f); // sin llegar al cielo del pueblo
            var bsr = backdrop.AddComponent<SpriteRenderer>();
            bsr.sprite = art.WhitePixel;
            bsr.color = new Color(0.02f, 0.012f, 0.035f);
            bsr.sortingOrder = -40;

            var room = new GameObject("Cuarto");
            room.transform.SetParent(inside, false);
            room.transform.localPosition = new Vector3(-0.5f, -1f, 0f);
            var rsr = room.AddComponent<SpriteRenderer>();
            rsr.sprite = art.Clips[$"interior{variant}_idle"][0];
            rsr.sortingOrder = -30;

            // Paredes, suelo y techo invisibles.
            void Wall(string name, Vector2 center, Vector2 size)
            {
                var w = new GameObject(name);
                w.layer = groundLayer;
                w.transform.SetParent(inside, false);
                w.transform.localPosition = center;
                w.AddComponent<BoxCollider2D>().size = size;
            }
            Wall("Suelo", new Vector2(7f, -0.5f), new Vector2(17f, 1f));
            Wall("ParedIzq", new Vector2(-1f, 3.5f), new Vector2(1f, 10f));
            Wall("ParedDer", new Vector2(15f, 3.5f), new Vector2(1f, 10f));
            Wall("Techo", new Vector2(7f, 7.25f), new Vector2(17f, 1f));

            // Muebles: mesa con vela y el mueble del oficio.
            void Furniture(string name, string clip, float x, int frames)
            {
                var m = new GameObject(name);
                m.transform.SetParent(inside, false);
                m.transform.localPosition = new Vector3(x, 0f, 0f);
                var msr = m.AddComponent<SpriteRenderer>();
                msr.sprite = art.Clips[clip + "_idle"][0];
                msr.sortingOrder = -15;
                if (frames > 1) SetClips(m.AddComponent<SpriteFlipbook>(), art, clip, false, ("idle", 3f, true));
            }
            Furniture("Mesa", "mesa", 4.2f, 1);
            Furniture("Oficio", $"oficio{variant}", 12.3f, 2);

            AddLight(inside, new Vector2(7.3f, 4.6f), new Color(0.65f, 0.6f, 1f), 0.6f, 3.2f, false);   // luna
            AddLight(inside, new Vector2(4.2f, 1.7f), new Color(1f, 0.75f, 0.4f), 0.9f, 2.8f, true);    // vela
            var propLight = new[] { new Vector2(12.3f, 1f), new Vector2(13.4f, 0.3f), new Vector2(12.3f, 2.1f), new Vector2(12.3f, 1.5f), new Vector2(12.3f, 3f) }[variant];
            var propColor = new[] { new Color(1f, 0.5f, 0.25f), new Color(1f, 0.45f, 0.2f), new Color(1f, 0.8f, 0.45f), new Color(0.7f, 0.6f, 1f), new Color(0.8f, 0.7f, 1f) }[variant];
            AddLight(inside, propLight, propColor, variant < 3 ? 1f : 0.4f, variant < 3 ? 3.2f : 2f, variant < 3);

            // Puertas enlazadas: calle -> interior y de vuelta.
            var arriveInside = new GameObject("LlegadaInterior").transform;
            arriveInside.SetParent(inside, false);
            arriveInside.localPosition = new Vector3(2.7f, 0.05f, 0f);
            var arriveStreet = new GameObject("LlegadaCalle").transform;
            arriveStreet.SetParent(house.transform, false);
            arriveStreet.localPosition = new Vector3(1.3f, 0.05f, 0f);
            CreateDoor(house.transform, art, Vector2.zero, arriveInside, 2.5f);
            CreateDoor(inside, art, new Vector2(1.125f, 0f), arriveStreet, 3.6f);

            // --- El aldeano en su cama, bajo la ventana.
            var go = new GameObject("Aldeano_" + number);
            go.transform.SetParent(inside, false);
            go.transform.position = origin + new Vector2(7.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Clips[$"aldeano{variant}_sleep"][0];
            sr.sortingOrder = 4;
            var book = go.AddComponent<SpriteFlipbook>();
            SetClips(book, art, "aldeano" + variant, false, ("sleep", 2.5f, true), ("awake", 1.5f, true));

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.8f, 2.4f);
            col.offset = new Vector2(0f, 1.2f);

            var zs = new Object[3];
            for (int i = 0; i < zs.Length; i++)
            {
                var z = new GameObject("Zzz" + i);
                z.transform.SetParent(go.transform, false);
                z.transform.localPosition = new Vector3(-0.75f, 1.5f, 0f);
                var zsr = z.AddComponent<SpriteRenderer>();
                zsr.sprite = art.SleepZ;
                zsr.sortingOrder = 6;
                zs[i] = z.transform;
            }

            var prompt = new GameObject("Hablar");
            prompt.transform.SetParent(go.transform, false);
            prompt.transform.localPosition = new Vector3(-0.8f, 2.3f, 0f);
            var psr = prompt.AddComponent<SpriteRenderer>();
            psr.sprite = art.Prompt;
            psr.sortingOrder = 20;
            prompt.SetActive(false);

            var npc = go.AddComponent<DreamerNPC>();
            SetRefs(npc, ("body", book), ("prompt", prompt), ("sleepZ", zs));
            var so = new SerializedObject(npc);
            so.FindProperty("dreamerId").stringValue = go.name;
            so.FindProperty("displayName").stringValue = info.Name;
            FillStrings(so.FindProperty("sleepingLines"), info.Sleeping);
            FillStrings(so.FindProperty("awakeLines"), info.Awake);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateRoom(Transform parent, string name, Rect area)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, true);
            var room = go.AddComponent<Room>();
            var so = new SerializedObject(room);
            so.FindProperty("area").rectValue = area;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Puerta con flecha dorada; lleva al jugador a "destination".</summary>
        private static void CreateDoor(Transform parent, Art art, Vector2 local, Transform destination, float arrowHeight)
        {
            var go = new GameObject("Puerta");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.3f, 2.2f);
            col.offset = new Vector2(0f, 1.1f);

            var arrow = new GameObject("Flecha");
            arrow.transform.SetParent(go.transform, false);
            arrow.transform.localPosition = new Vector3(0f, arrowHeight, 0f);
            var asr = arrow.AddComponent<SpriteRenderer>();
            asr.sprite = art.DoorArrow;
            asr.sortingOrder = 20;
            arrow.SetActive(false);

            var door = go.AddComponent<Door>();
            SetRefs(door, ("destination", destination), ("prompt", arrow));
        }

        private static void FillStrings(SerializedProperty prop, string[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private const int VillagerVariants = 5;

        private struct VillagerInfo
        {
            public string Name;
            public string[] Sleeping, Awake;
        }

        /// <summary>Quién duerme en el pueblo y qué murmura (en el orden de Hub.txt, de arriba a abajo y de izquierda a derecha).</summary>
        private static readonly VillagerInfo[] Villagers =
        {
            new VillagerInfo
            {
                Name = "Maela, la panadera",
                Sleeping = new[]
                {
                    "...el horno... no deja de arder... ¿quién está ahí dentro?",
                    "Mmm... las sombras me piden pan... pan de ceniza...",
                    "(Su respiración es agitada. Algo la persigue en sueños.)",
                },
                Awake = new[]
                {
                    "Kael... soñé que el fuego tenía ojos. Y tú lo apagaste.",
                    "Ya no tengo miedo de cerrar los ojos. Gracias.",
                },
            },
            new VillagerInfo
            {
                Name = "Bram, el herrero",
                Sleeping = new[]
                {
                    "...el martillo pesa... cada golpe suena como una campana...",
                    "No... no abras esa puerta, hija...",
                    "(Aprieta los puños bajo la manta.)",
                },
                Awake = new[]
                {
                    "Desperté con las manos temblando... pero desperté.",
                    "Si alguna vez necesitas una hoja nueva, la forja es tuya.",
                },
            },
            new VillagerInfo
            {
                Name = "Hermana Ilse",
                Sleeping = new[]
                {
                    "...las velas se apagan una a una... y nadie reza...",
                    "La catedral... respira... la oigo respirar...",
                    "(Murmura una oración que nunca termina.)",
                },
                Awake = new[]
                {
                    "Las velas volvieron a encenderse. Lo vi en el sueño.",
                    "Que la luz guíe tu máscara, caballero.",
                },
            },
            new VillagerInfo
            {
                Name = "Tobi",
                Sleeping = new[]
                {
                    "...mamá... la polilla grande está en la ventana...",
                    "No quiero jugar más a las escondidas... me están buscando...",
                    "(Se abraza a un muñeco de trapo.)",
                },
                Awake = new[]
                {
                    "¡Kael! ¡Le pegaste a la polilla gigante! ¿Verdad que sí?",
                    "Ya no me escondo debajo de la cama.",
                },
            },
            new VillagerInfo
            {
                Name = "El viejo Orren",
                Sleeping = new[]
                {
                    "...he soñado este sueño mil veces... siempre termina igual...",
                    "El ojo... el ojo que nunca parpadea...",
                    "(Sonríe con tristeza, sin despertar.)",
                },
                Awake = new[]
                {
                    "Mil noches en el mismo sueño... y hoy terminó distinto.",
                    "Hay más durmientes ahí abajo, muchacho. El sueño eterno no descansa.",
                },
            },
        };

        /// <summary>Caja de diálogo (abajo de la pantalla) con nombre, texto, flecha y dos opciones.</summary>
        private static void CreateDialogueBox(Art art)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            var canvasGo = new GameObject("Dialogo", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = NewUI("Panel", canvasGo.transform);
            panel.anchorMin = new Vector2(0.5f, 0f);
            panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 12f);
            panel.sizeDelta = new Vector2(540f, 92f);
            var img = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = art.DialoguePanel;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.5f; // borde al doble: pixelado y nítido
            img.raycastTarget = false;

            TMP_Text Text(string name, Vector2 pos, Vector2 size, float fontSize, Color color)
            {
                var r = NewUI(name, panel);
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = pos;
                r.sizeDelta = size;
                var t = r.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) t.font = font;
                t.fontSize = fontSize;
                t.color = color;
                t.alignment = TextAlignmentOptions.TopLeft;
                t.raycastTarget = false;
                return t;
            }

            var nameText = Text("Nombre", new Vector2(14f, -8f), new Vector2(300f, 18f), 13f, PixelArt.Gold);
            var body = Text("Texto", new Vector2(14f, -28f), new Vector2(512f, 44f), 12f, PixelArt.Moon);
            var choiceA = Text("OpcionA", new Vector2(14f, -70f), new Vector2(250f, 16f), 12f, PixelArt.Lilac);
            var choiceB = Text("OpcionB", new Vector2(270f, -70f), new Vector2(250f, 16f), 12f, PixelArt.Lilac);

            var next = NewUI("Siguiente", panel);
            next.anchorMin = next.anchorMax = next.pivot = new Vector2(1f, 0f);
            next.anchoredPosition = new Vector2(-12f, 8f);
            next.sizeDelta = new Vector2(14f, 10f);
            var nextImg = next.gameObject.AddComponent<UnityEngine.UI.Image>();
            nextImg.sprite = art.DialogueNext;
            nextImg.raycastTarget = false;

            var box = canvasGo.AddComponent<DialogueBox>();
            SetRefs(box, ("root", panel.gameObject), ("nameText", nameText), ("bodyText", body),
                ("nextIcon", next.gameObject), ("choiceTexts", new Object[] { choiceA, choiceB }));
        }

        // ---------- Pueblo: personajes, objetos, faroles, coleccionables y secretos ----------

        private struct TalkerInfo
        {
            public char Key;
            public string Name, Art;
            public float Fps;
            public bool Face, Object;
            public Vector2? Light;
            public int SecretAfter;
            public string[] Lines, Secret, Reward;
        }

        /// <summary>
        /// Quién o qué hay en cada letra del mapa del pueblo:
        /// V Vesna · O Grim · U Lune · Y Ash · K gato · A estatua · W pozo · R lápida rara · m espejo (sala secreta) · D diario
        /// I piedra de la torre · T inscripción del mausoleo.
        /// </summary>
        private static readonly TalkerInfo[] Talkers =
        {
            new TalkerInfo
            {
                Key = 'V', Name = "Vesna, la farolera", Art = "npc_vesna", Fps = 4f, Face = true, Light = new Vector2(-0.25f, 1.52f),
                Lines = new[]
                {
                    "Enciendo los faroles cada noche para que los durmientes encuentren el camino de vuelta.",
                    "Dicen que las Lágrimas de Luna caen donde alguien tuvo un sueño bonito. Aún quedan algunas por el pueblo.",
                    "Si buscas bien... algunas paredes no son tan sólidas como parecen.",
                },
            },
            new TalkerInfo
            {
                Key = 'O', Name = "Grim, el mercader", Art = "npc_grim", Fps = 3f, Face = true, Light = new Vector2(0.7f, 0.2f),
                Lines = new[]
                {
                    "¿Comprar? No, no... ya no vendo nada. Las monedas no sirven en los sueños.",
                    "Pero colecciono cosas brillantes. Tráeme todas las Lágrimas de Luna del pueblo y te contaré un secreto.",
                },
                Reward = new[]
                {
                    "¡Todas las Lágrimas! Brillan como el primer día...",
                    "Un trato es un trato: antes de ti hubo otro caballero con máscara. Entró en el sueño eterno y nunca volvió.",
                    "Su estatua está a la entrada del pueblo. Quizás, en algún lugar, él también te está soñando.",
                },
            },
            new TalkerInfo
            {
                Key = 'U', Name = "Lune", Art = "npc_lune", Fps = 5f, Face = true,
                Lines = new[]
                {
                    "¡Mira! Atrapé una polilla de luz. Es de las buenas, no muerde.",
                    "Mi hermano Tobi está dormido y no despierta... ¿Tú puedes ayudarlo?",
                    "Cuando sea grande quiero ser caballera. Con máscara y todo.",
                },
            },
            new TalkerInfo
            {
                Key = 'Y', Name = "Ash, el sepulturero", Art = "npc_ash", Fps = 3f, Face = true,
                Lines = new[]
                {
                    "Cavo tumbas para los que nunca despertaron.",
                    "No me mires así. Hoy no he cavado ninguna. Será gracias a ti.",
                    "Hay una lápida muy rara al fondo del cementerio. Léela, si te atreves.",
                },
            },
            new TalkerInfo
            {
                Key = 'K', Name = "Gato negro", Art = "npc_gato", Fps = 4f, Face = true, SecretAfter = 5,
                Lines = new[] { "Miau.", "(El gato parpadea lentamente. Es una señal de confianza.)" },
                Secret = new[]
                {
                    "...",
                    "Está bien. Ya que insistes tanto: los sueños también sueñan.",
                    "(El gato bosteza y hace como si no hubiera dicho nada.)",
                },
            },
            new TalkerInfo
            {
                Key = 'A', Name = "Estatua", Art = "obj_estatua", Object = true,
                Lines = new[]
                {
                    "Una estatua de un caballero con máscara. Se parece muchísimo a ti.",
                    "La placa dice: \"Al primer soñador, que entró en el sueño eterno y nunca volvió\".",
                },
            },
            new TalkerInfo
            {
                Key = 'W', Name = "Pozo", Art = "obj_pozo", Object = true, SecretAfter = 3,
                Lines = new[] { "Te asomas al pozo. Muy al fondo, algo brilla." },
                Secret = new[]
                {
                    "Gritas tu nombre al pozo.",
                    "\"...Kael...\" responde el eco. Y luego, mucho más bajo: \"...gracias...\"",
                },
            },
            new TalkerInfo
            {
                Key = 'R', Name = "Lápida", Art = "obj_tumba3", Object = true,
                Lines = new[]
                {
                    "\"Aquí yace el último bug de este juego.\"",
                    "\"Murió como vivió: sin que nadie pudiera reproducirlo.\"",
                    "(Alguien le dejó una flor.)",
                },
            },
            new TalkerInfo
            {
                Key = 'm', Name = "Espejo", Art = "obj_espejo", Object = true, Light = new Vector2(0f, 1.1f),
                Lines = new[]
                {
                    "Te miras en el espejo.",
                    "Tu reflejo tarda un segundo de más en moverse...",
                    "...y por un instante, juraría que sonrió.",
                },
            },
            new TalkerInfo
            {
                Key = 'I', Name = "Piedra grabada", Art = "obj_tumba1", Object = true,
                Lines = new[]
                {
                    "\"Más allá de esta torre duermen el último aldeano y el cementerio.\"",
                    "\"Quien solo salta una vez, que mire desde abajo.\"",
                    "(Cada aldeano que despiertes te dejará un regalo.)",
                },
            },
            new TalkerInfo
            {
                Key = 'T', Name = "Inscripción", Art = "obj_tumba2", Object = true,
                Lines = new[]
                {
                    "\"Mausoleo de los que nunca despertaron.\"",
                    "\"El velo no se abre con fuerza: hay que cruzarlo siendo sombra.\"",
                    "\"Dentro aguarda el Corazón de Luna, para quien sepa saltar el abismo.\"",
                },
            },
            new TalkerInfo
            {
                Key = 'D', Name = "Diario", Art = "obj_diario", Object = true,
                Lines = new[]
                {
                    "Un diario viejo, abierto en la última página:",
                    "\"Si estás leyendo esto, gracias por jugar hasta aquí arriba.\"",
                    "\"Que tus sueños sean siempre amables. —A.B.\"",
                },
            },
        };

        private static bool IsTalker(char c) => Talkers.Any(t => t.Key == c);

        private static void CreateTalker(Transform parent, Art art, Vector2 feet, char key)
        {
            var info = Talkers.First(t => t.Key == key);
            var go = new GameObject(info.Name);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;

            var frames = art.Clips[info.Art + "_idle"];
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = info.Object ? -5 : 5;
            if (frames.Length > 1) SetClips(go.AddComponent<SpriteFlipbook>(), art, info.Art, false, ("idle", info.Fps, true));

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2.4f, 2.4f);
            col.offset = new Vector2(0f, 1.2f);

            var prompt = new GameObject("Hablar");
            prompt.transform.SetParent(go.transform, false);
            prompt.transform.localPosition = new Vector3(0f, frames[0].bounds.size.y + 0.35f, 0f);
            var psr = prompt.AddComponent<SpriteRenderer>();
            psr.sprite = art.Prompt;
            psr.sortingOrder = 20;
            prompt.SetActive(false);

            var npc = go.AddComponent<TalkingNPC>();
            SetRefs(npc, ("prompt", prompt), ("body", sr));
            var so = new SerializedObject(npc);
            so.FindProperty("npcId").stringValue = info.Art;
            so.FindProperty("displayName").stringValue = info.Name;
            so.FindProperty("facePlayer").boolValue = info.Face;
            so.FindProperty("secretAfterTalks").intValue = info.SecretAfter;
            FillStrings(so.FindProperty("lines"), info.Lines);
            FillStrings(so.FindProperty("secretLines"), info.Secret ?? new string[0]);
            FillStrings(so.FindProperty("rewardLines"), info.Reward ?? new string[0]);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (info.Light.HasValue) AddLight(go.transform, info.Light.Value, new Color(1f, 0.8f, 0.45f), 0.9f, 2.6f, true);
        }

        private static void CreateCollectible(Transform parent, Art art, Vector2 center, string id, bool hidden)
        {
            var go = new GameObject("LagrimaDeLuna");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Clips["lagrima_idle"][0];
            sr.sortingOrder = hidden ? -1 : 6; // las escondidas quedan detrás de la pared falsa
            SetClips(go.AddComponent<SpriteFlipbook>(), art, "lagrima", false, ("idle", 6f, true));

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;
            var c = go.AddComponent<Collectible>();
            var so = new SerializedObject(c);
            so.FindProperty("id").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();

            AddLight(go.transform, Vector2.zero, new Color(0.6f, 0.8f, 1f), hidden ? 0.35f : 0.7f, 1.7f, true);
        }

        /// <summary>Una casilla de velo de sombra: pared sólida que solo cruza el Paso de Sombra.</summary>
        private static void CreateVeil(Transform parent, Art art, Vector2 center)
        {
            var go = new GameObject("VeloDeSombra");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Veil[0];
            sr.sortingOrder = 8;
            SetRefs(go.AddComponent<SpriteFlipbook>(), ("frames", art.Veil));
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            go.AddComponent<DashVeil>();
            AddLight(go.transform, Vector2.zero, new Color(0.55f, 0.35f, 1f), 0.6f, 1.8f, true);
        }

        private static void CreateMoonHeart(Transform parent, Art art, Vector2 center, string id)
        {
            var go = new GameObject("CorazonDeLuna");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.MoonHeart;
            sr.sortingOrder = 6;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            var heart = new SerializedObject(go.AddComponent<HeartPickup>());
            heart.FindProperty("id").stringValue = id;
            heart.ApplyModifiedPropertiesWithoutUndo();
            AddLight(go.transform, Vector2.zero, new Color(1f, 0.5f, 0.65f), 0.9f, 2.4f, true);
        }

        private static void CreateLamp(Transform parent, Art art, Vector2 feet)
        {
            var go = new GameObject("Farola");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Clips["farol_idle"][0];
            sr.sortingOrder = -5;
            SetClips(go.AddComponent<SpriteFlipbook>(), art, "farol", false, ("idle", 4f, true));
            AddLight(go.transform, new Vector2(0f, 2.75f), new Color(1f, 0.72f, 0.38f), 1f, 4.5f, true);
        }

        private static void CreateGrave(Transform parent, Art art, Vector2 feet, int type)
        {
            var go = new GameObject("Tumba");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Clips[$"obj_tumba{type}_idle"][0];
            sr.sortingOrder = -5;
        }

        private static void AddLight(Transform parent, Vector2 local, Color color, float intensity, float radius, bool flicker)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.15f;
            l.falloffIntensity = 0.7f;
            if (flicker) go.AddComponent<LightFlicker>();
        }

        /// <summary>Agrupa las casillas de pared falsa que se tocan y crea una zona secreta por grupo.</summary>
        /// <summary>Separa las casillas en grupos de casillas vecinas (arriba, abajo, izquierda, derecha).</summary>
        private static List<List<Vector3Int>> ConnectedGroups(List<Vector3Int> cells)
        {
            var groups = new List<List<Vector3Int>>();
            var left = new HashSet<Vector3Int>(cells);
            while (left.Count > 0)
            {
                var group = new List<Vector3Int>();
                var queue = new Queue<Vector3Int>();
                var first = left.First();
                queue.Enqueue(first);
                left.Remove(first);
                while (queue.Count > 0)
                {
                    var c = queue.Dequeue();
                    group.Add(c);
                    foreach (var d in new[] { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down })
                        if (left.Remove(c + d)) queue.Enqueue(c + d);
                }
                groups.Add(group);
            }
            return groups;
        }

        /// <summary>Suelo agrietado ('%'): suelo sólido más oscuro que solo rompe el golpe al suelo.</summary>
        private static void CreateCrackedFloors(Transform grid, Art art, List<Vector3Int> cells, int groundLayer)
        {
            int n = 0;
            foreach (var group in ConnectedGroups(cells))
            {
                var map = NewTilemap("SueloAgrietado_" + (++n), grid, 0);
                map.gameObject.layer = groundLayer;
                map.color = new Color(0.7f, 0.62f, 0.8f);
                foreach (var c in group) map.SetTile(c, art.Ground);
                map.gameObject.AddComponent<TilemapCollider2D>();
                map.gameObject.AddComponent<BreakableFloor>();
            }
        }

        private static GameObject CreateUpgradePickup(Transform parent, Art art, Vector2 center, Upgrade upgrade)
        {
            var go = new GameObject("Mejora_" + upgrade);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art.Orb[0]; // de momento usa el orbe de sueño, teñido
            sr.color = new Color(0.8f, 0.7f, 1f);
            sr.sortingOrder = 6;
            SetRefs(go.AddComponent<SpriteFlipbook>(), ("frames", art.Orb));

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;
            var pickup = new SerializedObject(go.AddComponent<UpgradePickup>());
            pickup.FindProperty("upgrade").enumValueIndex = (int)upgrade;
            pickup.ApplyModifiedPropertiesWithoutUndo();
            AddLight(go.transform, Vector2.zero, new Color(0.7f, 0.6f, 1f), 0.9f, 2.4f, true);
            return go;
        }

        private static void CreateSecrets(Tilemap ground, List<Vector3Int> cells)
        {
            int n = 0;
            foreach (var group in ConnectedGroups(cells))
            {
                int minX = group.Min(c => c.x), maxX = group.Max(c => c.x);
                int minY = group.Min(c => c.y), maxY = group.Max(c => c.y);
                var go = new GameObject("Secreto_" + (++n));
                go.transform.position = new Vector3((minX + maxX + 1) * 0.5f, (minY + maxY + 1) * 0.5f, 0f);
                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(maxX - minX + 1, maxY - minY + 1);

                var area = go.AddComponent<SecretArea>();
                var so = new SerializedObject(area);
                so.FindProperty("wall").objectReferenceValue = ground;
                so.FindProperty("secretId").stringValue = go.name;
                var prop = so.FindProperty("cells");
                prop.arraySize = group.Count;
                for (int i = 0; i < group.Count; i++) prop.GetArrayElementAtIndex(i).vector3IntValue = group[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Dibuja y guarda el arte del pueblo y la pantalla de muerte.</summary>
        private static void CreateVillageClips()
        {
            var feet = new Vector2(0.5f, 0f);
            float ppu = VillageArt.PPU;
            float npc = VillageArtHD.NpcPPU, home = VillageArtHD.HousePPU;
            for (int v = 0; v < VillagerVariants; v++)
            {
                SaveClip($"fachada{v}_idle", new[] { VillageArtHD.Facade(v) }, feet, false, home);
                SaveClip($"interior{v}_idle", new[] { VillageArtHD.Interior(v) }, Vector2.zero, false, home);
                SaveClip($"oficio{v}_idle", VillageArtHD.Prop(v), feet, false, home);
                SaveClip($"aldeano{v}_sleep", VillageArtHD.Sleeping(v), feet, false, VillageArtHD.BedPPU);
                SaveClip($"aldeano{v}_awake", VillageArtHD.Awake(v), feet, false, VillageArtHD.BedPPU);
            }
            SaveClip("mesa_idle", new[] { VillageArtHD.Table() }, feet, false, home);
            SaveClip("npc_vesna_idle", VillageArtHD.Lamplighter(), feet, false, npc);
            SaveClip("npc_grim_idle", VillageArtHD.Merchant(), feet, false, npc);
            SaveClip("npc_ash_idle", VillageArtHD.Gravedigger(), feet, false, npc);
            SaveClip("npc_lune_idle", VillageArtHD.Girl(), feet, false, npc);
            SaveClip("npc_gato_idle", VillageArtHD.Cat(), feet, false, npc);
            SaveClip("obj_estatua_idle", new[] { VillageArt.Statue() }, feet, false, ppu);
            SaveClip("obj_pozo_idle", new[] { VillageArt.Well() }, feet, false, ppu);
            for (int t = 0; t < 4; t++) SaveClip($"obj_tumba{t}_idle", new[] { VillageArt.Grave(t) }, feet, false, ppu);
            SaveClip("obj_espejo_idle", new[] { VillageArt.Mirror() }, feet, false, ppu);
            SaveClip("obj_diario_idle", new[] { VillageArt.Diary() }, feet, false, ppu);
            SaveClip("farol_idle", VillageArt.LampPost(), feet, false, ppu);
            SaveClip("lagrima_idle", VillageArt.MoonTear(), new Vector2(0.5f, 0.5f), false, ppu);
        }

        /// <summary>Pantalla de muerte (oculta): fondo oscuro, viñeta, máscara partida, título, frase y opciones.</summary>
        private static void CreateDeathScreen(Art art)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            var canvasGo = new GameObject("PantallaMuerte", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform Stretch(string name, Transform parent)
            {
                var r = NewUI(name, parent);
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
                return r;
            }

            var root = Stretch("Contenido", canvasGo.transform);
            var overlay = Stretch("Oscuridad", root).gameObject.AddComponent<UnityEngine.UI.Image>();
            overlay.color = new Color(0.03f, 0.02f, 0.05f, 0f);
            overlay.raycastTarget = false;
            var vignette = Stretch("Vineta", root).gameObject.AddComponent<UnityEngine.UI.Image>();
            vignette.sprite = art.Vignette;
            vignette.raycastTarget = false;

            RectTransform MaskHalf(string name, Sprite sprite)
            {
                var r = NewUI(name, root);
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(96f, 120f);
                var img = r.gameObject.AddComponent<UnityEngine.UI.Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                return r;
            }
            var maskL = MaskHalf("MascaraIzq", art.MaskLeft);
            var maskR = MaskHalf("MascaraDer", art.MaskRight);

            TMP_Text Text(string name, float y, float size, Color color, string text)
            {
                var r = NewUI(name, root);
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, y);
                r.sizeDelta = new Vector2(620f, size + 10f);
                var t = r.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null) t.font = font;
                t.fontSize = size;
                t.color = color;
                t.alignment = TextAlignmentOptions.Center;
                t.raycastTarget = false;
                t.text = text;
                return t;
            }

            var title = Text("Titulo", -30f, 24f, PixelArt.Pale, "EL SUEÑO TE CONSUMIÓ");
            title.characterSpacing = 8f;
            var quote = Text("Frase", -56f, 11f, PixelArt.Soft, "");
            quote.fontStyle = FontStyles.Italic;
            var options = new Object[]
            {
                Text("Opcion0", -92f, 13f, PixelArt.Lilac, ""),
                Text("Opcion1", -112f, 13f, PixelArt.Lilac, ""),
                Text("Opcion2", -132f, 13f, PixelArt.Lilac, ""),
            };

            var screen = canvasGo.AddComponent<DeathScreen>();
            SetRefs(screen, ("root", root.gameObject), ("overlay", overlay), ("vignette", vignette),
                ("maskLeft", maskL), ("maskRight", maskR), ("title", title), ("quote", quote),
                ("options", options), ("moteSprite", art.Mote));
        }

        private static void CreateLevelManager(Rect bounds)
        {
            var go = new GameObject("LevelManager");
            var manager = go.AddComponent<LevelManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("bounds").rectValue = bounds;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Paredes invisibles en los extremos para no salirse del nivel por los lados.
            AddWall(go.transform, "LeftWall", bounds.xMin - 0.5f, bounds);
            AddWall(go.transform, "RightWall", bounds.xMax + 0.5f, bounds);
        }

        private static void AddWall(Transform parent, string name, float x, Rect bounds)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = new Vector3(x, bounds.center.y + 5f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, bounds.height + 30f);
        }

        private static void CreateLight(float intensity)
        {
            // Sin luz global, los sprites con el material 2D Lit de URP se verían negros.
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = intensity;
        }

        private static Camera CreateCamera(Transform player, Vector2 start, Rect bounds, float size)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PixelArt.SkyTop;
            go.transform.position = new Vector3(start.x, start.y + 1.5f, -10f);
            go.AddComponent<AudioListener>();

            var follow = go.AddComponent<CameraFollow2D>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player;
            so.FindProperty("useBounds").boolValue = true;
            so.FindProperty("bounds").rectValue = bounds;
            so.ApplyModifiedPropertiesWithoutUndo();
            return cam;
        }

        private static void CreateBackground(Art art, Camera cam, Rect bounds, string levelName)
        {
            var root = new GameObject("Background").transform;
            float span = Mathf.Max(60f, cam.orthographicSize * 2f * 16f / 9f * 2f);

            // Dónde empieza realmente la cámara (CameraFollow2D la limita al nivel).
            float viewH = cam.orthographicSize * 2f, viewW = viewH * 16f / 9f;
            Vector3 c = cam.transform.position;
            float camMinX = bounds.xMin + viewW / 2f, camMaxX = Mathf.Max(camMinX, bounds.xMax - viewW / 2f);
            c.x = Mathf.Clamp(c.x, camMinX, camMaxX);
            if (bounds.height > viewH) c.y = Mathf.Clamp(c.y, bounds.yMin + viewH / 2f, bounds.yMax - viewH / 2f);
            layerCameraReference = c; // todas las capas se colocan para esta posición de cámara

            var image = LoadLevelBackground(levelName);
            if (image == null && levelName == "Hub") image = LoadLevelBackground(HubBackgroundFallback);
            if (image == null && levelName == "Arena") image = LoadLevelBackground(ArenaBackgroundFallback);
            if (image != null)
            {
                // Fondo ilustrado del nivel: se mueve un poco más lento que la cámara.
                const float followX = 0.85f;
                float travel = camMaxX - camMinX;
                float needW = viewW * 1.35f + travel * (1f - followX); // 1.35: margen para pantallas más anchas
                float needH = viewH * 1.1f;
                float s = Mathf.Max(needW / image.bounds.size.x, needH / image.bounds.size.y);

                var bg = AddLayer(root, "LevelBackground", image, -100,
                    new Vector2(c.x + travel * (1f - followX) / 2f, c.y), followX, 1f, false);
                bg.transform.localScale = new Vector3(s, s, 1f);
                bg.color = new Color(0.72f, 0.72f, 0.8f); // un poco más oscuro para que resalten el jugador y los enemigos
            }
            else
            {
                // Cielo pegado a la cámara (cubre toda la vista).
                var sky = AddLayer(root, "Sky", art.Sky, -100, new Vector2(c.x, c.y), 1f, 1f, false);
                sky.transform.localScale = new Vector3(span / art.Sky.bounds.size.x, 16f / art.Sky.bounds.size.y, 1f);

                Tiled(AddLayer(root, "Stars", art.Stars, -90, new Vector2(c.x, c.y + 1f), 0.05f, 0.95f, true), span, 16f);
                AddLayer(root, "Moon", art.Moon, -85, new Vector2(c.x + 6f, c.y + 3.5f), 0.02f, 0.97f, false);
                Tiled(AddLayer(root, "HillsFar", art.HillsFar, -80, new Vector2(c.x, 4.5f), 0.25f, 0.6f, true), span, 4f);
                Tiled(AddLayer(root, "HillsNear", art.HillsNear, -70, new Vector2(c.x, 3f), 0.5f, 0.75f, true), span, 3f);
            }

            if (image != null) return; // el fondo ilustrado ya trae su niebla

            var fog = AddLayer(root, "Fog", art.Fog, -60, new Vector2(c.x, 3.5f), 0.7f, 0.8f, true);
            Tiled(fog, span, 4f);
            fog.color = PixelArt.WithAlpha(Color.white, 0.35f);
            var so = new SerializedObject(fog.GetComponent<ParallaxLayer>());
            so.FindProperty("drift").floatValue = 0.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Busca Assets/Levels/Backgrounds/&lt;nivel&gt;.png/.jpg y lo prepara como sprite.</summary>
        private static Sprite LoadLevelBackground(string levelName)
        {
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                string path = $"{LevelsFolder}/Backgrounds/{levelName}{ext}";
                if (!File.Exists(path)) continue;

                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                if (imp == null) { AssetDatabase.ImportAsset(path); imp = (TextureImporter)AssetImporter.GetAtPath(path); }
                if (imp == null) continue;

                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.maxTextureSize = 4096;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        private static Vector2 layerCameraReference;

        private static SpriteRenderer AddLayer(Transform parent, string name, Sprite sprite, int order,
            Vector2 position, float followX, float followY, bool repeat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;

            var layer = go.AddComponent<ParallaxLayer>();
            var so = new SerializedObject(layer);
            so.FindProperty("followX").floatValue = followX;
            so.FindProperty("followY").floatValue = followY;
            so.FindProperty("repeat").boolValue = repeat;
            so.FindProperty("useCameraReference").boolValue = true;
            so.FindProperty("cameraReference").vector2Value = layerCameraReference;
            so.ApplyModifiedPropertiesWithoutUndo();
            return sr;
        }

        private static void Tiled(SpriteRenderer sr, float width, float height)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, height);
        }

        private const int MaxHearts = 6; // 3 iniciales + los de las mejoras (HealthHUD oculta los que sobran)

        private static void CreateHud(Art art, bool collectibles)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = NewUI("Hearts", canvasGo.transform);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(12f, -12f);
            panel.sizeDelta = new Vector2(MaxHearts * 22f, 16f);
            var layout = panel.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var hearts = new UnityEngine.UI.Image[MaxHearts];
            for (int i = 0; i < hearts.Length; i++)
            {
                var h = NewUI("Heart" + i, panel);
                h.sizeDelta = new Vector2(18f, 16f); // 2× el sprite de 9×8
                hearts[i] = h.gameObject.AddComponent<UnityEngine.UI.Image>();
                hearts[i].sprite = art.HeartFull;
                hearts[i].raycastTarget = false;
            }

            var hud = canvasGo.AddComponent<HealthHUD>();
            SetRefs(hud, ("hearts", hearts), ("fullHeart", art.HeartFull), ("emptyHeart", art.HeartEmpty));

            if (!collectibles) return;
            var tears = NewUI("Lagrimas", canvasGo.transform);
            tears.anchorMin = tears.anchorMax = tears.pivot = new Vector2(0f, 1f);
            tears.anchoredPosition = new Vector2(13f, -34f);
            tears.sizeDelta = new Vector2(90f, 18f);
            var icon = NewUI("Icono", tears);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(13f, 16f);
            var iconImg = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
            iconImg.sprite = art.Clips["lagrima_idle"][0];
            iconImg.raycastTarget = false;
            var label = NewUI("Texto", tears);
            label.anchorMin = label.anchorMax = label.pivot = new Vector2(0f, 0.5f);
            label.anchoredPosition = new Vector2(18f, 0f);
            label.sizeDelta = new Vector2(70f, 18f);
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font != null) text.font = font;
            text.fontSize = 12f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            SetRefs(canvasGo.AddComponent<CollectibleCounter>(), ("label", text), ("icon", icon));
        }

        // ---------- Build Settings ----------

        private static void UpdateBuildSettings(List<string> levelScenes)
        {
            // Orden del juego: menú, luego los niveles. SampleScene queda como escena de pruebas.
            var list = new List<EditorBuildSettingsScene>();
            if (File.Exists(MenuScenePath)) list.Add(new EditorBuildSettingsScene(MenuScenePath, true));
            list.AddRange(levelScenes.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ---------- Arte ----------

        private static Art CreateArt()
        {
            var a = new Art();
            var bottomCenter = new Vector2(0.5f, 0f);

            var groundPieces = new Sprite[16][];
            for (int open = 0; open < groundPieces.Length; open++)
            {
                // Las piezas que más se ven repetidas (relleno y superficie plana) tienen variantes.
                int variants = open == 0 || open == GroundArt.OpenUp ? GroundArt.GroundVariants : 1;
                groundPieces[open] = new Sprite[variants];
                for (int v = 0; v < variants; v++)
                    groundPieces[open][v] = PixelArt.SaveSprite(GroundArtFolder, $"suelo_{open}_{v}", GroundArt.Ground(open, v), 0, GroundArt.PPU);
            }
            a.Ground = SaveRuleTile("ground", groundPieces, Tile.ColliderType.Grid);
            a.GroundSecret = SaveRuleTile("ground_secret", groundPieces, Tile.ColliderType.None);
            a.BackWall = SaveTile("back_wall", groundPieces[0][0], Tile.ColliderType.None);

            var platformPieces = new Sprite[16][];
            for (int open = 0; open < platformPieces.Length; open++)
                platformPieces[open] = new[] { PixelArt.SaveSprite(GroundArtFolder, $"plataforma_{open & 6}", GroundArt.Platform(open), 0, GroundArt.PPU) };
            a.Platform = SaveRuleTile("platform", platformPieces, Tile.ColliderType.Sprite);

            a.Spike = SaveTile("spikes", PixelArt.SaveSprite(GroundArtFolder, "pinchos", GroundArt.Spikes(), 0, GroundArt.PPU), Tile.ColliderType.Sprite);
            a.Veil = Enumerable.Range(0, 4)
                .Select(i => PixelArt.SaveSprite(GroundArtFolder, "velo_" + i, GroundArt.Veil(i, 4), 0, GroundArt.PPU))
                .ToArray();

            a.Portal = Enumerable.Range(0, 3)
                .Select(i => PixelArt.SaveSprite(ArtFolder, "portal_" + i, PortalTexture(i), 0, TilePPU, bottomCenter))
                .ToArray();

            a.Orb = new[]
            {
                PixelArt.SaveSprite(ArtFolder, "orbe_0", OrbTexture(0), 0, TilePPU),
                PixelArt.SaveSprite(ArtFolder, "orbe_1", OrbTexture(1), 0, TilePPU),
            };
            a.Alert = PixelArt.SaveSprite(ArtFolder, "alerta", PixelArt.FromPattern(new[]
            {
                ".o.", "oyo", "oyo", "oyo", "oyo", ".o.", "...", ".o.", "oyo", ".o.",
            }, ('o', PixelArt.Ink), ('y', PixelArt.Gold)), 0, TilePPU, bottomCenter);
            a.OrbPrefab = SaveOrbPrefab(a.Orb);

            CreateCreatureClips();
            CreateVillageClips();
            a.Vignette = PixelArt.SaveSprite(ArtFolder, "muerte_vineta", VillageArt.Vignette(), 0, 100f);
            var mask = VillageArt.BrokenMask();
            a.MaskLeft = PixelArt.SaveSprite(ArtFolder, "muerte_mascara_izq", mask.left, 0, 100f);
            a.MaskRight = PixelArt.SaveSprite(ArtFolder, "muerte_mascara_der", mask.right, 0, 100f);
            a.DoorArrow = PixelArt.SaveSprite(ArtFolder, "puerta_flecha", VillageArtHD.DoorArrow(), 0, VillageArtHD.NpcPPU, new Vector2(0.5f, 0f));
            a.WhitePixel = PixelArt.SaveSprite(ArtFolder, "pixel_blanco", PixelArt.Solid(4, 4, Color.white), 0, 4f);
            a.Mote = PixelArt.SaveSprite(ArtFolder, "muerte_mota", PixelArt.FromPattern(new[] { ".w.", "www", ".w." }, ('w', Color.white)), 0, 100f);
            a.SleepZ = PixelArt.SaveSprite(ArtFolder, "zzz", CreatureArt.SleepZ(), 0, CreatureArt.PPU);
            a.Prompt = PixelArt.SaveSprite(ArtFolder, "hablar", CreatureArt.TalkBubble(), 0, CreatureArt.PPU, new Vector2(0.5f, 0f));
            a.DialoguePanel = PixelArt.SaveSprite(ArtFolder, "dialogo_panel",
                PixelArt.Box(12, 12, PixelArt.Lilac, PixelArt.WithAlpha(PixelArt.Deep, 0.94f), PixelArt.Mid, PixelArt.Ink), 3, 100f);
            a.DialogueNext = PixelArt.SaveSprite(ArtFolder, "dialogo_siguiente", PixelArt.FromPattern(new[]
            {
                "ooooooo", "oyyyyyo", ".oyyyo.", "..oyo..", "...o...",
            }, ('o', PixelArt.Ink), ('y', PixelArt.Gold)), 0, 100f);

            a.HeartFull = PixelArt.SaveSprite(ArtFolder, "heart_full", HeartTexture(true));
            a.HeartEmpty = PixelArt.SaveSprite(ArtFolder, "heart_empty", HeartTexture(false));
            a.MoonHeart = PixelArt.SaveSprite(ArtFolder, "corazon_luna", HeartTexture(true), 0, 8f);

            a.Sky = PixelArt.SaveSprite(ArtFolder, "bg_sky", PixelArt.Sky(), 0, 4f);
            a.Stars = PixelArt.SaveSprite(ArtFolder, "bg_stars", StarsTexture(), 0, TilePPU);
            a.Moon = PixelArt.SaveSprite(ArtFolder, "bg_moon", PixelArt.CrescentMoon(), 0, TilePPU);
            a.HillsFar = PixelArt.SaveSprite(ArtFolder, "bg_hills_far",
                HillsTexture(64, 50f, 6f, 3f, 0f, PixelArt.Base, PixelArt.Mid), 0, TilePPU);
            a.HillsNear = PixelArt.SaveSprite(ArtFolder, "bg_hills_near",
                HillsTexture(48, 34f, 4f, 2f, 2f, PixelArt.Deep, PixelArt.Base), 0, TilePPU);
            a.Fog = PixelArt.SaveSprite(ArtFolder, "bg_fog", PixelArt.Fog(), 0, TilePPU);
            return a;
        }

        /// <summary>Dibuja y guarda todas las animaciones de enemigos (con silueta para el destello) y aldeanos.</summary>
        private static void CreateCreatureClips()
        {
            clipPaths.Clear();
            var feet = new Vector2(0.5f, 0f);
            foreach (var clip in new[] { "idle", "walk", "run", "windup", "lunge", "hurt" })
                SaveClip("sombra_" + clip, CreatureArt.ShadowClip(clip), feet, true, CreatureArt.EnemyPPU);
            foreach (var clip in new[] { "fly", "flyfast", "windup", "swoop", "hurt" })
                SaveClip("polilla_" + clip, CreatureArt.MothClip(clip), new Vector2(0.5f, 0.5f), true, CreatureArt.EnemyPPU);
            foreach (var clip in new[] { "idle", "aim", "shoot", "hurt" })
                SaveClip("vigia_" + clip, CreatureArt.WatcherClip(clip), feet, true, CreatureArt.EnemyPPU);

            // Jefes.
            var middle = new Vector2(0.5f, 0.5f);
            foreach (var clip in new[] { "idle", "walk", "windup", "charge", "leap", "roar", "hurt" })
                SaveClip("jefe_bestia_" + clip, BossArt.BeastClip(clip), feet, true, BossArt.PPU);
            foreach (var clip in new[] { "fly", "windup", "swoop", "cast", "hurt" })
                SaveClip("jefe_reina_" + clip, BossArt.QueenClip(clip), middle, true, BossArt.PPU);
            foreach (var clip in new[] { "idle", "aim", "shoot", "close", "hurt" })
                SaveClip("jefe_ojo_" + clip, BossArt.EyeClip(clip), middle, true, BossArt.PPU);
        }

        private static void SaveClip(string name, Texture2D[] frames, Vector2 pivot, bool flash, float ppu = CreatureArt.PPU)
        {
            const string folder = ArtFolder + "/Creatures";
            var paths = new string[frames.Length];
            var flashPaths = new string[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                if (flash)
                    flashPaths[i] = AssetDatabase.GetAssetPath(PixelArt.SaveSprite(folder, $"{name}_flash_{i}",
                        SpritePainter.Silhouette(frames[i]), 0, ppu, pivot));
                paths[i] = AssetDatabase.GetAssetPath(PixelArt.SaveSprite(folder, $"{name}_{i}", frames[i], 0, ppu, pivot));
            }
            clipPaths[name] = paths;
            if (flash) clipPaths[name + "_flash"] = flashPaths;
        }

        /// <summary>Prefab del orbe que dispara el Vigía (Assets/Levels/Art/Prefabs/DreamOrb.prefab).</summary>
        private static DreamOrb SaveOrbPrefab(Sprite[] frames)
        {
            string folder = ArtFolder + "/Prefabs";
            Directory.CreateDirectory(folder);

            var go = new GameObject("DreamOrb");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 15;
            SetRefs(go.AddComponent<SpriteFlipbook>(), ("frames", frames));
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;
            go.AddComponent<DreamOrb>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, folder + "/DreamOrb.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<DreamOrb>();
        }

        private static Tile SaveTile(string name, Sprite sprite, Tile.ColliderType collider)
        {
            Directory.CreateDirectory(TilesFolder);
            string path = $"{TilesFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = sprite;
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        /// <summary>
        /// Casilla que elige sola su pieza según qué lados tiene al aire.
        /// pieces[máscara de GroundArt.Open*] son los sprites de esa combinación (varios = uno al azar).
        /// </summary>
        private static RuleTile SaveRuleTile(string name, Sprite[][] pieces, Tile.ColliderType collider)
        {
            Directory.CreateDirectory(TilesFolder);
            string path = $"{TilesFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<GroundRuleTile>(path);
            if (tile == null)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path); // era otro tipo de casilla
                tile = ScriptableObject.CreateInstance<GroundRuleTile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.m_DefaultSprite = pieces[0][0];
            tile.m_DefaultColliderType = collider;
            tile.m_TilingRules = new List<RuleTile.TilingRule>();

            var sides = new[]
            {
                (GroundArt.OpenUp, new Vector3Int(0, 1, 0)), (GroundArt.OpenLeft, new Vector3Int(-1, 0, 0)),
                (GroundArt.OpenRight, new Vector3Int(1, 0, 0)), (GroundArt.OpenDown, new Vector3Int(0, -1, 0)),
            };
            for (int open = 0; open < pieces.Length; open++)
            {
                var rule = new RuleTile.TilingRule
                {
                    m_Sprites = pieces[open],
                    m_ColliderType = collider,
                    m_Output = pieces[open].Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Random : RuleTile.TilingRuleOutput.OutputSprite.Single,
                    m_PerlinScale = 0.95f,
                    m_NeighborPositions = sides.Select(side => side.Item2).ToList(),
                    m_Neighbors = sides.Select(side => (open & side.Item1) != 0
                        ? RuleTile.TilingRuleOutput.Neighbor.NotThis
                        : RuleTile.TilingRuleOutput.Neighbor.This).ToList(),
                };
                tile.m_TilingRules.Add(rule);
            }

            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        // ---------- Dibujos ----------

        /// <summary>Portal ovalado con un remolino que gira entre los 3 cuadros.</summary>
        private static Texture2D PortalTexture(int frame)
        {
            const int w = 16, h = 32;
            var t = PixelArt.Solid(w, h, Color.clear);
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float r = Mathf.Sqrt(Sq((x - 7.5f) / 7f) + Sq((y - 15.5f) / 15f));
                if (r > 1f) continue;
                if (r > 0.9f) t.SetPixel(x, y, PixelArt.Pale);
                else if (r > 0.72f) t.SetPixel(x, y, PixelArt.Lilac);
                else t.SetPixel(x, y, PixelArt.WithAlpha(PixelArt.Deep, 0.85f));
            }

            float spin = frame * Mathf.PI * 2f / 3f;
            for (int arm = 0; arm < 2; arm++)
            for (float s = 0.05f; s < 0.7f; s += 0.02f)
            {
                float a = s * Mathf.PI * 4f + spin + arm * Mathf.PI;
                int x = Mathf.RoundToInt(7.5f + Mathf.Cos(a) * s * 7f);
                int y = Mathf.RoundToInt(15.5f + Mathf.Sin(a) * s * 15f);
                if (x >= 0 && x < w && y >= 0 && y < h) t.SetPixel(x, y, s < 0.35f ? PixelArt.Moon : PixelArt.Soft);
            }
            return t;
        }

        /// <summary>Orbe de sueño: bolita brillante que late entre los dos cuadros.</summary>
        private static Texture2D OrbTexture(int frame)
        {
            var t = PixelArt.Solid(8, 8, Color.clear);
            float outer = frame == 0 ? 3.6f : 3.1f;
            for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
            {
                float d = PixelArt.Dist(x, y, 3.5f, 3.5f);
                if (d > outer) continue;
                t.SetPixel(x, y, d < 1.5f ? Color.white : d < 2.6f ? PixelArt.Rose : PixelArt.WithAlpha(PixelArt.Lilac, 0.7f));
            }
            return t;
        }

        private static Texture2D HeartTexture(bool full)
        {
            Color fill = full ? PixelArt.Rose : PixelArt.Deep;
            Color shine = full ? Color.white : PixelArt.Base;
            return PixelArt.FromPattern(new[]
            {
                ".oo...oo.",
                "orro.orro",
                "orwrorrro",
                "orrrrrrro",
                ".orrrrro.",
                "..orrro..",
                "...oro...",
                "....o....",
            }, ('o', PixelArt.Ink), ('r', fill), ('w', shine));
        }

        private static Texture2D StarsTexture()
        {
            const int w = 64, h = 48;
            var rng = new System.Random(11);
            var t = PixelArt.Solid(w, h, Color.clear);
            for (int i = 0; i < 40; i++)
            {
                Color c = rng.NextDouble() < 0.3 ? PixelArt.Pale : PixelArt.Moon;
                t.SetPixel(rng.Next(w), rng.Next(h), PixelArt.WithAlpha(c, 0.4f + (float)rng.NextDouble() * 0.6f));
            }
            for (int i = 0; i < 5; i++) // algunas estrellas en cruz, más brillantes
            {
                int x = rng.Next(1, w - 1), y = rng.Next(1, h - 1);
                t.SetPixel(x, y, Color.white);
                var arm = PixelArt.WithAlpha(PixelArt.Pale, 0.6f);
                t.SetPixel(x - 1, y, arm); t.SetPixel(x + 1, y, arm); t.SetPixel(x, y - 1, arm); t.SetPixel(x, y + 1, arm);
            }
            return t;
        }

        /// <summary>Silueta de colinas que se repite de lado a lado sin cortes.</summary>
        private static Texture2D HillsTexture(int h, float baseHeight, float amp1, float amp2, float phase, Color fill, Color edge)
        {
            const int w = 64;
            var t = PixelArt.Solid(w, h, Color.clear);
            for (int x = 0; x < w; x++)
            {
                float a = x / (float)w * Mathf.PI * 2f;
                int top = Mathf.Clamp(Mathf.RoundToInt(baseHeight + amp1 * Mathf.Sin(a + phase) + amp2 * Mathf.Sin(3f * a + phase * 2f)), 1, h - 1);
                for (int y = 0; y <= top; y++) t.SetPixel(x, y, y == top ? edge : fill);
            }
            return t;
        }

        private static void Ellipse(Texture2D t, float cx, float cy, float rx, float ry, Color c)
        {
            for (int x = 0; x < t.width; x++)
            for (int y = 0; y < t.height; y++)
                if (Sq((x - cx) / rx) + Sq((y - cy) / ry) <= 1f) t.SetPixel(x, y, c);
        }

        private static float Sq(float v) => v * v;

        // ---------- Utilidades ----------

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Asigna campos [SerializeField] (también arreglos de objetos).</summary>
        private static void SetRefs(Object target, params (string field, object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (value is Object[] array)
                {
                    prop.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                }
                else prop.objectReferenceValue = (Object)value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
