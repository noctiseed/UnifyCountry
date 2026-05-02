using UnifyCountry.UI;
using UnityEditor;
using UnityEngine;

namespace UnifyCountry.Editor
{
    public static class PrototypeBattleUiMenu
    {
        [MenuItem("UnifyCountry/Prototype/Create Battle UI Preview")]
        public static void CreateBattleUiPreview()
        {
            var existing = GameObject.Find("PrototypeBattleUi");
            var root = existing != null ? existing : new GameObject("PrototypeBattleUi");

            var preview = root.GetComponent<PrototypeBattleUi>();
            if (preview == null)
                preview = root.AddComponent<PrototypeBattleUi>();

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
    }
}
