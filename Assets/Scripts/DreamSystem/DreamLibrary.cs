using System.Collections.Generic;
using UnityEngine;

namespace EternalSleep.Dream
{
    /// <summary>Los tramos de Assets/Resources/Dreams/Tramos.txt, leídos una sola vez.</summary>
    public static class DreamLibrary
    {
        private static List<DreamGenerator.Chunk> chunks;

        public static List<DreamGenerator.Chunk> Chunks
        {
            get
            {
                if (chunks != null) return chunks;
                var text = Resources.Load<TextAsset>("Dreams/Tramos");
                chunks = text != null ? DreamGenerator.Parse(text.text) : new List<DreamGenerator.Chunk>();
                if (chunks.Count == 0) Debug.LogWarning("[Eternal Sleep] No se encontraron tramos en Resources/Dreams/Tramos.txt.");
                return chunks;
            }
        }
    }
}
