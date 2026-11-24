using System.Collections.Generic;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Diario de sueños: un registro de las pesadillas que ya se completaron (cada vez que despierta un aldeano).
    /// Guarda el título del sueño, su tema y quién lo soñaba. Se guarda con la partida.
    /// </summary>
    public static class DreamDiary
    {
        public struct Entry
        {
            public string titulo;
            public string tema;    // vacío en los niveles hechos a mano
            public string aldeano;
        }

        private static readonly List<Entry> entries = new List<Entry>();

        public static IReadOnlyList<Entry> Entries => entries;

        public static void Add(string titulo, string tema, string aldeano)
        {
            if (string.IsNullOrWhiteSpace(titulo)) titulo = "Un sueño sin nombre";
            entries.Add(new Entry { titulo = titulo, tema = tema ?? "", aldeano = aldeano ?? "" });
        }

        public static void Clear() => entries.Clear();

        public static void WriteTo(SaveData data)
        {
            foreach (var e in entries)
            {
                data.diarioTitulos.Add(e.titulo);
                data.diarioTemas.Add(e.tema);
                data.diarioAldeanos.Add(e.aldeano);
            }
        }

        public static void ReadFrom(SaveData data)
        {
            entries.Clear();
            for (int i = 0; i < data.diarioTitulos.Count; i++)
                Add(data.diarioTitulos[i],
                    i < data.diarioTemas.Count ? data.diarioTemas[i] : "",
                    i < data.diarioAldeanos.Count ? data.diarioAldeanos[i] : "");
        }
    }
}
