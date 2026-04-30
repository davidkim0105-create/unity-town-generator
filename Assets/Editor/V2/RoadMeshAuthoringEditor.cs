using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TownGen.V2
{
    [CustomEditor(typeof(RoadMeshAuthoring))]
    public class RoadMeshAuthoringEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var auth = (RoadMeshAuthoring)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build Road Mesh", GUILayout.Height(32)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Build Road Mesh");
                    auth.Build();
                    EditorUtility.SetDirty(auth);
                }
                if (GUILayout.Button("Clear", GUILayout.Width(80), GUILayout.Height(32)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Clear Road Mesh");
                    auth.Clear();
                    EditorUtility.SetDirty(auth);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("⤴ Sync Block Inset to Road Width", GUILayout.Height(24)))
            {
                TrySyncBlockInset(auth);
            }

            EditorGUILayout.HelpBox(
                "Sync 버튼: 빌딩이 도로 위로 튀어나오지 않도록\n" +
                "RoadGraphAuthoring의 블록 인셋을 가장 굵은 도로 폭의 절반에 맞춥니다.\n" +
                "(인셋 변경 후 [Build Blocks] + [Fill Buildings] 다시 실행 필요)",
                MessageType.Info);
        }

        void TrySyncBlockInset(RoadMeshAuthoring auth)
        {
            var rga = auth.GetComponent<RoadGraphAuthoring>();
            if (rga == null) { Debug.LogWarning("RoadGraphAuthoring 없음."); return; }

            float recommended = auth.ComputeRecommendedBlockInset();
            if (recommended <= 0f) { Debug.LogWarning("그래프가 비어있거나 도로 폭 0."); return; }

            // 인셋 필드명 후보들을 리플렉션으로 탐색
            string[] candidates = { "roadInset", "blockInset", "inset", "buildInset" };
            FieldInfo found = null;
            foreach (var name in candidates)
            {
                var fi = rga.GetType().GetField(name,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null && fi.FieldType == typeof(float)) { found = fi; break; }
            }

            if (found == null)
            {
                Debug.LogWarning("[Sync] RoadGraphAuthoring에서 인셋 필드를 찾지 못함. " +
                    "수동으로 Road Inset을 약 " + recommended.ToString("F2") + " 로 설정하세요.");
                return;
            }

            Undo.RecordObject(rga, "Sync Block Inset");
            float old = (float)found.GetValue(rga);
            found.SetValue(rga, recommended);
            EditorUtility.SetDirty(rga);
            Debug.Log($"[Sync] {found.Name}: {old:F2} → {recommended:F2}");
        }
    }
}