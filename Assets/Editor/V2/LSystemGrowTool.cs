using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    [EditorTool("L-System Grow", typeof(RoadGraphAuthoring))]
    public class LSystemGrowTool : EditorTool
    {
        public static LSystemGenerator.Settings settings = LSystemGenerator.Default;
        public static bool autoRebuild = true;

        Vector3 currentMouse;

        public override GUIContent toolbarIcon =>
            new GUIContent("🌱", "L-System Grow — 클릭한 점에서 도시 자동 성장");

        public override void OnToolGUI(EditorWindow window)
        {
            var auth = target as RoadGraphAuthoring;
            if (auth == null) return;
            var sv = window as SceneView;
            if (sv == null) return;

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            if (!RaycastGroundPlane(e.mousePosition, out currentMouse))
                return;

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "L-System Grow");
                        int added = LSystemGenerator.Grow(auth.graph, currentMouse, settings);
                        auth.InvalidateFaceCache();
                        EditorUtility.SetDirty(auth);

                        // 매 클릭마다 다른 결과
                        settings.seed++;

                        if (autoRebuild)
                        {
                            var region = auth.GetComponentInChildren<RegionAuthoring>();
                            if (region == null)
                                region = Object.FindAnyObjectByType<RegionAuthoring>();
                            BlockBuilder.RebuildAllBlocks(auth, auth.roadInset,
                                regions: region, blockThickness: auth.blockThickness,
                                useEdgeWidthForInset: auth.useEdgeWidthForInset,
                                insetExtraMargin: auth.insetExtraMargin);
                            BlockBuilder.FillAllBlocksWithBuildings(auth, auth.buildSettings, region);
                            var rm = auth.GetComponent<RoadMeshAuthoring>();
                            if (rm != null) rm.Build();
                        }

                        Debug.Log($"[L-System] {added} edges added.");
                        SceneView.RepaintAll();
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    // Ctrl+휠 = Max Radius, Shift+휠 = Segment Length
                    if (e.control)
                    {
                        settings.maxRadius = Mathf.Clamp(
                            settings.maxRadius + (-e.delta.y * 3f), 20f, 300f);
                        e.Use();
                    }
                    else if (e.shift)
                    {
                        settings.segmentLength = Mathf.Clamp(
                            settings.segmentLength + (-e.delta.y * 0.5f), 3f, 30f);
                        e.Use();
                    }
                    break;
            }

            DrawHandles();
            DrawOverlay();
            sv.Repaint();
        }

        void DrawHandles()
        {
            Handles.color = new Color(0.4f, 1f, 0.6f, 0.9f);
            Handles.SphereHandleCap(0, currentMouse, Quaternion.identity, 1f, EventType.Repaint);
            Handles.color = new Color(0.4f, 1f, 0.6f, 0.3f);
            Handles.DrawWireDisc(currentMouse, Vector3.up, settings.maxRadius);

            // 초기 가지 방향 미리보기
            Handles.color = new Color(0.4f, 1f, 0.6f, 0.6f);
            int branches = Mathf.Clamp(settings.initialBranches, 1, 8);
            float step = 360f / branches;
            for (int i = 0; i < branches; i++)
            {
                float r = i * step * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(r), 0, Mathf.Sin(r));
                Handles.DrawDottedLine(currentMouse,
                    currentMouse + dir * settings.segmentLength, 4f);
            }
        }

        void DrawOverlay()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 320, 150), GUI.skin.box);
            GUILayout.Label("🌱 L-System Grow", EditorStyles.boldLabel);
            GUILayout.Label($"Iter: {settings.iterations}   Segment: {settings.segmentLength:F1}m");
            GUILayout.Label($"Branch P: {settings.branchProbability:F2}   Angle Jit: {settings.angleJitter:F0}°");
            GUILayout.Label($"Radius: {settings.maxRadius:F0}m   Branches: {settings.initialBranches}");
            GUILayout.Label($"Seed: {settings.seed}   AutoRebuild: {(autoRebuild ? "ON" : "OFF")}");
            GUILayout.Label("Click to spawn city center", EditorStyles.miniLabel);
            GUILayout.Label("Ctrl+Wheel=Radius · Shift+Wheel=Segment Length",
                EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        static bool RaycastGroundPlane(Vector2 mp, out Vector3 wp)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mp);
            Plane g = new Plane(Vector3.up, Vector3.zero);
            if (g.Raycast(ray, out float t)) { wp = ray.GetPoint(t); return true; }
            wp = default; return false;
        }
    }
}