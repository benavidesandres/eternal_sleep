using System;
using System.Collections.Generic;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Comprueba que un mapa de texto tiene solución: que desde P se puede llegar a G saltando.
    /// Usa los alcances de Kael (README): salta 3 casillas de alto y 5 de largo; con doble salto, 6 y 9.
    /// Aquí se cuenta un poco menos (margen de seguridad) para que nunca salga un salto al límite.
    /// Busca por las casillas donde se puede estar de pie (suelo o plataforma debajo, hueco de 2 de alto).
    /// </summary>
    public static class DreamValidator
    {
        public static bool IsSolvable(string[] map, bool canDoubleJump)
        {
            if (map == null || map.Length == 0) return false;
            var grid = new Grid(map);
            if (!grid.Find('P', out var start) || !grid.Find('G', out var goal)) return false;

            int up = canDoubleJump ? 5 : 3;
            int across = canDoubleJump ? 7 : 4;

            var seen = new HashSet<(int, int)> { start };
            var open = new Queue<(int x, int y)>();
            open.Enqueue(start);
            while (open.Count > 0)
            {
                var (x, y) = open.Dequeue();
                if (Math.Abs(x - goal.x) <= 1 && Math.Abs(y - goal.y) <= 1) return true;

                for (int dy = up; dy >= -grid.H; dy--)
                {
                    int reach = across + (dy < 0 ? -dy / 2 : 0); // cayendo se avanza más
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        int tx = x + dx, ty = y + dy;
                        if (!grid.Standable(tx, ty) || seen.Contains((tx, ty))) continue;
                        if (!grid.ClearJump(x, y, tx, ty)) continue;
                        seen.Add((tx, ty));
                        open.Enqueue((tx, ty));
                    }
                }
            }
            return false;
        }

        private class Grid
        {
            private readonly string[] rows;
            public readonly int W, H;

            public Grid(string[] map)
            {
                rows = map;
                H = map.Length;
                W = 0;
                foreach (var r in map) W = Math.Max(W, r.Length);
            }

            /// <summary>Carácter en (x, y), con y = 0 abajo. Fuera por los lados es pared; por arriba y abajo, vacío.</summary>
            private char At(int x, int y)
            {
                if (x < 0 || x >= W) return '#';
                if (y < 0 || y >= H) return '.';
                string r = rows[H - 1 - y];
                return x < r.Length ? r[x] : '.';
            }

            private bool Solid(int x, int y) => At(x, y) == '#';

            public bool Standable(int x, int y)
            {
                if (y < 1 || y >= H || x < 0 || x >= W) return false;
                char here = At(x, y), below = At(x, y - 1);
                return here != '#' && here != '^' && !Solid(x, y + 1) && (below == '#' || below == '=');
            }

            /// <summary>El cuerpo (2 de alto) cabe por el camino del salto: sube, cruza por arriba y baja.</summary>
            public bool ClearJump(int x0, int y0, int x1, int y1)
            {
                int top = Math.Max(y0, y1);
                for (int y = y0; y <= top + 1; y++) if (Solid(x0, y)) return false;
                for (int y = y1; y <= top + 1; y++) if (Solid(x1, y)) return false;
                int step = Math.Sign(x1 - x0);
                for (int x = x0; x != x1; x += step)
                    if (Solid(x, top) || Solid(x, top + 1)) return false;
                return true;
            }

            public bool Find(char c, out (int x, int y) pos)
            {
                for (int row = 0; row < H; row++)
                {
                    int x = rows[row].IndexOf(c);
                    if (x < 0) continue;
                    pos = (x, H - 1 - row);
                    return true;
                }
                pos = default;
                return false;
            }
        }
    }
}
