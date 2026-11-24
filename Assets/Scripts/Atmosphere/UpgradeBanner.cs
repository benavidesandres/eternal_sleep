using System.Collections;
using EternalSleep.Player;
using EternalSleep.UI;
using TMPro;
using UnityEngine;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Cartel de "mejora obtenida": una banda oscura en la parte alta con el nombre de la mejora y la
    /// tecla para usarla. Mismo estilo que el título de zona: líneas finas, rombo y letras que se juntan.
    /// </summary>
    public class UpgradeBanner : MonoBehaviour
    {
        private static readonly Color Ink = new Color(0.85f, 0.8f, 1f);
        private static readonly Color Dim = new Color(0.6f, 0.55f, 0.8f);
        private static readonly Color Gold = new Color(1f, 0.89f, 0.54f);

        private static GameObject current;

        public static void Show(Upgrade upgrade)
        {
            Info(upgrade, out string name, out string hint);
            if (current != null) Destroy(current); // si llegan dos seguidas, la nueva reemplaza a la vieja

            var canvas = RuntimeUI.Canvas("CartelDeMejora", null, 820);
            current = canvas.gameObject;
            canvas.gameObject.AddComponent<UpgradeBanner>().StartCoroutine(Run(canvas, name.ToUpperInvariant(), hint));
        }

        private static void Info(Upgrade upgrade, out string name, out string hint)
        {
            switch (upgrade)
            {
                case Upgrade.DoubleJump: name = "Alas de Polilla"; hint = "Saltar de nuevo en el aire  ·  Espacio  /  A"; break;
                case Upgrade.Dash: name = "Paso de Sombra"; hint = "Shift o K  /  RB"; break;
                case Upgrade.WallJump: name = "Garras de Hiedra"; hint = "Empuja contra la pared y salta  ·  Espacio"; break;
                case Upgrade.GroundSlam: name = "Peso del Sueño"; hint = "En el aire: abajo + Shift o K  /  abajo + RB"; break;
                case Upgrade.ChargedAttack: name = "Aliento Contenido"; hint = "Mantén atacar (J, Z o clic) y suelta al brillar"; break;
                default: name = "Corazón de Luna"; hint = "+1 corazón de vida máxima"; break;
            }
            hint = hint ?? "";
        }

        private static IEnumerator Run(Canvas canvas, string name, string hint)
        {
            var root = canvas.transform;
            const float y = 95f;
            var shadeColor = new Color(0.02f, 0.015f, 0.05f, 0f);

            var band = RuntimeUI.Panel("Banda", root, shadeColor);
            band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            band.rectTransform.anchoredPosition = new Vector2(0f, y);
            band.rectTransform.sizeDelta = new Vector2(0f, 76f);

            var small = RuntimeUI.Text("Rotulo", root, y + 22f, 9f, Dim, "MEJORA OBTENIDA");
            small.characterSpacing = 14f;
            small.alpha = 0f;

            var nameText = RuntimeUI.Text("Nombre", root, y + 1f, 20f, Gold, name);
            nameText.fontStyle = FontStyles.Bold;
            nameText.alpha = 0f;

            var hintText = RuntimeUI.Text("Tecla", root, y - 22f, 9f, Ink, hint);
            hintText.characterSpacing = 3f;
            hintText.alpha = 0f;

            var top = Line(root, y + 38f);
            var bottom = Line(root, y - 38f);

            var diamond = RuntimeUI.Panel("Rombo", root, Gold);
            diamond.rectTransform.anchorMin = diamond.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            diamond.rectTransform.sizeDelta = new Vector2(7f, 7f);
            diamond.rectTransform.anchoredPosition = new Vector2(0f, y + 38f);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            diamond.transform.localScale = Vector3.zero;

            const float inTime = 0.7f, hold = 3.8f, outTime = 0.8f;
            const float lineMax = 240f;

            for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / inTime);
                band.color = new Color(shadeColor.r, shadeColor.g, shadeColor.b, 0.6f * k);
                small.alpha = k;
                nameText.alpha = k;
                nameText.characterSpacing = Mathf.Lerp(36f, 8f, k);
                nameText.color = Color.Lerp(Color.white, Gold, k); // entra como un destello
                hintText.alpha = Mathf.Clamp01(k * 2f - 0.8f);
                top.sizeDelta = bottom.sizeDelta = new Vector2(lineMax * k, 1f);
                diamond.transform.localScale = Vector3.one * Mathf.Clamp01(k * 2f - 0.4f);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Ease(t / outTime);
                band.color = new Color(shadeColor.r, shadeColor.g, shadeColor.b, 0.6f * k);
                small.alpha = nameText.alpha = hintText.alpha = k;
                top.sizeDelta = bottom.sizeDelta = new Vector2(lineMax * k, 1f);
                diamond.transform.localScale = Vector3.one * k;
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
            r.anchoredPosition = new Vector2(0f, y);
            r.sizeDelta = new Vector2(0f, 1f);
            return r;
        }
    }
}
