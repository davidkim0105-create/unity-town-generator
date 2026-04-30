using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Width Brush", typeof(RoadGraphAuthoring))]
    public class WidthBrushTool : EditorTool
    {
        public enum BrushMode { Click, Paint }

        public static float targetWidth = 4f;
        public static BrushMode mode = BrushMode.Click;
        public static float brushRadius = 6f;
        public static bool autoRebuild = true;

        Vector3 currentMouse;
        bool isPainting;
        HashSet<int> paintedThisStroke = new HashSet<int>();
        RoadEdge hoveredEdge;
        Vector3 hoveredEdgePoint;

        public override GUIContent toolbarIcon =>
            new GUIContent("📏", "Width Brush — 도로 폭 변경 (클릭 또는 드래그 페인트)");

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

            // 가장 가까운 엣지 추적 (Click 모드 강조용)
            hoveredEdge = FindClosestEdge(auth.graph, currentMouse, out hoveredEdgePoint, brushRadius);

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        // Alt+클릭: 스포이드
                        if (e.alt)
                        {
                            if (hoveredEdge != null)
                            {
                                targetWidth = hoveredEdge.width;
                                Debug.Log($"[Width Brush] Picked width: {targetWidth:F2}m");
                            }
                            e.Use();
                            break;
                        }

                        Undo.RegisterCompleteObjectUndo(auth, "Width Brush");
                        isPainting = true;
                        paintedThisStroke.Clear();
                        ApplyAtCursor(auth);
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isPainting && mode == BrushMode.Paint)
                    {
                        ApplyAtCursor(auth);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && isPainting)
                    {
                        isPainting = false;
                        if (paintedThisStroke.Count > 0)
                        {
                            auth.InvalidateFaceCache();
                            EditorUtility.SetDirty(auth);
                            if (autoRebuild) Rebuild(auth);
                        }
                        paintedThisStroke.Clear();
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    // Ctrl+휠: Brush Radius (Paint 모드에서만 의미)
                    // Shift+휠: Target Width
                    if (e.control)
                    {
                        float delta = -e.delta.y * 0.5f;  // 위로=증가
                        brushRadius = Mathf.Clamp(brushRadius + delta, 1f, 50f);
                        e.Use();
                    }
                    else if (e.shift)
                    {
                        float delta = -e.delta.y * 0.2f;
                        targetWidth = Mathf.Clamp(targetWidth + delta, 0.5f, 15f);
                        e.Use();
                    }
                    break;
            }

            DrawHandles(auth);
            DrawOverlay();
            sv.Repaint();
        }

        // ─── 적용 ───
        void ApplyAtCursor(RoadGraphAuthoring auth)
        {
            if (mode == BrushMode.Click)
            {
                if (hoveredEdge != null && !paintedThisStroke.Contains(hoveredEdge.id))
                {
                    hoveredEdge.width = targetWidth;
                    paintedThisStroke.Add(hoveredEdge.id);
                }
            }
            else // Paint
            {
                foreach (var ed in auth.graph.edges)
                {
                    if (paintedThisStroke.Contains(ed.id)) continue;
                    var nA = FindNodeById(auth.graph, ed.nodeAId);
                    var nB = FindNodeById(auth.graph, ed.nodeBId);
                    if (nA == null || nB == null) continue;
                    Vector3 pt = ClosestPointOnSegment(nA.position, nB.position, currentMouse);
                    if (Vector3.Distance(pt, currentMouse) <= brushRadius)
                    {
                        ed.width = targetWidth;
                        paintedThisStroke.Add(ed.id);
                    }
                }
            }
        }

        // ─── 자동 재빌드 ───
        static void Rebuild(RoadGraphAuthoring auth)
        {
            var region = auth.GetComponentInChildren<RegionAuthoring>();
            if (region == null) region = Object.FindAnyObjectByType<RegionAuthoring>();
            BlockBuilder.RebuildAllBlocks(auth, auth.roadInset,
                regions: region, blockThickness: auth.blockThickness,
                useEdgeWidthForInset: auth.useEdgeWidthForInset,
                insetExtraMargin: auth.insetExtraMargin);
            BlockBuilder.FillAllBlocksWithBuildings(auth, auth.buildSettings, region);
            var rm = auth.GetComponent<RoadMeshAuthoring>();
            if (rm != null) rm.Build();
        }

        // ─── 시각 ───
        void DrawHandles(RoadGraphAuthoring auth)
        {
            // 커서
            Handles.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Handles.SphereHandleCap(0, currentMouse, Quaternion.identity, 0.4f, EventType.Repaint);

            if (mode == BrushMode.Paint)
            {
                Handles.color = new Color(1f, 0.6f, 0.2f, 0.3f);
                Handles.DrawWireDisc(currentMouse, Vector3.up, brushRadius);
            }

            // 호버된 엣지 강조 (Click 모드 + 후보 표시)
            if (hoveredEdge != null && mode == BrushMode.Click)
            {
                var nA = FindNodeById(auth.graph, hoveredEdge.nodeAId);
                var nB = FindNodeById(auth.graph, hoveredEdge.nodeBId);
                if (nA != null && nB != null)
                {
                    Handles.color = new Color(1f, 1f, 0.2f, 1f);
                    Handles.DrawAAPolyLine(6f, nA.position, nB.position);

                    // 폭 라벨
                    Handles.BeginGUI();
                    var labelPos = HandleUtility.WorldToGUIPoint((nA.position + nB.position) * 0.5f);
                    var rect = new Rect(labelPos.x + 8, labelPos.y - 10, 120, 20);
                    GUI.Label(rect, $"{hoveredEdge.width:F1}m → {targetWidth:F1}m",
                        EditorStyles.whiteBoldLabel);
                    Handles.EndGUI();
                }
            }

            // Paint 모드에서 칠해질 엣지 미리 강조
            if (mode == BrushMode.Paint)
            {
                Handles.color = new Color(1f, 0.6f, 0.2f, 0.7f);
                foreach (var ed in auth.graph.edges)
                {
                    var nA = FindNodeById(auth.graph, ed.nodeAId);
                    var nB = FindNodeById(auth.graph, ed.nodeBId);
                    if (nA == null || nB == null) continue;
                    Vector3 pt = ClosestPointOnSegment(nA.position, nB.position, currentMouse);
                    if (Vector3.Distance(pt, currentMouse) <= brushRadius)
                    {
                        Handles.DrawAAPolyLine(4f, nA.position, nB.position);
                    }
                }
            }
        }

        void DrawOverlay()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 320, 130), GUI.skin.box);
            GUILayout.Label("📏 Width Brush", EditorStyles.boldLabel);
            GUILayout.Label($"Target: {targetWidth:F1}m   Mode: {mode}");
            if (mode == BrushMode.Paint)
                GUILayout.Label($"Radius: {brushRadius:F1}m");
            GUILayout.Label($"Auto Rebuild: {(autoRebuild ? "ON" : "OFF")}");
            GUILayout.Label("Click=apply · Drag(Paint)=stroke · Alt+Click=eyedropper",
                EditorStyles.miniLabel);
            GUILayout.Label("Ctrl+Wheel=Radius · Shift+Wheel=Target Width",
                EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // ─── helpers ───
        static RoadEdge FindClosestEdge(RoadGraph g, Vector3 p, out Vector3 closestPt, float maxDist)
        {
            closestPt = p;
            RoadEdge best = null;
            float bestD = maxDist;
            foreach (var ed in g.edges)
            {
                var nA = FindNodeById(g, ed.nodeAId);
                var nB = FindNodeById(g, ed.nodeBId);
                if (nA == null || nB == null) continue;
                Vector3 pt = ClosestPointOnSegment(nA.position, nB.position, p);
                float d = Vector3.Distance(pt, p);
                if (d < bestD) { bestD = d; best = ed; closestPt = pt; }
            }
            return best;
        }

        static RoadNode FindNodeById(RoadGraph g, int id)
        {
            foreach (var n in g.nodes) if (n.id == id) return n;
            return null;
        }

        static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Mathf.Max(Vector3.Dot(ab, ab), 1e-9f);
            t = Mathf.Clamp01(t);
            return a + ab * t;
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