using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Página de la pausa con los logros: los desbloqueados en dorado y los pendientes apagados,
    /// cada uno con lo que hay que hacer. Con más de una pantalla se desplaza con W/S.
    /// </summary>
    public class AchievementsPanel : IPausePage
    {
        private const int Visible = 5;
        private static readonly Color Gold = new Color(1f, 0.89f, 0.54f);
        private static readonly Color Locked = new Color(0.45f, 0.4f, 0.6f);
        private static readonly Color Dim = new Color(0.6f, 0.55f, 0.8f);

        private readonly GameObject root;
        private readonly List<TMP_Text> names = new List<TMP_Text>();
        private readonly List<TMP_Text> details = new List<TMP_Text>();
        private readonly TMP_Text counter;
        private int offset;

        public string Heading => "LOGROS";
        public string Help => "W/S desplazar - Escape volver";

        public AchievementsPanel(Transform parent)
        {
            root = new GameObject("Logros", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            for (int i = 0; i < Visible; i++)
            {
                float y = 28f - i * 30f;
                names.Add(RuntimeUI.Text("NombreLogro" + i, root.transform, y, 12f, Gold, ""));
                details.Add(RuntimeUI.Text("DescripcionLogro" + i, root.transform, y - 12f, 8f, Dim, ""));
            }
            counter = RuntimeUI.Text("ContadorLogros", root.transform, -128f, 8f, Dim, "");
            root.SetActive(false);
        }

        public void Show()
        {
            offset = 0;
            root.SetActive(true);
            Refresh();
        }

        public void Hide() => root.SetActive(false);

        public void Scroll(int direction)
        {
            int max = Mathf.Max(0, Achievements.All.Length - Visible);
            int next = Mathf.Clamp(offset + direction, 0, max);
            if (next == offset) return;
            offset = next;
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < Visible; i++)
            {
                int index = offset + i;
                bool has = index < Achievements.All.Length;
                names[i].gameObject.SetActive(has);
                details[i].gameObject.SetActive(has);
                if (!has) continue;

                var def = Achievements.All[index];
                bool done = Achievements.IsUnlocked(def.id);
                names[i].text = done ? def.nombre : "- " + def.nombre + " -";
                names[i].color = done ? Gold : Locked;
                details[i].text = def.descripcion;
                details[i].color = done ? Dim : Locked;
            }
            counter.text = $"{Achievements.UnlockedCount} de {Achievements.All.Length} desbloqueados";
        }
    }
}
