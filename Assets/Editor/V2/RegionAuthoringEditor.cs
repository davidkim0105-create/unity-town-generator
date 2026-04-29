using UnityEditor;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [CustomEditor(typeof(RegionAuthoring))]
    public class RegionAuthoringEditor : Editor
    {
        static GUIContent GC(string label, string tooltip) => new GUIContent(label, tooltip);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var a = (RegionAuthoring)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Add Residential",
                    "주거지 프리셋 추가 (초록).\n• Lot Size 4 (작은 집)\n• Height 3~6m (저층)\n• Density 0.85")))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Add Region");
                    a.regions.Add(new TownRegionV2
                    {
                        name = "Residential", color = new Color(0.5f, 0.8f, 0.5f),
                        lotSize = 4f, lotMargin = 0.2f,
                        minHeight = 3f, maxHeight = 6f, density = 0.85f,
                    });
                    EditorUtility.SetDirty(a);
                }
                if (GUILayout.Button(GC("Add Commercial",
                    "상업지 프리셋 추가 (주황).\n• Lot Size 5\n• Height 6~16m (중고층)\n• Density 0.95 (빽빽)")))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Add Region");
                    a.regions.Add(new TownRegionV2
                    {
                        name = "Commercial", color = new Color(0.9f, 0.7f, 0.3f),
                        lotSize = 5f, lotMargin = 0.1f,
                        minHeight = 6f, maxHeight = 16f, density = 0.95f,
                    });
                    EditorUtility.SetDirty(a);
                }
                if (GUILayout.Button(GC("Add Industrial",
                    "산업단지 프리셋 추가 (회갈).\n• Lot Size 8 (큰 공장)\n• Height 4~8m (창고형)\n• Density 0.6 (듬성)")))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Add Region");
                    a.regions.Add(new TownRegionV2
                    {
                        name = "Industrial", color = new Color(0.6f, 0.5f, 0.45f),
                        lotSize = 8f, lotMargin = 0.05f,
                        minHeight = 4f, maxHeight = 8f, density = 0.6f,
                    });
                    EditorUtility.SetDirty(a);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Clear All Polygons",
                    "Region 정의는 유지하고 그려진 폴리곤만 비움. 다시 그릴 때 유용")))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Clear Regions");
                    foreach (var r in a.regions) r.polygon.Clear();
                    EditorUtility.SetDirty(a);
                }
                if (GUILayout.Button(GC("Remove All Regions",
                    "모든 Region 통째로 삭제. 처음부터 다시 정의할 때")))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Remove Regions");
                    a.regions.Clear();
                    EditorUtility.SetDirty(a);
                }
            }
        }

        [MenuItem("GameObject/TownGen V2/Region Container", false, 11)]
        static void Create(MenuCommand mc)
        {
            var go = new GameObject("Regions");
            go.AddComponent<RegionAuthoring>();
            GameObjectUtility.SetParentAndAlign(go, mc.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Regions");
            Selection.activeObject = go;
        }
    }
}