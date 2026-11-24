using System.Collections.Generic;
using EternalSleep.Dream;
using TMPro;
using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>
    /// Página del diario de sueños dentro de la pausa: lista los sueños completados con su título,
    /// el tema y quién los soñaba. Con más de una pantalla de sueños se desplaza con W/S.
    /// </summary>
    public class DreamDiaryPanel
    {
        private const int Visible = 5;
        private static readonly Color TitleColor = new Color(1f, 0.89f, 0.54f);
        private static readonly Color Ink = new Color(0.85f, 0.8f, 1f);
        private static readonly Color Dim = new Color(0.6f, 0.55f, 0.8f);

        private readonly GameObject root;
        private readonly List<TMP_Text> titles = new List<TMP_Text>();
        private readonly List<TMP_Text> details = new List<TMP_Text>();
        private readonly TMP_Text empty;
        private readonly TMP_Text counter;
        private int offset;

        public DreamDiaryPanel(Transform parent)
        {
            root = new GameObject("Diario", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            for (int i = 0; i < Visible; i++)
            {
                float y = 22f - i * 30f;
                titles.Add(RuntimeUI.Text("TituloSueno" + i, root.transform, y, 12f, TitleColor, ""));
                details.Add(RuntimeUI.Text("DetalleSueno" + i, root.transform, y - 12f, 8f, Dim, ""));
            }
            empty = RuntimeUI.Text("Vacio", root.transform, 0f, 11f, Dim, "Aún no has despertado a nadie.\nTus sueños completados aparecerán aquí.");
            counter = RuntimeUI.Text("Contador", root.transform, -128f, 8f, Dim, "");
            root.SetActive(false);
        }

        public static int Count => DreamDiary.Entries.Count;
        public bool IsMoreBelow => offset + Visible < Count;

        public void Show()
        {
            offset = 0;
            root.SetActive(true);
            Refresh();
        }

        public void Hide() => root.SetActive(false);

        /// <summary>Baja (+1) o sube (-1) una fila, sin pasarse de los extremos.</summary>
        public void Scroll(int direction)
        {
            int max = Mathf.Max(0, Count - Visible);
            int next = Mathf.Clamp(offset + direction, 0, max);
            if (next == offset) return;
            offset = next;
            Refresh();
        }

        private void Refresh()
        {
            var list = DreamDiary.Entries;
            empty.gameObject.SetActive(list.Count == 0);
            for (int i = 0; i < Visible; i++)
            {
                int index = offset + i;
                bool has = index < list.Count;
                titles[i].gameObject.SetActive(has);
                details[i].gameObject.SetActive(has);
                if (!has) continue;

                var e = list[index];
                titles[i].text = $"{index + 1}.  {e.titulo}";
                string who = string.IsNullOrEmpty(e.aldeano) ? "" : "sueño de " + e.aldeano;
                string theme = string.IsNullOrEmpty(e.tema) ? "" : Capitalize(e.tema);
                details[i].text = theme.Length > 0 && who.Length > 0 ? $"{theme}  -  {who}" : theme + who;
                details[i].color = Dim;
                titles[i].color = i % 2 == 0 ? TitleColor : Color.Lerp(TitleColor, Ink, 0.35f);
            }
            counter.text = list.Count == 0 ? "" : $"{Mathf.Min(offset + 1, list.Count)}-{Mathf.Min(offset + Visible, list.Count)} de {list.Count} sueños";
        }

        private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
