using System;
using System.Collections;
using EternalSleep.Dream;
using EternalSleep.Effects;
using EternalSleep.Player;
using EternalSleep.Village;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EternalSleep.UI
{
    /// <summary>
    /// Final del juego: cuando despierta el último aldeano, Vesna se despide, la pantalla se funde a negro,
    /// aparece el texto final y suben los créditos con lo que encontraste en la partida.
    /// Mantener aceptar los acelera. Al terminar vuelve al menú (la partida sigue guardada para seguir explorando).
    /// DreamWorldManager lo lanza con Play().
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        private const string Author = "Lhansqw";
        private const float CreditsSpeed = 22f; // unidades de la interfaz (640x360) por segundo

        private static readonly Color Pale = new Color(0.85f, 0.8f, 1f);
        private static readonly Color Soft = new Color(0.62f, 0.56f, 0.82f);
        private static readonly Color Gold = new Color(1f, 0.89f, 0.54f);

        private static readonly string[] Farewell =
        {
            "Kael... ¿lo oyes? Las campanas del pueblo están sonando.",
            "Por primera vez en años, nadie duerme en el pueblo.",
            "Las pesadillas se han quedado vacías. Ya puedes descansar tú también.",
        };

        public static bool IsPlaying { get; private set; }

        private InputAction confirm;

        public static void Play()
        {
            if (IsPlaying) return;
            new GameObject("Final").AddComponent<EndingSequence>();
        }

        private void Awake()
        {
            IsPlaying = true;
            confirm = new InputAction("EndingConfirm", InputActionType.Button);
            foreach (var b in new[] { "<Keyboard>/space", "<Keyboard>/enter", "<Keyboard>/j", "<Keyboard>/z",
                                      "<Gamepad>/buttonSouth", "<Mouse>/leftButton" })
                confirm.AddBinding(b);
            StartCoroutine(Run());
        }

        private void OnDestroy()
        {
            IsPlaying = false;
            confirm?.Dispose();
        }

        private IEnumerator Run()
        {
            var input = FindAnyObjectByType<PlayerInputReader>();
            if (input != null) input.enabled = false; // Kael se queda quieto

            // 1. Despedida.
            if (DialogueBox.Instance != null)
            {
                bool done = false;
                DialogueBox.Instance.Show("Vesna, la farolera", Farewell, null, _ => done = true);
                while (!done) yield return null;
            }

            // 2. Fundido a negro con la música del menú.
            AudioManager.OverrideMusic("musica_menu");
            var canvas = RuntimeUI.Canvas("FinalCanvas", transform, 950);
            var black = RuntimeUI.Panel("Negro", canvas.transform, new Color(0.03f, 0.02f, 0.05f, 0f));
            yield return Fade(black, 0f, 1f, 2f);

            // 3. Texto final.
            var line1 = RuntimeUI.Text("Final1", canvas.transform, 20f, 16f, Pale, "Y el pueblo, por fin, despertó.");
            var line2 = RuntimeUI.Text("Final2", canvas.transform, -10f, 11f, Soft, "Pero los sueños nunca se van del todo.");
            line1.fontStyle = line2.fontStyle = FontStyles.Italic;
            yield return FadeText(line1, 0f, 1f, 1.5f);
            yield return FadeText(line2, 0f, 1f, 1.5f);
            yield return Wait(3f);
            yield return FadeText(line1, 1f, 0f, 1f);
            line2.alpha = 0f;

            // 4. Créditos que suben.
            var credits = Credits(canvas.transform);
            credits.ForceMeshUpdate();
            float height = credits.preferredHeight;
            var rt = credits.rectTransform;
            rt.sizeDelta = new Vector2(600f, height);
            float y = -180f - height / 2f;          // empieza justo debajo de la pantalla
            float end = 180f + height / 2f;         // termina justo encima
            confirm.Enable();
            while (y < end)
            {
                float speed = confirm.IsPressed() ? CreditsSpeed * 4f : CreditsSpeed;
                y += speed * Time.unscaledDeltaTime;
                rt.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }
            confirm.Disable();

            var fin = RuntimeUI.Text("Fin", canvas.transform, 0f, 28f, Gold, "FIN");
            fin.characterSpacing = 12f;
            yield return FadeText(fin, 0f, 1f, 1.5f);
            yield return Wait(3f);
            yield return FadeText(fin, 1f, 0f, 1.5f);

            DreamWorldManager.ClearDream();
            SceneManager.LoadScene(0);
        }

        private static TMP_Text Credits(Transform parent)
        {
            int upgrades = 0, kinds = 0;
            foreach (Upgrade u in Enum.GetValues(typeof(Upgrade)))
            {
                kinds++;
                if (PlayerUpgrades.Has(u)) upgrades++;
            }

            string text =
                "<size=26><color=#E6DCFF>ETERNAL SLEEP</color></size>\n\n\n" +
                "<color=#9E8FD1>Un juego de</color>\n" + Author + "\n\n" +
                "<color=#9E8FD1>Hecho con</color>\nUnity 6 y URP 2D\n\n" +
                "<color=#9E8FD1>Arte, música y niveles</color>\ngenerados por código\n\n" +
                "<color=#9E8FD1>Sueños</color>\nGemini, cuando está despierta\n\n\n" +
                "<color=#FFE38A>Tu partida</color>\n\n" +
                $"Lágrimas de Luna: {CollectibleTracker.Count} / {CollectibleTracker.Total}\n" +
                $"Secretos encontrados: {CollectibleTracker.SecretsFound}\n" +
                $"Mejoras: {upgrades} / {kinds}\n" +
                $"Jefes vencidos: {PlayStats.BossesDefeated}\n" +
                $"Golpes dados: {PlayStats.HitsDealt}\n" +
                $"Veces que caíste: {PlayStats.Deaths}\n" +
                $"Tiempo jugado: {PlayStats.FormatTime()}\n\n\n" +
                "<color=#9E8FD1>Gracias por jugar.</color>\n\n" +
                "<size=10><color=#7A6FA8>(Pulsa Continuar en el menú para seguir buscando lo que falta)</color></size>";

            var t = RuntimeUI.Text("Creditos", parent, -400f, 13f, Pale, text);
            t.lineSpacing = 8f;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) yield return null;
        }

        private static IEnumerator Fade(Graphic g, float from, float to, float seconds)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                var c = g.color;
                c.a = Mathf.Lerp(from, to, t);
                g.color = c;
                yield return null;
            }
            var end = g.color;
            end.a = to;
            g.color = end;
        }

        private static IEnumerator FadeText(TMP_Text text, float from, float to, float seconds)
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / seconds)
            {
                text.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            text.alpha = to;
        }
    }
}
