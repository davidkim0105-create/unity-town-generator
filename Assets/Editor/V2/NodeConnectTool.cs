using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Node Connect", typeof(RoadGraphAuthoring))]
    public class NodeConnectTool : EditorTool
    {
        static GUIContent s_Icon;
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_Icon == null)
                    s_Icon = new GUIContent(
                        EditorGUIUtility.IconContent("d_Linked").image,
                        "Node Connect — click two nodes to merge them");
                return s_Icon;
            }
        }

        [SerializeField] float nodeSnapDist = 1.5f;

        int sourceNodeId = -1;
        int hoveredNodeId = -1;

        public override void OnActivated() { sourceNodeId = -1; }
        public override void OnWillBeDeactivated() { sourceNodeId = -1; }

        public override void OnToolGUI(EditorWindow window)
        {
            var a = target as RoadGraphAuthoring;
            if (a == null || a.graph == null) return;

            Event e = Event.current;
            var graph = a.graph;
            int passiveId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveId);

            // 마우스 평면 투영
            if (!ToolUtil.TryGetMouseOnGraphPlane(a, e, out var mousePos))
            {
                window.Repaint();
                return;
            }

            // 호버 노드
            var hoverNode = RoadGraphOps.FindNearestNode(graph, mousePos, nodeSnapDist);
            int newHover = hoverNode?.id ?? -1;
            if (newHover != hoveredNodeId)
            {
                hoveredNodeId = newHover;
                window.Repaint();
            }

            // stale source 체크
            if (sourceNodeId != -1 && graph.GetNode(sourceNodeId) == null)
                sourceNodeId = -1;

            // 클릭 처리
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (hoverNode != null)
                {
                    if (sourceNodeId == -1)
                    {
                        // 첫 번째 노드 선택
                        sourceNodeId = hoverNode.id;
                    }
                    else if (hoverNode.id == sourceNodeId)
                    {
                        // 같은 노드 다시 클릭 → 취소
                        sourceNodeId = -1;
                    }
                    else
                    {
                        // 두 번째 노드 → 병합
                        Undo.RegisterCompleteObjectUndo(a, "Merge Nodes");
                        RoadGraphOps.MergeNodes(graph, sourceNodeId, hoverNode.id);
                        sourceNodeId = -1;
                        a.InvalidateFaceCache();
                        EditorUtility.SetDirty(a);
                    }
                    e.Use();
                }
            }

            // ESC
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                sourceNodeId = -1;
                e.Use();
            }

            DrawPreview(a, mousePos);
            window.Repaint();
        }

        void DrawPreview(RoadGraphAuthoring a, Vector3 mousePos)
        {
            var graph = a.graph;
            var prevMatrix = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            // 호버 노드 강조
            if (hoveredNodeId != -1 && hoveredNodeId != sourceNodeId)
            {
                var n = graph.GetNode(hoveredNodeId);
                if (n != null)
                {
                    Handles.color = Color.cyan;
                    Handles.SphereHandleCap(0, n.position, Quaternion.identity,
                        a.nodeRadius * 1.5f, EventType.Repaint);
                }
            }

            // 소스 노드 강조 + 마우스로 가는 선
            if (sourceNodeId != -1)
            {
                var src = graph.GetNode(sourceNodeId);
                if (src != null)
                {
                    Handles.color = Color.yellow;
                    Handles.SphereHandleCap(0, src.position, Quaternion.identity,
                        a.nodeRadius * 1.7f, EventType.Repaint);

                    // 미리보기 선 (소스 → 호버 또는 마우스)
                    Vector3 endPoint = mousePos;
                    bool snapping = false;
                    if (hoveredNodeId != -1 && hoveredNodeId != sourceNodeId)
                    {
                        var hov = graph.GetNode(hoveredNodeId);
                        if (hov != null) { endPoint = hov.position; snapping = true; }
                    }
                    Handles.color = snapping
                        ? new Color(0.3f, 1f, 0.3f, 0.95f)
                        : new Color(1f, 1f, 0.4f, 0.7f);
                    Handles.DrawDottedLine(
                        src.position + Vector3.up * 0.05f,
                        endPoint + Vector3.up * 0.05f, 4f);
                }
            }

            Handles.matrix = prevMatrix;

            Handles.BeginGUI();
            string msg = sourceNodeId == -1
                ? "Node Connect: click first node to merge"
                : "Node Connect: click second node to merge into  (ESC to cancel)";
            GUI.Label(new Rect(10, 10, 600, 20), msg, EditorStyles.boldLabel);
            Handles.EndGUI();
        }
    }
}