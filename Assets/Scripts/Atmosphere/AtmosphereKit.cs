using EternalSleep.Dream;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Arranca solo las luciérnagas y el título de zona en cada escena de juego
    /// (no hace falta tocar ninguna escena) y decide los colores según el lugar.
    /// </summary>
    public static class AtmosphereKit
    {
        public struct Mood
        {
            public Color firefly;
            public Color mist;
            public float mistAmount;
            public int fireflies;
            public float vignette;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (scene.name == "MainMenu" || scene.name == "SampleScene") return;
            if (Object.FindAnyObjectByType<Camera>() == null) return;

            var go = new GameObject("Atmosfera");
            go.AddComponent<AtmosphereDirector>().Setup(scene.name);
        }

        /// <summary>Colores por escena. Los sueños generados usan la luz de su receta.</summary>
        public static Mood MoodFor(string scene)
        {
            var mood = BaseMood(scene);
            // En el pueblo, que es de día y de paso, el borde es más suave; en la arena del jefe, más cerrado.
            mood.vignette = scene == "Hub" ? 0.3f : scene == "Arena" ? 0.62f : 0.5f;
            return mood;
        }

        private static Mood BaseMood(string scene)
        {
            switch (scene)
            {
                case "Hub": return Make(new Color(1f, 0.85f, 0.45f), new Color(0.55f, 0.5f, 0.75f), 0.16f, 26);
                case "Level_01": return Make(new Color(0.65f, 1f, 0.55f), new Color(0.5f, 0.7f, 0.55f), 0.16f, 30);
                case "Level_02": return Make(new Color(0.55f, 1f, 0.8f), new Color(0.45f, 0.65f, 0.55f), 0.24f, 26);
                case "Level_03": return Make(new Color(0.55f, 0.85f, 1f), new Color(0.4f, 0.5f, 0.7f), 0.18f, 22);
                case "Level_04": return Make(new Color(0.8f, 0.65f, 1f), new Color(0.6f, 0.5f, 0.8f), 0.14f, 22);
                case "Level_05": return Make(new Color(1f, 0.8f, 0.5f), new Color(0.7f, 0.6f, 0.5f), 0.12f, 20);
                case "Level_06": return Make(new Color(1f, 0.85f, 0.5f), new Color(0.55f, 0.5f, 0.7f), 0.16f, 24);
                case "Arena": return Make(new Color(0.85f, 0.6f, 1f), new Color(0.5f, 0.4f, 0.7f), 0.12f, 12);
                case "Sueno": return FromRecipe();
                default: return Make(new Color(0.8f, 0.75f, 1f), new Color(0.55f, 0.5f, 0.75f), 0.14f, 22);
            }
        }

        private static Mood FromRecipe()
        {
            var r = DreamWorldManager.CurrentRecipe;
            if (r == null) return Make(new Color(0.8f, 0.75f, 1f), new Color(0.55f, 0.5f, 0.75f), 0.14f, 22);
            Color light = r.LightColor;
            return Make(Color.Lerp(light, Color.white, 0.25f), Color.Lerp(light, new Color(0.4f, 0.4f, 0.6f), 0.4f),
                        Mathf.Clamp(0.08f + r.niebla * 0.3f, 0.08f, 0.28f), 24);
        }

        private static Mood Make(Color firefly, Color mist, float mistAmount, int count) =>
            new Mood { firefly = firefly, mist = mist, mistAmount = mistAmount, fireflies = count };
    }

    /// <summary>Crea los efectos de la escena; todo se destruye al cambiar de escena.</summary>
    public class AtmosphereDirector : MonoBehaviour
    {
        public void Setup(string scene)
        {
            var mood = AtmosphereKit.MoodFor(scene);
            gameObject.AddComponent<Fireflies>().Init(mood.firefly, mood.fireflies);
            ScreenVignette.Create(transform, mood.vignette);
            ZoneTitle.ShowFor(scene);
        }
    }
}
