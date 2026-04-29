using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TownGenerator))]
public class TownGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 안내 박스
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "💡 모든 컨트롤은 Town Generator 윈도우에서 사용하세요.\n메뉴: Tools > Town Generator > Open Window\n단축키: Ctrl + Shift + T",
            MessageType.Info);

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
        if (GUILayout.Button("🪟 Open Town Generator Window", GUILayout.Height(40)))
        {
            TownGeneratorWindow.OpenWindow();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // 기본 인스펙터도 표시 (직접 보고 싶을 때)
        EditorGUILayout.LabelField("Raw Parameters", EditorStyles.boldLabel);
        DrawDefaultInspector();
    }
}

// TownBlock 인스펙터는 그대로 유지
[CustomEditor(typeof(TownBlock))]
public class TownBlockEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TownBlock block = (TownBlock)target;

        EditorGUILayout.LabelField($"📦 Block ({block.gridX}, {block.gridZ})", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("자동 밀도", block.autoDensity.ToString("F2"));
        EditorGUILayout.LabelField("적용 밀도", block.EffectiveDensity.ToString("F2"));
        EditorGUILayout.LabelField("시드 오프셋", block.seedOffset.ToString());

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("⚙️ 밀도 오버라이드", EditorStyles.boldLabel);

        float newDensity = EditorGUILayout.Slider(
            new GUIContent("Density Override",
                "-1 = 자동 밀도 사용\n0~1 = 이 블록만 강제 밀도\n↑ 더 밀집/고층 / ↓ 더 한산"),
            block.densityOverride, -1f, 1f);
        if (!Mathf.Approximately(newDensity, block.densityOverride))
        {
            Undo.RecordObject(block, "Change Block Density");
            block.densityOverride = newDensity;
            EditorUtility.SetDirty(block);
        }

        if (block.densityOverride < 0f)
            EditorGUILayout.HelpBox("자동 밀도 사용 중 (Override 끄짐)", MessageType.None);
        else
            EditorGUILayout.HelpBox($"수동 밀도: {block.densityOverride:F2}", MessageType.Info);

        EditorGUILayout.Space(10);

        int newOffset = EditorGUILayout.IntField(
            new GUIContent("Seed Offset",
                "이 블록만의 추가 시드. 같은 밀도라도 다른 배치를 만듦."),
            block.seedOffset);
        if (newOffset != block.seedOffset)
        {
            Undo.RecordObject(block, "Change Block Seed");
            block.seedOffset = newOffset;
            EditorUtility.SetDirty(block);
        }

        EditorGUILayout.Space(15);

        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button("🔄 이 블록만 재생성", GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(block.gameObject, "Regen Block");
            block.Regenerate();
        }

        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
        if (GUILayout.Button("🎲 랜덤 재생성 (배치 변경)", GUILayout.Height(35)))
        {
            Undo.RecordObject(block, "Random Block Seed");
            Undo.RegisterFullObjectHierarchyUndo(block.gameObject, "Random Regen Block");
            block.RandomRegenerate();
            EditorUtility.SetDirty(block);
        }
        GUI.backgroundColor = Color.white;
    }
}