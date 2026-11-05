#if UNITY_EDITOR
using System;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Dibujos del pueblo: casas (vista en corte, para ver la cama dentro), aldeanos despiertos,
    /// el gato, objetos con secretos, faroles, Lágrimas de Luna y la máscara de la pantalla de muerte.
    /// Misma paleta y estilo que CreatureArt (contorno oscuro, luz arriba-izquierda).
    /// </summary>
    public static class VillageArt
    {
        public const float PPU = 24f;

        private static readonly Color Ink = PixelArt.Hex("#0B0814");
        private static readonly Color Gold = PixelArt.Hex("#FFE38A");
        private static readonly Color GoldHot = PixelArt.Hex("#FFF6CC");
        private static readonly Color Amber = PixelArt.Hex("#E8A33C");
        private static readonly Color Rose = PixelArt.Hex("#E86A92");
        private static readonly Color White = PixelArt.Hex("#F7F2FF");
        private static readonly Color Moss = PixelArt.Hex("#4E6B45");

        private static Vector2 V(float x, float y) => new Vector2(x, y);
        private static int R(float v) => Mathf.RoundToInt(v);

        private static readonly Ramp WoodDark = new Ramp("#1E1520", "#36242E", "#553A44");
        private static readonly Ramp WoodLight = new Ramp("#3E2A30", "#6A4A4E", "#94706A");
        private static readonly Ramp Plaster = new Ramp("#1C1628", "#2C2340", "#40345A");
        private static readonly Ramp PlasterDark = new Ramp("#151020", "#211A31", "#2E2546");
        private static readonly Ramp Stone = new Ramp("#1F1A2B", "#352E48", "#524870");
        private static readonly Ramp StoneLight = new Ramp("#3A3352", "#5E567E", "#8A82AA");
        private static readonly Ramp Iron = new Ramp("#0F0C16", "#231E30", "#3C344E");
        private static readonly Ramp GoldRamp = new Ramp("#6B5320", "#B8924A", "#FFE38A");
        private static readonly Ramp Skin = new Ramp("#8C6E6A", "#C9A698", "#EBCDBE");
        private static readonly Ramp Pale = new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF");

        private static readonly Ramp[] Roofs =
        {
            new Ramp("#2A1428", "#4A2444", "#6E3A62"), // ciruela
            new Ramp("#151A28", "#253048", "#3A4A6A"), // pizarra
            new Ramp("#18221C", "#2A3A30", "#405A48"), // musgo
            new Ramp("#14262A", "#22404A", "#36606C"), // verde azulado
            new Ramp("#2A1A14", "#4A2E24", "#6E4636"), // óxido
        };

        /// <summary>Rellena píxel a píxel con un degradado vertical (cristales iluminados).</summary>
        private static void Glow(SpritePainter c, Func<int, int, bool> inside, int x0, int y0, int x1, int y1, Color bottom, Color top)
        {
            for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                if (inside(x, y)) c.Dot(x, y, Color.Lerp(bottom, top, (y - y0) / (float)Mathf.Max(1, y1 - y0)));
        }

        // ---------------------------------------------------------------- Casa

        /// <summary>Casa en corte (sin pared delantera): 168×144 px = 7×6 casillas. El aldeano duerme en el centro.</summary>
        public static Texture2D House(int variant)
        {
            var c = new SpritePainter(168, 144);
            var ramps = new[] { default, Stone, WoodDark, Plaster, Roofs[variant % Roofs.Length], PlasterDark, WoodLight, StoneLight, Iron };

            // Chimenea (detrás del tejado).
            int chX = variant % 2 == 0 ? 118 : 40;
            c.Rect(chX, 108, chX + 12, 138, 1);
            c.Rect(chX - 1, 137, chX + 13, 140, 7);

            // Tejado empinado y frontón con entramado.
            c.Poly(4, V(0, 94), V(168, 94), V(84, 143));
            for (int y = 98; y < 140; y += 5) c.Recolor(0, y, 167, y, 4, 5); // tejas
            c.Poly(3, V(18, 100), V(150, 100), V(84, 135));
            c.Line(V(84, 100), V(84, 132), 3f, 3f, 2);
            c.Line(V(34, 101), V(84, 128), 2.5f, 2.5f, 2);
            c.Line(V(134, 101), V(84, 128), 2.5f, 2.5f, 2);
            c.Ellipse(84f, 114f, 8f, 8f, 2);
            Glow(c, (x, y) => (x - 84) * (x - 84) + (y - 114) * (y - 114) <= 30, 78, 108, 90, 120, PixelArt.Hex("#6B5A9E"), PixelArt.Hex("#D9CCFF"));

            // Pared interior de tablones.
            c.Rect(14, 0, 153, 93, 3);
            for (int x = 20; x < 150; x += 10) c.Recolor(x, 0, x, 93, 3, 5);
            c.Rect(14, 0, 153, 3, 6); // zócalo

            // Viga y vigas cruzadas.
            c.Rect(14, 62, 153, 66, 2);
            c.Line(V(16, 66), V(40, 90), 3f, 3f, 2);
            c.Line(V(151, 66), V(127, 90), 3f, 3f, 2);

            // Ventana con arco y luz de luna.
            c.Rect(66, 26, 102, 54, 2);
            c.Ellipse(84f, 54f, 18f, 10f, 2);
            Func<int, int, bool> glass = (x, y) => (x >= 69 && x <= 99 && y >= 29 && y <= 54) ||
                                                   ((x - 84) * (x - 84) / 225f + (y - 54) * (y - 54) / 56f <= 1f && y >= 54);
            Glow(c, glass, 69, 29, 99, 61, PixelArt.Hex("#2E2550"), PixelArt.Hex("#9D8CD0"));
            c.EllipseDot(91f, 50f, 4f, 4f, PixelArt.Hex("#F2ECFF")); // luna
            c.EllipseDot(93f, 51f, 3.5f, 3.5f, PixelArt.Hex("#5E4F8C"));
            foreach (var (sx, sy) in new[] { (74, 52), (79, 58), (96, 40), (72, 36) }) c.Dot(sx, sy, White);
            c.Rect(83, 29, 85, 62, 2);
            c.Rect(69, 43, 99, 44, 2);
            c.Rect(63, 23, 105, 26, 6); // alféizar

            // Repisa con libros y vela (izquierda o derecha según la variante).
            int sx0 = variant % 2 == 0 ? 22 : 112;
            c.Rect(sx0, 46, sx0 + 32, 48, 6);
            c.Rect(sx0 + 2, 49, sx0 + 4, 58, 8);
            c.Rect(sx0 + 5, 49, sx0 + 7, 56, 4);
            c.Rect(sx0 + 8, 49, sx0 + 10, 59, 6);
            c.Line(V(sx0 + 12, 49), V(sx0 + 15, 57), 2.5f, 2.5f, 8);
            c.Rect(sx0 + 25, 49, sx0 + 27, 54, 7); // vela
            c.Dot(sx0 + 26, 55, Gold);
            c.Dot(sx0 + 26, 56, GoldHot);

            // Cuadro (al otro lado): una polilla dibujada.
            int fx = variant % 2 == 0 ? 116 : 26;
            c.Rect(fx, 36, fx + 22, 56, 6);
            Glow(c, (x, y) => true, fx + 3, 39, fx + 19, 53, PixelArt.Hex("#1A1428"), PixelArt.Hex("#2A2140"));
            c.EllipseDot(fx + 8f, 47f, 3f, 2.5f, PixelArt.Hex("#8C7BC4"));
            c.EllipseDot(fx + 14f, 47f, 3f, 2.5f, PixelArt.Hex("#8C7BC4"));
            c.Rect(fx + 11, 44, fx + 11, 50, 7);

            // Postes, viga superior y base de piedra.
            c.Rect(8, 0, 15, 97, 2);
            c.Rect(152, 0, 159, 97, 2);
            c.Rect(2, 92, 165, 99, 2);
            c.Rect(4, 0, 18, 7, 1);
            c.Rect(149, 0, 163, 7, 1);

            // Farolito colgado del poste derecho.
            c.Line(V(160, 88), V(165, 84), 1.5f, 1.5f, 8);
            c.Rect(162, 72, 166, 83, 8);
            Glow(c, (x, y) => true, 163, 74, 165, 81, Amber, GoldHot);

            return c.Render(ramps, Ink);
        }

        // ---------------------------------------------------------------- Camas con aldeano

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

        private static readonly Ramp[] Skins =
        {
            new Ramp("#8C6E6A", "#C9A698", "#EBCDBE"),
            new Ramp("#7A5A50", "#B08878", "#D4AE9C"),
            new Ramp("#8C6E6A", "#C9A698", "#EBCDBE"),
            new Ramp("#5E4038", "#8A6050", "#B08470"),
            new Ramp("#8E7A78", "#CDB8B0", "#EEDDD6"),
        };

        private static Ramp[] BedRamps(int v) => new[]
        {
            default, WoodLight, Pale, Skins[v % 5], Caps[v % 5], Blankets[v % 5], Caps[(v + 2) % 5], WoodDark,
        };

        private static void Bed(SpritePainter c)
        {
            c.Rect(5, 0, 7, 5, 7);
            c.Rect(56, 0, 58, 5, 7);
            c.Rect(4, 5, 59, 8, 1);       // larguero
            c.Rect(2, 5, 8, 26, 1);       // cabecero
            c.Ellipse(5f, 27f, 3.5f, 2f, 1);
            c.Rect(3, 18, 7, 19, 7);
            c.Rect(55, 5, 61, 16, 1);     // pie de cama
            c.Ellipse(58f, 17f, 3.5f, 2f, 1);
            c.Rect(8, 9, 55, 11, 2);      // colchón y sábana
        }

        /// <summary>Aldeano dormido en su cama, respirando (4 cuadros). 64×34 px.</summary>
        public static Texture2D[] Sleeping(int v)
        {
            float[] breath = { 0f, 0.5f, 1f, 0.5f };
            var frames = new Texture2D[4];
            for (int f = 0; f < 4; f++)
            {
                var c = new SpritePainter(64, 34);
                float lift = breath[f];
                Bed(c);
                c.Ellipse(14f, 14f, 6.5f, 3f, 2); // almohada

                c.Ellipse(36f, 12f + lift * 0.5f, 19f, 4.5f + lift, 5, 10f);
                c.Ellipse(50f, 13f, 4f, 4f, 5, 10f);
                for (int x = 25; x <= 49; x += 8) c.Recolor(x, 10, x + 1, 20, 5, 6);
                c.Recolor(17, 10, 55, 11, 5, 2); // borde de sábana doblada

                c.Ellipse(17f, 17f, 6f, 5f, 3);
                c.Poly(4, V(11, 19), V(23.5f, 19), V(22, 23), V(16, 24.5f), V(9, 24), V(6.5f, 21));
                c.Ellipse(7f, 21f, 2f, 2f, 2);
                c.Ellipse(26f, 17f + lift, 2f, 1.6f, 3); // mano

                foreach (int ex in new[] { 14, 19 })
                {
                    c.Dot(ex - 1, 17, Ink);
                    c.Dot(ex, 16, Ink);
                    c.Dot(ex + 1, 17, Ink);
                }
                c.Dot(13, 15, PixelArt.WithAlpha(Rose, 0.8f));
                c.Dot(21, 15, PixelArt.WithAlpha(Rose, 0.8f));
                c.Dot(17, 14, Ink);
                if (lift >= 1f) c.Dot(17, 13, Ink);
                frames[f] = c.Render(BedRamps(v), Ink);
            }
            return frames;
        }

        /// <summary>Aldeano despierto, sentado en la cama (2 cuadros). 64×48 px.</summary>
        public static Texture2D[] AwakeInBed(int v)
        {
            var frames = new Texture2D[2];
            for (int f = 0; f < 2; f++)
            {
                var c = new SpritePainter(64, 48);
                Bed(c);
                c.Ellipse(10f, 15f, 5f, 4f, 2);
                c.Ellipse(38f, 12f, 17f, 3.5f, 5, 10f);
                for (int x = 27; x <= 51; x += 8) c.Recolor(x, 10, x + 1, 17, 5, 6);

                c.Poly(6, V(12, 11), V(25, 11), V(24, 25), V(13, 25));
                c.Line(V(14, 23), V(13, 15), 3f, 2.6f, 6);
                c.Line(V(23, 23), V(27, 15), 3f, 2.6f, 6);
                c.Ellipse(13f, 14f, 1.8f, 1.6f, 3);
                c.Ellipse(27.5f, 14f, 1.8f, 1.6f, 3);

                float hy = 31f + f * 0.5f;
                c.Ellipse(18.5f, hy, 6f, 6f, 3);
                c.Poly(4, V(12, hy + 2.5f), V(25, hy + 2.5f), V(23.5f, hy + 7), V(17, hy + 9), V(10, hy + 7), V(6.5f, hy + 3));
                c.Ellipse(6.5f, hy + 2.5f, 2f, 2f, 2);
                int y = R(hy);
                foreach (int ex in new[] { 16, 21 }) { c.Dot(ex, y - 1, Ink); c.Dot(ex, y - 2, Ink); }
                c.Dot(17, y - 4, Ink); c.Dot(18, y - 5, Ink); c.Dot(19, y - 5, Ink); c.Dot(20, y - 4, Ink);
                c.Dot(14, y - 3, PixelArt.WithAlpha(Rose, 0.8f));
                c.Dot(23, y - 3, PixelArt.WithAlpha(Rose, 0.8f));
                frames[f] = c.Render(BedRamps(v), Ink);
            }
            return frames;
        }

        // ---------------------------------------------------------------- Aldeanos despiertos (40×56)

        /// <summary>Vesna, la farolera: abrigo largo, capucha y una pértiga con farol que se balancea.</summary>
        public static Texture2D[] Lamplighter() => Frames(4, f =>
        {
            var c = new SpritePainter(40, 56);
            var ramps = new[] { default, new Ramp("#122226", "#1F3A40", "#33585E"), Skin, WoodDark, Iron, new Ramp("#5A1C30", "#9C3355", "#D35A80") };
            float bob = f % 2 == 0 ? 0f : 0.6f;
            float sway = new[] { 0f, 1f, 0f, -1f }[f];

            c.Line(V(10, 1), V(9 + sway * 0.3f, 46), 2.2f, 2f, 3); // pértiga
            c.Rect(16, 0, 19, 3, 4);
            c.Rect(23, 0, 26, 3, 4);
            c.Poly(1, V(13, 2), V(29, 2), V(26, 31 + bob), V(16, 31 + bob));       // abrigo
            c.Recolor(20, 2, 21, 24, 1, 4);                                          // abertura
            c.Ellipse(21f, 38f + bob, 7.5f, 7.5f, 1);                                // capucha
            c.Rect(15, 30, 27, 32, 5);                                               // bufanda
            c.Line(V(24, 31), V(31, 22), 2f, 1.5f, 5);
            c.Ellipse(20f, 36.5f + bob, 4.5f, 4.5f, 2);                              // cara
            c.Line(V(16, 28 + bob), V(10, 30), 3.2f, 2.6f, 1);                       // brazo
            c.Ellipse(10f, 30f, 1.8f, 1.8f, 2);
            c.Dot(18, R(37 + bob), Ink); c.Dot(22, R(37 + bob), Ink);
            c.Dot(19, R(34 + bob), PixelArt.Hex("#8C5A5A")); c.Dot(20, R(34 + bob), PixelArt.Hex("#8C5A5A"));

            // Farol que se balancea.
            float lx = 7f + sway;
            c.Line(V(9 + sway * 0.3f, 46), V(lx + 1, 44), 1.2f, 1.2f, 4);
            c.Rect(R(lx) - 3, 35, R(lx) + 3, 43, 4);
            c.Poly(4, V(lx - 4, 43), V(lx + 4, 43), V(lx, 47));
            Glow(c, (x, y) => true, R(lx) - 2, 36, R(lx) + 2, 42, f % 2 == 0 ? Amber : Gold, GoldHot);
            return c.Render(ramps, Ink);
        });

        /// <summary>Grim, el mercader: encorvado bajo una mochila enorme, sombrero ancho y ojos que brillan en la sombra.</summary>
        public static Texture2D[] Merchant() => Frames(4, f =>
        {
            var c = new SpritePainter(44, 56);
            var ramps = new[] { default, new Ramp("#2A1C1C", "#47302C", "#6E4C42"), new Ramp("#241A30", "#3C2E52", "#5A4878"),
                                WoodDark, new Ramp("#1A1426", "#2A2240", "#40345E"), Pale, GoldRamp, new Ramp("#3A1E2A", "#6A3048", "#9A4A6A") };
            float bob = f % 2 == 0 ? 0f : 0.7f;

            c.Line(V(7, 0), V(8, 30), 2f, 2f, 3);                                    // bastón
            c.Ellipse(30f, 26f + bob, 11f, 15f, 1);                                  // mochila
            c.Ellipse(32f, 42f + bob, 6f, 4f, 7);                                    // manta enrollada
            c.Ellipse(37f, 30f + bob, 3f, 4f, 6);                                    // olla dorada
            c.Rect(24, 10, 26, 40, 3);                                               // correa
            c.Line(V(38, 18), V(41, 12), 1f, 1f, 3);
            c.Rect(39, 6, 43, 12, 3);                                                // farolito colgado
            c.Dot(41, 9, f < 2 ? Gold : Amber);
            c.Poly(2, V(9, 1), V(25, 1), V(25, 26 + bob), V(12, 30 + bob));          // capa encorvada
            c.Rect(12, 0, 15, 2, 3);
            c.Rect(19, 0, 22, 2, 3);
            c.Line(V(12, 24 + bob), V(8, 28), 3f, 2.6f, 2);                          // brazo al bastón
            c.Ellipse(16f, 33f + bob, 5.5f, 4.5f, 2);                                // cabeza en sombra
            c.Poly(5, V(12, 30 + bob), V(18, 30 + bob), V(15, 24 + bob));            // barba
            c.Ellipse(16f, 38f + bob, 12f, 2.5f, 4);                                 // ala del sombrero
            c.Rect(11, 39, 21, 45, 4);
            c.Rect(11, 40, 21, 41, 7);                                               // cinta
            c.EllipseDot(15f, 34f + bob, 4f, 2.5f, PixelArt.Hex("#120D1F"));
            bool blink = f == 3;
            c.Dot(13, R(34 + bob), blink ? PixelArt.Hex("#120D1F") : Gold);
            c.Dot(17, R(34 + bob), blink ? PixelArt.Hex("#120D1F") : Gold);
            return c.Render(ramps, Ink);
        });

        /// <summary>Ash, el sepulturero: alto y delgado, chistera, cara pálida y pala.</summary>
        public static Texture2D[] Gravedigger() => Frames(4, f =>
        {
            var c = new SpritePainter(40, 60);
            var ramps = new[] { default, new Ramp("#100C18", "#1C1628", "#30284A"), Pale, WoodDark, new Ramp("#3C3A4A", "#6A687E", "#A6A4BA"),
                                new Ramp("#1A0F1E", "#2A1830", "#40284A") };
            float bob = f % 2 == 0 ? 0f : 0.5f;

            c.Line(V(31, 4), V(31, 44), 2f, 2f, 3);                                  // mango de la pala
            c.Rect(29, 44, 33, 46, 3);
            c.Poly(4, V(27, 0), V(35, 0), V(34, 8), V(28, 8));                       // hoja
            c.Poly(1, V(13, 2), V(25, 2), V(24, 36 + bob), V(14, 36 + bob));         // abrigo
            c.Recolor(19, 2, 19, 30, 1, 5);
            c.Rect(14, 0, 17, 3, 5);
            c.Rect(21, 0, 24, 3, 5);
            c.Line(V(23, 33 + bob), V(30, 26), 2.6f, 2.2f, 1);                       // brazo a la pala
            c.Ellipse(30.5f, 26f, 1.6f, 1.6f, 2);
            c.Ellipse(19f, 42f + bob, 4.5f, 5.5f, 2);                                // cara alargada
            c.Rect(12, 47, 26, 48, 1);                                               // ala
            c.Rect(14, 49, 24, 58, 1);                                               // chistera
            c.Rect(14, 50, 24, 51, 5);
            c.EllipseDot(17f, 43f + bob, 1.4f, 1.6f, PixelArt.Hex("#3A2D57"));       // ojeras
            c.EllipseDot(21f, 43f + bob, 1.4f, 1.6f, PixelArt.Hex("#3A2D57"));
            if (f != 2) { c.Dot(17, R(43 + bob), Ink); c.Dot(21, R(43 + bob), Ink); }
            c.Dot(18, R(39 + bob), Ink); c.Dot(19, R(39 + bob), Ink); c.Dot(20, R(39 + bob), Ink);
            c.Dot(31, 51, Moss); c.Dot(16, 1, Moss); // un poco de tierra y musgo
            return c.Render(ramps, Ink);
        });

        /// <summary>Lune, la niña: lazo, vestido y un frasco con una polilla de luz.</summary>
        public static Texture2D[] Girl() => Frames(4, f =>
        {
            var c = new SpritePainter(32, 40);
            var ramps = new[] { default, new Ramp("#33284F", "#5E4F8C", "#8E7FC2"), Skin, new Ramp("#120D1F", "#241A36", "#3A2D57"),
                                new Ramp("#7A2845", "#C2416B", "#F08AAE"), Pale };
            float bob = f % 2 == 0 ? 0f : 0.5f;

            c.Rect(11, 0, 13, 4, 3);
            c.Rect(17, 0, 19, 4, 3);
            c.Poly(1, V(8, 4), V(22, 4), V(18, 18 + bob), V(12, 18 + bob));          // vestido
            c.Recolor(8, 4, 22, 5, 1, 5);                                            // volante
            c.Ellipse(15f, 25f + bob, 7f, 6.5f, 3);                                  // pelo
            c.Ellipse(15f, 23.5f + bob, 5f, 5f, 2);                                  // cara
            c.Rect(9, 17, 10, 24, 3);                                                // trenzas
            c.Rect(20, 17, 21, 24, 3);
            c.Ellipse(19f, 31f + bob, 3f, 2f, 4);                                    // lazo
            c.Ellipse(24f, 31f + bob, 3f, 2f, 4);
            c.Line(V(19, 15 + bob), V(24, 11), 2.4f, 2f, 1);                         // brazo
            c.Ellipse(25.5f, 10f, 3.5f, 4.5f, 5);                                    // frasco
            Glow(c, (x, y) => (x - 25.5f) * (x - 25.5f) / 7f + (y - 10f) * (y - 10f) / 13f <= 1f, 23, 6, 28, 13,
                PixelArt.Hex("#3A2D57"), PixelArt.Hex("#6B5A9E"));
            int mx = 25 + (f == 1 ? 1 : f == 3 ? -1 : 0), my = 10 + (f == 2 ? 1 : 0);
            c.Dot(mx, my, GoldHot); c.Dot(mx - 1, my, Gold); c.Dot(mx + 1, my, Gold);
            c.Rect(23, 14, 28, 15, 3);                                               // tapa
            c.Dot(13, R(24 + bob), Ink); c.Dot(17, R(24 + bob), Ink);
            c.Dot(13, R(25 + bob), Ink); c.Dot(17, R(25 + bob), Ink);
            c.Dot(15, R(21 + bob), Ink);
            c.Dot(12, R(22 + bob), PixelArt.WithAlpha(Rose, 0.8f)); c.Dot(18, R(22 + bob), PixelArt.WithAlpha(Rose, 0.8f));
            return c.Render(ramps, Ink);
        });

        /// <summary>Gato negro sentado: cola que se mueve y ojos dorados que parpadean.</summary>
        public static Texture2D[] Cat() => Frames(4, f =>
        {
            var c = new SpritePainter(30, 22);
            var ramps = new[] { default, new Ramp("#0E0A16", "#1A1428", "#2E2546") };
            float tail = new[] { 0f, 2f, 3f, 1f }[f];
            c.Curve(V(21, 3), V(28, 4 + tail), V(25, 12 + tail), 2.8f, 1.8f, 1);    // cola
            c.Ellipse(16f, 7f, 7f, 6.5f, 1);                                         // cuerpo
            c.Ellipse(9f, 13f, 5f, 4.5f, 1);                                         // cabeza
            c.Poly(1, V(5, 15), V(8, 16), V(5.5f, 20));                              // orejas
            c.Poly(1, V(10, 16), V(13, 15), V(12.5f, 20));
            c.Rect(10, 0, 12, 3, 1);
            c.Rect(15, 0, 17, 2, 1);
            if (f == 2) { c.Dot(7, 13, Gold); c.Dot(8, 13, Gold); c.Dot(10, 13, Gold); c.Dot(11, 13, Gold); }
            else { c.Dot(7, 13, Gold); c.Dot(7, 14, GoldHot); c.Dot(11, 13, Gold); c.Dot(11, 14, GoldHot); }
            c.Dot(9, 11, PixelArt.Hex("#C2416B"));
            return c.Render(ramps, Ink);
        });

        // ---------------------------------------------------------------- Objetos

        /// <summary>Estatua de un caballero con máscara, con musgo.</summary>
        public static Texture2D Statue()
        {
            var c = new SpritePainter(40, 66);
            var ramps = new[] { default, Stone, StoneLight };
            c.Rect(5, 0, 35, 9, 1);
            c.Rect(8, 9, 32, 11, 1);
            c.Poly(2, V(10, 11), V(30, 11), V(27, 44), V(13, 44));                   // capa
            c.Ellipse(20f, 50f, 8f, 8f, 2);                                          // capucha
            c.Ellipse(20f, 48.5f, 5f, 6f, 2);
            c.EllipseDot(18f, 49f, 1.3f, 2f, Ink);
            c.EllipseDot(22.5f, 49f, 1.3f, 2f, Ink);
            c.Line(V(20, 12), V(20, 32), 2.5f, 2f, 1);                               // espada
            c.Rect(15, 31, 25, 33, 1);
            c.Rect(19, 33, 21, 37, 1);
            foreach (var (x, y) in new[] { (6, 9), (7, 9), (12, 11), (28, 20), (14, 40), (25, 55), (16, 57), (33, 9) }) c.Dot(x, y, Moss);
            return c.Render(ramps, Ink);
        }

        /// <summary>Pozo de piedra con tejadillo y cubo.</summary>
        public static Texture2D Well()
        {
            var c = new SpritePainter(52, 48);
            var ramps = new[] { default, Stone, WoodDark, Roofs[0], WoodLight };
            c.Rect(8, 16, 10, 36, 2);
            c.Rect(42, 16, 44, 36, 2);
            c.Poly(3, V(2, 34), V(50, 34), V(26, 47));
            c.Rect(10, 31, 42, 32, 2);
            c.Line(V(26, 31), V(26, 22), 1f, 1f, 4);
            c.Rect(23, 17, 29, 22, 4);
            c.Rect(6, 0, 46, 15, 1);
            for (int y = 4; y < 15; y += 4) c.Recolor(6, y, 46, y, 1, 0 + 2);
            c.EllipseDot(26f, 15f, 18f, 1.6f, Ink);
            c.Dot(9, 3, Moss); c.Dot(40, 7, Moss); c.Dot(41, 7, Moss);
            return c.Render(ramps, Ink);
        }

        /// <summary>Lápidas del cementerio. Tipo 0-2 normales, 3 = la lápida rara.</summary>
        public static Texture2D Grave(int type)
        {
            var c = new SpritePainter(26, 32);
            var ramps = new[] { default, Stone, StoneLight };
            switch (type)
            {
                case 0:
                    c.Rect(5, 0, 20, 18, 1);
                    c.Ellipse(12.5f, 18f, 7.5f, 6f, 1);
                    c.Rect(12, 8, 13, 18, 2); c.Rect(9, 14, 16, 15, 2);
                    break;
                case 1:
                    c.Rect(11, 0, 14, 26, 1);
                    c.Rect(5, 16, 20, 19, 1);
                    break;
                case 2:
                    c.Poly(1, V(4, 0), V(21, 0), V(23, 16), V(8, 21));                // inclinada y rota
                    c.Dot(11, 12, Ink); c.Dot(12, 11, Ink); c.Dot(13, 10, Ink);
                    break;
                default:
                    c.Rect(4, 0, 21, 20, 2);
                    c.Ellipse(12.5f, 20f, 8.5f, 7f, 2);
                    c.EllipseDot(10f, 18f, 1f, 1.5f, Ink); c.EllipseDot(15f, 18f, 1f, 1.5f, Ink); // una carita
                    c.Dot(9, 13, Ink); c.Dot(10, 12, Ink); c.Dot(11, 12, Ink); c.Dot(12, 12, Ink); c.Dot(13, 12, Ink); c.Dot(14, 12, Ink); c.Dot(15, 13, Ink);
                    c.Dot(19, 26, Rose); c.Dot(20, 27, Rose); c.Dot(18, 27, Rose); c.Dot(19, 28, Rose); c.Dot(19, 27, Gold); // flor
                    break;
            }
            c.Dot(5, 1, Moss); c.Dot(6, 1, Moss); c.Dot(19, 2, Moss);
            return c.Render(ramps, Ink);
        }

        /// <summary>Espejo de cuerpo entero con marco dorado.</summary>
        public static Texture2D Mirror()
        {
            var c = new SpritePainter(30, 48);
            var ramps = new[] { default, GoldRamp, WoodDark };
            c.Rect(6, 0, 8, 8, 2); c.Rect(22, 0, 24, 8, 2);
            c.Ellipse(15f, 27f, 12f, 19f, 1);
            Glow(c, (x, y) => (x - 15f) * (x - 15f) / 90f + (y - 27f) * (y - 27f) / 272f <= 1f, 5, 10, 25, 44,
                PixelArt.Hex("#2A2140"), PixelArt.Hex("#8C7BC4"));
            for (int i = 0; i < 5; i++) c.Dot(10 + i, 34 + i, White);
            c.Dot(20, 20, White);
            c.Dot(15, 46, Gold); c.Dot(15, 47, GoldHot);
            return c.Render(ramps, Ink);
        }

        /// <summary>Atril con un diario abierto y una pluma.</summary>
        public static Texture2D Diary()
        {
            var c = new SpritePainter(26, 30);
            var ramps = new[] { default, WoodDark, Pale, new Ramp("#4A1C30", "#7E3352", "#AD5579") };
            c.Rect(11, 0, 14, 16, 1);
            c.Rect(6, 0, 19, 2, 1);
            c.Poly(3, V(2, 16), V(24, 16), V(22, 22), V(4, 22));
            c.Poly(2, V(4, 18), V(12, 18), V(12, 24), V(5, 23));
            c.Poly(2, V(13, 18), V(21, 18), V(20, 23), V(13, 24));
            for (int y = 19; y <= 22; y += 2) { c.Dot(6, y, Ink); c.Dot(7, y, Ink); c.Dot(9, y, Ink); c.Dot(15, y, Ink); c.Dot(17, y, Ink); c.Dot(18, y, Ink); }
            c.Line(V(19, 22), V(24, 29), 1.2f, 1f, 2);
            return c.Render(ramps, Ink);
        }

        /// <summary>Farola de hierro con farol (2 cuadros de llama).</summary>
        public static Texture2D[] LampPost() => Frames(2, f =>
        {
            var c = new SpritePainter(20, 74);
            var ramps = new[] { default, Iron };
            c.Rect(5, 0, 14, 4, 1);
            c.Rect(8, 4, 11, 60, 1);
            c.Rect(7, 20, 12, 22, 1);
            c.Curve(V(10, 58), V(10, 66), V(15, 64), 2f, 1.5f, 1);
            c.Rect(5, 59, 14, 61, 1);
            c.Rect(5, 61, 6, 70, 1); c.Rect(13, 61, 14, 70, 1);
            c.Poly(1, V(4, 70), V(15, 70), V(9.5f, 74));
            Glow(c, (x, y) => true, 7, 62, 12, 69, f == 0 ? Amber : Gold, GoldHot);
            return c.Render(ramps, Ink);
        });

        /// <summary>Lágrima de Luna: gota de luz con un destello que gira (4 cuadros). 16×20 px.</summary>
        public static Texture2D[] MoonTear() => Frames(4, f =>
        {
            var c = new SpritePainter(16, 20);
            var ramps = new[] { default, new Ramp("#5E6FB5", "#A6C4F2", "#E6F4FF") };
            c.Ellipse(8f, 7f, 5f, 5.5f, 1);
            c.Poly(1, V(3.3f, 9), V(12.7f, 9), V(8, 19));
            c.Dot(6, 9, White); c.Dot(6, 10, White); c.Dot(7, 11, White);
            var spark = new[] { (11, 4), (12, 9), (4, 4), (8, 2) }[f];
            c.Dot(spark.Item1, spark.Item2, White);
            c.Dot(8, 6, GoldHot);
            return c.Render(ramps, PixelArt.Hex("#1A1440"));
        });

        // ---------------------------------------------------------------- Pantalla de muerte

        /// <summary>Máscara de Kael partida: devuelve las dos mitades (izquierda, derecha). 64×80 px cada una.</summary>
        public static (Texture2D left, Texture2D right) BrokenMask()
        {
            var c = new SpritePainter(64, 80);
            c.Ellipse(32f, 42f, 24f, 32f, 1);
            c.Ellipse(32f, 16f, 15f, 10f, 1);
            c.EllipseDot(22f, 42f, 6f, 9f, Ink);
            c.EllipseDot(42f, 42f, 6f, 9f, Ink);
            c.EllipseDot(22f, 40f, 2.5f, 3f, PixelArt.Hex("#B04ACF"));
            c.EllipseDot(42f, 40f, 2.5f, 3f, PixelArt.Hex("#B04ACF"));
            c.Dot(21, 42, White); c.Dot(41, 42, White);
            for (int y = 31; y > 20; y--) c.Dot(22 + (y % 3 == 0 ? 1 : 0), y, PixelArt.Hex("#8E7FC2")); // lágrima
            var full = c.Render(new[] { default, new Ramp("#8E84A8", "#D9D2EE", "#FFFFFF") }, Ink);

            // Grieta en zigzag por el centro.
            int CrackX(int y) => 32 + new[] { 0, 2, 3, 1, -1, -3, -2, 0, 2, 1 }[(y / 8) % 10];
            var left = PixelArt.Solid(64, 80, Color.clear);
            var right = PixelArt.Solid(64, 80, Color.clear);
            for (int y = 0; y < 80; y++)
            for (int x = 0; x < 64; x++)
            {
                var col = full.GetPixel(x, y);
                if (col.a <= 0f) continue;
                int cx = CrackX(y);
                bool edge = x == cx - 1 || x == cx;
                (x < cx ? left : right).SetPixel(x, y, edge ? Ink : col);
            }
            return (left, right);
        }

        /// <summary>Viñeta: transparente en el centro, oscura en los bordes.</summary>
        public static Texture2D Vignette()
        {
            const int s = 64;
            var t = PixelArt.Solid(s, s, Color.clear);
            for (int x = 0; x < s; x++)
            for (int y = 0; y < s; y++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.15f, d));
                t.SetPixel(x, y, new Color(0.03f, 0.02f, 0.06f, a));
            }
            return t;
        }

        private static Texture2D[] Frames(int n, Func<int, Texture2D> draw)
        {
            var result = new Texture2D[n];
            for (int i = 0; i < n; i++) result[i] = draw(i);
            return result;
        }
    }
}
#endif
