using UnifyCountry.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnifyCountry.Editor
{
    public static class PrototypeBattleUiMenu
    {
        private const string BattleScenePath = "Assets/Scenes/Battle/SCN_BattlePrototype.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu/SCN_MainMenu.unity";

        [MenuItem("UnifyCountry/Battle/Open Battle Scene")]
        public static void OpenBattleScene()
        {
            EditorSceneManager.OpenScene(BattleScenePath);
        }

        [MenuItem("UnifyCountry/Battle/Rebuild Current Battle UI")]
        public static void RebuildCurrentBattleUi()
        {
            if (EditorSceneManager.GetActiveScene().path == MainMenuScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Battle UI",
                    "当前打开的是主菜单场景。请先打开战斗场景，再重建战斗 UI。",
                    "OK");
                return;
            }

            var root = GameObject.Find("PrototypeBattleUi");
            if (root == null || !root.TryGetComponent<PrototypeBattleUi>(out var preview))
            {
                EditorUtility.DisplayDialog(
                    "Battle UI",
                    "当前场景没有 PrototypeBattleUi。请打开战斗场景，或确认场景中存在 PrototypeBattleUi 对象。",
                    "OK");
                return;
            }

            var serialized = new SerializedObject(preview);
            serialized.FindProperty("cardsCsv").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Configs/Cards/cards_v001.csv");
            serialized.FindProperty("unitsCsv").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Configs/Cards/units_v001.csv");
            serialized.FindProperty("effectsCsv").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Configs/Cards/effects_v001.csv");
            serialized.FindProperty("startingDeckCsv").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Configs/Cards/player_starting_deck_v001.csv");
            serialized.FindProperty("wavesCsv").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Configs/Waves/battle_001_waves_v001.csv");
            serialized.ApplyModifiedProperties();

            preview.Rebuild();
            Selection.activeObject = root;
            EditorUtility.SetDirty(root);
        }

        [MenuItem("UnifyCountry/Prototype/Create Battle UI Preview")]
        public static void CreateBattleUiPreviewLegacy()
        {
            RebuildCurrentBattleUi();
        }
    }
}
