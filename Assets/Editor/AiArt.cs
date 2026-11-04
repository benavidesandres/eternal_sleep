#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Arte hecho con IA: si existe ArtIA/&lt;nombre&gt;.png (en la raíz del proyecto, fuera de Assets),
    /// se usa en lugar del dibujo generado por código.
    ///
    /// La imagen se adapta sola al juego:
    /// 1. Si no tiene transparencia, se quita el fondo (el color de las esquinas).
    /// 2. Se recorta al contenido y se reduce al tamaño del sprite original manteniendo la proporción.
    /// 3. Los bordes quedan nítidos (sin semitransparencias), salvo en niebla y estrellas.
    ///
    /// Los nombres y los prompts sugeridos están en ArtIA/PROMPTS.md.
    /// </summary>
    internal static class AiArt
    {
        public const string Folder = "ArtIA";

        // Texturas que ocupan todo el cuadro y se repiten (no se les quita el fondo ni se recortan).
        private static readonly HashSet<string> FullFrame = new HashSet<string>
        {
            "ground_top", "ground_fill", "platform", "spikes", "bg_sky", "sky", "bg_stars", "bg_fog", "fog",
            "bg_hills_far", "bg_hills_near", "button", "button_hover", "button_pressed", "panel",
        };

        // Texturas sin transparencia: nunca se les quita el fondo.
        private static readonly HashSet<string> Opaque = new HashSet<string>
        {
            "ground_top", "ground_fill", "bg_sky", "sky", "button", "button_hover", "button_pressed", "panel",
        };

        // Texturas con transparencias suaves.
        private static readonly HashSet<string> SoftAlpha = new HashSet<string> { "bg_fog", "fog", "bg_stars" };

        // Sprites que se apoyan en el suelo (se alinean abajo en vez de al centro).
        private static readonly string[] BottomAligned = { "sombra_", "vigia_", "portal_", "alerta" };

        [MenuItem("Eternal Sleep/Abrir carpeta de arte IA")]
        private static void OpenFolder()
        {
            Directory.CreateDirectory(Folder);
            EditorUtility.RevealInFinder(Path.GetFullPath(Folder));
        }

        /// <summary>Devuelve el arte IA adaptado si existe; si no, el dibujo generado tal cual.</summary>
        public static Texture2D Apply(string name, Texture2D generated)
        {
            string path = Path.Combine(Folder, name + ".png");
            if (!File.Exists(path)) return generated;

            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(File.ReadAllBytes(path)))
            {
                Debug.LogWarning($"[Eternal Sleep] No se pudo leer {path}; se usa el dibujo generado.");
                Object.DestroyImmediate(src);
                return generated;
            }

            int w = generated.width, h = generated.height;
            bool fullFrame = FullFrame.Contains(name);
            bool soft = SoftAlpha.Contains(name);

            Color[] pixels = src.GetPixels();
            int sw = src.width, sh = src.height;
            Object.DestroyImmediate(src);

            if (!Opaque.Contains(name) && IsOpaque(pixels)) RemoveBackground(pixels, sw, sh);
            RectInt content = fullFrame ? new RectInt(0, 0, sw, sh) : ContentBounds(pixels, sw, sh);

            var result = PixelArt.Solid(w, h, Color.clear);
            if (fullFrame) Resample(pixels, sw, content, result, new RectInt(0, 0, w, h), soft);
            else
            {
                // Mantiene la proporción y centra (o apoya abajo).
                float scale = Mathf.Min((float)w / content.width, (float)h / content.height);
                int dw = Mathf.Max(1, Mathf.RoundToInt(content.width * scale));
                int dh = Mathf.Max(1, Mathf.RoundToInt(content.height * scale));
                int dx = (w - dw) / 2;
                int dy = IsBottomAligned(name) ? 0 : (h - dh) / 2;
                Resample(pixels, sw, content, result, new RectInt(dx, dy, dw, dh), soft);
            }

            Object.DestroyImmediate(generated);
            Debug.Log($"[Eternal Sleep] Usando arte IA: {path} → {w}×{h}");
            return result;
        }

        private static bool IsBottomAligned(string name)
        {
            foreach (var prefix in BottomAligned)
                if (name.StartsWith(prefix)) return true;
            return false;
        }

        private static bool IsOpaque(Color[] px)
        {
            foreach (var c in px)
                if (c.a < 0.99f) return false;
            return true;
        }

        /// <summary>Borra el color de fondo empezando por las 4 esquinas (relleno por inundación).</summary>
        private static void RemoveBackground(Color[] px, int w, int h, float tolerance = 0.12f)
        {
            Color bg = px[0];
            var visited = new bool[px.Length];
            var queue = new Queue<int>();
            foreach (int start in new[] { 0, w - 1, (h - 1) * w, h * w - 1 }) { queue.Enqueue(start); visited[start] = true; }

            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                Color c = px[i];
                if (Mathf.Abs(c.r - bg.r) + Mathf.Abs(c.g - bg.g) + Mathf.Abs(c.b - bg.b) > tolerance * 3f) continue;
                px[i] = Color.clear;

                int x = i % w, y = i / w;
                TryAdd(x - 1, y); TryAdd(x + 1, y); TryAdd(x, y - 1); TryAdd(x, y + 1);
            }

            void TryAdd(int x, int y)
            {
                if (x < 0 || y < 0 || x >= w || y >= h) return;
                int j = y * w + x;
                if (visited[j]) return;
                visited[j] = true;
                queue.Enqueue(j);
            }
        }

        private static RectInt ContentBounds(Color[] px, int w, int h)
        {
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (px[y * w + x].a < 0.1f) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
            return maxX < 0 ? new RectInt(0, 0, w, h) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>Reduce una zona de la imagen a otra más pequeña promediando los píxeles.</summary>
        private static void Resample(Color[] src, int srcWidth, RectInt from, Texture2D dst, RectInt to, bool soft)
        {
            for (int ty = 0; ty < to.height; ty++)
            for (int tx = 0; tx < to.width; tx++)
            {
                int x0 = from.x + tx * from.width / to.width, x1 = Mathf.Max(x0 + 1, from.x + (tx + 1) * from.width / to.width);
                int y0 = from.y + ty * from.height / to.height, y1 = Mathf.Max(y0 + 1, from.y + (ty + 1) * from.height / to.height);

                float r = 0, g = 0, b = 0, a = 0;
                int n = 0;
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    Color c = src[y * srcWidth + x];
                    r += c.r * c.a; g += c.g * c.a; b += c.b * c.a; a += c.a;
                    n++;
                }
                if (a <= 0f) continue;

                var color = new Color(r / a, g / a, b / a, a / n);
                if (!soft) color.a = color.a >= 0.5f ? 1f : 0f; // bordes nítidos
                if (color.a > 0f) dst.SetPixel(to.x + tx, to.y + ty, color);
            }
        }
    }
}
#endif
