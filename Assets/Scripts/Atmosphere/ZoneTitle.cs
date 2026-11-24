using System.Collections;
using EternalSleep.Dream;
using EternalSleep.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Título de zona al entrar a un lugar: una línea fina que se abre desde el centro, el nombre en
    /// letras que se van juntando, un rombo y una frase pequeña debajo. Todo en lila pálido sobre una
    /// sombra oscura, como el resto de la interfaz del juego.
    /// </summary>
    public class ZoneTitle : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.85f, 0.8f, 1f);
        private static readonly Color Dim = new Color(0.6f, 0.55f, 0.8f);

        public static void ShowFor(string scene)
        {
            Names(scene, out string title, out string subtitle);
            if (string.IsNullOrEmpty(title)) return;

            var canvas = RuntimeUI.Canvas("TituloDeZona", null, 800);
            canvas.gameObject.AddComponent<ZoneTitle>().StartCoroutine(Run(canvas, title.ToUpperInvariant(), subtitle));
        }

        /// <summary>Nombre de una zona (null si la escena no tiene).</summary>
        public static string NameOf(string scene)
        {
            Names(scene, out string title, out _);
            return title;
        }

        private static void Names(string scene, out string title, out string subtitle)
        {
            subtitle = null;
            switch (scene)
            {
                case "Hub": title = "El Pueblo Dormido"; subtitle = "Aquí nadie despierta"; break;
                case "Level_01": title = "Bosque de los Susurros"; subtitle = "Primer sueño"; break;
                case "Level_02": title = "Pantano Hundido"; subtitle = "Segundo sueño"; break;
                case "Level_03": title = "Cueva del Eco"; subtitle = "Tercer sueño"; break;
                case "Level_04": title = "Catedral sin Nombre"; subtitle = "Cuarto sueño"; break;
                case "Level_05": title = "Biblioteca Olvidada"; subtitle = "Quinto sueño"; break;
                case "Level_06": title = "Calles del Último Sueño"; subtitle = "Sexto sueño"; break;
                case "Level_07": title = "El Umbral"; subtitle = "Séptimo sueño"; break;
                case "Level_08": title = "El Fondo del Sueño"; subtitle = "Octavo sueño"; break;
                case "Arena": title = "La Arena del Insomnio"; subtitle = "Algo vela aquí"; break;
                case "Sueno":
                    var r = DreamWorldManager.CurrentRecipe;
                    title = r != null && !string.IsNullOrWhiteSpace(r.titulo) ? r.titulo : "Un Sueño Ajeno";
                    subtitle = r != null ? "Pesadilla de " + r.tema : "Pesadilla";
                    break;
                default: title = null; break;
            }
        }

        private static IEnumerator Run(Canvas canvas, string title, string subtitle)
        {
            var root = canvas.transform;
            const float baseY = -70f;

            // Sombra tenue detrás para que se lea sobre cualquier fondo.
            var shade = RuntimeUI.Panel("Sombra", root, new Color(0.02f, 0.015f, 0.05f, 0f));
            shade.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            shade.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            shade.rectTransform.anchoredPosition = new Vector2(0f, baseY - 6f);
            shade.rectTransform.sizeDelta = new Vector2(0f, 90f);

            var nameText = RuntimeUI.Text("Nombre", root, baseY + 12f, 22f, Ink, title);
            nameText.alpha = 0f;
            nameText.characterSpacing = 40f;
            nameText.fontStyle = FontStyles.Bold;

            TMP_Text sub = null;
            if (!string.IsNullOrEmpty(subtitle))
            {
                sub = RuntimeUI.Text("Frase", root, baseY - 22f, 10f, Dim, subtitle);
                sub.alpha = 0f;
                sub.characterSpacing = 6f;
            }

            var lineL = Line(root, baseY - 4f);
            var lineR = Line(root, baseY - 4f);
            lineL.pivot = new Vector2(1f, 0.5f);
            lineR.pivot = new Vector2(0f, 0.5f);
            lineL.anchoredPosition = new Vector2(-7f, baseY - 4f);
            lineR.anchoredPosition = new Vector2(7f, baseY - 4f);

            var diamond = RuntimeUI.Panel("Rombo", root, Ink);
            diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            diamond.rectTransform.sizeDelta = new Vector2(6f, 6f);
            diamond.rectTransform.anchoredPosition = new Vector2(0f, baseY - 4f);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            diamond.transform.localScale = Vector3.zero;

            // Espera a que pase el fundido de entrada y se asiente la escena.
            yield return new WaitForSecondsRealtime(0.9f);

            float maxLine = Mathf.Clamp(title.Length * 7f + 30f, 120f, 280f);
            const float inTime = 1.4f, hold = 1.8f, outTime = 1.1f;

            for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / inTime);
                shade.color = new Color(0.02f, 0.015f, 0.05f, 0.45f * k);
                nameText.alpha = k;
                nameText.characterSpacing = Mathf.Lerp(40f, 10f, k); // las letras se van juntando
                lineL.sizeDelta = lineR.sizeDelta = new Vector2(maxLine * k, 1f);
                diamond.transform.localScale = Vector3.one * Mathf.Clamp01(k * 2f - 0.3f);
                if (sub != null) sub.alpha = Mathf.Clamp01(k * 2f - 0.8f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Ease(t / outTime);
                shade.color = new Color(0.02f, 0.015f, 0.05f, 0.45f * k);
                nameText.alpha = k;
                nameText.characterSpacing = Mathf.Lerp(10f, 14f, 1f - k);
                lineL.sizeDelta = lineR.sizeDelta = new Vector2(maxLine * k, 1f);
                diamond.transform.localScale = Vector3.one * k;
                if (sub != null) sub.alpha = k;
                yield return null;
            }

            Destroy(canvas.gameObject);
        }

        private static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static RectTransform Line(Transform parent, float y)
        {
            var img = RuntimeUI.Panel("Linea", parent, Dim);
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(0f, 1f);
            return r;
        }
    }
}
