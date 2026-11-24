using System.Collections;
using EternalSleep.UI;
using TMPro;
using UnityEngine;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Cartel de presentación del jefe: su nombre en grande, en rosa de peligro, con las líneas y el rombo
    /// del título de zona. Sale mientras el jefe ruge, antes de que aparezca su barra de vida.
    /// </summary>
    public class BossIntroCard : MonoBehaviour
    {
        private static readonly Color Danger = new Color(0.91f, 0.42f, 0.57f);
        private static readonly Color Dim = new Color(0.6f, 0.55f, 0.8f);

        private static GameObject current;

        public static void Show(string bossName)
        {
            if (current != null) Destroy(current);
            var canvas = RuntimeUI.Canvas("PresentacionDeJefe", null, 810);
            current = canvas.gameObject;
            canvas.gameObject.AddComponent<BossIntroCard>().StartCoroutine(Run(canvas, bossName.ToUpperInvariant()));
        }

        private static IEnumerator Run(Canvas canvas, string bossName)
        {
            var root = canvas.transform;
            const float y = 40f;
            var shadeColor = new Color(0.04f, 0.01f, 0.04f, 0f);

            var band = RuntimeUI.Panel("Banda", root, shadeColor);
            band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            band.rectTransform.anchoredPosition = new Vector2(0f, y);
            band.rectTransform.sizeDelta = new Vector2(0f, 70f);

            var name = RuntimeUI.Text("Nombre", root, y + 4f, 30f, Danger, bossName);
            name.fontStyle = FontStyles.Bold;
            name.alpha = 0f;

            var small = RuntimeUI.Text("Rotulo", root, y - 24f, 9f, Dim, "ALGO VELA AQUÍ");
            small.characterSpacing = 14f;
            small.alpha = 0f;

            var top = Line(root, y + 33f);
            var bottom = Line(root, y - 37f);

            const float inTime = 0.6f, hold = 1.5f, outTime = 0.7f;
            const float lineMax = 260f;

            for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / inTime);
                band.color = new Color(shadeColor.r, shadeColor.g, shadeColor.b, 0.6f * k);
                name.alpha = k;
                name.characterSpacing = Mathf.Lerp(60f, 14f, k); // las letras se cierran de golpe
                small.alpha = Mathf.Clamp01(k * 2f - 0.8f);
                top.sizeDelta = bottom.sizeDelta = new Vector2(lineMax * k, 1f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Ease(t / outTime);
                band.color = new Color(shadeColor.r, shadeColor.g, shadeColor.b, 0.6f * k);
                name.alpha = small.alpha = k;
                top.sizeDelta = bottom.sizeDelta = new Vector2(lineMax * k, 1f);
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
            var img = RuntimeUI.Panel("Linea", parent, Danger);
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(0f, 1f);
            return r;
        }
    }
}
