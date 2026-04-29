using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Node Move", typeof(RoadGraphAuthoring))]
    public class NodeMoveTool : EditorTool
    {
        static GUIContent s_Icon;
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_Icon == null)
                    s_Icon = new GUIContent(
                        EditorGUIUtility.IconContent("MoveTool").image,
                        "Node Move — drag nodes, box-select with empty drag");
                return s_Icon;
            }
        }

        readonly HashSet<int> selected = new HashSet<int>();
        int hoveredNodeId = -1;

        // 박스 드래그 상태
        bool isBoxDragging = false;
        Vector2 boxStartGui;
        Vector2 boxEndGui;

        // 다중 이동용
        Vector3 multiHandlePos;          // 핸들의 현재 위치 (월드)
        Vector3 multiHandlePosLast;      // 직전 핸들 위치 (델타 계산)
        bool multiHandleInitialized = false;

        public override void OnToolGUI(EditorWindow window)
        {
            var a = target as RoadGraphAuthoring;
            if (a == null || a.graph == null) return;

            Event e = Event.current;
            var graph = a.graph;
            var tr = a.transform;

            int passiveId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveId);

            UpdateHover(a, e);

            // ─── 1. PositionHandle (선택 노드가 1개 이상이면) ───
            if (selected.Count > 0 && !isBoxDragging)
            {
                Vector3 center = ComputeSelectionCenterWorld(a);

                if (!multiHandleInitialized)
                {
                    multiHandlePos = center;
                    multiHandlePosLast = center;
                    multiHandleInitialized = true;
                }
                else
                {
                    // 선택이 바뀌었거나 노드가 외부에서 움직였으면 핸들 위치 동기화
                    // (드래그 중이 아닐 때만)
                    if (GUIUtility.hotControl == 0)
                    {
                        multiHandlePos = center;
                        multiHandlePosLast = center;
                    }
                }

                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(multiHandlePos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 deltaWorld = newPos - multiHandlePosLast;
                    if (deltaWorld.sqrMagnitude > 0f)
                    {
                        Undo.RegisterCompleteObjectUndo(a, "Move Nodes");
                        Vector3 deltaLocal = tr.InverseTransformVector(deltaWorld);
                        foreach (var id in selected)
                        {
                            var n = graph.GetNode(id);
                            if (n == null || n.isFixed) continue;
                            n.position += deltaLocal;
                        }
                        a.InvalidateFaceCache();
                        EditorUtility.SetDirty(a);
                        multiHandlePos = newPos;
                        multiHandlePosLast = newPos;
                    }
                }

                // Delete 키
                if (e.type == EventType.KeyDown &&
                    (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Delete Nodes");
                    foreach (var id in new List<int>(selected))
                    {
                        var n = graph.GetNode(id);
                        if (n != null && !n.isFixed) graph.RemoveNode(id);
                    }
                    selected.Clear();
                    multiHandleInitialized = false;
                    a.InvalidateFaceCache();
                    EditorUtility.SetDirty(a);
                    e.Use();
                }
            }

            // ─── 2. 마우스 다운 ───
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (hoveredNodeId != -1)
                {
                    // 노드 클릭
                    if (e.shift)
                    {
                        if (!selected.Add(hoveredNodeId)) selected.Remove(hoveredNodeId);
                    }
                    else
                    {
                        if (!selected.Contains(hoveredNodeId))
                        {
                            selected.Clear();
                            selected.Add(hoveredNodeId);
                        }
                        // 이미 포함된 노드를 그냥 클릭하면 그대로 (다중 선택 유지)
                    }
                    multiHandleInitialized = false;
                    GUIUtility.hotControl = passiveId;
                    e.Use();
                }
                else
                {
                    // 빈 공간 → 박스 드래그 시작
                    isBoxDragging = true;
                    boxStartGui = e.mousePosition;
                    boxEndGui = e.mousePosition;
                    GUIUtility.hotControl = passiveId;
                    e.Use();
                }
            }

            // ─── 3. 박스 드래그 진행 ───
            if (isBoxDragging && e.type == EventType.MouseDrag)
            {
                boxEndGui = e.mousePosition;
                window.Repaint();
                e.Use();
            }

            // ─── 4. 마우스 업 ───
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (isBoxDragging)
                {
                    isBoxDragging = false;
                    Rect r = MakeRect(boxStartGui, boxEndGui);
                    bool isClick = r.width < 3f && r.height < 3f;
                    if (!isClick)
                    {
                        if (!e.shift) selected.Clear();
                        foreach (var n in graph.nodes)
                        {
                            Vector3 world = tr.TransformPoint(n.position);
                            Vector2 gui = HandleUtility.WorldToGUIPoint(world);
                            if (r.Contains(gui)) selected.Add(n.id);
                        }
                        multiHandleInitialized = false;
                        EditorUtility.SetDirty(a);
                    }
                    else if (!e.shift)
                    {
                        // 그냥 빈 공간 단순 클릭이면 선택 해제
                        selected.Clear();
                        multiHandleInitialized = false;
                    }
                    e.Use();
                }
                if (GUIUtility.hotControl == passiveId) GUIUtility.hotControl = 0;
            }

            // ─── 5. ESC로 전체 해제 ───
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                selected.Clear();
                multiHandleInitialized = false;
                isBoxDragging = false;
                e.Use();
            }

            DrawHoverAndSelection(a);
            DrawBox(e);

            if (e.type == EventType.MouseMove) window.Repaint();
        }

        // ─────────── helpers ───────────
        Vector3 ComputeSelectionCenterWorld(RoadGraphAuthoring a)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var id in selected)
            {
                var n = a.graph.GetNode(id);
                if (n == null) continue;
                sum += a.transform.TransformPoint(n.position);
                count++;
            }
            return count > 0 ? sum / count : Vector3.zero;
        }

        static Rect MakeRect(Vector2 a, Vector2 b)
        {
            float x = Mathf.Min(a.x, b.x);
            float y = Mathf.Min(a.y, b.y);
            return new Rect(x, y, Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        void UpdateHover(RoadGraphAuthoring a, Event e)
        {
            if (isBoxDragging) { hoveredNodeId = -1; return; }
            if (e.type != EventType.MouseMove && e.type != EventType.Repaint &&
                e.type != EventType.MouseDown && e.type != EventType.Layout)
                return;

            float bestDistPx = 18f;
            int bestId = -1;
            Vector2 mouse = e.mousePosition;
            foreach (var n in a.graph.nodes)
            {
                Vector3 world = a.transform.TransformPoint(n.position);
                Vector2 gui = HandleUtility.WorldToGUIPoint(world);
                float d = Vector2.Distance(gui, mouse);
                if (d < bestDistPx) { bestDistPx = d; bestId = n.id; }
            }
            if (bestId != hoveredNodeId)
            {
                hoveredNodeId = bestId;
                SceneView.RepaintAll();
            }
        }

        void DrawHoverAndSelection(RoadGraphAuthoring a)
        {
            var graph = a.graph;
            var prevMatrix = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            if (hoveredNodeId != -1 && !selected.Contains(hoveredNodeId))
            {
                var n = graph.GetNode(hoveredNodeId);
                if (n != null)
                {
                    Handles.color = Color.white;
                    Handles.SphereHandleCap(0, n.position, Quaternion.identity,
                        a.nodeRadius * 1.4f, EventType.Repaint);
                }
            }
            foreach (var id in selected)
            {
                var n = graph.GetNode(id);
                if (n == null) continue;
                Handles.color = Color.yellow;
                Handles.SphereHandleCap(0, n.position, Quaternion.identity,
                    a.nodeRadius * 1.6f, EventType.Repaint);
            }

            Handles.matrix = prevMatrix;
        }

        void DrawBox(Event e)
        {
            if (!isBoxDragging) return;
            Handles.BeginGUI();
            Rect r = MakeRect(boxStartGui, boxEndGui);
            var fill = new Color(0.3f, 0.7f, 1f, 0.15f);
            var line = new Color(0.3f, 0.7f, 1f, 0.9f);
            EditorGUI.DrawRect(r, fill);
            // 외곽선
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), line);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), line);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), line);
            EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), line);
            Handles.EndGUI();
        }
    }
}