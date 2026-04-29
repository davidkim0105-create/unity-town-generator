using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Road Draw", typeof(RoadGraphAuthoring))]
    public class RoadDrawTool : EditorTool
    {
        static GUIContent s_Icon;
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_Icon == null)
                    s_Icon = new GUIContent(
                        EditorGUIUtility.IconContent("d_Grid.PaintTool").image,
                        "Road Draw — click to place road points (ESC/right-click to end)\nShift: angle snap (45°)\nCtrl: grid snap (1m)");
                return s_Icon;
            }
        }

        int currentFromId = -1;
        Vector3 mouseGroundPoint;
        bool mouseValid = false;
        RoadGraphOps.SnapKind hoverSnap;

        [SerializeField] float nodeSnapDist = 1.5f;
        [SerializeField] float edgeSnapDist = 1.0f;
        [SerializeField] float gridSize = 1f;
        [SerializeField] float angleStep = 45f;

        public override void OnActivated() { currentFromId = -1; }
        public override void OnWillBeDeactivated() { currentFromId = -1; }

        public override void OnToolGUI(EditorWindow window)
        {
            var a = target as RoadGraphAuthoring;
            if (a == null || a.graph == null) return;

            Event e = Event.current;
            var graph = a.graph;
            var tr = a.transform;

            int passiveId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveId);

            // 1. 마우스 평면 투영
            mouseValid = TryGetMouseOnGraphPlane(a, e, out mouseGroundPoint);

            // ★ 스냅 적용 (각도 anchor = 직전 노드 위치)
            Vector3? anchor = null;
            if (currentFromId != -1)
            {
                var fromNode = graph.GetNode(currentFromId);
                if (fromNode != null) anchor = fromNode.position;
            }
            var snapSettings = SnapUtil.FromEvent(e, gridSize, angleStep);
            Vector3 snappedMouse = SnapUtil.Apply(mouseGroundPoint, anchor, snapSettings);

            // 2. 미리보기 스냅 판정 (snappedMouse 기준)
            hoverSnap = RoadGraphOps.SnapKind.CreatedNew;
            RoadNode hoverNode = null;
            RoadEdge hoverEdge = null;
            Vector3 previewPoint = snappedMouse;

            if (mouseValid)
            {
                hoverNode = RoadGraphOps.FindNearestNode(graph, snappedMouse, nodeSnapDist);
                if (hoverNode != null)
                {
                    hoverSnap = RoadGraphOps.SnapKind.ExistingNode;
                    previewPoint = hoverNode.position;
                }
                else if (RoadGraphOps.FindNearestEdge(graph, snappedMouse,
                    edgeSnapDist, out hoverEdge, out var p, out _))
                {
                    hoverSnap = RoadGraphOps.SnapKind.OnEdgeSplit;
                    previewPoint = p;
                }
            }

            // 3. 클릭
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && mouseValid)
            {
                Undo.RegisterCompleteObjectUndo(a, "Draw Road");

                var node = RoadGraphOps.AcquireNodeAt(
                    graph, snappedMouse, nodeSnapDist, edgeSnapDist, out _);

                if (node == null) { e.Use(); return; }
                if (currentFromId != -1 && graph.GetNode(currentFromId) == null)
                    currentFromId = -1;

                if (currentFromId != -1 && node.id != currentFromId)
                    RoadGraphOps.AddEdgeWithSplits(graph, currentFromId, node.id);

                currentFromId = node.id;
                a.InvalidateFaceCache();
                EditorUtility.SetDirty(a);
                e.Use();
            }

            // 4. ESC / 우클릭
            if ((e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) ||
                (e.type == EventType.MouseDown && e.button == 1))
            {
                currentFromId = -1;
                e.Use();
            }

            DrawPreview(a, previewPoint, hoverNode, hoverEdge, snapSettings);
            window.Repaint();
        }

        bool TryGetMouseOnGraphPlane(RoadGraphAuthoring a, Event e, out Vector3 localPoint)
        {
            var tr = a.transform;
            Plane plane = new Plane(tr.up, tr.position);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 world = ray.GetPoint(dist);
                localPoint = tr.InverseTransformPoint(world);
                return true;
            }
            localPoint = Vector3.zero;
            return false;
        }

        void DrawPreview(RoadGraphAuthoring a, Vector3 previewLocal,
            RoadNode hoverNode, RoadEdge hoverEdge, SnapUtil.Settings snap)
        {
            if (!mouseValid) return;
            var prevMatrix = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            Color pointColor = hoverSnap switch
            {
                RoadGraphOps.SnapKind.ExistingNode => Color.cyan,
                RoadGraphOps.SnapKind.OnEdgeSplit => Color.magenta,
                _ => Color.green
            };
            Handles.color = pointColor;
            Handles.SphereHandleCap(0, previewLocal, Quaternion.identity,
                a.nodeRadius * 1.3f, EventType.Repaint);

            if (hoverEdge != null)
            {
                var na = a.graph.GetNode(hoverEdge.nodeAId);
                var nb = a.graph.GetNode(hoverEdge.nodeBId);
                if (na != null && nb != null)
                {
                    Handles.color = Color.magenta;
                    Handles.DrawAAPolyLine(4f,
                        na.position + Vector3.up * 0.05f,
                        nb.position + Vector3.up * 0.05f);
                }
            }

            if (currentFromId != -1)
            {
                var from = a.graph.GetNode(currentFromId);
                if (from == null) currentFromId = -1;
                else
                {
                    Handles.color = new Color(1f, 1f, 0.4f, 0.9f);
                    Handles.DrawDottedLine(
                        from.position + Vector3.up * 0.05f,
                        previewLocal + Vector3.up * 0.05f, 4f);
                    Handles.color = Color.yellow;
                    Handles.SphereHandleCap(0, from.position, Quaternion.identity,
                        a.nodeRadius * 1.6f, EventType.Repaint);
                }
            }

            Handles.matrix = prevMatrix;

            // ★ 상태 + 스냅 표시
            Handles.BeginGUI();
            string snapInfo = "";
            if (snap.angleSnap) snapInfo += $" [Angle {snap.angleStep}°]";
            if (snap.gridSnap) snapInfo += $" [Grid {snap.gridSize}m]";
            string msg = currentFromId == -1
                ? "Road Draw: click to start" + snapInfo
                : "Road Draw: click to extend  |  ESC = finish" + snapInfo;
            GUI.Label(new Rect(10, 10, 700, 20), msg, EditorStyles.boldLabel);
            GUI.Label(new Rect(10, 30, 700, 20),
                "Hold Shift = angle snap (45°)   Hold Ctrl = grid snap (1m)",
                EditorStyles.miniLabel);

            // ★ 길이/각도 표시 (그리는 중일 때)
            if (currentFromId != -1)
            {
                var from = a.graph.GetNode(currentFromId);
                if (from != null)
                {
                    Vector3 d = previewLocal - from.position;
                    float len = new Vector2(d.x, d.z).magnitude;
                    float ang = Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
                    if (ang < 0) ang += 360f;
                    string info = $"L = {len:F2} m    θ = {ang:F1}°";

                    // 마우스 옆에 표시
                    Vector2 m = Event.current.mousePosition;
                    var rect = new Rect(m.x + 18, m.y + 18, 200, 22);
                    EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.7f));
                    var st = new GUIStyle(EditorStyles.boldLabel);
                    st.normal.textColor = Color.white;
                    GUI.Label(new Rect(rect.x + 6, rect.y + 2, rect.width, rect.height), info, st);
                }
            }
            Handles.EndGUI();
        }
    }
}