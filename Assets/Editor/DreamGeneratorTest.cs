#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EternalSleep.Dream;
using UnityEditor;
using UnityEngine;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Menú: Eternal Sleep > Probar generador de sueños.
    /// Genera 300 sueños sin IA con semillas distintas y comprueba que todos tienen solución y que no se repiten.
    /// Guarda 3 ejemplos en Temp/SuenosDeEjemplo para mirarlos.
    /// </summary>
    public static class DreamGeneratorTest
    {
        private const string ChunksPath = "Assets/Resources/Dreams/Tramos.txt";

        [MenuItem("Eternal Sleep/Probar generador de sueños")]
        public static void Run()
        {
            if (!File.Exists(ChunksPath))
            {
                EditorUtility.DisplayDialog("Eternal Sleep", "No se encuentra " + ChunksPath, "OK");
                return;
            }
            var library = DreamGenerator.Parse(File.ReadAllText(ChunksPath));
            var seen = new HashSet<string>();
            int unsolvable = 0;
            const int count = 300;
            var rng = new System.Random();
            Directory.CreateDirectory("Temp/SuenosDeEjemplo");

            for (int i = 0; i < count; i++)
            {
                int seed = rng.Next();
                var recipe = DreamRecipe.Procedural(seed, i % 5);
                var map = DreamGenerator.Generate(recipe, seed, library, i % 2 == 0);
                if (!DreamValidator.IsSolvable(map, false)) unsolvable++;
                seen.Add(string.Join("\n", map));
                if (i < 3)
                    File.WriteAllText($"Temp/SuenosDeEjemplo/sueno_{i + 1}.txt",
                        $"{recipe.titulo} ({recipe.tema}, dificultad {recipe.dificultad})\n\n{string.Join("\n", map)}\n");
            }

            string result = $"{library.Count} tramos. {count} sueños generados: {seen.Count} distintos, " +
                            $"{unsolvable} sin solución (sin doble salto). Ejemplos en Temp/SuenosDeEjemplo.";
            if (unsolvable > 0) Debug.LogWarning("[Eternal Sleep] " + result);
            else Debug.Log("[Eternal Sleep] " + result);
            EditorUtility.DisplayDialog("Eternal Sleep", result, "OK");
        }
    }
}
#endif
