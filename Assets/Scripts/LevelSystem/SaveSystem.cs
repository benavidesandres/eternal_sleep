using System.Collections.Generic;
using System.IO;
using EternalSleep.Dream;
using EternalSleep.Player;
using EternalSleep.Village;
using UnityEngine;

namespace EternalSleep
{
    /// <summary>Lo que se guarda de una partida (en JSON).</summary>
    [System.Serializable]
    public class SaveData
    {
        public int partida;
        public List<string> despiertos = new List<string>();
        public bool finalVisto;
        public List<string> frasesId = new List<string>();
        public List<string> frasesTexto = new List<string>();
        public List<string> mejoras = new List<string>();
        public List<string> recogidosMejoras = new List<string>();
        public int recompensasDespertar;
        public int corazonesExtra;
        public List<string> lagrimas = new List<string>();
        public List<string> secretos = new List<string>();
        public List<string> diarioTitulos = new List<string>();
        public List<string> diarioTemas = new List<string>();
        public List<string> diarioAldeanos = new List<string>();
        public int estMuertes;
        public int estGolpes;
        public int estRecibidos;
        public int estJefes;
        public float estSegundos;
        public List<string> logros = new List<string>();
    }

    /// <summary>
    /// Guarda la partida en un archivo JSON (Application.persistentDataPath/partida.json).
    /// Se guarda al volver al pueblo y al recoger algo fuera de un sueño;
    /// dentro de un sueño no se guarda, así que salir a medias equivale a abandonarlo.
    /// </summary>
    public static class SaveSystem
    {
        private static string FilePath => Path.Combine(Application.persistentDataPath, "partida.json");

        public static bool HasSave => File.Exists(FilePath);

        public static void Save()
        {
            var data = new SaveData { partida = DreamSeed.Game };
            DreamWorldManager.WriteTo(data);
            PlayerUpgrades.WriteTo(data);
            CollectibleTracker.WriteTo(data);
            PlayStats.WriteTo(data);
            Achievements.WriteTo(data);
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data, true)); }
            catch (IOException e) { Debug.LogWarning("[Eternal Sleep] No se pudo guardar la partida: " + e.Message); }
        }

        /// <summary>Guarda solo si no estamos dentro de un sueño (ni en la arena del jefe).</summary>
        public static void SaveOutsideDream()
        {
            if (!DreamWorldManager.IsInDream) Save();
        }

        /// <summary>Carga la partida guardada sobre una sesión recién reiniciada. Devuelve false si no hay o está rota.</summary>
        public static bool Load()
        {
            if (!HasSave) return false;
            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)); }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Eternal Sleep] La partida guardada no se pudo leer: " + e.Message);
                return false;
            }
            if (data == null) return false;

            DreamSeed.ContinueGame(data.partida);
            DreamWorldManager.ReadFrom(data);
            PlayerUpgrades.ReadFrom(data);
            CollectibleTracker.ReadFrom(data);
            PlayStats.ReadFrom(data);
            Achievements.ReadFrom(data);
            return true;
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(FilePath);
        }
    }
}
