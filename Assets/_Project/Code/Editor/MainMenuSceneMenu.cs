using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnifyCountry.Editor
{
    [InitializeOnLoad]
    public static class MainMenuSceneMenu
    {
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu/SCN_MainMenu.unity";

        static MainMenuSceneMenu()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (sceneAsset != null)
                EditorSceneManager.playModeStartScene = sceneAsset;
        }

        [MenuItem("UnifyCountry/Main Menu/Open Main Menu Scene")]
        public static void OpenMainMenuScene()
        {
            EditorSceneManager.OpenScene(MainMenuScenePath);
        }

        [MenuItem("UnifyCountry/Main Menu/Use Main Menu As Play Start Scene")]
        public static void UseMainMenuAsPlayStartScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (sceneAsset != null)
                EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }
}
