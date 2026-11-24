using System.Collections.Generic;
using EternalSleep.Dream;
using EternalSleep.Village;
using TMPro;
using UnityEngine;

namespace EternalSleep.UI
{
    /// <summary>Página de la pausa con las estadísticas de la partida (tiempo, golpes, muertes, jefes...).</summary>
    public class StatsPanel : IPausePage
    {
        private const string Label = "<color=#9E8FD1>";
        private const string Value = "<color=#FFE38A>";

        private readonly GameObject root;
        private readonly List<TMP_Text> rows = new List<TMP_Text>();

        public string Heading => "ESTADÍSTICAS";
        public string Help => "Escape volver";

        public StatsPanel(Transform parent)
        {
            root = new GameObject("Estadisticas", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            for (int i = 0; i < 7; i++)
                rows.Add(RuntimeUI.Text("Fila" + i, root.transform, 30f - i * 20f, 12f, new Color(0.85f, 0.8f, 1f), ""));
            root.SetActive(false);
        }

        public void Show()
        {
            Set(0, "Tiempo jugado", PlayStats.FormatTime());
            Set(1, "Golpes dados", PlayStats.HitsDealt.ToString());
            Set(2, "Golpes recibidos", PlayStats.HitsTaken.ToString());
            Set(3, "Veces que caíste", PlayStats.Deaths.ToString());
            Set(4, "Jefes vencidos", PlayStats.BossesDefeated.ToString());
            Set(5, "Lágrimas de Luna", $"{CollectibleTracker.Count} / {CollectibleTracker.Total}");
            Set(6, "Sueños completados", DreamDiary.Entries.Count.ToString());
            root.SetActive(true);
        }

        public void Hide() => root.SetActive(false);

        public void Scroll(int direction) { }

        private void Set(int row, string label, string value) =>
            rows[row].text = $"{Label}{label}</color>    {Value}{value}</color>";
    }
}
