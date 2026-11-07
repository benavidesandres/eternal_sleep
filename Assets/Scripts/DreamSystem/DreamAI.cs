using System;
using System.Collections;
using System.Collections.Generic;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace EternalSleep.Dream
{
    /// <summary>
    /// Pide a la IA (Google Gemini, a través de nuestro servidor gratuito de Cloudflare) la receta de un sueño.
    /// - La dirección del servidor está en Assets/Resources/Dreams/IA.json. Sin dirección, no se usa la IA.
    /// - El jugador puede apagarla en Opciones ("Sueños con IA").
    /// - Prefetch() la pide en segundo plano mientras el aldeano habla; Get() espera como mucho unos segundos.
    /// - Nunca lanza errores: si algo falla (sin internet, límite gratuito agotado, respuesta rara) devuelve null
    ///   y el juego usa la receta sin IA.
    /// Solo se envía: nombre del aldeano, sus murmullos, cuántos aldeanos despertaste y la semilla.
    /// </summary>
    public static class DreamAI
    {
        [Serializable]
        private class Config
        {
            public string url = "";
            public bool activado = true;
            public float espera = 4f;
        }

        /// <summary>Lo que se le cuenta a la IA sobre el sueño.</summary>
        [Serializable]
        public class Request
        {
            public string aldeano;
            public string[] murmullos;
            public int despiertos;
            public int semilla;
            public string[] temas = DreamRecipe.Themes;
        }

        private static Config config;
        private static readonly Dictionary<string, DreamRecipe> cache = new Dictionary<string, DreamRecipe>();
        private static readonly HashSet<string> pending = new HashSet<string>();

        private static Config Settings
        {
            get
            {
                if (config != null) return config;
                var text = Resources.Load<TextAsset>("Dreams/IA");
                try { config = text != null ? JsonUtility.FromJson<Config>(text.text) : new Config(); }
                catch (Exception) { config = new Config(); }
                return config;
            }
        }

        public static bool Enabled =>
            Settings.activado && !string.IsNullOrWhiteSpace(Settings.url) && GameSettings.AIDreams &&
            Application.internetReachability != NetworkReachability.NotReachable;

        private static string Key(Request r) => r.aldeano + "#" + r.semilla;

        /// <summary>Empieza a pedir la receta sin esperar (al hablar con el aldeano).</summary>
        public static void Prefetch(MonoBehaviour runner, Request request)
        {
            if (!Enabled || runner == null) return;
            string key = Key(request);
            if (cache.ContainsKey(key) || pending.Contains(key)) return;
            pending.Add(key);
            runner.StartCoroutine(Send(request, key));
        }

        /// <summary>Espera la receta (como mucho "espera" segundos) y la entrega, o null si no llegó.</summary>
        public static IEnumerator Get(MonoBehaviour runner, Request request, Action<DreamRecipe> done)
        {
            if (!Enabled) { done(null); yield break; }
            string key = Key(request);
            Prefetch(runner, request);
            float until = Time.realtimeSinceStartup + Mathf.Clamp(Settings.espera, 0.5f, 10f);
            while (pending.Contains(key) && Time.realtimeSinceStartup < until) yield return null;
            done(cache.TryGetValue(key, out var recipe) ? recipe : null);
        }

        private static IEnumerator Send(Request request, string key)
        {
            using (var web = new UnityWebRequest(Settings.url.Trim(), UnityWebRequest.kHttpVerbPOST))
            {
                byte[] body = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));
                web.uploadHandler = new UploadHandlerRaw(body);
                web.downloadHandler = new DownloadHandlerBuffer();
                web.SetRequestHeader("Content-Type", "application/json");
                web.timeout = Mathf.CeilToInt(Mathf.Clamp(Settings.espera, 0.5f, 10f)) + 4;
                yield return web.SendWebRequest();

                if (web.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var recipe = JsonUtility.FromJson<DreamRecipe>(web.downloadHandler.text);
                        if (recipe != null)
                        {
                            recipe.fromAI = true;
                            cache[key] = recipe;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("[Eternal Sleep] La IA devolvió algo que no se pudo leer; se usa el sueño sin IA. " + e.Message);
                    }
                }
                else Debug.Log($"[Eternal Sleep] Sin respuesta de la IA ({web.error}); se usa el sueño sin IA.");
            }
            pending.Remove(key);
        }
    }
}
