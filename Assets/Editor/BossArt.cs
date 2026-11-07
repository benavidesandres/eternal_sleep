#if UNITY_EDITOR
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Dibujos de los jefes (mismo estilo que CreatureArt, pero mucho más grandes y con su propio aspecto).
    /// Todo mira a la izquierda; los scripts voltean el sprite.
    /// - Bestia: mole de sombra que anda sobre los nudillos, máscara de tres ojos y astas carmesí.
    /// - Reina de las Polillas: cuatro alas azul noche con lunas doradas, colas largas, corona y orbe.
    /// - Ojo que no Duerme: ojo flotante de pupila rasgada, anillo de piedras rúnicas y tentáculos con ojitos.
    /// </summary>
    public static class BossArt
    {
        /// <summary>Mismos píxeles por casilla que los enemigos normales: el tamaño viene del dibujo.</summary>
        public const float PPU = CreatureArt.EnemyPPU;

        private static readonly Color Ink = PixelArt.Hex("#0B0814");
        private static readonly Color Gold = PixelArt.Hex("#FFE38A");
        private static readonly Color GoldHot = PixelArt.Hex("#FFF6CC");
        private static readonly Color Rose = PixelArt.Hex("#E86A92");
        private static readonly Color RoseHot = PixelArt.Hex("#FFC2D6");
        private static readonly Color White = PixelArt.Hex("#F7F2FF");
        private static readonly Color Vein = PixelArt.Hex("#A8436A");

        private static Vector2 V(float x, float y) => new Vector2(x, y);
        private static int R(float v) => Mathf.RoundToInt(v);

        // ---------------------------------------------------------------- Bestia

        private static readonly Ramp[] BeastRamps =
        {
            default,
            new Ramp("#120C1E", "#251A3D", "#4B3A73"), // 1 cuerpo
            new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF"), // 2 máscara de hueso
            new Ramp("#5A1A33", "#A8436A", "#E86A92"), // 3 astas carmesí
            new Ramp("#0B0714", "#1A1229", "#30244D"), // 4 brazos, patas y melena
            new Ramp("#6B5320", "#B8924A", "#FFE38A"), // 5 garras doradas
        };

        private struct BeastPose
        {
            public float Bob, Swing, Crouch, Lean, HeadUp, Wave;
            public bool Mouth, Air, Angry, Hurt;
        }

        public static Texture2D[] BeastClip(string clip)
        {
            switch (clip)
            {
                case "walk":
                    return Frames(4, f => Beast(new BeastPose { Swing = Mathf.Sin(f * Mathf.PI / 2f) * 4f, Bob = f % 2, Wave = f }));
                case "windup":
                    return Frames(2, f => Beast(new BeastPose { Crouch = 4 + f, Lean = 3, Mouth = true, Angry = true, Wave = f * 2 }));
                case "charge":
                    return Frames(2, f => Beast(new BeastPose { Lean = 6, Crouch = 2, Swing = f == 0 ? 5 : -5, Bob = f, Mouth = true, Angry = true, Wave = f * 3 }));
                case "leap":
                    return Frames(1, f => Beast(new BeastPose { Air = true, HeadUp = 2, Mouth = true, Angry = true, Wave = 2 }));
                case "roar":
                    return Frames(2, f => Beast(new BeastPose { HeadUp = 4 + f, Mouth = true, Angry = true, Bob = f, Wave = f * 2 }));
                case "hurt":
                    return Frames(1, f => Beast(new BeastPose { Crouch = 3, HeadUp = -2, Hurt = true, Mouth = true, Wave = 1 }));
                default: // idle: respira
                    return Frames(2, f => Beast(new BeastPose { Bob = f, Wave = f }));
            }
        }

        private static Texture2D Beast(BeastPose p)
        {
            var c = new SpritePainter(104, 72);
            float cr = p.Crouch, b = p.Bob, lean = p.Lean, sw = p.Swing;

            // Cola de jirones (detrás de todo).
            for (int k = 0; k < 4; k++)
            {
                float w = Mathf.Sin((p.Wave + k * 1.3f) * 0.9f) * 3f;
                c.Curve(V(80f, 30f - cr + k * 3f + b), V(93f, 34f + k * 4f + w), V(102f, 20f + k * 9f + w * 0.5f), 6f - k, 1.2f, 4);
            }

            // Patas traseras, cortas y gruesas.
            for (int k = 0; k < 2; k++)
            {
                var hip = V(72f + k * 7f - lean * 0.3f, 24f - cr + b * 0.5f);
                var foot = p.Air ? V(hip.x + 7f, hip.y - 11f) : V(hip.x + 2f + (k == 0 ? sw : -sw) * 0.6f, 1f);
                var knee = V(hip.x + 8f, (hip.y + foot.y) * 0.5f + 2f);
                c.Curve(hip, knee, foot, 10f, 5f, 4);
                c.Ellipse(foot.x - 1f, foot.y + 1f, 4.5f, 2.2f, 4);
                for (int t = 0; t < 2; t++)
                    c.Poly(5, V(foot.x - 5f + t * 3f, foot.y + 1f), V(foot.x - 3f + t * 3f, foot.y + 1f), V(foot.x - 6f + t * 3f, foot.y - 1f));
            }

            // Brazo de atrás (detrás del cuerpo).
            var shoulderB = V(56f - lean, 40f - cr + b);
            var handB = p.Air ? V(shoulderB.x - 6f, shoulderB.y - 15f) : V(50f - lean * 1.5f - sw, 3f);
            c.Curve(shoulderB, V(shoulderB.x + 4f, (shoulderB.y + handB.y) * 0.5f), handB, 9f, 6f, 4);
            c.Ellipse(handB.x, handB.y + 1f, 5f, 3.5f, 4);

            // Púas de la melena (detrás de la joroba).
            float humpX = 52f - lean, humpY = 40f - cr + b;
            for (int i = 0; i < 6; i++)
            {
                float x = 40f + i * 5f - lean;
                float k = (x - humpX) / 15f;
                float top = humpY + 13f * Mathf.Sqrt(Mathf.Max(0f, 1f - k * k));
                float h = 6f + (i % 2) * 3f - cr * 0.5f;
                c.Poly(4, V(x - 3f, top - 3f), V(x + 3f, top - 3f), V(x + 5f, top + h));
            }

            // Torso enorme y joroba de los hombros, con el borde de abajo deshilachado.
            float torsoY = 30f - cr + b, torsoRy = 15f - cr * 0.4f;
            c.Ellipse(64f - lean * 0.5f, torsoY, 22f, torsoRy, 1);
            c.Ellipse(humpX, humpY, 15f, 13f, 1);
            for (float x = 48f; x <= 78f; x += 5f)
                c.Poly(1, V(x, torsoY - torsoRy + 3f), V(x + 4f, torsoY - torsoRy + 3f), V(x + 1.5f, torsoY - torsoRy - 3f - (x % 2)));

            // Grieta que brilla en el pecho, como un corazón encendido.
            Color crack = p.Hurt ? White : p.Angry ? RoseHot : Rose;
            float chx = 50f - lean, chy = 30f - cr + b;
            int[][] crackPts = { new[] { 0, 0 }, new[] { 1, 1 }, new[] { 1, 2 }, new[] { 2, 3 }, new[] { 1, 4 }, new[] { 2, 5 }, new[] { 0, -1 }, new[] { -1, -2 }, new[] { 3, 3 }, new[] { 4, 2 } };
            foreach (var pt in crackPts) c.Dot(R(chx) + pt[0], R(chy) + pt[1], crack);

            // Brazo de delante, apoyado en los nudillos, con garras doradas.
            var shoulder = V(46f - lean, 38f - cr + b);
            var hand = p.Air ? V(34f - lean, 22f) : V(36f - lean * 1.5f + sw, 3f);
            c.Curve(shoulder, V(shoulder.x - 6f, (shoulder.y + hand.y) * 0.5f + 2f), hand, 12f, 7f, 4);
            c.Ellipse(hand.x, hand.y + 1.5f, 6f, 4f, 4);
            for (int t = 0; t < 3; t++)
                c.Poly(5, V(hand.x - 4f + t * 3f, hand.y), V(hand.x - 2f + t * 3f, hand.y), V(hand.x - 6f + t * 3f, hand.y - 3f));

            // Cabeza.
            float hx = 26f - lean * 1.2f, hy = 38f - cr + b + p.HeadUp;

            // Astas carmesí ramificadas (detrás de la máscara).
            c.Curve(V(hx + 2f, hy + 7f), V(hx + 1f, hy + 17f), V(hx + 9f, hy + 23f), 3.6f, 1.2f, 3);
            c.Line(V(hx + 2f, hy + 14f), V(hx - 3f, hy + 19f), 2f, 1f, 3);
            c.Curve(V(hx + 6f, hy + 7f), V(hx + 10f, hy + 18f), V(hx + 22f, hy + 24f), 4.6f, 1.4f, 3);
            c.Line(V(hx + 9f, hy + 15f), V(hx + 6f, hy + 22f), 2.4f, 1f, 3);
            c.Line(V(hx + 15f, hy + 21f), V(hx + 14f, hy + 27f), 2.2f, 1f, 3);

            // Melena alrededor del cuello.
            for (int i = 0; i < 5; i++)
            {
                float a = (200f + i * 28f) * Mathf.Deg2Rad;
                var basePt = V(hx + 6f, hy - 2f);
                var tip = basePt + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 13f;
                c.Poly(4, basePt + V(-2f, 2f), basePt + V(2f, -2f), tip);
            }

            // Máscara de hueso con hocico.
            c.Ellipse(hx, hy, 10f, 9f, 2);
            c.Ellipse(hx - 6f, hy - 4f, 7.5f, 4.5f, 2);

            // Tres ojos: dos y uno vertical en la frente.
            Color eye = p.Hurt ? White : p.Angry ? Rose : Gold;
            Color hot = p.Hurt ? White : p.Angry ? RoseHot : GoldHot;
            foreach (var (ex, ey) in new[] { (hx - 4.5f, hy + 0.5f), (hx + 3f, hy + 1f) })
            {
                c.EllipseDot(ex, ey, 2.2f, p.Hurt ? 1f : 2.6f, Ink);
                if (p.Hurt) continue;
                c.Dot(R(ex), R(ey), eye);
                c.Dot(R(ex), R(ey) + 1, hot);
            }
            c.EllipseDot(hx - 0.5f, hy + 5.5f, 1.2f, p.Hurt ? 0.8f : 2.4f, Ink);
            if (!p.Hurt)
            {
                c.Dot(R(hx - 0.5f), R(hy + 5f), eye);
                c.Dot(R(hx - 0.5f), R(hy + 6f), hot);
            }

            // Boca: abierta con colmillos, o cosida.
            if (p.Mouth)
            {
                c.EllipseDot(hx - 7f, hy - 6f, 5f, 2.6f, Ink);
                foreach (int fx in new[] { -10, -8, -5, -3 }) c.Dot(R(hx) + fx, R(hy - 4f), White);
                foreach (int fx in new[] { -9, -6 }) c.Dot(R(hx) + fx, R(hy - 8f), White);
            }
            else
            {
                for (int i = 0; i < 7; i++) c.Dot(R(hx - 11f + i), R(hy - 5f), Ink);
                for (int i = 0; i < 7; i += 2) c.Dot(R(hx - 11f + i), R(hy - 4f), Ink);
            }

            // Grietas en la máscara.
            c.Dot(R(hx + 6f), R(hy + 5f), Ink);
            c.Dot(R(hx + 7f), R(hy + 4f), Ink);
            c.Dot(R(hx + 7f), R(hy + 3f), Ink);
            c.Dot(R(hx + 8f), R(hy + 2f), Ink);

            return c.Render(BeastRamps, Ink);
        }

        // ---------------------------------------------------------------- Reina de las Polillas

        private static readonly Ramp[] QueenRamps =
        {
            default,
            new Ramp("#1C2C4A", "#2F4F7A", "#5B86B8"), // 1 ala superior
            new Ramp("#16213A", "#243A5E", "#3F6596"), // 2 ala inferior y colas
            new Ramp("#8E84A8", "#D2C9EA", "#F7F2FF"), // 3 pelaje
            new Ramp("#6B5320", "#B8924A", "#FFE38A"), // 4 oro (lunas y corona)
            new Ramp("#120D1F", "#221A36", "#3A2D57"), // 5 cabeza, antenas, patas
            new Ramp("#0E1628", "#1A2740", "#2A3D60"), // 6 borde del ala
            new Ramp("#3A2D57", "#6B5A9E", "#A898DA"), // 7 abdomen
            new Ramp("#6E1F3D", "#C2416B", "#F08AAE"), // 8 manchas-ojo
        };

        private struct QueenPose
        {
            public float Lift, Lower, Fold, Sway;
            public bool Cast, Angry, Hurt;
        }

        public static Texture2D[] QueenClip(string clip)
        {
            float[] lifts = { 6, 1, -6, 1 };
            float[] lower = { -2, 0, 3, 0 };
            switch (clip)
            {
                case "windup":
                    return Frames(2, f => Queen(new QueenPose { Lift = 7 - f, Lower = -3, Angry = true, Sway = f }));
                case "swoop":
                    return Frames(1, f => Queen(new QueenPose { Lift = -8, Lower = 4, Fold = 1f, Angry = true, Sway = 2 }));
                case "cast":
                    return Frames(2, f => Queen(new QueenPose { Lift = 4 - f * 3, Lower = -1, Cast = true, Angry = f == 1, Sway = f }));
                case "hurt":
                    return Frames(1, f => Queen(new QueenPose { Lift = -4, Lower = 4, Hurt = true, Sway = -1 }));
                default: // fly
                    return Frames(4, f => Queen(new QueenPose { Lift = lifts[f], Lower = lower[f], Sway = Mathf.Sin(f * Mathf.PI / 2f) }));
            }
        }

        private static Texture2D Queen(QueenPose p)
        {
            const int W = 120;
            const float cx = 60f;
            var c = new SpritePainter(W, 92);
            float l = p.Lift, ll = p.Lower;
            float fold = 1f - p.Fold * 0.45f;

            for (int side = 0; side < 2; side++)
            {
                Vector2 M(float x, float y) => side == 0 ? V(x, y) : V(W - x, y);
                Vector2 F(float x, float y) => M(cx - (cx - x) * fold, y);

                // Ala inferior con una cola larga, como una polilla luna.
                c.Poly(2, F(56, 46), F(40, 40 + ll), F(26, 30 + ll), F(20, 18 + ll), F(28, 14 + ll), F(42, 26), F(56, 38));
                c.Curve(F(25, 19 + ll), F(17, 9 + ll * 0.5f), F(24 + p.Sway * 2f, 2), 5f, 1.6f, 2);
                var spot = F(33, 27 + ll);
                c.Ellipse(spot.x, spot.y, 3.6f * fold, 3.4f, 8);
                c.EllipseDot(spot.x, spot.y, 1.4f, 1.6f, Ink);
                c.Dot(R(spot.x - 0.5f), R(spot.y + 0.5f), White);

                // Ala superior: borde oscuro y relleno.
                var upper = new[] { F(57, 54), F(46, 62 + l * 0.4f), F(28, 74 + l), F(10, 80 + l), F(3, 70 + l * 0.7f), F(8, 58 + l * 0.4f), F(22, 48), F(56, 46) };
                c.Poly(6, upper);
                var center = Vector2.zero;
                foreach (var v in upper) center += v;
                center /= upper.Length;
                var inner = new Vector2[upper.Length];
                for (int i = 0; i < upper.Length; i++) inner[i] = Vector2.Lerp(upper[i], center, 0.18f);
                c.Poly(1, inner);

                // Luna creciente dorada en el ala.
                var moon = Vector2.Lerp(center, F(12, 72 + l), 0.3f);
                c.Ellipse(moon.x, moon.y, 5f * fold, 5f, 4);
                c.Ellipse(moon.x + (side == 0 ? 2.2f : -2.2f) * fold, moon.y + 1.2f, 4.3f * fold, 4.3f, 1);

                // Puntitos dorados en el borde.
                for (int i = 1; i < 5; i++)
                {
                    var d = Vector2.Lerp(F(46, 62 + l * 0.4f), F(10, 80 + l), i / 5f) + V(0f, -2f);
                    c.Dot(R(d.x), R(d.y), Gold);
                }

                // Antenas plumosas.
                var a0 = M(57, 66); var a1 = M(52, 76); var a2 = M(44, 84 - (p.Angry ? 3 : 0));
                c.Curve(a0, a1, a2, 2f, 1f, 5);
                for (int k = 1; k <= 4; k++)
                {
                    var bp = Vector2.Lerp(a1, a2, k / 5f);
                    c.Set(R(bp.x), R(bp.y) + 1, 5);
                    c.Set(R(bp.x) + (side == 0 ? 1 : -1), R(bp.y) + 2, 5);
                }

                // Patitas (o brazos que sujetan el orbe al lanzar un hechizo).
                if (p.Cast)
                {
                    c.Line(M(55, 51), M(52, 45), 2.2f, 1.8f, 5);
                    c.Line(M(52, 45), M(56, 41), 1.8f, 1.5f, 5);
                }
                else
                {
                    c.Line(M(55, 50), M(50, 43), 2f, 1.2f, 5);
                    c.Line(M(56, 46), M(52, 39), 2f, 1.2f, 5);
                }
            }

            // Abdomen largo y segmentado que se balancea.
            for (int i = 0; i < 5; i++)
            {
                float sx = cx + p.Sway * i * 0.6f;
                c.Ellipse(sx, 42f - i * 6f, 6.5f - i * 0.8f, 4f, i % 2 == 0 ? 7 : 5);
            }
            c.Dot(R(cx + p.Sway * 2.4f), 15, RoseHot);
            c.Dot(R(cx + p.Sway * 2.4f), 14, Rose);

            // Tórax peludo con cuello de gorguera, cabeza y corona.
            c.Ellipse(cx, 50f, 8f, 7f, 3);
            c.Ellipse(cx, 56f, 11f, 3.5f, 3);
            for (float x = cx - 9f; x <= cx + 9f; x += 3f) c.Dot(R(x), 54, PixelArt.Hex("#8E84A8"));
            c.Ellipse(cx, 61f, 6.5f, 5.5f, 5);
            for (int i = 0; i < 5; i++)
            {
                float x = cx - 6f + i * 3f;
                float h = i == 2 ? 10f : i % 2 == 0 ? 6f : 8f;
                c.Poly(4, V(x - 1.6f, 64f), V(x + 1.6f, 64f), V(x, 64f + h));
            }
            c.Rect(R(cx - 7f), 63, R(cx + 7f), 65, 4);
            c.Dot(R(cx), 64, Rose);
            c.Dot(R(cx), 65, RoseHot);

            // Seis ojos: dos grandes y cuatro pequeños.
            Color glow = p.Angry ? Rose : Gold;
            Color hot = p.Angry ? RoseHot : GoldHot;
            if (p.Hurt)
            {
                foreach (float ex in new[] { cx - 3f, cx + 3f })
                    for (int i = -1; i <= 1; i++) c.Dot(R(ex) + i, 60, Ink);
            }
            else
            {
                foreach (float ex in new[] { cx - 3f, cx + 3f })
                {
                    c.EllipseDot(ex, 60.5f, 1.8f, 2.2f, Ink);
                    c.Dot(R(ex), 60, glow);
                    c.Dot(R(ex), 61, hot);
                }
                foreach (float ex in new[] { cx - 5f, cx + 5f, cx - 1.5f, cx + 1.5f })
                    c.Dot(R(ex), ex == cx - 5f || ex == cx + 5f ? 62 : 58, glow);
            }

            // Orbe de luz entre las patas al lanzar un hechizo.
            if (p.Cast)
            {
                c.EllipseDot(cx, 40f, 4f, 4f, PixelArt.WithAlpha(Gold, 0.85f));
                c.EllipseDot(cx, 40f, 2.4f, 2.4f, GoldHot);
                c.Dot(R(cx - 1f), 41, White);
            }

            return c.Render(QueenRamps, Ink);
        }

        // ---------------------------------------------------------------- Ojo que no Duerme

        private static readonly Ramp[] EyeRamps =
        {
            default,
            new Ramp("#1F182F", "#3A2F55", "#5E4F85"), // 1 piedra
            new Ramp("#120D1F", "#221A36", "#3A2D57"), // 2 piedra oscura
            new Ramp("#9F95BC", "#E3DCF5", "#FFFFFF"), // 3 blanco del ojo
            new Ramp("#6E1F3D", "#C2416B", "#F08AAE"), // 4 iris
            new Ramp("#2A1F3F", "#4A3A6A", "#6E5C99"), // 5 párpado
            new Ramp("#6B5320", "#B8924A", "#FFE38A"), // 6 oro
            new Ramp("#8A6A1E", "#FFC94A", "#FFF3C4"), // 7 iris encendido
            new Ramp("#160F24", "#2C2145", "#4E3D75"), // 8 tentáculos
        };

        private struct EyePose
        {
            public float Open, LookX, LookY, Pupil, Ring, Wave, Squash;
            public bool Glow, Halo, Hurt;
        }

        public static Texture2D[] EyeClip(string clip)
        {
            switch (clip)
            {
                case "aim":
                    return Frames(2, f => Eye(new EyePose { Open = 1f, LookX = -5, Pupil = 1.2f, Ring = f * 11f, Wave = f, Glow = true, Halo = f == 0 }));
                case "shoot":
                    return Frames(1, f => Eye(new EyePose { Open = 1f, LookX = -6, Pupil = 0.8f, Ring = 5f, Wave = 2, Squash = -1, Glow = true, Halo = true }));
                case "close":
                    return Frames(1, f => Eye(new EyePose { Open = 0f, Ring = 22f, Wave = 1 }));
                case "hurt":
                    return Frames(1, f => Eye(new EyePose { Open = 0.25f, Pupil = 3f, Squash = 3, Wave = 3, Hurt = true }));
                default: // idle: mira, gira el anillo y parpadea
                    float[] open = { 1f, 1f, 1f, 0.3f };
                    float[] look = { -4f, -4f, -2f, -4f };
                    return Frames(4, f => Eye(new EyePose { Open = open[f], LookX = look[f], LookY = f == 2 ? 2 : 0, Pupil = 2.4f, Ring = f * 11.25f, Wave = f }));
            }
        }

        private static Texture2D Eye(EyePose p)
        {
            var c = new SpritePainter(96, 96);
            const float cx = 48f, cy = 54f;

            // Tentáculos que cuelgan y se mecen, algunos con un ojito en la punta.
            for (int k = 0; k < 6; k++)
            {
                float bx = 34f + k * 5.6f;
                var start = V(bx, cy - 15f);
                var ctrl = V(bx + Mathf.Sin(p.Wave * 0.9f + k) * 6f, 24f);
                var end = V(bx + Mathf.Sin(p.Wave * 1.2f + k * 2f) * 7f + (k - 2.5f) * 3f, 4f + (k % 2) * 5f);
                c.Curve(start, ctrl, end, 5.5f - (k % 2), 1.4f, 8);
                if (k % 2 == 0)
                {
                    c.EllipseDot(end.x, end.y, 1.8f, 1.8f, PixelArt.Hex("#E3DCF5"));
                    c.Dot(R(end.x - 0.5f), R(end.y), p.Glow ? Gold : Rose);
                }
            }

            // Anillo de piedras rúnicas que gira alrededor.
            for (int i = 0; i < 8; i++)
            {
                float a = (p.Ring + i * 45f) * Mathf.Deg2Rad;
                var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var t = new Vector2(-n.y, n.x);
                var m = new Vector2(cx, cy) + n * 37f;
                c.Poly(i % 2 == 0 ? 1 : 2, m - t * 4.5f - n * 2.5f, m + t * 4.5f - n * 2.5f, m + t * 3f + n * 3f, m - t * 3f + n * 3f);
                c.Dot(R(m.x), R(m.y), p.Glow ? GoldHot : Gold);
            }

            // Espinas sobre el párpado.
            float ry = 19f - p.Squash;
            for (int i = 0; i < 5; i++)
            {
                float x = cx - 14f + i * 7f;
                float top = cy + (ry + 2f) * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((x - cx) / 25f, 2f)));
                c.Poly(5, V(x - 3f, top - 3f), V(x + 3f, top - 3f), V(x + (i - 2) * 1.5f, top + 6f + (i == 2 ? 3f : 0f)));
            }

            // Ojo.
            c.Ellipse(cx, cy, 25f, ry + 3f, 5);
            c.Ellipse(cx, cy, 22f, ry, 3);

            // Venas.
            foreach (var (x0, y0, dx, dy) in new[] { (28f, cy + 3f, 1f, 0.3f), (68f, cy - 3f, -1f, 0.2f), (58f, cy + 13f, -0.6f, -0.6f), (36f, cy - 13f, 0.5f, 0.6f), (30f, cy - 6f, 1f, 0.1f) })
                for (int i = 0; i < 6; i++) c.Dot(R(x0 + dx * i), R(y0 + dy * i + (i % 2) * 0.5f), Vein);

            // Iris, pupila rasgada y brillos.
            float ix = cx + p.LookX, iy = cy + p.LookY;
            c.Ellipse(ix, iy, 10f, Mathf.Min(10f, ry - 2f), p.Glow ? 7 : 4);
            c.EllipseDot(ix, iy, p.Pupil, Mathf.Min(8f, ry - 4f), Ink);
            c.Dot(R(ix - 4f), R(iy + 5f), White);
            c.Dot(R(ix - 3f), R(iy + 4f), White);
            c.Dot(R(ix - 4f), R(iy + 4f), White);

            // Párpados que se cierran desde arriba y desde abajo.
            if (p.Open < 1f)
            {
                float gap = p.Open * ry;
                c.Ellipse(cx, cy, 23f, ry + 1f, 5, cy + gap);
                c.Ellipse(cx, cy, 23f, ry + 1f, 5, float.MinValue, cy - gap);
                for (int x = R(cx - 21f); x <= R(cx + 21f); x++) c.Dot(x, R(cy + gap), Ink);
                if (p.Open <= 0.01f)
                    for (int x = R(cx - 18f); x <= R(cx + 18f); x += 4) c.Dot(x, R(cy) - 2, Ink); // pestañas
            }

            // Aura cuando va a disparar.
            if (p.Halo)
                for (int a = 0; a < 16; a++)
                {
                    float ang = a * Mathf.PI / 8f;
                    c.Dot(R(cx + Mathf.Cos(ang) * 29f), R(cy + Mathf.Sin(ang) * (ry + 8f)), a % 2 == 0 ? Rose : RoseHot);
                }

            return c.Render(EyeRamps, Ink);
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
