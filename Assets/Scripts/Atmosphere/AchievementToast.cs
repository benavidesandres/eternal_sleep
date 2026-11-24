using System.Collections;
using System.Collections.Generic;
using EternalSleep.Effects;
using EternalSleep.UI;
using UnityEngine;
using UnityEngine.UI;

namespace EternalSleep.Atmosphere
{
    /// <summary>
    /// Aviso de logro: una tarjeta oscura que entra deslizándose por la esquina de arriba a la derecha,
    /// con el nombre del logro en dorado. Si se desbloquean varios a la vez, salen uno detrás de otro.
    /// Se crea solo al arrancar y sobrevive a los cambios de escena.
    /// </summary>
    public class AchievementToast : MonoBehaviour
    {
        private static readonly Color Gold = new Color(1f, 0.89f, 0.54f);
        private static readonly Color Dim = new Color(0.6f, 0.55f, 0.8f);

        private readonly Queue<Achievements.Def> queue = new Queue<Achievements.Def>();
        private bool showing;
        private Canvas canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("AchievementToast");
            DontDestroyOnLoad(go);
            go.AddComponent<AchievementToast>();
        }

        private void OnEnable() => Achievements.Unlocked += OnUnlocked;
        private void OnDisable() => Achievements.Unlocked -= OnUnlocked;

        private void OnUnlocked(Achievements.Def def)
        {
            queue.Enqueue(def);
            if (!showing) StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            showing = true;
            if (canvas == null) canvas = RuntimeUI.Canvas("LogroCanvas", transform, 830);

            while (queue.Count > 0)
            {
                var def = queue.Dequeue();
                yield return Show(def);
            }
            showing = false;
        }

        private IEnumerator Show(Achievements.Def def)
        {
            const float width = 200f, height = 38f, margin = 8f;

            // La tarjeta se ancla arriba a la derecha; empieza fuera de la pantalla y entra deslizándose.
            var card = RuntimeUI.Panel("Tarjeta", canvas.transform, new Color(0.03f, 0.02f, 0.07f, 0.85f));
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            Vector2 hidden = new Vector2(width + 4f, -margin);
            Vector2 shown = new Vector2(-margin, -margin);
            rt.anchoredPosition = hidden;

            var edge = RuntimeUI.Panel("Borde", card.transform, Gold);
            edge.rectTransform.anchorMin = Vector2.zero;
            edge.rectTransform.anchorMax = new Vector2(0f, 1f);
            edge.rectTransform.sizeDelta = new Vector2(2f, 0f);
            edge.rectTransform.pivot = new Vector2(0f, 0.5f);
            edge.rectTransform.offsetMin = Vector2.zero;
            edge.rectTransform.offsetMax = new Vector2(2f, 0f);

            var small = RuntimeUI.Text("Rotulo", card.transform, 9f, 7f, Dim, "LOGRO DESBLOQUEADO");
            var name = RuntimeUI.Text("Nombre", card.transform, -5f, 11f, Gold, def.nombre);
            small.characterSpacing = 6f;
            small.rectTransform.sizeDelta = new Vector2(width - 8f, 12f);
            name.rectTransform.sizeDelta = new Vector2(width - 8f, 16f);

            AudioManager.Play("mejora", 0f);

            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / 0.4f, 3f);
                rt.anchoredPosition = Vector2.Lerp(hidden, shown, k);
                yield return null;
            }
            rt.anchoredPosition = shown;

            yield return new WaitForSecondsRealtime(3f);

            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Pow(t / 0.4f, 2f);
                rt.anchoredPosition = Vector2.Lerp(shown, hidden, k);
                yield return null;
            }
            Destroy(card.gameObject);
        }
    }
}
