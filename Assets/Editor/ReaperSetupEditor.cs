#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using EternalSleep.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Menú: Eternal Sleep > Importar reaper.
    /// Corta las hojas de Assets/sprites/player/reaper, reescribe los clips de
    /// Assets/sprites/player/animations con ellas (mismo asset, mismo GUID), reconstruye
    /// Player.controller y cambia el sprite del Player en las escenas.
    /// Las hojas miran a la derecha; la izquierda la hace PlayerAnimator volteando el sprite.
    /// </summary>
    public static class ReaperSetupEditor
    {
        private const string SheetFolder = "Assets/sprites/player/reaper";
        private const string AnimFolder = "Assets/sprites/player/animations";
        private const int CellW = 201, CellH = 140, Columns = 8;
        private const float PixelsPerUnit = 52f;   // el reaper (95 px) mide lo mismo que el jugador anterior
        private const float FeetFromBottom = 36.96f; // pivote 0.264 de una celda de 140 px

        // hoja -> (frames, alto de celda)
        private static readonly Dictionary<string, (int frames, int cellH)> Sheets = new Dictionary<string, (int, int)>
        {
            { "reposo", (12, CellH) },
            { "correr", (10, CellH) },
            { "salto_inicio", (5, CellH) },
            { "subiendo", (4, CellH) },
            { "cayendo", (4, CellH) },
            { "aterrizaje", (6, CellH) },
            { "dash", (10, CellH) },
            { "recibir_dano", (8, CellH) },
            { "ataque_basico", (21, CellH) },
            { "ataque_abajo", (33, CellH) },
            { "ataque_arriba", (21, 200) }, // generado; celda más alta para el corte sobre la cabeza
        };

        private static readonly int[] AttackFrames = { 1, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

        // clip -> (hoja, frames, fps, bucle)
        private static readonly (string clip, string sheet, int[] frames, float fps, bool loop)[] Clips =
        {
            ("idle", "reposo", Range(0, 11), 12f, true),
            ("run", "correr", Range(0, 9), 15f, true),
            ("jump_start", "salto_inicio", Range(0, 4), 30f, false),
            ("jump_up", "subiendo", Range(0, 3), 15f, true),
            ("fall", "cayendo", Range(0, 3), 15f, true),
            ("land", "aterrizaje", Range(0, 5), 30f, false),
            ("dash", "dash", Range(1, 9), 40f, false),
            ("hurt", "recibir_dano", Range(0, 7), 30f, false),
            ("attack_forward", "ataque_basico", AttackFrames, 30f, false),
            ("attack_up", "ataque_arriba", AttackFrames, 30f, false),
            ("attack_down", "ataque_abajo", Range(11, 22), 30f, false),
            ("slam", "ataque_abajo", new[] { 11 }, 30f, true),
        };

        /// <summary>Parte del clip de ataque en la que termina el desvanecido del corte (frame 15).</summary>
        internal static float AttackFadeEnd => (System.Array.IndexOf(AttackFrames, 15) + 1f) / AttackFrames.Length;

        internal static string IdleSpritePath => $"{SheetFolder}/reposo.png";

        private static int[] Range(int from, int to) => Enumerable.Range(from, to - from + 1).ToArray();

        [MenuItem("Eternal Sleep/Importar reaper")]
        public static void ImportReaper()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Eternal Sleep", "Detén el juego (botón Play) antes de usar esta opción.", "OK");
                return;
            }

            foreach (var sheet in Sheets)
                if (!SliceSheet(sheet.Key, sheet.Value.frames, sheet.Value.cellH)) return;

            foreach (var c in Clips)
                if (!WriteClip(c.clip, c.sheet, c.frames, c.fps, c.loop)) return;
            AssetDatabase.SaveAssets();

            if (PlayerSetupEditor.BuildController() == null) return;
            UpdateScenes();
            Debug.Log("[Eternal Sleep] Reaper importado: hojas, clips, Animator y escenas.");
        }

        // ---------- Hojas ----------

        private static bool SliceSheet(string name, int frames, int cellH)
        {
            string path = $"{SheetFolder}/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                EditorUtility.DisplayDialog("Eternal Sleep", $"Falta la hoja {path}.", "OK");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport(); // para que el proveedor de datos vea el tamaño real

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
            dp.InitSpriteEditorDataProvider();

            dp.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out int texW, out int texH);
            // Reusa los ID de un corte anterior para que los clips no pierdan sus sprites al reimportar.
            var oldIds = dp.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            var pivot = new Vector2(0.5f, FeetFromBottom / cellH);

            var rects = new List<SpriteRect>();
            for (int i = 0; i < frames; i++)
            {
                int col = i % Columns, row = i / Columns;
                string spriteName = $"{name}_{i}";
                rects.Add(new SpriteRect
                {
                    name = spriteName,
                    spriteID = oldIds.TryGetValue(spriteName, out var id) ? id : GUID.Generate(),
                    rect = new Rect(col * CellW, texH - (row + 1) * cellH, CellW, cellH),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                });
            }

            dp.SetSpriteRects(rects.ToArray());
            dp.GetDataProvider<ISpriteNameFileIdDataProvider>()
              .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            dp.Apply();
            importer.SaveAndReimport();
            return true;
        }

        // ---------- Clips ----------

        private static bool WriteClip(string clipName, string sheet, int[] frames, float fps, bool loop)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath($"{SheetFolder}/{sheet}.png")
                .OfType<Sprite>().ToDictionary(s => s.name);

            string path = $"{AnimFolder}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                AnimationUtility.SetObjectReferenceCurve(clip, b, null);
            clip.ClearCurves();
            clip.frameRate = fps;

            var keys = new List<ObjectReferenceKeyframe>();
            for (int i = 0; i < frames.Length; i++)
            {
                if (!sprites.TryGetValue($"{sheet}_{frames[i]}", out var sprite))
                {
                    EditorUtility.DisplayDialog("Eternal Sleep", $"Falta el sprite {sheet}_{frames[i]}.", "OK");
                    return false;
                }
                keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = sprite });
            }
            // El último frame dura lo mismo que los demás.
            keys.Add(new ObjectReferenceKeyframe { time = frames.Length / fps, value = keys[keys.Count - 1].value });

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return true;
        }

        // ---------- Escenas ----------

        /// <summary>Pone el reposo nuevo como sprite del Player en todas las escenas que tengan uno.</summary>
        private static void UpdateScenes()
        {
            var idle = AssetDatabase.LoadAllAssetsAtPath(IdleSpritePath).OfType<Sprite>().FirstOrDefault(s => s.name == "reposo_0");
            if (idle == null) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string active = EditorSceneManager.GetActiveScene().path;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
                var player = Object.FindAnyObjectByType<PlayerMotor>(FindObjectsInactive.Include);
                var sr = player != null ? player.GetComponent<SpriteRenderer>() : null;
                if (sr == null || sr.sprite == idle) continue;
                sr.sprite = idle;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (!string.IsNullOrEmpty(active)) EditorSceneManager.OpenScene(active);
        }
    }
}
#endif
