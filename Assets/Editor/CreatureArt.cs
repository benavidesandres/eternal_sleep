#if UNITY_EDITOR
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Dibujos de los enemigos y aldeanos (32 píxeles = 1 casilla, el doble de detalle que el escenario).
    /// Todo mira a la izquierda; los scripts voltean el sprite.
    /// Paleta: sombras violeta profundo, máscaras de hueso pálido, brillos dorados y rosa, como Kael.
    /// </summary>
    public static class CreatureArt
    {
        public const float PPU = 32f;
        /// <summary>Los aldeanos se ven un poco más grandes (acostados ocupan poco alto).</summary>
        public const float VillagerPPU = 24f;
        /// <summary>Enemigos grandes e imponentes al lado de Kael.</summary>
        public const float EnemyPPU = 24f;

        private static readonly Color Ink = PixelArt.Hex("#0B0814");
        private static readonly Color Gold = PixelArt.Hex("#FFE38A");
        private static readonly Color GoldHot = PixelArt.Hex("#FFF6CC");
        private static readonly Color Rose = PixelArt.Hex("#E86A92");
        private static readonly Color RoseHot = PixelArt.Hex("#FFC2D6");
        private static readonly Color White = PixelArt.Hex("#F7F2FF");
        private static readonly Color Vein = PixelArt.Hex("#A8436A");

        private static Vector2 V(float x, float y) => new Vector2(x, y);
        private static int R(float v) => Mathf.RoundToInt(v);

        // ---------------------------------------------------------------- Sombra

        private struct ShadowPose
        {
            public float Bob, LegPhase, Stride, Crouch, Stretch, HeadLift, Wave;
            public bool Angry, MouthOpen, Hurt;
        }

        private static readonly Ramp[] ShadowRamps =
        {
            default,
            new Ramp("#150F24", "#2A1F44", "#5A4A85"), // 1 cuerpo
            new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF"), // 2 máscara de hueso
            new Ramp("#4A3E6E", "#8C7FB5", "#C9BCEB"), // 3 cuernos
            new Ramp("#0F0A1A", "#1E1631", "#352A52"), // 4 jirones y patas
        };

        public static Texture2D[] ShadowClip(string clip)
        {
            const float P = Mathf.PI;
            switch (clip)
            {
                case "walk":
                    return Frames(4, f => Shadow(new ShadowPose { LegPhase = f * P / 2f, Stride = 1f, Bob = f % 2, Wave = f }));
                case "run":
                    return Frames(4, f => Shadow(new ShadowPose { LegPhase = f * P / 2f, Stride = 1.5f, Bob = f % 2, Wave = f * 2, HeadLift = -1, Stretch = 1 }));
                case "windup":
                    return Frames(2, f => Shadow(new ShadowPose { LegPhase = P / 2f, Crouch = 3 + f, Angry = true, MouthOpen = true, HeadLift = -1, Wave = f * 3 }));
                case "lunge":
                    return Frames(1, f => Shadow(new ShadowPose { LegPhase = P, Stride = 1.8f, Stretch = 3, Crouch = 1, Angry = true, MouthOpen = true, Wave = 4 }));
                case "hurt":
                    return Frames(1, f => Shadow(new ShadowPose { LegPhase = P / 2f, HeadLift = 3, Crouch = 1, MouthOpen = true, Hurt = true, Wave = 2 }));
                default: // idle
                    return Frames(2, f => Shadow(new ShadowPose { LegPhase = P / 2f, Bob = f, Wave = f }));
            }
        }

        private static Texture2D Shadow(ShadowPose p)
        {
            var c = new SpritePainter(56, 44);
            float crouch = p.Crouch, bob = p.Bob;

            // Patas (detrás del cuerpo), con la rodilla hacia atrás como un animal.
            float hipY = 12f - crouch + bob * 0.5f;
            float[] hips = { 18, 24, 36, 42 };
            for (int k = 0; k < 4; k++)
            {
                float phase = p.LegPhase + (k % 2 == 0 ? 0f : Mathf.PI) + (k >= 2 ? Mathf.PI / 2f : 0f);
                float footX = hips[k] + Mathf.Sin(phase) * 3f * p.Stride + p.Stretch;
                float footY = Mathf.Max(0f, Mathf.Cos(phase)) * 2.5f * p.Stride;
                var hip = V(hips[k] - p.Stretch * 0.5f, hipY);
                c.Curve(hip, V(hip.x + 3f, (hipY + footY) * 0.5f + 1f), V(footX, footY + 0.5f), 3.2f, 2f, 4);
            }

            // Cola de jirones de sombra que ondea.
            for (int k = 0; k < 3; k++)
            {
                float w = Mathf.Sin((p.Wave + k * 1.7f) * 0.9f) * 2.5f;
                var start = V(43f + p.Stretch, 19f + bob - crouch + k * 2f);
                c.Curve(start, V(50f + p.Stretch, 20f - crouch + k * 3f + w), V(54f, 12f + k * 6f + w - crouch * 0.5f), 4.5f - k * 0.5f, 1f, 4);
            }

            // Cuerpo largo y bajo, con joroba en los hombros y el borde de abajo deshilachado.
            float bodyY = 16f + bob - crouch;
            float bodyRx = 16f + p.Stretch, bodyRy = 8f - crouch * 0.3f, bodyCx = 31f + p.Stretch * 0.3f;

            // Púas del lomo, inclinadas hacia atrás (detrás del cuerpo).
            for (int i = 0; i < 5; i++)
            {
                float x = 22f + i * 5f + p.Stretch * 0.3f;
                float k = (x - bodyCx) / bodyRx;
                float top = bodyY + bodyRy * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k)) + (x < 30f ? 3f : 0f);
                float h = (i == 1 || i == 2 ? 7f : 5f) - crouch * 0.5f;
                c.Poly(3, V(x - 2f, top - 2f), V(x + 2.5f, top - 2f), V(x + 4f + p.Stretch, top + h));
            }

            c.Ellipse(bodyCx, bodyY, bodyRx, bodyRy, 1);
            c.Ellipse(26f - p.Stretch * 0.3f, bodyY + 3f, 9.5f, 8f - crouch * 0.4f, 1);
            for (int x = 19; x <= 45; x += 5)
                c.Poly(1, V(x, bodyY - 5f), V(x + 4f, bodyY - 5f), V(x + 1.5f, bodyY - 9.5f - (x / 5) % 2));

            // Cuello.
            float hx = 13f - p.Stretch, hy = 19f + bob + p.HeadLift - crouch;
            c.Ellipse(hx + 7f, hy + 1f, 7f, 7f, 1);

            // Cuernos de carnero barridos hacia atrás (antes que la máscara, que los tapa en la base).
            c.Curve(V(hx + 1f, hy + 5f), V(hx + 6f, hy + 13f), V(hx + 15f, hy + 11f), 4.2f, 1.2f, 3);
            c.Curve(V(hx + 4f, hy + 4f), V(hx + 11f, hy + 9f), V(hx + 17f, hy + 6f), 3.4f, 1f, 3);

            // Cráneo de hueso con hocico alargado.
            c.Ellipse(hx + 1f, hy + 0.5f, 7f, 6.5f, 2);
            c.Ellipse(hx - 4f, hy - 2.5f, 6f, 3.4f, 2);

            // Cuencas y ojos.
            Color eye = p.Hurt ? White : p.Angry ? Rose : Gold;
            Color hot = p.Hurt ? White : p.Angry ? RoseHot : GoldHot;
            foreach (var (ex, ey) in new[] { (hx - 2.5f, hy + 1f), (hx + 3f, hy + 1.5f) })
            {
                c.EllipseDot(ex, ey, 2f, p.Hurt ? 1f : 2.6f, Ink);
                if (p.Hurt) continue;
                c.Dot(R(ex), R(ey), eye);
                c.Dot(R(ex), R(ey) + 1, hot);
            }

            // Grieta de la boca.
            if (p.MouthOpen)
            {
                c.EllipseDot(hx - 4.5f, hy - 4f, 3.2f, 1.6f, Ink);
                c.Dot(R(hx - 6f), R(hy - 3f), White);
                c.Dot(R(hx - 4f), R(hy - 3f), White);
                c.Dot(R(hx - 2.5f), R(hy - 3f), White);
            }
            else
            {
                for (int i = 0; i < 5; i++) c.Dot(R(hx - 7f + i), R(hy - 4f + (i % 2)), Ink);
            }
            // Grieta en la máscara.
            c.Dot(R(hx + 5f), R(hy + 5f), Ink);
            c.Dot(R(hx + 6f), R(hy + 4f), Ink);
            c.Dot(R(hx + 6f), R(hy + 3f), Ink);

            return c.Render(ShadowRamps, Ink);
        }

        // ---------------------------------------------------------------- Polilla

        private static readonly Ramp[] MothRamps =
        {
            default,
            new Ramp("#4E4278", "#8C7BC4", "#CDBFF5"), // 1 ala superior
            new Ramp("#3A2D57", "#6B5A9E", "#A898DA"), // 2 ala inferior
            new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF"), // 3 pelaje
            new Ramp("#6E1F3D", "#C2416B", "#F08AAE"), // 4 mancha-ojo
            new Ramp("#1F182F", "#3A2D57", "#5A4A85"), // 5 antenas, rayas, patas
            new Ramp("#2A2140", "#3E3160", "#5E4D8C"), // 6 borde del ala
        };

        private struct MothPose
        {
            public float Lift, LowerLift, Fold, BodyStretch;
            public bool Angry, Hurt;
        }

        public static Texture2D[] MothClip(string clip)
        {
            float[] lifts = { 5, 1, -6, 1 };
            float[] lower = { -1, 0, 2, 0 };
            switch (clip)
            {
                case "windup":
                    return Frames(2, f => Moth(new MothPose { Lift = 6 - f, LowerLift = -2, Angry = true, BodyStretch = -1 }));
                case "swoop":
                    return Frames(1, f => Moth(new MothPose { Lift = -7, LowerLift = 3, Fold = 1f, BodyStretch = 2, Angry = true }));
                case "hurt":
                    return Frames(1, f => Moth(new MothPose { Lift = -3, LowerLift = 3, Hurt = true, BodyStretch = -1 }));
                default: // fly y flyfast
                    return Frames(4, f => Moth(new MothPose { Lift = lifts[f], LowerLift = lower[f] }));
            }
        }

        private static Texture2D Moth(MothPose p)
        {
            const int W = 56;
            var c = new SpritePainter(W, 48);
            float l = p.Lift, ll = p.LowerLift;
            float fold = 1f - p.Fold * 0.45f;

            for (int side = 0; side < 2; side++)
            {
                // Puntos del ala izquierda; la derecha es su espejo.
                Vector2 M(float x, float y) => side == 0 ? V(x, y) : V(W - x, y);
                Vector2 F(float x, float y) => M(24f - (24f - x) * fold, y);

                // Ala inferior.
                c.Poly(2, F(24, 24), F(14, 22 + ll), F(8, 14 + ll), F(13, 8 + ll), F(22, 16));

                // Ala superior: borde oscuro y relleno.
                var upper = new[] { F(24, 30), F(18, 34 + l * 0.4f), F(8, 40 + l), F(2, 37 + l), F(3, 30 + l * 0.6f), F(11, 24 + l * 0.2f), F(23, 24) };
                c.Poly(6, upper);
                var center = Vector2.zero;
                foreach (var v in upper) center += v;
                center /= upper.Length;
                var inner = new Vector2[upper.Length];
                for (int i = 0; i < upper.Length; i++) inner[i] = Vector2.Lerp(upper[i], center, 0.22f);
                c.Poly(1, inner);

                // Mancha en forma de ojo.
                var spot = Vector2.Lerp(center, side == 0 ? M(8, 36 + l) : M(8, 36 + l), 0.35f);
                c.Ellipse(spot.x, spot.y, 3.4f * fold, 3f, 4);
                c.EllipseDot(spot.x, spot.y, 1.4f, 1.4f, Ink);
                c.Dot(R(spot.x - 0.5f), R(spot.y + 0.5f), White);

                // Antenas plumosas.
                var a0 = M(26, 38); var a1 = M(23, 44); var a2 = M(18, 46 - (p.Angry ? 2 : 0));
                c.Curve(a0, a1, a2, 1.6f, 1f, 5);
                for (int k = 1; k <= 3; k++)
                {
                    var b = Vector2.Lerp(a1, a2, k / 4f);
                    c.Set(R(b.x), R(b.y) + 1, 5);
                }

                // Patitas.
                c.Line(M(26, 18), M(22, 12), 1.5f, 1f, 5);
                c.Line(M(26, 22), M(21, 18), 1.5f, 1f, 5);
            }

            // Cuerpo peludo con rayas, cabeza y collar.
            float bs = p.BodyStretch;
            c.Ellipse(27.5f, 21f - bs, 4f, 10f + bs, 3);
            for (int y = 12 - (int)bs; y <= 20; y += 3) c.Recolor(23, y, 32, y, 3, 5);
            c.Ellipse(27.5f, 32f, 6f, 3f, 3);
            c.Ellipse(27.5f, 35f, 5f, 4f, 3);

            // Ojos compuestos.
            Color glow = p.Angry ? Rose : Gold;
            foreach (float ex in new[] { 25f, 30f })
            {
                if (p.Hurt)
                {
                    for (int i = -1; i <= 1; i++) c.Dot(R(ex) + i, 35, Ink);
                    continue;
                }
                c.EllipseDot(ex, 35.5f, 1.8f, 2.2f, Ink);
                c.Dot(R(ex), 36, glow);
                c.Dot(R(ex), 37, p.Angry ? RoseHot : GoldHot);
            }

            return c.Render(MothRamps, Ink);
        }

        // ---------------------------------------------------------------- Vigía

        private static readonly Ramp[] WatcherRamps =
        {
            default,
            new Ramp("#1F182F", "#3A2F55", "#5E4F85"), // 1 piedra
            new Ramp("#120D1F", "#221A36", "#3A2D57"), // 2 piedra oscura
            new Ramp("#9F95BC", "#E3DCF5", "#FFFFFF"), // 3 blanco del ojo
            new Ramp("#6E1F3D", "#C2416B", "#F08AAE"), // 4 iris
            new Ramp("#2A1F3F", "#4A3A6A", "#6E5C99"), // 5 párpado
            new Ramp("#6B5320", "#B8924A", "#FFE38A"), // 6 oro
            new Ramp("#8A6A1E", "#FFC94A", "#FFF3C4"), // 7 iris encendido
        };

        private struct WatcherPose
        {
            public float Open, Pupil, Squash, Look;
            public bool Glow, Halo;
        }

        public static Texture2D[] WatcherClip(string clip)
        {
            switch (clip)
            {
                case "aim":
                    return Frames(2, f => Watcher(new WatcherPose { Open = 1f, Pupil = f == 0 ? 1.2f : 0.8f, Look = -3, Glow = true, Halo = f == 0 }));
                case "shoot":
                    return Frames(1, f => Watcher(new WatcherPose { Open = 1f, Pupil = 0.7f, Look = -4, Squash = -1, Glow = true, Halo = true }));
                case "hurt":
                    return Frames(1, f => Watcher(new WatcherPose { Open = 0.12f, Pupil = 1.5f, Squash = 2 }));
                default: // idle: mira y parpadea
                    float[] open = { 1f, 1f, 1f, 0.35f };
                    float[] look = { -3f, -3f, -2f, -3f };
                    return Frames(4, f => Watcher(new WatcherPose { Open = open[f], Pupil = 1.6f, Look = look[f] }));
            }
        }

        private static Texture2D Watcher(WatcherPose p)
        {
            var c = new SpritePainter(40, 56);

            // Pedestal gótico.
            c.Poly(2, V(5, 0), V(35, 0), V(31, 5), V(9, 5));
            c.Rect(9, 5, 31, 7, 1);
            c.Rect(15, 8, 25, 19, 1);
            c.Rect(13, 11, 27, 12, 2);
            c.Rect(14, 17, 26, 18, 2);
            c.Ellipse(19.5f, 14.5f, 2f, 1.5f, 6);

            // Cuna con garras.
            c.Poly(1, V(6, 25), V(34, 25), V(27, 19), V(13, 19));
            c.Curve(V(8, 24), V(2, 31), V(6, 38), 3.5f, 1.2f, 1);
            c.Curve(V(32, 24), V(38, 31), V(34, 38), 3.5f, 1.2f, 1);

            // Corona de púas.
            float ey = 36f - p.Squash * 0.5f;
            c.Poly(1, V(13, ey + 9), V(17, ey + 10), V(14.5f, ey + 16));
            c.Poly(1, V(18, ey + 10), V(22, ey + 10), V(20, ey + 18));
            c.Poly(1, V(23, ey + 10), V(27, ey + 9), V(25.5f, ey + 16));

            // Ojo.
            float ry = 10f - p.Squash;
            c.Ellipse(19.5f, ey, 13f, ry + 1.5f, 5);
            c.Ellipse(19.5f, ey, 11f, ry, 3);

            // Venas.
            foreach (var (x0, y0, dx, dy) in new[] { (9f, ey + 2f, 1f, 0.3f), (29f, ey - 2f, -1f, 0.2f), (25f, ey + 7f, -0.6f, -0.6f), (14f, ey - 7f, 0.5f, 0.6f) })
                for (int i = 0; i < 4; i++) c.Dot(R(x0 + dx * i), R(y0 + dy * i + (i % 2) * 0.5f), Vein);

            // Iris, pupila y brillo.
            float ix = 19.5f + p.Look;
            c.Ellipse(ix, ey, 5f, Mathf.Min(6f, ry - 1f), p.Glow ? 7 : 4);
            c.EllipseDot(ix, ey, p.Pupil, Mathf.Min(4f, ry - 3f), Ink);
            c.Dot(R(ix - 2f), R(ey + 3f), White);
            c.Dot(R(ix - 1f), R(ey + 2f), White);

            // Párpado que se cierra desde arriba.
            if (p.Open < 1f)
            {
                float lidBottom = ey + (2f * p.Open - 1f) * (ry + 1f);
                c.Ellipse(19.5f, ey, 12f, ry + 1f, 5, lidBottom);
                for (int x = 9; x <= 30; x++) c.Dot(x, R(lidBottom) - 1, Ink); // pestañas
            }

            // Aura cuando va a disparar.
            if (p.Halo)
                for (int a = 0; a < 12; a++)
                {
                    float ang = a * Mathf.PI / 6f;
                    c.Dot(R(19.5f + Mathf.Cos(ang) * 15f), R(ey + Mathf.Sin(ang) * (ry + 4f)), a % 2 == 0 ? Rose : RoseHot);
                }

            return c.Render(WatcherRamps, Ink);
        }

        // ---------------------------------------------------------------- Aldeanos

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

        private static Ramp[] VillagerRamps(int v) => new[]
        {
            default,
            new Ramp("#3A2C1C", "#5E4A30", "#86704A"), // 1 esterilla de paja
            new Ramp("#8E84A8", "#CFC6E6", "#F2ECFF"), // 2 almohada
            Skins[v % Skins.Length],                   // 3 piel
            Caps[v % Caps.Length],                     // 4 gorro de dormir
            Blankets[v % Blankets.Length],             // 5 manta
            Caps[(v + 2) % Caps.Length],               // 6 rayas / ropa
        };

        /// <summary>Aldeano acostado bajo su manta, respirando.</summary>
        public static Texture2D[] VillagerSleeping(int variant)
        {
            float[] breath = { 0f, 0.5f, 1f, 0.5f };
            return Frames(4, f => VillagerAsleep(variant, breath[f]));
        }

        private static Texture2D VillagerAsleep(int v, float breath)
        {
            var c = new SpritePainter(64, 30);

            c.Rect(2, 0, 61, 3, 1);
            for (int x = 4; x < 60; x += 3) c.Dot(x, 1 + x % 2, PixelArt.Hex("#2A2014"));

            c.Ellipse(10f, 7f, 7.5f, 3.5f, 2);

            // Manta con rayas y bulto de los pies.
            float lift = breath;
            c.Ellipse(39f, 6f + lift * 0.5f, 22f, 6.5f + lift, 5, 3f);
            c.Ellipse(57f, 7f, 4.5f, 4.5f, 5, 3f);
            for (int x = 24; x <= 52; x += 8) c.Recolor(x, 3, x + 1, 16, 5, 6);

            // Cabeza y gorro de dormir caído hacia atrás.
            c.Ellipse(12f, 10f, 6.5f, 5.5f, 3);
            c.Poly(4, V(5.5f, 13f), V(18.5f, 13f), V(17f, 17f), V(11f, 18.5f), V(4f, 18f), V(1.5f, 15f));
            c.Ellipse(2.5f, 15f, 2f, 2f, 2);

            // Mano sobre la manta.
            c.Ellipse(22f, 11f + lift, 2f, 1.6f, 3);

            // Cara: ojos cerrados, mejillas y boca entreabierta.
            foreach (int ex in new[] { 9, 14 })
            {
                c.Dot(ex - 1, 10, Ink);
                c.Dot(ex, 9, Ink);
                c.Dot(ex + 1, 10, Ink);
            }
            c.Dot(8, 8, PixelArt.WithAlpha(Rose, 0.8f));
            c.Dot(16, 8, PixelArt.WithAlpha(Rose, 0.8f));
            c.Dot(12, 7, Ink);
            if (breath >= 1f) c.Dot(12, 6, Ink);

            return c.Render(VillagerRamps(v), Ink);
        }

        /// <summary>Aldeano despierto, sentado en su esterilla.</summary>
        public static Texture2D[] VillagerAwake(int variant) => Frames(2, f => VillagerSitting(variant, f));

        private static Texture2D VillagerSitting(int v, int bob)
        {
            var c = new SpritePainter(40, 42);

            c.Rect(2, 0, 37, 3, 1);
            c.Ellipse(21f, 6f, 15f, 4.5f, 5, 3f);
            for (int x = 10; x <= 32; x += 7) c.Recolor(x, 3, x + 1, 12, 5, 6);

            // Torso y brazos.
            c.Poly(6, V(12, 8), V(25, 8), V(24, 22), V(13, 22));
            c.Line(V(14, 20), V(10, 12), 3.2f, 2.6f, 6);
            c.Line(V(23, 20), V(27, 12), 3.2f, 2.6f, 6);
            c.Ellipse(10f, 11f, 1.8f, 1.6f, 3);
            c.Ellipse(27f, 11f, 1.8f, 1.6f, 3);

            // Cabeza con gorro.
            float hy = 28f + bob * 0.5f;
            c.Ellipse(18.5f, hy, 6f, 6f, 3);
            c.Poly(4, V(12, hy + 2.5f), V(25, hy + 2.5f), V(23.5f, hy + 7), V(17, hy + 9), V(10, hy + 7), V(6.5f, hy + 3));
            c.Ellipse(6.5f, hy + 2.5f, 2f, 2f, 2);

            // Ojos abiertos, sonrisa cansada y mejillas.
            int y = R(hy);
            foreach (int ex in new[] { 16, 21 })
            {
                c.Dot(ex, y - 1, Ink);
                c.Dot(ex, y - 2, Ink);
            }
            c.Dot(17, y - 4, Ink);
            c.Dot(18, y - 5, Ink);
            c.Dot(19, y - 5, Ink);
            c.Dot(20, y - 4, Ink);
            c.Dot(14, y - 3, PixelArt.WithAlpha(Rose, 0.8f));
            c.Dot(23, y - 3, PixelArt.WithAlpha(Rose, 0.8f));

            return c.Render(VillagerRamps(v), Ink);
        }

        // ---------------------------------------------------------------- Iconos

        public static Texture2D SleepZ()
        {
            var c = new SpritePainter(12, 12);
            c.Rect(2, 8, 9, 9, 1);
            c.Line(V(8.5f, 8), V(2.5f, 3), 2.4f, 2.4f, 1);
            c.Rect(2, 2, 9, 3, 1);
            return c.Render(new[] { default, new Ramp("#8E84A8", "#D9CCFF", "#FFFFFF") }, Ink);
        }

        /// <summary>Bocadillo de "..." para hablar.</summary>
        public static Texture2D TalkBubble()
        {
            var c = new SpritePainter(18, 15);
            c.Ellipse(9f, 8.5f, 7.5f, 5f, 1);
            c.Poly(1, V(4, 6), V(8, 5), V(3, 0.5f));
            foreach (int x in new[] { 6, 9, 12 }) { c.Dot(x, 8, Ink); c.Dot(x, 9, Ink); }
            return c.Render(new[] { default, new Ramp("#B9A8F0", "#E6DCFF", "#FFFFFF") }, Ink);
        }

        // ----------------------------------------------------------------

        private static Texture2D[] Frames(int n, System.Func<int, Texture2D> draw)
        {
            var result = new Texture2D[n];
            for (int i = 0; i < n; i++) result[i] = draw(i);
            return result;
        }
    }
}
#endif
