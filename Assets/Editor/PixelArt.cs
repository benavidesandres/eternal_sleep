#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Herramientas para dibujar pixel art por código y guardarlo como PNG con filtro Point.
    /// Las usan el menú principal y los niveles.
    /// </summary>
    internal static class PixelArt
    {
        // Paleta "mundo de sueños"
        public static readonly Color Ink = Hex("#0B0814");
        public static readonly Color SkyTop = Hex("#07050D");
        public static readonly Color SkyBottom = Hex("#2A1F45");
        public static readonly Color Deep = Hex("#1A1428");
        public static readonly Color Base = Hex("#2A2140");
        public static readonly Color Mid = Hex("#43356A");
        public static readonly Color Hover = Hex("#4A3A78");
        public static readonly Color Soft = Hex("#6B5A9E");
        public static readonly Color Lilac = Hex("#B9A8F0");
        public static readonly Color Pale = Hex("#D9CCFF");
        public static readonly Color Moon = Hex("#F2ECFF");
        public static readonly Color Gold = Hex("#FFE38A");
        public static readonly Color Rose = Hex("#E86A92");

        // ---------- Dibujo ----------

        public static Texture2D Solid(int w, int h, Color c)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.SetPixels(Enumerable.Repeat(c, w * h).ToArray());
            return t;
        }

        /// <summary>Caja con esquinas recortadas: borde, relleno, luz arriba y sombra abajo.</summary>
        public static Texture2D Box(int w, int h, Color outline, Color fill, Color light, Color shadow)
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

        public static Texture2D Star()
        {
            var t = Solid(3, 3, Color.clear);
            t.SetPixel(1, 1, Color.white);
            var arm = WithAlpha(Color.white, 0.5f);
            t.SetPixel(0, 1, arm); t.SetPixel(2, 1, arm); t.SetPixel(1, 0, arm); t.SetPixel(1, 2, arm);
            return t;
        }

        /// <summary>Luna creciente de 24×24: un círculo menos otro desplazado.</summary>
        public static Texture2D CrescentMoon()
        {
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

        /// <summary>Degradado vertical en bandas (de abajo hacia arriba).</summary>
        public static Texture2D Sky(int height = 45, int bands = 9)
        {
            var t = Solid(1, height, Color.clear);
            for (int y = 0; y < height; y++)
            {
                float step = Mathf.Floor((float)y / height * bands) / (bands - 1);
                t.SetPixel(0, y, Color.Lerp(SkyBottom, SkyTop, step));
            }
            return t;
        }

        /// <summary>Niebla que se repite de lado a lado sin cortes.</summary>
        public static Texture2D Fog(int w = 128, int h = 64)
        {
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

        /// <summary>Dibuja un patrón de texto: cada carácter es un píxel según el diccionario de colores.
        /// La primera fila del patrón es la de arriba.</summary>
        public static Texture2D FromPattern(string[] rows, params (char key, Color color)[] colors)
        {
            int h = rows.Length, w = rows.Max(r => r.Length);
            var t = Solid(w, h, Color.clear);
            for (int row = 0; row < h; row++)
            for (int x = 0; x < rows[row].Length; x++)
            {
                char ch = rows[row][x];
                foreach (var (key, color) in colors)
                    if (key == ch) { t.SetPixel(x, h - 1 - row, color); break; }
            }
            return t;
        }

        // ---------- Guardado ----------

        public static Sprite SaveSprite(string folder, string name, Texture2D tex, int border = 0,
            float pixelsPerUnit = 100, Vector2? pivot = null)
        {
            string path = WritePng(folder, name, tex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = pixelsPerUnit;
            imp.spriteBorder = new Vector4(border, border, border, border);

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // necesario para el modo Tiled
            if (pivot.HasValue)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot.Value;
            }
            imp.SetTextureSettings(settings);

            ApplyPixelSettings(imp, TextureWrapMode.Clamp);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Texture2D SaveTexture(string folder, string name, Texture2D tex, TextureWrapMode wrap)
        {
            string path = WritePng(folder, name, tex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Default;
            ApplyPixelSettings(imp, wrap);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string WritePng(string folder, string name, Texture2D tex)
        {
            tex = AiArt.Apply(name, tex); // si hay arte hecho con IA para este nombre, se usa ese
            Directory.CreateDirectory(folder);
            string path = $"{folder}/{name}.png";
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

        // ---------- Utilidades ----------

        public static float Dist(float x, float y, float cx, float cy) =>
            Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
#endif
