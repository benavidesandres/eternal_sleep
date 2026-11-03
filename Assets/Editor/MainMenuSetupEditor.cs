#if UNITY_EDITOR
using System.IO;
using System.Linq;
using EternalSleep.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Menú: Eternal Sleep > Crear menú principal.
    /// Dibuja los sprites pixel art del menú, crea la fuente pixelada y arma la escena
    /// Assets/Scenes/MainMenu.unity con fondo animado, botones (Jugar, Opciones, Salir) y panel de opciones.
    /// </summary>
    public static class MainMenuSetupEditor
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ArtFolder = "Assets/UI/MainMenu";
        private const string FontFilePath = "Assets/UI/Fonts/PixelifySans.ttf";
        private const string FontAssetPath = "Assets/UI/Fonts/PixelifySans SDF.asset";
        private const string TmpEssentialFont = "Fonts & Materials/LiberationSans SDF";

        // Paleta "noche profunda"
        private static readonly Color Ink = Hex("#0B0814");
        private static readonly Color SkyTop = Hex("#07050D");
        private static readonly Color SkyBottom = Hex("#2A1F45");
        private static readonly Color Deep = Hex("#1A1428");
        private static readonly Color Base = Hex("#2A2140");
        private static readonly Color Mid = Hex("#43356A");
        private static readonly Color Hover = Hex("#4A3A78");
        private static readonly Color Soft = Hex("#6B5A9E");
        private static readonly Color Lilac = Hex("#B9A8F0");
        private static readonly Color Pale = Hex("#D9CCFF");
        private static readonly Color Moon = Hex("#F2ECFF");
        private static readonly Color Gold = Hex("#FFE38A");

        private class Art
        {
            public Sprite Button, ButtonHover, ButtonPressed, Panel;
            public Sprite SliderTrack, SliderFill, SliderHandle, ToggleBox, Pixel, Star, MoonSprite;
            public Texture2D Sky, Fog;
        }

        [MenuItem("Eternal Sleep/Crear menú principal")]
        public static void CreateMainMenu()
        {
            if (!EnsureTmpEssentials()) return;

            var font = GetPixelFont();
            if (font == null) return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Eternal Sleep", "Ya existe MainMenu.unity. ¿Reemplazarla?", "Reemplazar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var art = CreateArt();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            var canvas = CreateCanvas();
            CreateBackground(canvas.transform, art);

            // Panel principal
            var mainPanel = NewUI("MainPanel", canvas.transform);
            Stretch(mainPanel);
            CreateTitle(mainPanel, font);
            var buttons = CreateVerticalGroup("ButtonsPanel", mainPanel, new Vector2(140, 24 * 3 + 8 * 2), new Vector2(0, -40), 8, 0);
            var playButton = CreateButton(buttons, "JugarButton", "Jugar", font, art);
            var optionsButton = CreateButton(buttons, "OpcionesButton", "Opciones", font, art);
            var quitButton = CreateButton(buttons, "SalirButton", "Salir", font, art);

            // Panel de opciones
            var optionsPanel = CreateOptionsPanel(canvas.transform, font, art,
                out var master, out var music, out var sfx, out var fullscreen, out var backButton);

            // Lógica
            var controllerGo = new GameObject("MenuController");
            var controller = controllerGo.AddComponent<MainMenuController>();
            SetRefs(controller,
                ("mainPanel", mainPanel.gameObject), ("optionsPanel", optionsPanel.gameObject),
                ("mainFirstSelected", playButton.gameObject), ("optionsFirstSelected", master.gameObject));

            var options = controllerGo.AddComponent<OptionsMenu>();
            SetRefs(options, ("masterSlider", master), ("musicSlider", music), ("sfxSlider", sfx), ("fullscreenToggle", fullscreen));

            UnityEventTools.AddPersistentListener(playButton.onClick, new UnityAction(controller.Play));
            UnityEventTools.AddPersistentListener(optionsButton.onClick, new UnityAction(controller.OpenOptions));
            UnityEventTools.AddPersistentListener(quitButton.onClick, new UnityAction(controller.Quit));
            UnityEventTools.AddPersistentListener(backButton.onClick, new UnityAction(controller.CloseOptions));

            optionsPanel.gameObject.SetActive(false);
            CreateEventSystem(playButton.gameObject);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildFirst(ScenePath);

            Debug.Log("[Eternal Sleep] Menú principal creado en " + ScenePath + " y puesto como primera escena del juego.");
        }

        // ---------- TextMeshPro ----------

        private static bool EnsureTmpEssentials()
        {
            if (Resources.Load<TMP_FontAsset>(TmpEssentialFont) != null) return true;

            // Faltan los recursos básicos de TextMeshPro: se importan sin ventanas.
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();

            if (Resources.Load<TMP_FontAsset>(TmpEssentialFont) != null) return true;
            EditorUtility.DisplayDialog("Eternal Sleep",
                "Se importaron los recursos de texto (TextMeshPro). Vuelve a usar \"Crear menú principal\" cuando termine de cargar.",
                "OK");
            return false;
        }

        private static TMP_FontAsset GetPixelFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(FontFilePath);
            if (source == null)
            {
                EditorUtility.DisplayDialog("Eternal Sleep", "No se encontró la fuente " + FontFilePath, "OK");
                return null;
            }

            var font = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA_HINTED, 512, 512);
            if (font == null) return null;

            font.name = "PixelifySans SDF";
            AssetDatabase.CreateAsset(font, FontAssetPath);
            font.atlasTextures[0].name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            AssetDatabase.SaveAssets();
            return font;
        }

        // ---------- Sprites pixel art ----------

        private static Art CreateArt()
        {
            Directory.CreateDirectory(ArtFolder);
            var art = new Art
            {
                Button = SaveSprite("button", Box(16, 16, Ink, Base, Mid, Deep), 4),
                ButtonHover = SaveSprite("button_hover", Box(16, 16, Pale, Hover, Soft, Base), 4),
                ButtonPressed = SaveSprite("button_pressed", Box(16, 16, Pale, Deep, Deep, Base), 4),
                Panel = SaveSprite("panel", Box(16, 16, Soft, WithAlpha(Hex("#140E22"), 0.94f), Base, Ink), 4),
                SliderTrack = SaveSprite("slider_track", Box(8, 8, Ink, Deep, Deep, Ink), 3),
                SliderFill = SaveSprite("slider_fill", Box(8, 8, Ink, Lilac, Pale, Soft), 3),
                SliderHandle = SaveSprite("slider_handle", Box(6, 14, Ink, Color.white, Color.white, Hex("#C8C8D8")), 0),
                ToggleBox = SaveSprite("toggle_box", ToggleBoxTexture(), 0),
                Pixel = SaveSprite("pixel", Solid(1, 1, Color.white), 0),
                Star = SaveSprite("star", StarTexture(), 0),
                MoonSprite = SaveSprite("moon", MoonTexture(), 0),
                Sky = SaveTexture("sky", SkyTexture(), TextureWrapMode.Clamp),
                Fog = SaveTexture("fog", FogTexture(), TextureWrapMode.Repeat),
            };
            return art;
        }

        /// <summary>Caja con esquinas recortadas: borde, relleno, luz arriba y sombra abajo.</summary>
        private static Texture2D Box(int w, int h, Color outline, Color fill, Color light, Color shadow)
        {
            var t = Solid(w, h, Color.clear);
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                bool cornerX = x == 0 || x == w - 1;
                bool cornerY = y == 0 || y == h - 1;
                if (cornerX && cornerY) continue; // esquina redondeada
                Color c;
                if (cornerX || cornerY) c = outline;
                else if (y == h - 2) c = light;
                else if (y == 1) c = shadow;
                else c = fill;
                t.SetPixel(x, y, c);
            }
            return t;
        }

        private static Texture2D ToggleBoxTexture()
        {
            var t = Box(14, 14, Ink, Color.white, Color.white, Color.white);
            for (int x = 2; x < 12; x++)
            for (int y = 2; y < 12; y++)
                t.SetPixel(x, y, Deep);
            return t;
        }

        private static Texture2D StarTexture()
        {
            var t = Solid(3, 3, Color.clear);
            t.SetPixel(1, 1, Color.white);
            var arm = WithAlpha(Color.white, 0.5f);
            t.SetPixel(0, 1, arm); t.SetPixel(2, 1, arm); t.SetPixel(1, 0, arm); t.SetPixel(1, 2, arm);
            return t;
        }

        private static Texture2D MoonTexture()
        {
            // Luna creciente: un círculo menos otro desplazado.
            const int size = 24;
            var t = Solid(size, size, Color.clear);
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                bool inA = Dist(x, y, 11.5f, 11.5f) <= 10.5f;
                bool inB = Dist(x, y, 16.5f, 14.5f) <= 9f;
                if (!inA || inB) continue;
                bool edge = Dist(x, y, 11.5f, 11.5f) > 9.5f || Dist(x, y, 16.5f, 14.5f) <= 10f;
                t.SetPixel(x, y, edge ? Lilac : Moon);
            }
            return t;
        }

        private static Texture2D SkyTexture()
        {
            // Degradado en bandas (look pixel art), de abajo hacia arriba.
            const int h = 45, bands = 9;
            var t = Solid(1, h, Color.clear);
            for (int y = 0; y < h; y++)
            {
                float step = Mathf.Floor((float)y / h * bands) / (bands - 1);
                t.SetPixel(0, y, Color.Lerp(SkyBottom, SkyTop, step));
            }
            return t;
        }

        private static Texture2D FogTexture()
        {
            // Niebla que se repite de lado a lado sin cortes.
            const int w = 128, h = 64;
            const float r = 3f;
            var t = Solid(w, h, Color.clear);
            for (int x = 0; x < w; x++)
            {
                float a = x / (float)w * Mathf.PI * 2f;
                float u = Mathf.Cos(a) * r + 10f, v = Mathf.Sin(a) * r + 10f;
                for (int y = 0; y < h; y++)
                {
                    float n = (Mathf.PerlinNoise(u + y * 0.06f, v) + 0.5f * Mathf.PerlinNoise(u * 2f, v * 2f + y * 0.12f)) / 1.5f;
                    float alpha = Mathf.Clamp01((n - 0.35f) * 2.2f) * (1f - y / (float)(h - 1));
                    alpha = Mathf.Round(alpha * 4f) / 4f; // 4 niveles de opacidad
                    if (alpha > 0f) t.SetPixel(x, y, WithAlpha(Lilac, alpha));
                }
            }
            return t;
        }

        private static Texture2D Solid(int w, int h, Color c)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(Enumerable.Repeat(c, w * h).ToArray());
            return t;
        }

        private static Sprite SaveSprite(string name, Texture2D tex, int border)
        {
            string path = WritePng(name, tex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100;
            imp.spriteBorder = new Vector4(border, border, border, border);
            ApplyPixelSettings(imp, TextureWrapMode.Clamp);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Texture2D SaveTexture(string name, Texture2D tex, TextureWrapMode wrap)
        {
            string path = WritePng(name, tex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            ApplyPixelSettings(imp, wrap);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string WritePng(string name, Texture2D tex)
        {
            string path = $"{ArtFolder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        private static void ApplyPixelSettings(TextureImporter imp, TextureWrapMode wrap)
        {
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.wrapMode = wrap;
            imp.SaveAndReimport();
        }

        // ---------- Escena ----------

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyTop;
            go.transform.position = new Vector3(0, 0, -10);
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("MainMenuCanvas", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            // Resolución baja de referencia para que todo escale en "píxeles gordos".
            var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640, 360);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;

            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvas;
        }

        private static void CreateBackground(Transform parent, Art art)
        {
            var bg = NewUI("Background", parent);
            Stretch(bg);

            var sky = NewUI("Sky", bg);
            Stretch(sky);
            var skyImg = sky.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            skyImg.texture = art.Sky;
            skyImg.raycastTarget = false;

            // Estrellas en posiciones fijas (misma semilla siempre).
            var starsRoot = NewUI("Stars", bg);
            Stretch(starsRoot);
            var oldState = Random.state;
            Random.InitState(7);
            var stars = new UnityEngine.UI.Image[60];
            for (int i = 0; i < stars.Length; i++)
            {
                var s = NewUI("Star" + i, starsRoot);
                s.anchorMin = s.anchorMax = new Vector2(Random.Range(0.02f, 0.98f), Random.Range(0.3f, 0.98f));
                bool big = Random.value < 0.25f;
                s.sizeDelta = big ? new Vector2(3, 3) : Vector2.one;
                var img = s.gameObject.AddComponent<UnityEngine.UI.Image>();
                img.sprite = big ? art.Star : art.Pixel;
                img.color = Random.value < 0.3f ? Pale : Color.white;
                img.raycastTarget = false;
                stars[i] = img;
            }
            Random.state = oldState;

            var moon = NewUI("Moon", bg);
            moon.anchorMin = moon.anchorMax = new Vector2(0.84f, 0.78f);
            moon.sizeDelta = new Vector2(48, 48); // 2x el sprite de 24 px
            var moonImg = moon.gameObject.AddComponent<UnityEngine.UI.Image>();
            moonImg.sprite = art.MoonSprite;
            moonImg.raycastTarget = false;

            var fogBack = CreateFog(bg, "FogBack", 150, 0.35f, new Rect(0, 0, 2.5f, 1));
            var fogFront = CreateFog(bg, "FogFront", 90, 0.6f, new Rect(0.3f, 0, 4f, 1));

            var anim = bg.gameObject.AddComponent<MenuBackground>();
            var so = new SerializedObject(anim);
            var starsProp = so.FindProperty("stars");
            starsProp.arraySize = stars.Length;
            for (int i = 0; i < stars.Length; i++)
                starsProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            var fogProp = so.FindProperty("fogLayers");
            fogProp.arraySize = 2;
            SetFog(fogProp.GetArrayElementAtIndex(0), fogBack, 0.004f);
            SetFog(fogProp.GetArrayElementAtIndex(1), fogFront, 0.012f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UnityEngine.UI.RawImage CreateFog(Transform parent, string name, float height, float alpha, Rect uv)
        {
            var rt = NewUI(name, parent);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, height);
            var img = rt.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            img.color = WithAlpha(Color.white, alpha);
            img.uvRect = uv;
            img.raycastTarget = false;
            return img;
        }

        private static void SetFog(SerializedProperty element, UnityEngine.UI.RawImage image, float speed)
        {
            element.FindPropertyRelative("image").objectReferenceValue = image;
            element.FindPropertyRelative("speed").floatValue = speed;
        }

        private static void CreateTitle(Transform parent, TMP_FontAsset font)
        {
            // Sombra pixel art: el mismo texto, oscuro y desplazado 2 px.
            CreateTitleText(parent, "TitleShadow", font, Soft, new Vector2(2, -62));
            CreateTitleText(parent, "Title", font, Pale, new Vector2(0, -60));
        }

        private static void CreateTitleText(Transform parent, string name, TMP_FontAsset font, Color color, Vector2 pos)
        {
            var rt = NewUI(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(600, 60);
            rt.anchoredPosition = pos;
            var text = NewText(rt, font, "ETERNAL SLEEP", 48, color, TextAlignmentOptions.Center);
            text.characterSpacing = 6;
        }

        private static RectTransform CreateVerticalGroup(string name, Transform parent, Vector2 size, Vector2 pos, float spacing, int padding)
        {
            var rt = NewUI(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var layout = rt.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rt;
        }

        private static UnityEngine.UI.Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Art art)
        {
            var rt = NewUI(name, parent);
            rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 24;

            var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = art.Button;
            img.type = UnityEngine.UI.Image.Type.Sliced;

            var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = img;
            button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
            button.spriteState = new UnityEngine.UI.SpriteState
            {
                highlightedSprite = art.ButtonHover,
                selectedSprite = art.ButtonHover,
                pressedSprite = art.ButtonPressed,
                disabledSprite = art.Button,
            };

            var textRt = NewUI("Label", rt);
            Stretch(textRt);
            var text = NewText(textRt, font, label, 16, Pale, TextAlignmentOptions.Center);

            AddSelector(rt.gameObject, text);
            return button;
        }

        private static RectTransform CreateOptionsPanel(Transform parent, TMP_FontAsset font, Art art,
            out UnityEngine.UI.Slider master, out UnityEngine.UI.Slider music, out UnityEngine.UI.Slider sfx,
            out UnityEngine.UI.Toggle fullscreen, out UnityEngine.UI.Button back)
        {
            var panel = CreateVerticalGroup("OptionsPanel", parent, new Vector2(300, 200), new Vector2(0, -20), 8, 16);
            var img = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = art.Panel;
            img.type = UnityEngine.UI.Image.Type.Sliced;

            var header = NewUI("Header", panel);
            header.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 28;
            NewText(header, font, "OPCIONES", 24, Pale, TextAlignmentOptions.Center);

            master = CreateSliderRow(panel, "MasterRow", "Volumen general", font, art);
            music = CreateSliderRow(panel, "MusicRow", "Música", font, art);
            sfx = CreateSliderRow(panel, "SfxRow", "Efectos", font, art);
            fullscreen = CreateToggleRow(panel, "FullscreenRow", "Pantalla completa", font, art);
            back = CreateButton(panel, "VolverButton", "Volver", font, art);
            return panel;
        }

        private static RectTransform CreateRow(Transform parent, string name, string label, TMP_FontAsset font, out TMP_Text text)
        {
            var row = NewUI(name, parent);
            row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 18;
            var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var labelRt = NewUI("Label", row);
            labelRt.sizeDelta = new Vector2(0, 18);
            labelRt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
            text = NewText(labelRt, font, label, 14, Pale, TextAlignmentOptions.MidlineLeft);
            return row;
        }

        private static UnityEngine.UI.Slider CreateSliderRow(Transform parent, string name, string label, TMP_FontAsset font, Art art)
        {
            var row = CreateRow(parent, name, label, font, out var text);

            var rt = NewUI("Slider", row);
            rt.sizeDelta = new Vector2(120, 16);
            var le = rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredWidth = le.minWidth = 120;

            var track = NewUI("Background", rt);
            MidStrip(track, 8);
            var trackImg = track.gameObject.AddComponent<UnityEngine.UI.Image>();
            trackImg.sprite = art.SliderTrack;
            trackImg.type = UnityEngine.UI.Image.Type.Sliced;
            trackImg.raycastTarget = false;

            var fillArea = NewUI("Fill Area", rt);
            MidStrip(fillArea, 8);
            var fill = NewUI("Fill", fillArea);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0, 1);
            fill.sizeDelta = Vector2.zero;
            var fillImg = fill.gameObject.AddComponent<UnityEngine.UI.Image>();
            fillImg.sprite = art.SliderFill;
            fillImg.type = UnityEngine.UI.Image.Type.Sliced;
            fillImg.raycastTarget = false;

            var handleArea = NewUI("Handle Slide Area", rt);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(3, 0);
            handleArea.offsetMax = new Vector2(-3, 0);
            var handle = NewUI("Handle", handleArea);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = new Vector2(0, 1);
            handle.sizeDelta = new Vector2(6, -2);
            var handleImg = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
            handleImg.sprite = art.SliderHandle;

            var slider = rt.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 1;
            slider.colors = HighlightColors();

            AddSelector(rt.gameObject, text);
            return slider;
        }

        private static UnityEngine.UI.Toggle CreateToggleRow(Transform parent, string name, string label, TMP_FontAsset font, Art art)
        {
            var row = CreateRow(parent, name, label, font, out var text);

            var rt = NewUI("Toggle", row);
            rt.sizeDelta = new Vector2(14, 14);
            rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredWidth = 14;
            var box = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            box.sprite = art.ToggleBox;

            var check = NewUI("Checkmark", rt);
            Stretch(check);
            check.offsetMin = new Vector2(4, 4);
            check.offsetMax = new Vector2(-4, -4);
            var checkImg = check.gameObject.AddComponent<UnityEngine.UI.Image>();
            checkImg.sprite = art.Pixel;
            checkImg.color = Lilac;
            checkImg.raycastTarget = false;

            var toggle = rt.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = box;
            toggle.graphic = checkImg;
            toggle.isOn = true;
            toggle.colors = HighlightColors();

            AddSelector(rt.gameObject, text);
            return toggle;
        }

        private static UnityEngine.UI.ColorBlock HighlightColors()
        {
            var colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
            colors.normalColor = Lilac;
            colors.highlightedColor = Gold;
            colors.selectedColor = Gold;
            colors.pressedColor = Color.white;
            colors.disabledColor = Soft;
            colors.fadeDuration = 0f; // cambio instantáneo, más "retro"
            return colors;
        }

        private static void AddSelector(GameObject target, TMP_Text label)
        {
            var selector = target.AddComponent<MenuButtonSelector>();
            SetRefs(selector, ("label", label));
        }

        private static void CreateEventSystem(GameObject firstSelected)
        {
            var go = new GameObject("EventSystem");
            var es = go.AddComponent<EventSystem>();
            es.firstSelectedGameObject = firstSelected; // para manejar el menú con teclado o mando
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ---------- Build Settings ----------

        private static void AddSceneToBuildFirst(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Utilidades ----------

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
                so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMeshProUGUI NewText(RectTransform rt, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions align)
        {
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Franja horizontal centrada verticalmente, del alto indicado.</summary>
        private static void MidStrip(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.sizeDelta = new Vector2(0, height);
        }

        private static float Dist(float x, float y, float cx, float cy) =>
            Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
#endif
