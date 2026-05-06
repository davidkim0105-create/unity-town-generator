using System;
using UnityEditor;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    /// <summary>
    /// Preset 슬롯 + Apply + Save As 버튼을 한 줄에 그리는 헬퍼.
    /// </summary>
    public static class PresetGUIHelper
    {
        /// <summary>
        /// Preset 한 줄 UI를 그림.
        /// onApply: 슬롯 값을 현재 세팅에 적용
        /// onSave: 현재 세팅을 새 에셋으로 저장
        /// </summary>
        public static T DrawPresetRow<T>(string label, T currentSlot,
            System.Action<T> onApply,
            System.Func<T> createNewFromCurrent) where T : ScriptableObject
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(80));

                var newSlot = (T)EditorGUILayout.ObjectField(currentSlot, typeof(T), false);

                using (new EditorGUI.DisabledScope(newSlot == null))
                {
                    if (GUILayout.Button(new GUIContent("Apply",
                        "선택한 프리셋의 값을 현재 세팅에 적용"), GUILayout.Width(55)))
                    {
                        if (newSlot != null) onApply(newSlot);
                    }
                }

                if (GUILayout.Button(new GUIContent("Save As...",
                    "현재 세팅을 새 프리셋 에셋으로 저장"), GUILayout.Width(80)))
                {
                    SaveAsAsset<T>(createNewFromCurrent);
                }

                return newSlot;
            }
        }

        static void SaveAsAsset<T>(System.Func<T> createNewFromCurrent) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Preset",
                $"{typeof(T).Name}_New",
                "asset",
                "프리셋 에셋 저장 위치 선택");
            if (string.IsNullOrEmpty(path)) return;

            var asset = createNewFromCurrent();
            if (asset == null) return;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            Debug.Log($"[TownGen V2] Preset saved: {path}");
        }
    }
}