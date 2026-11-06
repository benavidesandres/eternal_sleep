#if UNITY_EDITOR
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Suelo, plataformas, pinchos y velo de sombra, dibujados por código a 64 px por casilla.
    ///
    /// Regla de oro: la superficie pisable toca el borde superior de la textura (fila 63), justo donde
    /// está el collider. Por eso los personajes y objetos, que tienen los pies en el borde de la casilla,
    /// quedan apoyados y no flotando. Los pinchos, al revés: su base toca la fila 0.
    ///
    /// El suelo se dibuja según qué lados de la casilla están al aire (ver Open*):
    /// musgo arriba, roca biselada en los costados y sombra debajo.
    /// </summary>
    internal static class GroundArt
    {
        public const int Size = 64;
        public const float PPU = 64f;
        public const int GroundVariants = 3;

        public const int OpenUp = 1, OpenLeft = 2, OpenRight = 4, OpenDown = 8;

        private static readonly Color Ink = PixelArt.Hex("#0D0918");
        private static readonly Color Mortar = PixelArt.Hex("#120D20");
        private static readonly Color[] Stone =
        {
            PixelArt.Hex("#2A2145"), PixelArt.Hex("#32284F"), PixelArt.Hex("#3A2F5B"), PixelArt.Hex("#443768"),
        };
        private static readonly Color StoneLight = PixelArt.Hex("#5E4E8E");
        private static readonly Color StoneShade = PixelArt.Hex("#1E1733");
        private static readonly Color MossRim = PixelArt.Hex("#C4F7E2");
        private static readonly Color MossLight = PixelArt.Hex("#74D3B6");
        private static readonly Color Moss = PixelArt.Hex("#3C9488");
        private static readonly Color MossDark = PixelArt.Hex("#2A6A70");
        private static readonly Color MossEdge = PixelArt.Hex("#1E4757");
        private static readonly Color Vein = PixelArt.Hex("#8FE9FF");

        private const float Deep = 0.55f; // brillo de la roca lejos del aire

        // Centros de las piedras: repetidos en mosaico, las casillas encajan sin costuras.
        private static readonly Vector2[] Seeds = MakeSeeds();

        private static Vector2[] MakeSeeds()
        {
            var rng = new System.Random(11);
            var seeds = new Vector2[9];
            const float cell = Size / 3f;
            for (int i = 0; i < seeds.Length; i++)
                seeds[i] = new Vector2((i % 3 + 0.15f + (float)rng.NextDouble() * 0.7f) * cell,
                                       (i / 3 + 0.15f + (float)rng.NextDouble() * 0.7f) * cell);
            return seeds;
        }

        // ---------- Suelo ----------

        /// <param name="open">Lados al aire (OpenUp | OpenLeft...).</param>
        /// <param name="variant">Cambia grietas y musgo sin romper las uniones con las casillas vecinas.</param>
        public static Texture2D Ground(int open, int variant)
        {
            bool up = (open & OpenUp) != 0, left = (open & OpenLeft) != 0;
            bool right = (open & OpenRight) != 0, down = (open & OpenDown) != 0;
            var t = PixelArt.Solid(Size, Size, Color.clear);

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                int fromTop = Size - 1 - y, fromLeft = x, fromRight = Size - 1 - x, fromBottom = y;
                if (RoundedAway(up, left, fromTop, fromLeft) || RoundedAway(up, right, fromTop, fromRight) ||
                    RoundedAway(down, left, fromBottom, fromLeft) || RoundedAway(down, right, fromBottom, fromRight))
                    continue;

                float glow = 0f; // más claro cerca del aire
                if (up) glow = Mathf.Max(glow, 1f - fromTop / 46f);
                if (left) glow = Mathf.Max(glow, 0.8f * (1f - fromLeft / 22f));
                if (right) glow = Mathf.Max(glow, 0.8f * (1f - fromRight / 22f));
                if (down) glow = Mathf.Max(glow, 0.35f * (1f - fromBottom / 14f));
                float light = Mathf.Lerp(Deep, 1f, Mathf.Clamp01(glow));

                Color c = Scale(Rock(x, y), light);

                if (left && fromLeft < 2 || right && fromRight < 2 || down && fromBottom < 3) c = Ink;
                else if (left && fromLeft < 4) c = Scale(StoneLight, 0.9f);
                else if (right && fromRight < 4) c = Scale(StoneShade, 1.1f);
                else if (down && fromBottom < 5) c = StoneShade;

                t.SetPixel(x, y, c);
            }

            Decorate(t, open, variant);
            if (up) PaintMoss(t, left, right, variant);
            return t;
        }

        /// <summary>Esquina recortada solo donde dos lados al aire se encuentran.</summary>
        private static bool RoundedAway(bool a, bool b, int da, int db) => a && b && da + db < 3;

        /// <summary>Piedras irregulares con junta oscura y bisel de luz arriba a la izquierda.</summary>
        private static Color Rock(int x, int y)
        {
            float edge = StoneEdge(x, y, out int id);
            if (edge < 1.6f) return Mortar;

            Color c = Stone[id * 5 % Stone.Length];
            if (StoneEdge(x - 2, y + 2, out _) < 1.6f) c = StoneLight;
            else if (StoneEdge(x + 3, y - 3, out _) < 1.6f) c = StoneShade;
            else if (Hash(x, y, id) < 0.07f) c = Scale(c, 0.85f);
            else if (Hash(x, y, id + 9) < 0.05f) c = Scale(c, 1.15f);
            return c;
        }

        /// <summary>Distancia a la junta más cercana (Voronoi en mosaico) y qué piedra es.</summary>
        private static float StoneEdge(int x, int y, out int id)
        {
            float d1 = float.MaxValue, d2 = float.MaxValue;
            id = 0;
            for (int i = 0; i < Seeds.Length; i++)
            {
                float dx = Mathf.Abs(x - Seeds[i].x) % Size, dy = Mathf.Abs(y - Seeds[i].y) % Size;
                dx = Mathf.Min(dx, Size - dx);
                dy = Mathf.Min(dy, Size - dy);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < d1) { d2 = d1; d1 = d; id = i; }
                else if (d < d2) d2 = d;
            }
            return d2 - d1;
        }

        /// <summary>Detalles dentro de la casilla (nunca tocan el borde, para no romper las uniones).</summary>
        private static void Decorate(Texture2D t, int open, int variant)
        {
            if (variant == 1) // grieta
            {
                int x = 22, y = 34;
                var rng = new System.Random(4 + open);
                for (int i = 0; i < 20; i++)
                {
                    t.SetPixel(x, y, Mortar);
                    t.SetPixel(x + 1, y, Scale(Stone[0], 0.75f));
                    x += rng.Next(0, 3) - (i % 5 == 0 ? 2 : 0);
                    y -= rng.Next(0, 2);
                    x = Mathf.Clamp(x, 10, 52);
                    y = Mathf.Clamp(y, 10, 40);
                }
            }
            else if (variant == 2) // veta de cristal de sueño
            {
                foreach (var (x, y) in new[] { (40, 22), (41, 22), (41, 23), (42, 23), (42, 24), (43, 25), (24, 16), (25, 16), (25, 17) })
                {
                    t.SetPixel(x, y, PixelArt.WithAlpha(Vein, 1f));
                    t.SetPixel(x, y - 1, Scale(Vein, 0.45f));
                }
            }
        }

        /// <summary>Capa de musgo pegada al borde superior, con borde inferior irregular y gotas colgando.</summary>
        private static void PaintMoss(Texture2D t, bool left, bool right, int variant)
        {
            for (int x = 0; x < Size; x++)
            {
                float s = x / (float)Size * Mathf.PI * 2f;
                // Los senos valen lo mismo en x = 0 y x = 64 y la parte que depende de la variante vale 0 ahí:
                // cualquier casilla encaja con cualquier otra.
                float depth = 15f + 3f * Mathf.Sin(s * 2f) + 2f * Mathf.Sin(s * 5f)
                                  + 3f * Mathf.Sin(s * (3 + variant)) * (variant == 0 ? 0.6f : 1f);
                if (x > 2 && x < Size - 3 && Hash(x / 3, 7, variant) < 0.24f) depth += 5f + 10f * Hash(x / 3, 8, variant); // gota
                if (left) depth = Mathf.Min(depth, 7f + x * 0.9f);
                if (right) depth = Mathf.Min(depth, 7f + (Size - 1 - x) * 0.9f);
                int m = Mathf.RoundToInt(depth);

                for (int d = 0; d < m + 3 && d < Size; d++)
                {
                    int y = Size - 1 - d;
                    Color old = t.GetPixel(x, y);
                    if (old.a <= 0f) continue;

                    Color c;
                    if (d >= m) c = Scale(old, 0.62f); // sombra del musgo sobre la roca
                    else if (d >= m - 2) c = MossEdge;
                    else if (d < 2) c = MossRim;
                    else if (d < 5) c = MossLight;
                    else c = Hash(x, d, variant) < 0.22f ? MossDark : Hash(x, d, variant + 3) < 0.12f ? MossLight : Moss;
                    t.SetPixel(x, y, c);
                }
            }

            // Florecitas doradas sueltas (variantes con detalle).
            if (variant == 0) return;
            foreach (int x in variant == 1 ? new[] { 13, 44 } : new[] { 29 })
            {
                t.SetPixel(x, Size - 4, PixelArt.Gold);
                t.SetPixel(x + 1, Size - 4, PixelArt.Gold);
                t.SetPixel(x, Size - 5, PixelArt.Gold);
                t.SetPixel(x + 1, Size - 5, Scale(PixelArt.Gold, 0.75f));
            }
        }

        // ---------- Plataforma ----------

        /// <summary>Losa flotante clara, para distinguirla del suelo. La superficie toca el borde superior.</summary>
        public static Texture2D Platform(int open)
        {
            bool left = (open & OpenLeft) != 0, right = (open & OpenRight) != 0;
            var t = PixelArt.Solid(Size, Size, Color.clear);
            Color rim = PixelArt.Hex("#EDE5FF"), lit = PixelArt.Hex("#B5A5E6"), body = PixelArt.Hex("#6F5EA6");
            Color bodyDark = PixelArt.Hex("#54457F"), under = PixelArt.Hex("#2C2348");

            for (int x = 0; x < Size; x++)
            {
                int edge = Mathf.Min(left ? x : Size, right ? Size - 1 - x : Size);
                float s = x / (float)Size * Mathf.PI * 2f;
                int thick = 18 + Mathf.RoundToInt(2f * Mathf.Sin(s * 3f));
                if (edge < 10) thick = Mathf.Min(thick, 7 + edge);
                bool joint = x == 31 || x == 32;

                for (int d = 0; d < thick; d++)
                {
                    Color c;
                    if (d < 2) c = rim;
                    else if (d < 5) c = lit;
                    else if (d >= thick - 3) c = under;
                    else if (joint) c = under;
                    else c = Hash(x, d, 2) < 0.12f ? bodyDark : body;
                    if (edge < 2 && d >= 1) c = under;
                    t.SetPixel(x, Size - 1 - d, c);
                }
            }

            // Hilos de musgo y una gota de luz colgando por debajo.
            foreach (var (x, len) in new[] { (9, 8), (10, 5), (23, 4), (41, 11), (42, 7), (55, 5) })
            {
                if (left && x < 12 || right && x > 52) continue;
                for (int d = 18; d < 19 + len; d++)
                {
                    int y = Size - 1 - d;
                    t.SetPixel(x, y, d > 18 + len - 2 ? MossLight : MossDark);
                }
            }
            return t;
        }

        // ---------- Pinchos ----------

        /// <summary>Espinas de cristal. La base toca el borde inferior, sobre el suelo.</summary>
        public static Texture2D Spikes()
        {
            var t = PixelArt.Solid(Size, Size, Color.clear);
            Color outline = PixelArt.Hex("#1B1230"), light = PixelArt.Hex("#E9E2FF"), mid = PixelArt.Hex("#A997E0");
            Color dark = PixelArt.Hex("#6450A0");

            // (centro, media anchura en la base, altura), de atrás hacia delante.
            foreach (var (cx, half, h) in new[] { (8f, 8f, 36), (56f, 8f, 34), (32f, 9f, 42), (20f, 8f, 48), (44f, 8f, 46) })
            for (int y = 0; y < h; y++)
            {
                float k = 1f - y / (float)h;
                float w = half * k;
                for (int x = Mathf.FloorToInt(cx - w - 1f); x <= Mathf.CeilToInt(cx + w + 1f); x++)
                {
                    if (x < 0 || x >= Size) continue;
                    float dx = x + 0.5f - cx;
                    if (Mathf.Abs(dx) > w + 1f) continue;

                    Color c;
                    if (Mathf.Abs(dx) > w) c = outline;
                    else if (y > h - 9) c = y > h - 4 ? PixelArt.Moon : PixelArt.Rose; // punta peligrosa
                    else if (dx < -w * 0.25f) c = light;
                    else if (dx < w * 0.35f) c = mid;
                    else c = dark;
                    t.SetPixel(x, y, c);
                }
            }
            return t;
        }

        // ---------- Velo de sombra ----------

        /// <summary>Cortina de hilos oscuros que ondulan. Se repite en vertical para apilar casillas.</summary>
        public static Texture2D Veil(int frame, int frames)
        {
            var t = PixelArt.Solid(Size, Size, Color.clear);
            Color deep = PixelArt.Hex("#150C2E"), body = PixelArt.Hex("#3A1F7A"), thread = PixelArt.Hex("#B48CFF");
            float phase = frame / (float)frames * Mathf.PI * 2f;

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float v = y / (float)Size * Mathf.PI * 2f;
                float bend = 3f * Mathf.Sin(v + phase) + 1.5f * Mathf.Sin(v * 3f - phase);
                float sx = x + bend;
                float strand = Mathf.Sin(sx * 0.55f) * 0.5f + Mathf.Sin(sx * 0.21f + 1.3f) * 0.5f;
                float edge = Mathf.Min(x, Size - 1 - x) / 10f;
                if (edge < 0.25f) continue;

                Color c = strand > 0.72f ? thread : strand > 0.1f ? body : deep;
                c.a = strand > 0.72f ? 0.95f : Mathf.Lerp(0.55f, 0.9f, Mathf.Clamp01(edge));
                t.SetPixel(x, y, c);
            }
            return t;
        }

        // ---------- Utilidades ----------

        private static Color Scale(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
#endif
