using System;
using UnityEngine;

namespace EternalSleep.Dream
{
    /// <summary>
    /// "Receta" de un sueño: tema, textos, tamaño, dificultad, enemigos y ambiente.
    /// La puede escribir la IA (DreamAI, en JSON con estos mismos nombres) o crearse sin IA (Procedural).
    /// Sanitize() deja todo dentro de lo permitido: lo que diga la IA nunca puede romper el juego.
    /// </summary>
    [Serializable]
    public class DreamRecipe
    {
        public static readonly string[] Themes = { "bosque", "pantano", "cueva", "catedral", "biblioteca", "pueblo" };

        [Serializable]
        public class EnemyMix
        {
            public float sombra = 0.5f;
            public float polilla = 0.3f;
            public float vigia = 0.2f;
        }

        public string tema = "bosque";
        public string titulo = "";
        public string[] intro = new string[0];
        public string despertar = "";
        public int largo = 4;
        public int dificultad = 1;
        public int verticalidad = 1;
        public EnemyMix enemigos = new EnemyMix();
        public string luz = "#D9CCFF";
        public float niebla = 0.3f;
        public string[] tramo_extra = new string[0];

        /// <summary>True si la escribió la IA (para el registro y las pruebas).</summary>
        [NonSerialized] public bool fromAI;

        /// <summary>Índice del tema en Themes (0 si no es válido).</summary>
        public int ThemeIndex => Mathf.Max(0, Array.IndexOf(Themes, tema));

        public Color LightColor => ColorUtility.TryParseHtmlString(luz, out var c) ? c : new Color(0.85f, 0.8f, 1f);

        /// <summary>Recorta cada campo a su rango. Lo que falta o no vale se toma de "fallback".</summary>
        public DreamRecipe Sanitize(DreamRecipe fallback)
        {
            if (Array.IndexOf(Themes, tema) < 0) tema = fallback.tema;
            titulo = Clip(string.IsNullOrWhiteSpace(titulo) ? fallback.titulo : titulo, 60);
            intro = CleanLines(intro, 3, 120, fallback.intro);
            despertar = Clip(string.IsNullOrWhiteSpace(despertar) ? fallback.despertar : despertar, 140);
            largo = Mathf.Clamp(largo, 3, 6);
            dificultad = Mathf.Clamp(dificultad, 1, 3);
            verticalidad = Mathf.Clamp(verticalidad, 0, 2);

            if (enemigos == null) enemigos = fallback.enemigos;
            enemigos.sombra = Mathf.Clamp01(enemigos.sombra);
            enemigos.polilla = Mathf.Clamp01(enemigos.polilla);
            enemigos.vigia = Mathf.Clamp01(enemigos.vigia);
            if (enemigos.sombra + enemigos.polilla + enemigos.vigia < 0.01f) enemigos = fallback.enemigos;

            if (!ColorUtility.TryParseHtmlString(luz ?? "", out var c)) luz = fallback.luz;
            else
            {
                // Que nunca quede demasiado oscuro para jugar.
                Color.RGBToHSV(c, out float h, out float s, out float v);
                luz = "#" + ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(h, Mathf.Min(s, 0.6f), Mathf.Max(v, 0.65f)));
            }
            niebla = Mathf.Clamp(niebla, 0f, 0.8f);
            if (tramo_extra == null) tramo_extra = new string[0];
            return this;
        }

        private static string Clip(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ").Replace("<", "").Replace(">", "").Trim(); // sin etiquetas de texto enriquecido
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }

        private static string[] CleanLines(string[] lines, int maxLines, int maxChars, string[] fallback)
        {
            if (lines == null || lines.Length == 0) return fallback;
            var result = new System.Collections.Generic.List<string>();
            foreach (var l in lines)
            {
                var t = Clip(l, maxChars);
                if (t.Length > 0) result.Add(t);
                if (result.Count == maxLines) break;
            }
            return result.Count > 0 ? result.ToArray() : fallback;
        }

        // ---------- Sin IA ----------

        private static readonly string[][] Places =
        {
            new[] { "el bosque", "el claro", "el sendero de raíces", "el bosque de troncos huecos" },
            new[] { "el pantano", "la ciénaga", "la laguna de niebla", "el pantano de campanas hundidas" },
            new[] { "la cueva", "la gruta", "el pozo sin fondo", "la caverna de ecos" },
            new[] { "la catedral", "la nave en ruinas", "el campanario", "la cripta de vitrales" },
            new[] { "la biblioteca", "el archivo", "la sala de libros cerrados", "la torre de páginas" },
            new[] { "el pueblo", "la calle vacía", "la plaza sin faroles", "el pueblo del revés" },
        };

        private static readonly string[] Things =
        {
            "nadie recuerda su nombre", "las sombras cuentan los pasos", "el reloj camina hacia atrás",
            "las polillas guardan secretos", "llueve hacia arriba", "cada puerta lleva al mismo sitio",
            "la luna no quiere salir", "los ojos se abren al anochecer", "se oye una nana muy lejos",
            "el suelo respira", "todo huele a velas apagadas", "alguien te está esperando",
        };

        private static readonly string[] Intros =
        {
            "El aire pesa como una manta mojada.", "Algo se mueve justo fuera de la vista.",
            "Tus pasos suenan dos veces: el segundo no es tuyo.", "Aquí la luz llega cansada.",
            "Una voz tararea una canción que casi recuerdas.", "El sueño te reconoce. No le gustas.",
            "Las sombras se apartan... y luego vuelven.", "Este lugar se deshace por los bordes.",
        };

        private static readonly string[] Wakes =
        {
            "He soñado que alguien con máscara me sacaba de la oscuridad... eras tú.",
            "Por fin silencio. Gracias, Kael.", "No recuerdo la pesadilla. Solo tu luz.",
            "Sentía que caía sin parar, y de pronto una mano me sujetó.",
        };

        private static readonly string[] Lights = { "#C9D8B8", "#B8D0C0", "#A8B8E8", "#E0C8F0", "#F0D8B0", "#D9CCFF" };

        /// <summary>Receta sin IA, decidida solo por la semilla (y por cuántos aldeanos ya despertaron).</summary>
        public static DreamRecipe Procedural(int seed, int awakened)
        {
            var rng = new System.Random(seed);
            int theme = rng.Next(Themes.Length);
            var r = new DreamRecipe
            {
                tema = Themes[theme],
                titulo = Cap($"{Pick(rng, Places[theme])} donde {Pick(rng, Things)}"),
                intro = new[] { Pick(rng, Intros) },
                despertar = Pick(rng, Wakes),
                largo = 3 + Mathf.Min(3, awakened / 2 + rng.Next(2)),
                dificultad = Mathf.Clamp(1 + awakened / 2, 1, 3),
                verticalidad = rng.Next(3),
                enemigos = new EnemyMix
                {
                    sombra = 0.2f + (float)rng.NextDouble() * 0.6f,
                    polilla = 0.1f + (float)rng.NextDouble() * 0.5f,
                    vigia = (float)rng.NextDouble() * 0.4f,
                },
                luz = Lights[theme],
                niebla = 0.15f + (float)rng.NextDouble() * 0.4f,
            };
            return r;
        }

        private static string Pick(System.Random rng, string[] list) => list[rng.Next(list.Length)];

        private static string Cap(string s) => char.ToUpper(s[0]) + s.Substring(1);
    }
}
