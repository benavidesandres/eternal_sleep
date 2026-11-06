#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Arte del pueblo en alta definición (el doble de píxeles por casilla):
    /// aldeanos estilo "chibi" como Kael (cabeza grande, ojos con brillo), aldeanos en la cama,
    /// fachadas de las casas con su puerta e interiores con muebles según el oficio de cada uno.
    /// </summary>
    public static class VillageArtHD
    {
        public const float NpcPPU = 48f;
        public const float HousePPU = 32f;
        /// <summary>Los aldeanos en la cama, un poco más grandes para que la cama luzca en el cuarto.</summary>
        public const float BedPPU = 36f;

        private static readonly Color Ink = PixelArt.Hex("#0B0814");
        private static readonly Color EyeWhite = PixelArt.Hex("#EDE6FA");
        private static readonly Color White = PixelArt.Hex("#FFFFFF");
        private static readonly Color Gold = PixelArt.Hex("#FFE38A");
        private static readonly Color GoldHot = PixelArt.Hex("#FFF6CC");
        private static readonly Color Amber = PixelArt.Hex("#E8A33C");
        private static readonly Color Ember = PixelArt.Hex("#FF7A3C");
        private static readonly Color Blush = PixelArt.WithAlpha(PixelArt.Hex("#E86A92"), 0.55f);
        private static readonly Color Mouth = PixelArt.Hex("#7A3A4A");

        private static Vector2 V(float x, float y) => new Vector2(x, y);
        private static int R(float v) => Mathf.RoundToInt(v);

        // Rampas comunes
        private static readonly Ramp Skin = new Ramp("#9C7468", "#D9B2A0", "#F4D8C8");
        private static readonly Ramp SkinPale = new Ramp("#8E86A6", "#CFC8DE", "#F0EBF8");
        private static readonly Ramp Leather = new Ramp("#1E1418", "#36262A", "#54403E");
        private static readonly Ramp Wood = new Ramp("#2A1A1E", "#4A3034", "#70504C");
        private static readonly Ramp WoodLight = new Ramp("#4A3236", "#7A5852", "#A8846E");
        private static readonly Ramp Iron = new Ramp("#0F0C16", "#262036", "#463E5C");
        private static readonly Ramp Metal = new Ramp("#3C3A4A", "#6E6C84", "#B2B0C8");
        private static readonly Ramp GoldR = new Ramp("#6B5320", "#B8924A", "#FFE38A");
        private static readonly Ramp Cloth = new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF");
        private static readonly Ramp Rose = new Ramp("#5A1C30", "#A33A5E", "#DB6A8E");
        private static readonly Ramp Stone = new Ramp("#1F1A2B", "#3A3250", "#5A5078");
        private static readonly Ramp StoneLight = new Ramp("#3A3352", "#5E567E", "#8A82AA");
        private static readonly Ramp Plaster = new Ramp("#241C34", "#352A4C", "#4C3E68");
        private static readonly Ramp Brick = new Ramp("#3A1C24", "#5E2E36", "#86484E");

        // ---------------------------------------------------------------- Ayudas para caras

        /// <summary>Ojo grande con iris, pupila, brillo y párpado. blink = cerrado.</summary>
        private static void Eye(SpritePainter c, float x, float y, Color iris, bool blink, float look = -0.6f)
        {
            int ix = R(x), iy = R(y);
            if (blink)
            {
                for (int i = -2; i <= 2; i++) c.Dot(ix + i, iy - (Mathf.Abs(i) == 2 ? 1 : 0), Ink);
                return;
            }
            c.EllipseDot(x, y, 2.6f, 3.4f, EyeWhite);
            c.EllipseDot(x + look, y - 0.6f, 1.9f, 2.7f, iris);
            c.Dot(R(x + look), iy - 1, Ink);
            c.Dot(R(x + look), iy, Ink);
            c.Dot(R(x + look) - 1, iy + 1, White);
            c.Dot(R(x + look) - 1, iy + 2, White);
            c.Dot(R(x + look) + 1, iy - 2, PixelArt.WithAlpha(White, 0.7f));
            for (int i = -2; i <= 2; i++) c.Dot(ix + i, iy + 3 + (Mathf.Abs(i) == 2 ? -1 : 0), Ink); // párpado
        }

        private static void Cheeks(SpritePainter c, float x0, float x1, float y)
        {
            foreach (float x in new[] { x0, x1 })
            {
                c.Dot(R(x), R(y), Blush);
                c.Dot(R(x) + 1, R(y), Blush);
            }
        }

        private static void Smile(SpritePainter c, float x, float y)
        {
            c.Dot(R(x) - 2, R(y) + 1, Mouth);
            c.Dot(R(x) - 1, R(y), Mouth);
            c.Dot(R(x), R(y), Mouth);
            c.Dot(R(x) + 1, R(y), Mouth);
            c.Dot(R(x) + 2, R(y) + 1, Mouth);
        }

        private static Texture2D[] Frames(int n, Func<int, Texture2D> draw)
        {
            var r = new Texture2D[n];
            for (int i = 0; i < n; i++) r[i] = draw(i);
            return r;
        }

        private static void Glow(SpritePainter c, Func<int, int, bool> inside, int x0, int y0, int x1, int y1, Color bottom, Color top)
        {
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                if (inside(x, y)) c.Dot(x, y, Color.Lerp(bottom, top, (y - y0) / (float)Mathf.Max(1, y1 - y0)));
        }

        private static bool Any(int x, int y) => true;

        // ---------------------------------------------------------------- Vesna, la farolera (72×100)

        public static Texture2D[] Lamplighter() => Frames(4, f =>
        {
            var c = new SpritePainter(72, 100);
            var coat = new Ramp("#0F2226", "#1C3C42", "#2E6068");
            var hair = new Ramp("#2A1A20", "#4A2E34", "#6E4A4C");
            var ramps = new[] { default, coat, Skin, Wood, Iron, Rose, hair, Leather, GoldR };
            float bob = f % 2 == 1 ? 1f : 0f;
            float sway = new[] { 0f, 1.5f, 0f, -1.5f }[f];
            bool blink = f == 3;

            // Pértiga y farol.
            c.Line(V(14, 1), V(14, 84), 3.2f, 3f, 3);
            c.Curve(V(14, 84), V(15, 92), V(24 + sway, 88), 2.4f, 2f, 4);
            float lx = 24 + sway;
            c.Line(V(lx, 88), V(lx, 82), 1.2f, 1.2f, 4);
            c.Poly(4, V(lx - 6, 80), V(lx + 6, 80), V(lx, 86));
            c.Rect(R(lx) - 5, 66, R(lx) + 5, 80, 4);
            c.Rect(R(lx) - 6, 64, R(lx) + 6, 66, 4);
            Glow(c, Any, R(lx) - 4, 67, R(lx) + 4, 78, f % 2 == 0 ? Amber : Gold, GoldHot);
            c.Rect(R(lx), 67, R(lx), 78, 4);

            // Botas.
            c.Ellipse(34f, 3f, 5.5f, 3f, 7);
            c.Ellipse(50f, 3f, 5.5f, 3f, 7);

            // Abrigo largo con botones dorados.
            c.Poly(1, V(25, 4), V(59, 4), V(55, 44 + bob), V(29, 44 + bob));
            c.Recolor(42, 5, 42, 40, 1, 7);
            for (int y = 12; y <= 36; y += 8) c.Dot(45, y, Gold);
            c.Line(V(30, 6), V(34, 30), 1f, 1f, 7); // pliegues
            c.Line(V(54, 6), V(51, 28), 1f, 1f, 7);

            // Brazos.
            c.Line(V(32, 40 + bob), V(18, 47), 7f, 6f, 1);
            c.Ellipse(16f, 47f, 3.5f, 3.2f, 2);
            c.Line(V(53, 40 + bob), V(57, 24), 7f, 6f, 1);
            c.Ellipse(57.5f, 22f, 3.2f, 3f, 2);

            // Bufanda.
            c.Rect(29, R(42 + bob), 55, R(47 + bob), 5);
            c.Line(V(50, 44 + bob), V(57, 30 + bob), 4.5f, 3.5f, 5);
            c.Recolor(29, R(44 + bob), 58, R(44 + bob), 5, 8);

            // Capucha con la punta caída.
            float hy = 62 + bob;
            c.Curve(V(54, hy + 12), V(66, hy + 16), V(66, hy + 2), 9f, 3f, 1);
            c.Ellipse(42f, hy, 19f, 18.5f, 1);

            // Cara, flequillo y rasgos.
            c.Ellipse(42f, hy - 4f, 13.5f, 12.5f, 2);
            c.Poly(6, V(29, hy + 2), V(55, hy + 2), V(53, hy + 9), V(31, hy + 9));
            for (int x = 32; x <= 52; x += 5) c.Poly(6, V(x, hy + 3), V(x + 4, hy + 3), V(x + 2, hy - 1));
            Eye(c, 36.5f, hy - 4f, PixelArt.Hex("#C27A2E"), blink);
            Eye(c, 47.5f, hy - 4f, PixelArt.Hex("#C27A2E"), blink);
            Cheeks(c, 32f, 51f, hy - 9f);
            Smile(c, 42f, hy - 11f);
            return c.Render(ramps, Ink);
        });

        // ---------------------------------------------------------------- Grim, el mercader (84×96)

        public static Texture2D[] Merchant() => Frames(4, f =>
        {
            var c = new SpritePainter(84, 96);
            var cloak = new Ramp("#1E1530", "#34264E", "#4E3C72");
            var shadow = new Ramp("#07050D", "#100B1C", "#1C1430");
            var beard = new Ramp("#6E6880", "#A8A2BC", "#DCD6EC");
            var hat = new Ramp("#120D1F", "#211A36", "#3A2D57");
            var bag = new Ramp("#2E1E18", "#523628", "#7A5440");
            var ramps = new[] { default, cloak, Skin, Wood, hat, beard, GoldR, bag, Rose, shadow, Iron, new Ramp("#1A3038", "#2B4E5A", "#467A88") };
            float bob = f % 2 == 1 ? 1f : 0f;
            bool blink = f == 3;

            // Mochila gigante con sus cosas.
            c.Ellipse(58f, 40f + bob, 18f, 24f, 7);
            c.Recolor(40, R(28 + bob), 76, R(29 + bob), 7, 3);
            c.Ellipse(57f, 67f + bob, 13f, 6f, 8);
            for (int x = 47; x <= 67; x += 5) c.Recolor(x, R(62 + bob), x, R(73 + bob), 8, 6);
            c.Ellipse(73f, 36f + bob, 5.5f, 6.5f, 6);
            c.Rect(70, R(42 + bob), 76, R(43 + bob), 6);
            c.Line(V(48, 52 + bob), V(68, 58 + bob), 3f, 3f, 11);                    // pergamino
            c.Line(V(70, 22 + bob), V(76, 12), 1.2f, 1.2f, 10);                    // farolito colgado
            c.Rect(73, 2, 80, 12, 10);
            Glow(c, Any, 74, 4, 79, 10, f < 2 ? Amber : Gold, GoldHot);

            // Bastón.
            c.Line(V(12, 0), V(14, 58), 3f, 3f, 3);
            c.Ellipse(14f, 59f, 3.5f, 3.5f, 6);

            // Capa encorvada con remiendo y bajo deshilachado.
            c.Poly(1, V(17, 3), V(50, 3), V(50, 46 + bob), V(22, 52 + bob));
            for (int x = 18; x <= 46; x += 6) c.Poly(1, V(x, 4), V(x + 5, 4), V(x + 2, 0));
            c.Rect(38, 14, 45, 21, 8);
            for (int y = 15; y <= 20; y += 2) { c.Dot(38, y, Ink); c.Dot(45, y, Ink); }
            c.Line(V(26, 46 + bob), V(15, 51), 7f, 6f, 1);
            c.Ellipse(14.5f, 51f, 3.6f, 3.2f, 2);

            // Cabeza en sombra, nariz y barba larga.
            float hy = 58 + bob;
            c.Ellipse(32f, hy, 15f, 13f, 9);
            c.Poly(5, V(21, hy - 2), V(43, hy - 2), V(38, hy - 22), V(30, hy - 24), V(25, hy - 16));
            for (int x = 26; x <= 38; x += 4)
                for (int y = R(hy - 20); y <= R(hy - 5); y += 3) c.Dot(x + (y / 3) % 2, y, PixelArt.Hex("#8E88A2")); // mechones
            c.Ellipse(26f, hy - 1f, 4f, 3.2f, 2);

            // Ojos que brillan bajo el sombrero.
            foreach (float ex in new[] { 27f, 37f })
            {
                if (blink) { for (int i = -1; i <= 1; i++) c.Dot(R(ex) + i, R(hy + 3), Amber); continue; }
                c.EllipseDot(ex, hy + 3f, 2f, 1.6f, Gold);
                c.Dot(R(ex), R(hy + 3), GoldHot);
            }

            // Sombrero de ala ancha con pluma.
            c.Curve(V(42, hy + 13), V(56, hy + 26), V(50, hy + 34), 3f, 1f, 11);
            c.Ellipse(32f, hy + 9f, 25f, 4.5f, 4);
            c.Poly(4, V(20, hy + 10), V(44, hy + 10), V(41, hy + 26), V(23, hy + 26));
            c.Rect(21, R(hy + 11), 43, R(hy + 14), 8);
            return c.Render(ramps, Ink);
        });

        // ---------------------------------------------------------------- Lune, la niña (56×74)

        public static Texture2D[] Girl() => Frames(4, f =>
        {
            var c = new SpritePainter(56, 74);
            var dress = new Ramp("#33284F", "#5E4F8C", "#8E7FC2");
            var hair = new Ramp("#1A1028", "#2E1E44", "#4A3466");
            var ramps = new[] { default, dress, Skin, hair, Rose, Cloth, Leather, Wood };
            float bob = f % 2 == 1 ? 1f : 0f;
            bool blink = f == 3;

            c.Ellipse(20f, 3f, 4.5f, 2.6f, 6);
            c.Ellipse(32f, 3f, 4.5f, 2.6f, 6);
            c.Rect(18, 4, 22, 12, 3);
            c.Rect(30, 4, 34, 12, 3);
            c.Poly(1, V(11, 11), V(41, 11), V(35, 34 + bob), V(17, 34 + bob));       // vestido
            c.Poly(5, V(20, 13), V(32, 13), V(31, 31 + bob), V(21, 31 + bob));       // delantal
            c.Recolor(11, 11, 41, 12, 1, 5);
            c.Line(V(17, 31 + bob), V(13, 20), 5f, 4f, 1);
            c.Ellipse(12.5f, 19f, 2.6f, 2.4f, 2);
            c.Line(V(35, 31 + bob), V(41, 22), 5f, 4f, 1);
            c.Ellipse(42f, 21f, 2.6f, 2.4f, 2);

            // Frasco con polilla de luz.
            c.Ellipse(45f, 16f, 6f, 7.5f, 5);
            Glow(c, (x, y) => (x - 45f) * (x - 45f) / 25f + (y - 16f) * (y - 16f) / 42f <= 1f, 40, 9, 50, 23,
                PixelArt.Hex("#2E2550"), PixelArt.Hex("#6B5A9E"));
            c.Rect(40, 23, 50, 26, 7);
            int mx = 45 + new[] { 0, 2, 0, -2 }[f], my = 16 + new[] { 0, 1, 2, 1 }[f];
            c.Dot(mx, my, GoldHot); c.Dot(mx - 1, my, Gold); c.Dot(mx + 1, my, Gold); c.Dot(mx - 2, my + 1, Gold); c.Dot(mx + 2, my + 1, Gold);

            // Cabeza: coletas con lazos, pelo y cara.
            float hy = 46 + bob;
            c.Ellipse(9f, hy - 6f, 5.5f, 8f, 3);
            c.Ellipse(43f, hy - 6f, 5.5f, 8f, 3);
            c.Ellipse(26f, hy + 1f, 16f, 15f, 3);
            c.Ellipse(26f, hy - 2f, 12f, 11f, 2);
            c.Poly(3, V(14, hy + 4), V(38, hy + 4), V(36, hy + 12), V(16, hy + 12));
            for (int x = 16; x <= 34; x += 6) c.Poly(3, V(x, hy + 5), V(x + 5, hy + 5), V(x + 2, hy));
            foreach (float bx in new[] { 9f, 43f })
            {
                c.Ellipse(bx - 3f, hy + 3f, 3f, 2.2f, 4);
                c.Ellipse(bx + 3f, hy + 3f, 3f, 2.2f, 4);
                c.Ellipse(bx, hy + 3f, 1.4f, 1.4f, 4);
            }
            Eye(c, 21f, hy - 2f, PixelArt.Hex("#8E5AC2"), blink, 0.6f);
            Eye(c, 31f, hy - 2f, PixelArt.Hex("#8E5AC2"), blink, 0.6f);
            Cheeks(c, 16f, 35f, hy - 7f);
            c.Dot(26, R(hy - 9), Mouth); c.Dot(27, R(hy - 9), Mouth); c.Dot(26, R(hy - 10), Mouth); c.Dot(27, R(hy - 10), Mouth);
            return c.Render(ramps, Ink);
        });

        // ---------------------------------------------------------------- Ash, el sepulturero (64×114)

        public static Texture2D[] Gravedigger() => Frames(4, f =>
        {
            var c = new SpritePainter(64, 114);
            var coat = new Ramp("#0C0914", "#191426", "#2E2644");
            var lapel = new Ramp("#1E1530", "#34264E", "#4E3C72");
            var hair = new Ramp("#4A4658", "#7A768C", "#A8A4BA");
            var ramps = new[] { default, coat, SkinPale, Wood, Metal, lapel, hair, Leather, new Ramp("#2A1830", "#4A2A52", "#6E4278") };
            float bob = f % 2 == 1 ? 1f : 0f;
            bool blink = f == 2;

            // Pala con tierra.
            c.Line(V(51, 12), V(51, 78), 3f, 3f, 3);
            c.Rect(46, 78, 56, 81, 3);
            c.Poly(4, V(45, 0), V(57, 0), V(56, 13), V(46, 13));
            c.Dot(48, 3, PixelArt.Hex("#3A2C1C")); c.Dot(53, 2, PixelArt.Hex("#3A2C1C")); c.Dot(50, 5, PixelArt.Hex("#3A2C1C"));

            c.Ellipse(23f, 3f, 5f, 3f, 7);
            c.Ellipse(37f, 3f, 5f, 3f, 7);
            c.Poly(1, V(16, 4), V(44, 4), V(41, 60 + bob), V(19, 60 + bob));          // abrigo largo
            c.Poly(5, V(23, 58 + bob), V(30, 42), V(37, 58 + bob));                   // solapas
            c.Recolor(30, 5, 30, 40, 1, 5);
            for (int y = 16; y <= 36; y += 7) c.Dot(32, y, PixelArt.Hex("#8A82AA"));
            c.Line(V(39, 55 + bob), V(50, 46), 6f, 5f, 1);
            c.Ellipse(50f, 46f, 3f, 3f, 2);
            c.Line(V(21, 55 + bob), V(18, 36), 6f, 5f, 1);
            c.Ellipse(18f, 34.5f, 3f, 3f, 2);

            // Cara alargada y pálida, patillas grises.
            float hy = 70 + bob;
            c.Ellipse(30f, hy, 11f, 12.5f, 2);
            c.Rect(19, R(hy - 2), 21, R(hy + 8), 6);
            c.Rect(39, R(hy - 2), 41, R(hy + 8), 6);
            c.EllipseDot(25f, hy + 1f, 3.4f, 3.6f, PixelArt.Hex("#5E567E"));
            c.EllipseDot(35f, hy + 1f, 3.4f, 3.6f, PixelArt.Hex("#5E567E"));
            if (blink) { for (int i = -1; i <= 1; i++) { c.Dot(25 + i, R(hy + 1), Ink); c.Dot(35 + i, R(hy + 1), Ink); } }
            else
            {
                c.EllipseDot(25f, hy + 1f, 1.6f, 2f, EyeWhite); c.Dot(25, R(hy + 1), Ink);
                c.EllipseDot(35f, hy + 1f, 1.6f, 2f, EyeWhite); c.Dot(35, R(hy + 1), Ink);
            }
            c.Line(V(30, hy - 1), V(29, hy - 5), 1.5f, 1.5f, 6);
            for (int i = -3; i <= 3; i++) c.Dot(30 + i, R(hy - 8), Mouth);

            // Chistera.
            c.Rect(15, R(hy + 10), 45, R(hy + 13), 1);
            c.Rect(19, R(hy + 13), 41, R(hy + 34), 1);
            c.Rect(19, R(hy + 15), 41, R(hy + 18), 8);
            return c.Render(ramps, Ink);
        });

        // ---------------------------------------------------------------- Gato (60×44)

        public static Texture2D[] Cat() => Frames(4, f =>
        {
            var c = new SpritePainter(60, 44);
            var fur = new Ramp("#0A0712", "#171224", "#2E2546");
            var chest = new Ramp("#1E1830", "#2E2546", "#433868");
            var ramps = new[] { default, fur, chest };
            float tail = new[] { 0f, 3f, 5f, 2f }[f];
            bool blink = f == 2;

            c.Curve(V(42, 6), V(58, 8 + tail), V(52, 26 + tail), 5.5f, 3.5f, 1);
            c.Ellipse(32f, 14f, 14f, 13f, 1);
            c.Ellipse(24f, 14f, 6f, 9f, 2);
            c.Ellipse(20f, 3f, 4f, 3f, 1); c.Ellipse(30f, 3f, 4f, 3f, 1);
            c.Ellipse(18f, 27f, 10.5f, 9.5f, 1);
            c.Poly(1, V(9, 30), V(15, 33), V(10, 42));
            c.Poly(1, V(21, 33), V(27, 30), V(26, 42));
            c.Dot(11, 37, PixelArt.Hex("#5E2E46")); c.Dot(24, 37, PixelArt.Hex("#5E2E46"));
            foreach (float ex in new[] { 14f, 23f })
            {
                if (blink) { for (int i = -2; i <= 2; i++) c.Dot(R(ex) + i, 28, Gold); continue; }
                c.EllipseDot(ex, 28f, 2.8f, 2.2f, Gold);
                c.Dot(R(ex), 27, Ink); c.Dot(R(ex), 28, Ink); c.Dot(R(ex), 29, Ink);
                c.Dot(R(ex) - 1, 29, GoldHot);
            }
            c.Dot(18, 24, PixelArt.Hex("#C2416B")); c.Dot(19, 24, PixelArt.Hex("#C2416B"));
            for (int i = 0; i < 5; i++) { c.Dot(6 + i, 23 + i % 2, PixelArt.WithAlpha(White, 0.5f)); c.Dot(27 + i, 23 + i % 2, PixelArt.WithAlpha(White, 0.5f)); }
            return c.Render(ramps, Ink);
        });

        // ---------------------------------------------------------------- Aldeanos en su cama (128×70 / 128×96)

        private static readonly Ramp[] Blankets =
        {
            new Ramp("#4A1C30", "#7E3352", "#AD5579"),
            new Ramp("#1A3038", "#2B4E5A", "#467A88"),
            new Ramp("#4A3818", "#7A5A28", "#A88440"),
            new Ramp("#33284F", "#5E4F8C", "#8E7FC2"),
            new Ramp("#1F301F", "#355033", "#567652"),
        };
        private static readonly Ramp[] Caps =
        {
            new Ramp("#6B5A9E", "#B9A8F0", "#E6DCFF"),
            new Ramp("#7A2845", "#C2416B", "#F08AAE"),
            new Ramp("#6B5320", "#B8924A", "#FFE38A"),
            new Ramp("#2B4E5A", "#467A88", "#7FB3C0"),
            new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF"),
        };
        private static readonly Ramp[] Hairs =
        {
            new Ramp("#4A2A1E", "#7A4A30", "#A8704A"),
            new Ramp("#1A1418", "#2E2428", "#4A3C40"),
            new Ramp("#5A5468", "#8E88A2", "#C2BCD4"),
            new Ramp("#3A2018", "#5E3628", "#86543E"),
            new Ramp("#6E6880", "#A8A2BC", "#DCD6EC"),
        };
        private static readonly Ramp[] SkinsV =
        {
            new Ramp("#9C7468", "#D9B2A0", "#F4D8C8"),
            new Ramp("#7A5448", "#B08070", "#D4A890"),
            new Ramp("#9C7468", "#D9B2A0", "#F4D8C8"),
            new Ramp("#5E4038", "#8A6050", "#B08470"),
            new Ramp("#9C8480", "#D6C0B8", "#F2E2DA"),
        };

        private static Ramp[] BedRamps(int v) => new[]
        {
            default, WoodLight, Cloth, SkinsV[v % 5], Caps[v % 5], Blankets[v % 5], Caps[(v + 2) % 5], Wood, Hairs[v % 5],
        };

        private static void BedFrame(SpritePainter c)
        {
            c.Rect(8, 0, 12, 10, 7); c.Rect(114, 0, 118, 10, 7);
            c.Rect(6, 9, 120, 15, 1);
            c.Rect(3, 9, 15, 52, 1);                               // cabecero tallado
            c.Ellipse(9f, 54f, 7f, 4f, 1);
            c.Ellipse(9f, 58f, 3f, 3f, 7);
            c.Rect(5, 30, 13, 32, 7); c.Rect(5, 40, 13, 42, 7);
            c.Rect(111, 9, 123, 32, 1);                            // pie de cama
            c.Ellipse(117f, 34f, 7f, 3.5f, 1);
            c.Ellipse(117f, 37f, 2.6f, 2.6f, 7);
            c.Rect(16, 16, 110, 21, 2);                            // colchón
        }

        /// <summary>Durmiendo: respira bajo una colcha de cuadros (4 cuadros).</summary>
        public static Texture2D[] Sleeping(int v)
        {
            float[] breath = { 0f, 1f, 2f, 1f };
            return Frames(4, f =>
            {
                var c = new SpritePainter(128, 70);
                float lift = breath[f];
                BedFrame(c);
                c.Ellipse(30f, 28f, 14f, 6.5f, 2);                                  // almohada

                c.Ellipse(70f, 24f + lift * 0.5f, 40f, 9f + lift, 5, 20f);          // colcha
                c.Ellipse(101f, 26f, 8f, 8f, 5, 20f);
                for (int x = 38; x < 110; x += 8)
                for (int y = 20; y < 36; y += 8)
                    c.Recolor(x, y, x + 3, y + 3, 5, 6);                            // cuadros
                c.Recolor(34, 20, 110, 22, 5, 2);
                c.Rect(34, R(31 + lift), 40, R(34 + lift), 2);                     // borde de la sábana

                float hx = 34f, hy = 37f;
                c.Ellipse(hx, hy, 13f, 11.5f, 8);                                   // pelo
                c.Ellipse(hx + 1f, hy - 1f, 11f, 10f, 3);                           // cara
                c.Poly(4, V(hx - 13, hy + 4), V(hx + 12, hy + 4), V(hx + 9, hy + 13), V(hx - 3, hy + 15), V(hx - 15, hy + 12), V(hx - 20, hy + 6));
                c.Ellipse(hx - 20f, hy + 5f, 3.5f, 3.5f, 2);                        // borla del gorro
                c.Ellipse(52f, 34f + lift, 4f, 3f, 3);                              // mano

                foreach (float ex in new[] { hx - 4f, hx + 6f })
                    for (int i = -2; i <= 2; i++) c.Dot(R(ex) + i, R(hy - 1) - (Mathf.Abs(i) == 2 ? 0 : 1), Ink);
                Cheeks(c, hx - 8f, hx + 9f, hy - 5f);
                c.EllipseDot(hx + 1f, hy - 7f, 1.4f, 1f + lift * 0.3f, Mouth);
                return c.Render(BedRamps(v), Ink);
            });
        }

        /// <summary>Despierto: sentado en la cama, sonriendo (2 cuadros).</summary>
        public static Texture2D[] Awake(int v) => Frames(2, f =>
        {
            var c = new SpritePainter(128, 96);
            BedFrame(c);
            c.Ellipse(22f, 30f, 10f, 7f, 2);
            c.Ellipse(76f, 24f, 34f, 7f, 5, 20f);
            for (int x = 46; x < 110; x += 8)
            for (int y = 20; y < 32; y += 8)
                c.Recolor(x, y, x + 3, y + 3, 5, 6);

            c.Poly(6, V(26, 22), V(50, 22), V(48, 50), V(28, 50));                   // camisón
            c.Line(V(28, 46), V(26, 30), 6f, 5f, 6);
            c.Line(V(48, 46), V(54, 30), 6f, 5f, 6);
            c.Ellipse(26f, 28f, 3.6f, 3.2f, 3);
            c.Ellipse(55f, 28f, 3.6f, 3.2f, 3);

            float hx = 38f, hy = 63f + f;
            c.Ellipse(hx, hy, 14f, 13f, 8);
            c.Ellipse(hx, hy - 2f, 12f, 11f, 3);
            c.Poly(4, V(hx - 14, hy + 4), V(hx + 14, hy + 4), V(hx + 11, hy + 14), V(hx - 1, hy + 17), V(hx - 14, hy + 13), V(hx - 22, hy + 6));
            c.Ellipse(hx - 22f, hy + 5f, 3.5f, 3.5f, 2);
            Eye(c, hx - 5f, hy - 2f, PixelArt.Hex("#5E4F8C"), false);
            Eye(c, hx + 5f, hy - 2f, PixelArt.Hex("#5E4F8C"), false);
            Cheeks(c, hx - 10f, hx + 9f, hy - 7f);
            Smile(c, hx, hy - 9f);
            return c.Render(BedRamps(v), Ink);
        });

        // ---------------------------------------------------------------- Fachadas (224×192 = 7×6 casillas)

        private static readonly Ramp[] Roofs =
        {
            new Ramp("#2A1428", "#4A2444", "#6E3A62"),
            new Ramp("#151A28", "#253048", "#3A4A6A"),
            new Ramp("#18221C", "#2A3A30", "#405A48"),
            new Ramp("#14262A", "#22404A", "#36606C"),
            new Ramp("#2A1A14", "#4A2E24", "#6E4636"),
        };

        /// <summary>Fachada cerrada con puerta en arco, ventanas encendidas, farol y letrero del oficio.</summary>
        public static Texture2D Facade(int v)
        {
            var c = new SpritePainter(224, 192);
            var ramps = new[] { default, Stone, Wood, Plaster, Roofs[v % 5], new Ramp("#120D1F", "#1C1530", "#2A2040"), WoodLight, StoneLight, Iron, GoldR };

            // Chimenea y tejado de tejas.
            int ch = v % 2 == 0 ? 156 : 52;
            c.Rect(ch, 140, ch + 16, 186, 1);
            c.Rect(ch - 2, 184, ch + 18, 189, 7);
            c.Poly(4, V(0, 124), V(224, 124), V(112, 191));
            for (int y = 128; y < 188; y += 6) c.Recolor(0, y, 223, y, 4, 5);
            for (int y = 128; y < 188; y += 12)
                for (int x = (y / 6) % 2 == 0 ? 0 : 7; x < 224; x += 14) c.Recolor(x, y, x, y + 5, 4, 5);

            // Muro con entramado de madera.
            c.Rect(14, 0, 209, 125, 3);
            c.Rect(10, 120, 213, 127, 2);
            c.Rect(14, 60, 209, 64, 2);
            foreach (int x in new[] { 14, 70, 150, 205 }) c.Rect(x, 0, x + 4, 125, 2);
            c.Line(V(18, 64), V(68, 118), 3f, 3f, 2);
            c.Line(V(205, 64), V(155, 118), 3f, 3f, 2);

            // Zócalo de piedra.
            c.Rect(10, 0, 213, 12, 1);
            for (int x = 14; x < 210; x += 16) c.Recolor(x, 0, x, 12, 1, 5);
            c.Recolor(10, 6, 213, 6, 1, 5);

            // Ventanas encendidas a los lados.
            foreach (int wx in new[] { 40, 184 })
            {
                c.Rect(wx - 16, 72, wx + 16, 104, 2);
                c.Ellipse(wx, 104f, 16f, 8f, 2);
                Glow(c, (x, y) => (x >= wx - 13 && x <= wx + 13 && y >= 75 && y <= 104) ||
                                  ((x - wx) * (x - wx) / 169f + (y - 104) * (y - 104) / 36f <= 1f && y >= 104),
                    wx - 13, 75, wx + 13, 110, Amber, GoldHot);
                c.Rect(wx - 1, 75, wx + 1, 110, 2);
                c.Rect(wx - 13, 88, wx + 13, 89, 2);
                c.Rect(wx - 19, 68, wx + 19, 72, 6);                // alféizar
                c.Ellipse(wx - 10f, 74f, 5f, 3f, 5);                // maceta con flores
                c.Dot(wx - 12, 78, PixelArt.Hex("#E86A92")); c.Dot(wx - 8, 79, PixelArt.Hex("#B9A8F0"));
            }

            // Puerta en arco.
            c.Rect(92, 0, 132, 52, 7);
            c.Ellipse(112f, 52f, 20f, 18f, 7);
            c.Rect(96, 0, 128, 52, 2);
            c.Ellipse(112f, 52f, 16f, 14f, 2);
            for (int x = 102; x < 128; x += 7) c.Recolor(x, 0, x, 64, 2, 5);
            c.Rect(96, 30, 128, 32, 8);
            c.Rect(96, 12, 128, 14, 8);
            c.Ellipse(122f, 26f, 2.5f, 2.5f, 9);                     // pomo
            Glow(c, (x, y) => (x - 112) * (x - 112) + (y - 56) * (y - 56) <= 25, 107, 51, 117, 61, Amber, GoldHot);

            // Farol junto a la puerta.
            c.Line(V(138, 72), V(146, 72), 2f, 2f, 8);
            c.Rect(142, 54, 150, 70, 8);
            Glow(c, Any, 143, 56, 149, 68, Amber, GoldHot);

            // Letrero colgado con el símbolo del oficio.
            c.Line(V(76, 104), V(88, 104), 2f, 2f, 8);
            c.Rect(76, 82, 100, 100, 6);
            Sign(c, v, 88, 91);
            return c.Render(ramps, Ink);
        }

        private static void Sign(SpritePainter c, int v, int cx, int cy)
        {
            Color ink = PixelArt.Hex("#2A1A20");
            switch (v % 5)
            {
                case 0: // pan
                    c.EllipseDot(cx, cy, 7f, 4f, PixelArt.Hex("#C8904A"));
                    for (int i = -4; i <= 4; i += 4) c.Dot(cx + i, cy + 1, ink);
                    break;
                case 1: // martillo
                    c.EllipseDot(cx, cy + 3, 6f, 2.5f, PixelArt.Hex("#8A82AA"));
                    for (int y = -6; y <= 2; y++) c.Dot(cx, cy + y, PixelArt.Hex("#6E4A3A"));
                    break;
                case 2: // vela
                    for (int y = -6; y <= 3; y++) { c.Dot(cx - 1, cy + y, White); c.Dot(cx, cy + y, White); }
                    c.Dot(cx, cy + 5, Gold); c.Dot(cx - 1, cy + 6, GoldHot);
                    break;
                case 3: // estrella (el cuarto del niño)
                    for (int i = -4; i <= 4; i++) { c.Dot(cx + i, cy, Gold); c.Dot(cx, cy + i, Gold); }
                    c.Dot(cx, cy, GoldHot);
                    break;
                default: // reloj
                    c.EllipseDot(cx, cy, 6f, 6f, PixelArt.Hex("#E6DCFF"));
                    for (int y = 0; y <= 4; y++) c.Dot(cx, cy + y, ink);
                    for (int x = 0; x <= 3; x++) c.Dot(cx + x, cy, ink);
                    break;
            }
        }

        // ---------------------------------------------------------------- Interior (480×288 = 15×9 casillas)

        /// <summary>
        /// Habitación vista de frente: suelo de tablones, paredes de madera y yeso, vigas, ventana con luna,
        /// cortinas, alfombra, telarañas y la puerta de salida a la izquierda.
        /// El suelo jugable está a 32 px (1 casilla) del borde inferior.
        /// </summary>
        public static Texture2D Interior(int v)
        {
            const int W = 480, H = 288, F = 32; // F = altura del suelo
            var c = new SpritePainter(W, H);
            var wall = new Ramp("#1C1528", "#2A2140", "#3C3058");
            var floor = new Ramp("#2A1C1E", "#423034", "#5E464A");
            var ramps = new[] { default, Stone, Wood, Plaster, wall, floor, WoodLight, Blankets[v % 5], Caps[v % 5], Iron, GoldR, Rose };

            // Pared del fondo.
            c.Rect(0, F, W - 1, H - 1, 3);
            for (int x = 0; x < W; x += 24) c.Recolor(x, F + 60, x, H - 40, 3, 4);
            c.Rect(0, F, W - 1, F + 56, 6);                              // friso de madera
            for (int x = 8; x < W; x += 32) { c.Recolor(x, F + 6, x + 22, F + 6, 6, 2); c.Recolor(x, F + 50, x + 22, F + 50, 6, 2); c.Recolor(x, F + 6, x, F + 50, 6, 2); c.Recolor(x + 22, F + 6, x + 22, F + 50, 6, 2); }
            c.Rect(0, F + 56, W - 1, F + 60, 2);

            // Vigas del techo y columnas.
            c.Rect(0, H - 40, W - 1, H - 1, 2);
            for (int x = 20; x < W; x += 92) c.Rect(x, H - 40, x + 10, H - 1, 6);
            foreach (int x in new[] { 0, 150, 330, W - 14 }) c.Rect(x, F, x + 13, H - 40, 2);

            // Suelo de tablones.
            c.Rect(0, 0, W - 1, F - 1, 5);
            for (int y = 7; y < F; y += 8) c.Recolor(0, y, W - 1, y, 5, 2);
            for (int y = 0; y < F; y += 8)
                for (int x = (y / 8) % 2 == 0 ? 12 : 52; x < W; x += 80) c.Recolor(x, y, x, y + 6, 5, 2);

            // Ventana con luna y cortinas.
            int wx = 250, wy = F + 96;
            c.Rect(wx - 34, wy - 4, wx + 34, wy + 64, 2);
            c.Ellipse(wx, wy + 64, 34f, 18f, 2);
            Glow(c, (x, y) => (x >= wx - 30 && x <= wx + 30 && y >= wy && y <= wy + 64) ||
                              ((x - wx) * (x - wx) / 900f + (y - wy - 64) * (y - wy - 64) / 196f <= 1f && y >= wy + 64),
                wx - 30, wy, wx + 30, wy + 78, PixelArt.Hex("#2A2150"), PixelArt.Hex("#8C7BC4"));
            c.EllipseDot(wx + 14, wy + 52, 9f, 9f, PixelArt.Hex("#F2ECFF"));
            c.EllipseDot(wx + 18, wy + 54, 8f, 8f, PixelArt.Hex("#5E4F8C"));
            foreach (var (sx, sy) in new[] { (wx - 20, wy + 60), (wx - 10, wy + 30), (wx + 22, wy + 20), (wx - 24, wy + 12), (wx + 4, wy + 70) }) c.Dot(sx, sy, White);
            c.Rect(wx - 2, wy, wx + 2, wy + 80, 2);
            c.Rect(wx - 30, wy + 30, wx + 30, wy + 33, 2);
            c.Rect(wx - 42, wy - 10, wx + 42, wy - 4, 6);
            c.Rect(wx - 48, wy + 92, wx + 48, wy + 95, 9);               // barra de la cortina
            c.Poly(11, V(wx - 48, wy + 92), V(wx - 30, wy + 92), V(wx - 36, wy + 40), V(wx - 26, wy - 6), V(wx - 50, wy - 6));
            c.Poly(11, V(wx + 30, wy + 92), V(wx + 48, wy + 92), V(wx + 50, wy - 6), V(wx + 26, wy - 6), V(wx + 36, wy + 40));
            for (int x = wx - 46; x < wx - 30; x += 5) c.Recolor(x, wy - 6, x, wy + 90, 11, 2);
            for (int x = wx + 32; x < wx + 48; x += 5) c.Recolor(x, wy - 6, x, wy + 90, 11, 2);

            // Puerta de salida (izquierda).
            c.Rect(22, F, 82, F + 92, 1);
            c.Ellipse(52f, F + 92f, 30f, 22f, 1);
            c.Rect(28, F, 76, F + 92, 2);
            c.Ellipse(52f, F + 92f, 24f, 17f, 2);
            for (int x = 34; x < 76; x += 8) c.Recolor(x, F, x, F + 108, 2, 9);
            c.Rect(28, F + 60, 76, F + 62, 9);
            c.Rect(28, F + 24, 76, F + 26, 9);
            c.Ellipse(68f, F + 48f, 3f, 3f, 10);

            // Alfombra.
            c.Ellipse(300f, 10f, 90f, 7f, 7, 4f);
            c.Ellipse(300f, 10f, 82f, 4.5f, 8, 4f);
            c.Ellipse(300f, 10f, 74f, 3f, 7, 4f);

            // Cuadro con un retrato.
            c.Rect(370, F + 130, 410, F + 172, 10);
            Glow(c, Any, 374, F + 134, 406, F + 168, PixelArt.Hex("#1A1428"), PixelArt.Hex("#2E2546"));
            c.EllipseDot(390, F + 150, 7f, 8f, PixelArt.Hex("#8E84A8"));
            c.EllipseDot(390, F + 140, 11f, 5f, PixelArt.Hex("#5E4F8C"));

            // Telarañas en las esquinas.
            Color web = PixelArt.WithAlpha(PixelArt.Hex("#B9A8F0"), 0.35f);
            for (int i = 0; i < 26; i++) { c.Dot(14 + i, H - 41 - i, web); c.Dot(14 + i, H - 41 - i / 3, web); c.Dot(W - 15 - i, H - 41 - i, web); }
            for (int i = 0; i < 14; i++) { c.Dot(14 + i * 2, H - 50 - i, web); c.Dot(W - 16 - i * 2, H - 48 - i, web); }

            // Marco oscuro alrededor (paredes laterales y techo en sombra).
            for (int y = 0; y < H; y++)
            for (int x = 0; x < 6; x++) { c.Dot(x, y, Ink); c.Dot(W - 1 - x, y, Ink); }
            return c.Render(ramps, Ink);
        }

        // ---------------------------------------------------------------- Muebles

        /// <summary>Mesa con vela y una silla.</summary>
        public static Texture2D Table()
        {
            var c = new SpritePainter(96, 64);
            var ramps = new[] { default, WoodLight, Wood, Cloth, Rose };
            c.Rect(8, 0, 12, 34, 2); c.Rect(60, 0, 64, 34, 2);
            c.Rect(2, 34, 70, 40, 1);
            c.Rect(14, 41, 22, 44, 4);                                  // libro
            c.Rect(46, 41, 52, 52, 3);                                  // vela
            c.Dot(49, 54, Gold); c.Dot(49, 55, GoldHot); c.Dot(48, 53, Amber);
            c.Ellipse(32f, 43f, 6f, 3f, 3);                             // taza
            c.Rect(78, 0, 81, 30, 2); c.Rect(90, 0, 93, 58, 2);         // silla
            c.Rect(76, 22, 94, 26, 1);
            c.Rect(90, 30, 94, 58, 1);
            c.Rect(84, 44, 94, 47, 2);
            return c.Render(ramps, Ink);
        }

        /// <summary>Mueble temático de cada casa: horno, yunque, altar, juguetes o reloj de pie.</summary>
        public static Texture2D[] Prop(int v) => Frames(2, f =>
        {
            var c = new SpritePainter(112, 128);
            var ramps = new[] { default, Brick, Wood, Metal, Stone, GoldR, Cloth, Rose, Iron, WoodLight, Blankets[v % 5] };
            Color fire = f == 0 ? Ember : Amber;
            switch (v % 5)
            {
                case 0: // horno de pan
                    c.Rect(8, 0, 104, 70, 1);
                    c.Ellipse(56f, 70f, 48f, 30f, 1, 70f);
                    for (int y = 8; y < 96; y += 10) c.Recolor(8, y, 104, y, 1, 4);
                    for (int y = 0; y < 96; y += 10) for (int x = (y / 10) % 2 == 0 ? 16 : 28; x < 104; x += 24) c.Recolor(x, y, x, y + 9, 1, 4);
                    c.Ellipse(56f, 36f, 22f, 18f, 8);
                    Glow(c, (x, y) => (x - 56) * (x - 56) / 324f + (y - 32) * (y - 32) / 196f <= 1f, 38, 18, 74, 46, fire, GoldHot);
                    c.Rect(34, 14, 78, 18, 2);
                    for (int i = 0; i < 3; i++) c.Ellipse(24f + i * 22f, 104f, 9f, 5f, 9);  // panes encima
                    break;
                case 1: // yunque y estante de herramientas
                    c.Rect(30, 0, 70, 10, 4);
                    c.Rect(40, 10, 60, 36, 8);
                    c.Poly(8, V(18, 36), V(84, 36), V(94, 46), V(84, 52), V(26, 52), V(18, 46));
                    c.Line(V(62, 54), V(82, 76), 4f, 4f, 2);
                    c.Rect(76, 74, 92, 84, 3);
                    c.Rect(2, 96, 110, 100, 2);                               // estante
                    c.Line(V(12, 96), V(12, 120), 3f, 3f, 3); c.Rect(8, 116, 16, 122, 3);
                    c.Line(V(30, 96), V(30, 124), 3f, 3f, 2); c.Ellipse(30f, 124f, 5f, 3f, 3);
                    c.Line(V(50, 96), V(50, 118), 3f, 3f, 3); c.Poly(3, V(44, 118), V(56, 118), V(50, 126));
                    Glow(c, (x, y) => (x - 92) * (x - 92) + (y - 6) * (y - 6) <= 40, 85, 0, 99, 12, fire, GoldHot); // brasas
                    break;
                case 2: // altar con velas
                    c.Rect(10, 0, 102, 44, 4);
                    c.Rect(4, 44, 108, 50, 9);
                    c.Rect(30, 10, 82, 34, 6);
                    c.Rect(54, 12, 58, 32, 5); c.Rect(46, 24, 66, 27, 5);
                    for (int i = 0; i < 5; i++)
                    {
                        int x = 14 + i * 21, h = 14 + (i * 7) % 12;
                        c.Rect(x, 50, x + 5, 50 + h, 6);
                        c.Dot(x + 2, 52 + h, Gold); c.Dot(x + 3, 53 + h, f == i % 2 ? GoldHot : Amber);
                    }
                    break;
                case 3: // juguetes del niño
                    c.Rect(6, 0, 60, 34, 2);                                    // baúl
                    c.Rect(4, 34, 62, 42, 9);
                    c.Rect(30, 18, 36, 26, 5);
                    c.Curve(V(66, 4), V(88, -6), V(108, 4), 3f, 3f, 2);          // caballito
                    c.Line(V(76, 2), V(80, 22), 3f, 3f, 2); c.Line(V(98, 2), V(94, 22), 3f, 3f, 2);
                    c.Ellipse(87f, 28f, 14f, 8f, 7);
                    c.Ellipse(100f, 40f, 6f, 8f, 7);
                    c.Rect(96, 46, 104, 50, 10);
                    c.Ellipse(24f, 50f, 9f, 9f, 6);                             // pelota
                    c.Recolor(15, 48, 33, 52, 6, 7);
                    c.Rect(40, 44, 54, 60, 10);                                 // dibujo pegado
                    break;
                default: // reloj de pie
                    c.Rect(36, 0, 76, 8, 2);
                    c.Rect(40, 8, 72, 100, 2);
                    c.Ellipse(56f, 104f, 20f, 18f, 2);
                    c.Ellipse(56f, 104f, 14f, 14f, 6);
                    c.Rect(48, 22, 64, 76, 8);
                    float sw = f == 0 ? -5f : 5f;
                    c.Line(V(56, 74), V(56 + sw, 40), 1.5f, 1.5f, 5);
                    c.Ellipse(56f + sw, 36f, 5f, 5f, 5);
                    c.Rect(76, 0, 108, 80, 2);                                 // librero
                    for (int y = 18; y < 80; y += 20)
                    {
                        c.Rect(76, y, 108, y + 2, 9);
                        for (int x = 80; x < 106; x += 5) c.Rect(x, y + 3, x + 3, y + 12 + (x % 3) * 2, x % 2 == 0 ? 10 : 7);
                    }
                    break;
            }
            var t = c.Render(ramps, Ink);
            if (v % 5 == 4)
            {
                // Agujas del reloj.
                for (int i = 0; i < 8; i++) t.SetPixel(56, 104 + i, Ink);
                for (int i = 0; i < 6; i++) t.SetPixel(56 + i, 104, Ink);
            }
            return t;
        });

        /// <summary>Flecha dorada que indica que se puede entrar por una puerta.</summary>
        public static Texture2D DoorArrow()
        {
            var c = new SpritePainter(18, 18);
            c.Poly(1, V(2, 8), V(16, 8), V(9, 16));
            c.Rect(6, 2, 11, 8, 1);
            return c.Render(new[] { default, GoldR }, Ink);
        }
    }
}
#endif
