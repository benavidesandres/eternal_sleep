using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Arma el mapa de un sueño (mismo formato de texto que Assets/Levels) uniendo tramos de Tramos.txt:
    /// inicio + "largo" tramos normales del tema y la dificultad de la receta + final.
    /// - Algunos tramos se ponen al revés (espejo) para que haya más combinaciones.
    /// - Si la altura del suelo de dos tramos no coincide, pone un escalón entre los dos.
    /// - Rellena los sitios de enemigos (? y *) según los pesos de la receta.
    /// - Si la receta trae un tramo dibujado por la IA y es válido, lo mete en medio.
    /// - Comprueba con DreamValidator que se llega al portal; si no, prueba otra vez con otra semilla.
    /// Código puro (sin Unity) para poder probarlo fácilmente.
    /// </summary>
    public static class DreamGenerator
    {
        public const int Height = 12;
        private const int MaxAttempts = 10;
        private const string AllowedInChunk = "#=^.?*";

        public class Chunk
        {
            public string Type = "normal";
            public string[] Themes = { "todos" };
            public int Difficulty = 1;
            public int Vertical;
            public string[] Rows;
            public int Width => Rows[0].Length;
        }

        /// <summary>Lee los tramos del texto de Tramos.txt.</summary>
        public static List<Chunk> Parse(string text)
        {
            var chunks = new List<Chunk>();
            Chunk current = null;
            var rows = new List<string>();

            void Close()
            {
                if (current == null) return;
                current.Rows = Normalize(rows);
                if (current.Rows != null) chunks.Add(current);
                current = null;
                rows.Clear();
            }

            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith("#") && current == null) continue; // comentarios antes del primer tramo
                if (line.StartsWith("@tramo"))
                {
                    Close();
                    current = new Chunk();
                    foreach (var part in line.Split(' ').Skip(1))
                    {
                        var kv = part.Split('=');
                        if (kv.Length != 2) continue;
                        switch (kv[0])
                        {
                            case "tipo": current.Type = kv[1]; break;
                            case "temas": current.Themes = kv[1].Split(','); break;
                            case "dificultad": int.TryParse(kv[1], out current.Difficulty); break;
                            case "vertical": int.TryParse(kv[1], out current.Vertical); break;
                        }
                    }
                    continue;
                }
                if (current != null && line.Length > 0) rows.Add(line);
            }
            Close();
            return chunks;
        }

        /// <summary>Deja el tramo en 12 filas del mismo ancho (rellena arriba con vacío). Null si no vale.</summary>
        private static string[] Normalize(List<string> rows)
        {
            if (rows.Count == 0 || rows.Count > Height) return null;
            int w = rows.Max(r => r.Length);
            var result = new List<string>();
            for (int i = rows.Count; i < Height; i++) result.Add(new string('.', w));
            result.AddRange(rows.Select(r => r.PadRight(w, '.')));
            return result.ToArray();
        }

        /// <summary>
        /// ¿Sirve el tramo que dibujó la IA? 8-20 de ancho, hasta 12 de alto, solo caracteres permitidos
        /// y con suelo en los dos bordes.
        /// </summary>
        public static Chunk FromAI(string[] lines)
        {
            if (lines == null || lines.Length < 4 || lines.Length > Height) return null;
            if (lines.Any(l => l == null || l.Length < 8 || l.Length > 20)) return null;
            if (lines.Any(l => l.Any(c => AllowedInChunk.IndexOf(c) < 0))) return null;
            var rows = Normalize(lines.ToList());
            var chunk = new Chunk { Rows = rows, Difficulty = 1 };
            int left = EdgeHeight(rows, 0), right = EdgeHeight(rows, chunk.Width - 1);
            if (left < 1 || left > 6 || right < 1 || right > 6) return null;
            return chunk;
        }

        /// <summary>Genera el mapa. canDoubleJump: lo que el jugador puede alcanzar ya (para validar).</summary>
        public static string[] Generate(DreamRecipe recipe, int seed, List<Chunk> library, bool canDoubleJump)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var map = Build(recipe, seed + attempt * 7919, library, attempt < MaxAttempts / 2);
                if (map != null && DreamValidator.IsSolvable(map, canDoubleJump)) return map;
            }
            return Fallback(library);
        }

        /// <summary>Sueño mínimo que siempre tiene solución: inicio, suelo llano y final.</summary>
        public static string[] Fallback(List<Chunk> library)
        {
            var start = library.FirstOrDefault(c => c.Type == "inicio")?.Rows ?? Flat(14, 'P');
            var end = library.FirstOrDefault(c => c.Type == "final")?.Rows ?? Flat(14, 'G');
            return Join(new List<string[]> { Clean(start), Flat(14, '.'), Clean(end) });
        }

        private static string[] Build(DreamRecipe recipe, int seed, List<Chunk> library, bool useAI)
        {
            var rng = new Random(seed);
            var starts = library.Where(c => c.Type == "inicio" && Fits(c, recipe)).ToList();
            var ends = library.Where(c => c.Type == "final" && Fits(c, recipe)).ToList();
            var normals = library.Where(c => c.Type == "normal" && Fits(c, recipe)).ToList();
            if (starts.Count == 0 || ends.Count == 0 || normals.Count == 0) return null;

            var parts = new List<string[]> { Pick(rng, starts, recipe).Rows };
            Chunk last = null;
            for (int i = 0; i < recipe.largo; i++)
            {
                Chunk next;
                int tries = 0;
                do next = Pick(rng, normals, recipe);
                while (next == last && normals.Count > 1 && ++tries < 10);
                last = next;
                parts.Add(rng.NextDouble() < 0.5 ? Mirror(next.Rows) : next.Rows);
            }

            var extra = useAI ? FromAI(recipe.tramo_extra) : null;
            if (extra != null) parts.Insert(1 + rng.Next(recipe.largo) + 1, extra.Rows);
            parts.Add(Pick(rng, ends, recipe).Rows);

            var map = Join(parts);
            PlaceEnemies(map, recipe, rng);
            return map;
        }

        private static bool Fits(Chunk c, DreamRecipe r) =>
            c.Difficulty <= r.dificultad && (c.Themes.Contains("todos") || c.Themes.Contains(r.tema));

        /// <summary>Elige un tramo al azar, con más peso para los de su tema y su verticalidad.</summary>
        private static Chunk Pick(Random rng, List<Chunk> list, DreamRecipe r)
        {
            var weights = list.Select(c =>
                (c.Themes.Contains(r.tema) ? 3f : 1f) *
                (c.Vertical == r.verticalidad ? 2f : 1f) *
                (c.Difficulty == r.dificultad ? 1.5f : 1f)).ToArray();
            float total = weights.Sum(), roll = (float)rng.NextDouble() * total;
            for (int i = 0; i < list.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f) return list[i];
            }
            return list[list.Count - 1];
        }

        /// <summary>Une los tramos de izquierda a derecha, con un escalón si el suelo no coincide.</summary>
        private static string[] Join(List<string[]> parts)
        {
            var rows = Enumerable.Range(0, Height).Select(_ => new StringBuilder()).ToArray();
            int previous = -1;
            foreach (var part in parts)
            {
                int entry = EdgeHeight(part, 0);
                if (previous > 0 && entry > 0 && previous != entry)
                    Append(rows, Flat(2, '.', (previous + entry + 1) / 2)); // escalón intermedio
                Append(rows, part);
                previous = EdgeHeight(part, part[0].Length - 1);
            }
            return rows.Select(r => r.ToString()).ToArray();
        }

        private static void Append(StringBuilder[] rows, string[] part)
        {
            for (int i = 0; i < Height; i++) rows[i].Append(part[i]);
        }

        /// <summary>Cuántas casillas de suelo seguidas hay desde abajo en esa columna.</summary>
        private static int EdgeHeight(string[] rows, int x)
        {
            int h = 0;
            for (int row = rows.Length - 1; row >= 0 && rows[row][x] == '#'; row--) h++;
            return h;
        }

        private static string[] Flat(int width, char marker, int ground = 3)
        {
            var rows = new string[Height];
            for (int row = 0; row < Height; row++)
            {
                int y = Height - 1 - row;
                rows[row] = y < ground ? new string('#', width)
                    : y == ground && marker != '.' ? "." + marker + new string('.', width - 2)
                    : new string('.', width);
            }
            return rows;
        }

        private static string[] Mirror(string[] rows) =>
            rows.Select(r => new string(r.Reverse().ToArray())).ToArray();

        private static string[] Clean(string[] rows) =>
            rows.Select(r => r.Replace('?', '.').Replace('*', '.')).ToArray();

        /// <summary>Cambia los sitios ? y * por enemigos (o vacío) según la receta.</summary>
        private static void PlaceEnemies(string[] map, DreamRecipe r, Random rng)
        {
            float fill = 0.35f + r.dificultad * 0.2f; // dificultad 1: 55 %, 3: 95 %
            float ground = r.enemigos.sombra + r.enemigos.vigia;
            for (int row = 0; row < map.Length; row++)
            {
                var chars = map[row].ToCharArray();
                for (int x = 0; x < chars.Length; x++)
                {
                    if (chars[x] == '?')
                    {
                        bool place = rng.NextDouble() < fill * Math.Min(1f, ground * 1.5f);
                        bool shooter = rng.NextDouble() * ground < r.enemigos.vigia;
                        chars[x] = place ? (shooter ? 'S' : 'E') : '.';
                    }
                    else if (chars[x] == '*')
                        chars[x] = rng.NextDouble() < fill * Math.Min(1f, r.enemigos.polilla * 2f) ? 'F' : '.';
                }
                map[row] = new string(chars);
            }
        }
    }
}
