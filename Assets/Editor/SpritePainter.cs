#if UNITY_EDITOR
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Rampa de 3 tonos para un material (sombra, medio, luz).
    /// </summary>
    public struct Ramp
    {
        public Color Dark, Mid, Light;
        public Ramp(string dark, string mid, string light)
        {
            Dark = PixelArt.Hex(dark);
            Mid = PixelArt.Hex(mid);
            Light = PixelArt.Hex(light);
        }
    }

    /// <summary>
    /// Lienzo para dibujar sprites pixel art por código con un estilo uniforme:
    /// se pintan formas con "materiales" (índices de una lista de rampas) y al renderizar
    /// se añade solo la luz desde arriba-izquierda, la sombra abajo-derecha, un tramado
    /// en la transición y un contorno oscuro de 1 píxel.
    /// Coordenadas: (0,0) abajo a la izquierda, como Texture2D.
    /// </summary>
    public class SpritePainter
    {
        public readonly int W, H;
        private readonly int[] mat;      // 0 = vacío
        private readonly Color[] direct; // píxeles sin sombreado (ojos, brillos)
        private readonly bool[] isDirect;

        public SpritePainter(int w, int h)
        {
            W = w;
            H = h;
            mat = new int[w * h];
            direct = new Color[w * h];
            isDirect = new bool[w * h];
        }

        private bool In(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        public void Set(int x, int y, int m)
        {
            if (!In(x, y)) return;
            mat[y * W + x] = m;
            isDirect[y * W + x] = false;
        }

        /// <summary>Píxel de color fijo (no se sombrea). Color.clear borra.</summary>
        public void Dot(int x, int y, Color c)
        {
            if (!In(x, y)) return;
            int i = y * W + x;
            if (c.a <= 0f) { mat[i] = 0; isDirect[i] = false; return; }
            direct[i] = c;
            isDirect[i] = true;
        }

        public void Ellipse(float cx, float cy, float rx, float ry, int m, float yMin = float.MinValue, float yMax = float.MaxValue)
        {
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
            {
                if (y < yMin || y > yMax) continue;
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Set(x, y, m);
            }
        }

        public void EllipseDot(float cx, float cy, float rx, float ry, Color c)
        {
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
            {
                float dx = (x - cx) / rx, dy = (y - cy) / ry;
                if (dx * dx + dy * dy <= 1f) Dot(x, y, c);
            }
        }

        public void Rect(int x0, int y0, int x1, int y1, int m)
        {
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                Set(x, y, m);
        }

        /// <summary>Cambia un material por otro solo dentro del rectángulo (rayas, estampados).</summary>
        public void Recolor(int x0, int y0, int x1, int y1, int from, int to)
        {
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                if (In(x, y) && !isDirect[y * W + x] && mat[y * W + x] == from) mat[y * W + x] = to;
        }

        /// <summary>Polígono relleno (puntos en orden).</summary>
        public void Poly(int m, params Vector2[] pts)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var p in pts)
            {
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            for (int x = Mathf.FloorToInt(minX); x <= Mathf.CeilToInt(maxX); x++)
            for (int y = Mathf.FloorToInt(minY); y <= Mathf.CeilToInt(maxY); y++)
                if (Inside(pts, x + 0.5f, y + 0.5f)) Set(x, y, m);
        }

        private static bool Inside(Vector2[] pts, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                if ((pts[i].y > y) != (pts[j].y > y) &&
                    x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            }
            return inside;
        }

        /// <summary>Trazo grueso que se afina de "from" a "to".</summary>
        public void Line(Vector2 a, Vector2 b, float thickA, float thickB, int m)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * 2f) + 1;
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector2 p = Vector2.Lerp(a, b, t);
                float r = Mathf.Lerp(thickA, thickB, t) * 0.5f;
                Ellipse(p.x, p.y, Mathf.Max(0.5f, r), Mathf.Max(0.5f, r), m);
            }
        }

        /// <summary>Curva (Bézier cuadrática) gruesa: cuernos, antenas, colas.</summary>
        public void Curve(Vector2 a, Vector2 control, Vector2 b, float thickA, float thickB, int m)
        {
            const int segments = 12;
            Vector2 prev = a;
            for (int s = 1; s <= segments; s++)
            {
                float t = s / (float)segments;
                Vector2 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * control + t * t * b;
                Line(prev, p, Mathf.Lerp(thickA, thickB, (s - 1f) / segments), Mathf.Lerp(thickA, thickB, t), m);
                prev = p;
            }
        }

        public Texture2D Render(Ramp[] ramps, Color outline)
        {
            var t = PixelArt.Solid(W, H, Color.clear);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (isDirect[i]) { t.SetPixel(x, y, direct[i]); continue; }
                int m = mat[i];
                if (m == 0) continue;

                var ramp = ramps[m];
                int up = Run(x, y, 0, 1, m), down = Run(x, y, 0, -1, m);
                int left = Run(x, y, -1, 0, m), right = Run(x, y, 1, 0, m);

                Color c = ramp.Mid;
                bool checker = ((x + y) & 1) == 0;
                if (down == 1 || right == 1) c = ramp.Dark;                       // borde en sombra
                else if (up == 1 || (left == 1 && up <= 3)) c = ramp.Light;        // borde iluminado
                else if (down == 2 && checker) c = ramp.Dark;                      // tramado hacia la sombra
                else if (up == 2 && checker) c = ramp.Light;                       // tramado hacia la luz
                t.SetPixel(x, y, c);
            }

            // Contorno: píxeles vacíos que tocan algo pintado.
            var copy = t.GetPixels();
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                if (copy[y * W + x].a > 0f) continue;
                if (Filled(copy, x - 1, y) || Filled(copy, x + 1, y) || Filled(copy, x, y - 1) || Filled(copy, x, y + 1))
                    t.SetPixel(x, y, outline);
            }
            return t;
        }

        private bool Filled(Color[] px, int x, int y) => In(x, y) && px[y * W + x].a > 0f;

        /// <summary>Cuántos píxeles del mismo material hay en esa dirección (máx. 4) hasta el borde.</summary>
        private int Run(int x, int y, int dx, int dy, int m)
        {
            for (int k = 1; k <= 4; k++)
            {
                int nx = x + dx * k, ny = y + dy * k;
                if (!In(nx, ny) || isDirect[ny * W + nx] || mat[ny * W + nx] != m) return k;
            }
            return 5;
        }

        /// <summary>Silueta clara del sprite (para el destello al recibir un golpe).</summary>
        public static Texture2D Silhouette(Texture2D src)
        {
            var t = PixelArt.Solid(src.width, src.height, Color.clear);
            var px = src.GetPixels();
            var white = PixelArt.Hex("#F7F2FF");
            for (int i = 0; i < px.Length; i++)
                if (px[i].a > 0f) t.SetPixel(i % src.width, i / src.width, white);
            return t;
        }
    }
}
#endif
