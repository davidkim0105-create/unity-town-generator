using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Road Cut", typeof(RoadGraphAuthoring))]
    public class RoadCutTool : EditorTool
    {
        static GUIContent s_Icon;
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_Icon == null)
                    s_Icon = new GUIContent(
                        EditorGUIUtility.IconContent("d_Grid.EraserTool").image,
                        "Road Cut — click on an edge to split it");
                return s_Icon;
            }
        }

        [SerializeField] float edgeSnapDist = 1.5f;

        public override void OnToolGUI(EditorWindow window)
        {
            var a = target as RoadGraphAuthoring;
            if (a == null || a.graph == null) return;

            Event e = Event.current;
            int passiveId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveId);

            // 마우스를 평면에 투영
            if (!ToolUtil.TryGetMouseOnGraphPlane(a, e, out var mousePos))
            {
                window.Repaint();
                return;
            }

            // 가장 가까운 엣지
            RoadGraphOps.FindNearestEdge(a.graph, mousePos, edgeSnapDist,
                out var hoverEdge, out var hitPoint, out _);

            // 클릭 → 분할
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && hoverEdge != null)
            {
                Undo.RegisterCompleteObjectUndo(a, "Cut Road");
                a.graph.SplitEdgeAt(hoverEdge.id, hitPoint);
                a.InvalidateFaceCache();
                EditorUtility.SetDirty(a);
                e.Use();
            }

            // 미리보기
            DrawPreview(a, mousePos, hoverEdge, hitPoint);
            window.Repaint();
        }

        void DrawPreview(RoadGraphAuthoring a, Vector3 mousePos,
            RoadEdge hoverEdge, Vector3 hitPoint)
        {
            var prevMatrix = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            if (hoverEdge != null)
            {
                var na = a.graph.GetNode(hoverEdge.nodeAId);
                var nb = a.graph.GetNode(hoverEdge.nodeBId);
                if (na != null && nb != null)
                {
                    Handles.color = Color.magenta;
                    Handles.DrawAAPolyLine(5f,
                        na.position + Vector3.up * 0.05f,
                        nb.position + Vector3.up * 0.05f);
                }
                Handles.color = Color.magenta;
                Handles.SphereHandleCap(0, hitPoint, Quaternion.identity,
                    a.nodeRadius * 1.4f, EventType.Repaint);
            }

            Handles.matrix = prevMatrix;

            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 500, 20),
                "Road Cut: click on an edge to split it",
                EditorStyles.boldLabel);
            Handles.EndGUI();
        }
    }
}