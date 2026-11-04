#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EternalSleep.CameraSystem;
using EternalSleep.Enemies;
using EternalSleep.Level;
using EternalSleep.Player;
using EternalSleep.UI;
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
    /// </summary>
    public static class LevelBuilderEditor
    {
        private const string LevelsFolder = "Assets/Levels";
        private const string ArtFolder = "Assets/Levels/Art";
        private const string TilesFolder = "Assets/Levels/Art/Tiles";
        private const string ScenesFolder = "Assets/Scenes/Levels";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const float TilePPU = 16f; // 16 píxeles = 1 casilla

        private class Art
        {
            public RuleTile Ground;
            public Tile Platform, Spike;
            public Sprite[] Walker, Flyer, Shooter, Orb, Portal;
            public Sprite Alert, HeartFull, HeartEmpty, Sky, Stars, Moon, HillsFar, HillsNear, Fog;
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
                var art = CreateArt();

                for (int i = 0; i < files.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Eternal Sleep",
                        "Construyendo " + Path.GetFileNameWithoutExtension(files[i]), (i + 1f) / (files.Length + 1f));
                    scenes.Add(BuildLevel(files[i], art, controller, groundLayer));
                }
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

        private static string BuildLevel(string mapPath, Art art, AnimatorController controller, int groundLayer)
        {
            var lines = File.ReadAllLines(mapPath).Select(l => l.TrimEnd()).Where(l => l.Length > 0).ToArray();
            int height = lines.Length;
            int width = lines.Max(l => l.Length);
            string levelName = Path.GetFileNameWithoutExtension(mapPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Tilemaps
            var grid = new GameObject("Grid").AddComponent<Grid>();
            var ground = NewTilemap("Ground", grid.transform, 0);
            var platforms = NewTilemap("Platforms", grid.transform, 0);
            var hazards = NewTilemap("Hazards", grid.transform, 1);
            ground.gameObject.layer = groundLayer;
            platforms.gameObject.layer = groundLayer;

            var start = new Vector2(1.5f, 3f);
            var enemies = new GameObject("Enemies").transform;

            for (int row = 0; row < height; row++)
            for (int x = 0; x < lines[row].Length; x++)
            {
                int y = height - 1 - row; // la primera línea del texto es la de arriba
                var cell = new Vector3Int(x, y, 0);
                switch (lines[row][x])
                {
                    case '#': ground.SetTile(cell, art.Ground); break;
                    case '=': platforms.SetTile(cell, art.Platform); break;
                    case '^': hazards.SetTile(cell, art.Spike); break;
                    case 'P': start = new Vector2(x + 0.5f, y); break;
                    case 'E': CreateWalker(enemies, art, new Vector2(x + 0.5f, y)); break;
                    case 'F': CreateFlyer(enemies, art, new Vector2(x + 0.5f, y + 0.5f)); break;
                    case 'S': CreateShooter(enemies, art, new Vector2(x + 0.5f, y)); break;
                    case 'G': CreatePortal(art, new Vector2(x + 0.5f, y)); break;
                }
            }

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

            // Jugador
            var player = PlayerSetupEditor.CreatePlayer(controller, groundLayer);
            player.transform.position = new Vector3(start.x, start.y + 0.01f, 0f);
            player.AddComponent<PlayerHealth>();

            var bounds = new Rect(0, 0, width, height);
            CreateLevelManager(bounds);
            CreateLight();
            var cam = CreateCamera(player.transform, start, bounds);
            CreateBackground(art, cam, width);
            CreateHud(art);

            Directory.CreateDirectory(ScenesFolder);
            string scenePath = $"{ScenesFolder}/{levelName}.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            return scenePath;
        }

        private static Tilemap NewTilemap(string name, Transform parent, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var map = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return map;
        }

        private static void CreateWalker(Transform parent, Art art, Vector2 feet)
        {
            var go = new GameObject("Sombra");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            AddEnemyBase(go, art.Walker, 5);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.85f, 0.65f);
            col.offset = new Vector2(0f, 0.33f);

            AddSenses(go, 6f, 0.2f, new Vector2(0f, 0.4f)); // solo ve hacia delante
            var walker = go.AddComponent<WalkerEnemy>();
            SetRefs(walker, ("alertIcon", AddAlertIcon(go, art, 1.1f)));
        }

        private static void CreateFlyer(Transform parent, Art art, Vector2 center)
        {
            var go = new GameObject("Polilla");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            AddEnemyBase(go, art.Flyer, 6);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;

            AddSenses(go, 6f, -1f, Vector2.zero); // ve en todas direcciones
            var flyer = go.AddComponent<FlyerEnemy>();
            SetRefs(flyer, ("alertIcon", AddAlertIcon(go, art, 0.8f)));
        }

        private static void CreateShooter(Transform parent, Art art, Vector2 feet)
        {
            var go = new GameObject("Vigia");
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            AddEnemyBase(go, art.Shooter, 5);

            var health = new SerializedObject(go.GetComponent<EnemyHealth>());
            health.FindProperty("maxHealth").intValue = 3;
            health.ApplyModifiedPropertiesWithoutUndo();

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.9f);
            col.offset = new Vector2(0f, 0.45f);

            AddSenses(go, 8f, -1f, new Vector2(0f, 0.5f));
            var shooter = go.AddComponent<ShooterEnemy>();
            SetRefs(shooter, ("projectilePrefab", art.OrbPrefab), ("alertIcon", AddAlertIcon(go, art, 1.3f)));
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

        /// <summary>Lo que comparten todos los enemigos: sprite animado, física sin gravedad, vida y daño al tocar.</summary>
        private static void AddEnemyBase(GameObject go, Sprite[] frames, int sortingOrder)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = sortingOrder;
            SetRefs(go.AddComponent<SpriteFlipbook>(), ("frames", frames));

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            go.AddComponent<EnemyHealth>();
            go.AddComponent<EnemyContactDamage>();
        }

        private static void CreatePortal(Art art, Vector2 feet)
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

        private static void CreateLight()
        {
            // Sin luz global, los sprites con el material 2D Lit de URP se verían negros.
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        private static Camera CreateCamera(Transform player, Vector2 start, Rect bounds)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
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

        private static void CreateBackground(Art art, Camera cam, int levelWidth)
        {
            var root = new GameObject("Background").transform;
            Vector3 c = cam.transform.position;
            float span = Mathf.Max(60f, cam.orthographicSize * 2f * 16f / 9f * 2f);

            // Cielo pegado a la cámara (cubre toda la vista).
            var sky = AddLayer(root, "Sky", art.Sky, -100, new Vector2(c.x, c.y), 1f, 1f, false);
            sky.transform.localScale = new Vector3(span / art.Sky.bounds.size.x, 16f / art.Sky.bounds.size.y, 1f);

            Tiled(AddLayer(root, "Stars", art.Stars, -90, new Vector2(c.x, c.y + 1f), 0.05f, 0.95f, true), span, 16f);
            AddLayer(root, "Moon", art.Moon, -85, new Vector2(c.x + 6f, c.y + 3.5f), 0.02f, 0.97f, false);
            Tiled(AddLayer(root, "HillsFar", art.HillsFar, -80, new Vector2(c.x, 4.5f), 0.25f, 0.6f, true), span, 4f);
            Tiled(AddLayer(root, "HillsNear", art.HillsNear, -70, new Vector2(c.x, 3f), 0.5f, 0.75f, true), span, 3f);

            var fog = AddLayer(root, "Fog", art.Fog, -60, new Vector2(c.x, 3.5f), 0.7f, 0.8f, true);
            Tiled(fog, span, 4f);
            fog.color = PixelArt.WithAlpha(Color.white, 0.35f);
            var so = new SerializedObject(fog.GetComponent<ParallaxLayer>());
            so.FindProperty("drift").floatValue = 0.3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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
            so.ApplyModifiedPropertiesWithoutUndo();
            return sr;
        }

        private static void Tiled(SpriteRenderer sr, float width, float height)
        {
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(width, height);
        }

        private static void CreateHud(Art art)
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
            panel.sizeDelta = new Vector2(120f, 16f);
            var layout = panel.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var hearts = new UnityEngine.UI.Image[3];
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

            var groundTop = PixelArt.SaveSprite(ArtFolder, "ground_top", GroundTexture(true), 0, TilePPU);
            var groundFill = PixelArt.SaveSprite(ArtFolder, "ground_fill", GroundTexture(false), 0, TilePPU);
            a.Ground = SaveRuleTile("ground", groundTop, groundFill);
            a.Platform = SaveTile("platform", PixelArt.SaveSprite(ArtFolder, "platform", PlatformTexture(), 0, TilePPU), Tile.ColliderType.Sprite);
            a.Spike = SaveTile("spikes", PixelArt.SaveSprite(ArtFolder, "spikes", SpikeTexture(), 0, TilePPU), Tile.ColliderType.Sprite);

            a.Walker = new[]
            {
                PixelArt.SaveSprite(ArtFolder, "sombra_0", WalkerTexture(0), 0, TilePPU, bottomCenter),
                PixelArt.SaveSprite(ArtFolder, "sombra_1", WalkerTexture(1), 0, TilePPU, bottomCenter),
            };
            a.Flyer = new[]
            {
                PixelArt.SaveSprite(ArtFolder, "polilla_0", FlyerTexture(0), 0, TilePPU),
                PixelArt.SaveSprite(ArtFolder, "polilla_1", FlyerTexture(1), 0, TilePPU),
            };
            a.Portal = Enumerable.Range(0, 3)
                .Select(i => PixelArt.SaveSprite(ArtFolder, "portal_" + i, PortalTexture(i), 0, TilePPU, bottomCenter))
                .ToArray();

            a.Shooter = new[]
            {
                PixelArt.SaveSprite(ArtFolder, "vigia_0", ShooterTexture(0), 0, TilePPU, bottomCenter),
                PixelArt.SaveSprite(ArtFolder, "vigia_1", ShooterTexture(1), 0, TilePPU, bottomCenter),
            };
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

            a.HeartFull = PixelArt.SaveSprite(ArtFolder, "heart_full", HeartTexture(true));
            a.HeartEmpty = PixelArt.SaveSprite(ArtFolder, "heart_empty", HeartTexture(false));

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

        /// <summary>Suelo que elige solo la pieza: con borde si no tiene nada encima, relleno si sí.</summary>
        private static RuleTile SaveRuleTile(string name, Sprite top, Sprite fill)
        {
            Directory.CreateDirectory(TilesFolder);
            string path = $"{TilesFolder}/{name}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<RuleTile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.m_DefaultSprite = fill;
            tile.m_DefaultColliderType = Tile.ColliderType.Grid;

            var topRule = new RuleTile.TilingRule
            {
                m_Sprites = new[] { top },
                m_ColliderType = Tile.ColliderType.Grid,
                m_NeighborPositions = new List<Vector3Int> { new Vector3Int(0, 1, 0) },
                m_Neighbors = new List<int> { RuleTile.TilingRuleOutput.Neighbor.NotThis },
            };
            tile.m_TilingRules = new List<RuleTile.TilingRule> { topRule };

            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        // ---------- Dibujos ----------

        private static Texture2D GroundTexture(bool top)
        {
            var rng = new System.Random(top ? 3 : 5);
            var t = PixelArt.Solid(16, 16, PixelArt.Base);
            for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                if (rng.NextDouble() < 0.08) t.SetPixel(x, y, PixelArt.Deep);

            if (top)
            {
                for (int x = 0; x < 16; x++)
                {
                    t.SetPixel(x, 15, PixelArt.Lilac);
                    t.SetPixel(x, 14, PixelArt.Soft);
                    t.SetPixel(x, 13, rng.NextDouble() < 0.5 ? PixelArt.Mid : PixelArt.Soft);
                    if (rng.NextDouble() < 0.3) t.SetPixel(x, 12, PixelArt.Mid);
                }
                t.SetPixel(4, 15, PixelArt.Pale);
                t.SetPixel(11, 15, PixelArt.Pale);
            }
            return t;
        }

        private static Texture2D PlatformTexture()
        {
            var t = PixelArt.Solid(16, 16, Color.clear);
            for (int x = 0; x < 16; x++)
            {
                t.SetPixel(x, 15, PixelArt.Lilac);
                t.SetPixel(x, 14, PixelArt.Soft);
                t.SetPixel(x, 13, PixelArt.Mid);
                t.SetPixel(x, 12, PixelArt.Base);
                if (x % 2 == 0) t.SetPixel(x, 11, PixelArt.Deep);
            }
            // gotas de "sueño" colgando
            t.SetPixel(3, 10, PixelArt.Soft);
            t.SetPixel(11, 10, PixelArt.Soft);
            t.SetPixel(11, 9, PixelArt.Lilac);
            return t;
        }

        private static Texture2D SpikeTexture()
        {
            var t = PixelArt.Solid(16, 16, Color.clear);
            for (int s = 0; s < 4; s++)
            for (int lx = 0; lx < 4; lx++)
            for (int y = 0; y < 8; y++)
            {
                float halfWidth = 2f * (1f - y / 8f);
                if (Mathf.Abs(lx - 1.5f) > halfWidth) continue;
                Color c = y >= 6 ? PixelArt.Moon : (lx < 2 ? PixelArt.Pale : PixelArt.Lilac);
                t.SetPixel(s * 4 + lx, y, c);
            }
            for (int x = 0; x < 16; x++) t.SetPixel(x, 0, PixelArt.Soft);
            return t;
        }

        /// <summary>"Sombra": mancha oscura con ojos dorados. El cuadro 1 está un poco aplastado.</summary>
        private static Texture2D WalkerTexture(int frame)
        {
            const int w = 16, h = 12;
            float rx = frame == 0 ? 7f : 7.5f, ry = frame == 0 ? 5.5f : 5f, cy = frame == 0 ? 5.5f : 5f;
            bool Inside(int x, int y) => y >= 0 && y < h && x >= 0 && x < w &&
                                         Sq((x - 7.5f) / rx) + Sq((y - cy) / ry) <= 1f;

            var t = PixelArt.Solid(w, h, Color.clear);
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (!Inside(x, y)) continue;
                if (y == 0 && (x + frame) % 3 == 0) continue; // patitas que se mueven
                bool edge = !Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y + 1) || (y > 0 && !Inside(x, y - 1));
                Color c = edge ? PixelArt.Ink : PixelArt.Base;
                if (!edge && (y - cy) / ry > 0.35f && (x - 7.5f) / rx < -0.1f) c = PixelArt.Mid; // brillo
                t.SetPixel(x, y, c);
            }

            int ey = frame == 0 ? 6 : 5;
            foreach (int ex in new[] { 4, 10 })
            {
                t.SetPixel(ex, ey, PixelArt.Gold);
                t.SetPixel(ex + 1, ey, PixelArt.Gold);
                t.SetPixel(ex, ey + 1, PixelArt.Moon);
                t.SetPixel(ex + 1, ey + 1, PixelArt.Gold);
            }
            return t;
        }

        /// <summary>"Polilla" de luz: cuerpo claro y alas que aletean entre los dos cuadros.</summary>
        private static Texture2D FlyerTexture(int frame)
        {
            var t = PixelArt.Solid(16, 16, Color.clear);
            float upperRy = frame == 0 ? 3f : 1.5f, upperCy = frame == 0 ? 10.5f : 9f;
            float lowerRy = frame == 0 ? 2f : 1.2f;

            Ellipse(t, 4f, upperCy, 3.5f, upperRy, PixelArt.WithAlpha(PixelArt.Lilac, 0.9f));
            Ellipse(t, 11f, upperCy, 3.5f, upperRy, PixelArt.WithAlpha(PixelArt.Lilac, 0.9f));
            Ellipse(t, 4.5f, 5f, 2.5f, lowerRy, PixelArt.Soft);
            Ellipse(t, 10.5f, 5f, 2.5f, lowerRy, PixelArt.Soft);
            Ellipse(t, 7.5f, 7.5f, 1.5f, 4f, PixelArt.Pale);
            t.SetPixel(7, 11, PixelArt.Gold);
            t.SetPixel(8, 11, PixelArt.Gold);
            t.SetPixel(7, 12, PixelArt.Moon);
            t.SetPixel(8, 12, PixelArt.Moon);
            return t;
        }

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

        /// <summary>"Vigía": un ojo grande sobre un pedestal. El cuadro 1 parpadea.</summary>
        private static Texture2D ShooterTexture(int frame)
        {
            var t = PixelArt.Solid(16, 16, Color.clear);
            // pedestal
            for (int x = 4; x < 12; x++)
            for (int y = 0; y < 5; y++)
                t.SetPixel(x, y, x == 4 || x == 11 || y == 4 ? PixelArt.Ink : PixelArt.Base);
            // ojo
            for (int x = 0; x < 16; x++)
            for (int y = 4; y < 16; y++)
            {
                float r = Mathf.Sqrt(Sq((x - 7.5f) / 6.5f) + Sq((y - 10f) / 5.5f));
                if (r > 1f) continue;
                Color c = r > 0.85f ? PixelArt.Ink : PixelArt.Moon;
                if (frame == 1 && r <= 0.85f) c = y > 10 ? PixelArt.Mid : PixelArt.Moon; // párpado medio cerrado
                t.SetPixel(x, y, c);
            }
            // iris y pupila (mira a la izquierda; el script lo voltea)
            for (int x = 3; x < 8; x++)
            for (int y = 8; y < 12; y++)
                t.SetPixel(x, y, (x == 4 || x == 5) && (y == 9 || y == 10) ? PixelArt.Ink : PixelArt.Rose);
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
