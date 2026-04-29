using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Road Delete", typeof(RoadGraphAuthoring))]
    public class RoadDeleteTool : EditorTool
    {
        static GUIContent s_Icon;
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_Icon == null)
                    s_Icon = new GUIContent(
                        EditorGUIUtility.IconContent("TreeEditor.Trash").image,
                        "Road Delete — click edge to remove (shift: also remove orphan nodes)");
                return s_Icon;
            }
        }

        [SerializeField] float nodeSnapDist = 1.2f;
        [SerializeField] float edgeSnapDist = 1.2f;

        public override void OnToolGUI(EditorWindow window)
        {
            var a = target as RoadGraphAuthoring;
            if (a == null || a.graph == null) return;

            Event e = Event.current;
            var graph = a.graph;
            int passiveId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveId);

            if (!ToolUtil.TryGetMouseOnGraphPlane(a, e, out var mousePos))
            {
                window.Repaint();
                return;
            }

            // 노드 우선, 그다음 엣지
            var hoverNode = RoadGraphOps.FindNearestNode(graph, mousePos, nodeSnapDist);
            RoadEdge hoverEdge = null;
            Vector3 hitPoint = mousePos;
            if (hoverNode == null)
            {
                RoadGraphOps.FindNearestEdge(graph, mousePos, edgeSnapDist,
                    out hoverEdge, out hitPoint, out _);
            }

            // 클릭 → 삭제
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (hoverNode != null)
                {
                    Undo.RegisterCompleteObjectUndo(a, "Delete Node");
                    graph.RemoveNode(hoverNode.id);
                    a.InvalidateFaceCache();
                    EditorUtility.SetDirty(a);
                    e.Use();
                }
                else if (hoverEdge != null)
                {
                    Undo.RegisterCompleteObjectUndo(a, "Delete Road");
                    int aId = hoverEdge.nodeAId;
                    int bId = hoverEdge.nodeBId;
                    graph.RemoveEdge(hoverEdge.id);

                    if (e.shift)
                    {
                        // 고립된 끝점도 같이 정리
                        if (graph.Degree(aId) == 0) graph.RemoveNode(aId);
                        if (graph.Degree(bId) == 0) graph.RemoveNode(bId);
                    }

                    a.InvalidateFaceCache();
                    EditorUtility.SetDirty(a);
                    e.Use();
                }
            }

            DrawPreview(a, mousePos, hoverNode, hoverEdge);
            window.Repaint();
        }

        void DrawPreview(RoadGraphAuthoring a, Vector3 mousePos,
            RoadNode hoverNode, RoadEdge hoverEdge)
        {
            var prevMatrix = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            if (hoverNode != null)
            {
                Handles.color = Color.red;
                Handles.SphereHandleCap(0, hoverNode.position, Quaternion.identity,
                    a.nodeRadius * 1.7f, EventType.Repaint);
            }
            else if (hoverEdge != null)
            {
                var na = a.graph.GetNode(hoverEdge.nodeAId);
                var nb = a.graph.GetNode(hoverEdge.nodeBId);
                if (na != null && nb != null)
                {
                    Handles.color = Color.red;
                    Handles.DrawAAPolyLine(5f,
                        na.position + Vector3.up * 0.05f,
                        nb.position + Vector3.up * 0.05f);
                }
            }

            Handles.matrix = prevMatrix;

            Handles.BeginGUI();
            string msg = hoverNode != null
                ? "Road Delete: click to remove node + connected edges"
                : "Road Delete: click edge to remove  (shift: also remove orphan nodes)";
            GUI.Label(new Rect(10, 10, 600, 20), msg, EditorStyles.boldLabel);
            Handles.EndGUI();
        }
    }
}