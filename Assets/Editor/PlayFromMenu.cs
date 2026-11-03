#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EternalSleep.EditorTools
{
    /// <summary>
    /// Hace que el botón Play del editor empiece siempre desde el menú principal,
    /// aunque tengas abierta otra escena. Se activa o desactiva en:
    /// Eternal Sleep > Empezar Play desde el menú.
    /// Si el menú todavía no existe, lo crea una vez al abrir Unity.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMenu
    {
        private const string MenuPath = "Eternal Sleep/Empezar Play desde el menú";
        private const string PrefKey = "EternalSleep.PlayFromMenu";
        private const string AutoCreateKey = "EternalSleep.MainMenuAutoCreate.v2";
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        static PlayFromMenu()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(ScenePath) && !SessionState.GetBool(AutoCreateKey, false))
                {
                    SessionState.SetBool(AutoCreateKey, true); // solo un intento por sesión
                    MainMenuSetupEditor.CreateMainMenu();
                }
                Apply();
            };
        }

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        public static void Apply()
        {
            EditorSceneManager.playModeStartScene = Enabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)
                : null;
        }
    }
}
#endif
