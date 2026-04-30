using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Road Brush", typeof(RoadGraphAuthoring))]
    public class RoadBrushTool : EditorTool
    {
        // ─── 설정 (윈도우/인스펙터에서 조절) ───
        public static float spacing = 5f;
        public static float roadWidth = 4f;
        public static bool snapToExisting = true;
        public static float snapDistance = 2f;

        // ─── 내부 상태 ───
        bool isPainting = false;
        List<int> strokeNodeIds = new List<int>();
        Vector3 lastPoint;
        Vector3 currentMouse;
        bool hasCurrentMouse;

        public override GUIContent toolbarIcon =>
            new GUIContent("🖌", "Road Brush — 드래그로 도로 그리기");

        public override void OnToolGUI(EditorWindow window)
        {
            var auth = target as RoadGraphAuthoring;
            if (auth == null) return;
            var sv = window as SceneView;
            if (sv == null) return;

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            // ★ SceneView가 드래그를 가져가지 못하게 기본 컨트롤로 등록
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlId);
            }

            // 마우스 위치를 지면 평면(y=0)에 투영
            Vector3 worldPoint;
            if (!RaycastGroundPlane(e.mousePosition, out worldPoint))
                return;

            currentMouse = ApplySnaps(worldPoint, e);
            hasCurrentMouse = true;

            // ─── 입력 처리 ─── (controlId 기준 이벤트 타입 사용)
            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        BeginStroke(auth, currentMouse);
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isPainting)
                    {
                        TryAddSegment(auth, currentMouse);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0 && isPainting)
                    {
                        EndStroke(auth, currentMouse);
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Escape && isPainting)
                    {
                        CancelStroke();
                        e.Use();
                    }
                    break;
            }

            DrawHandles(e);

            // 정보 오버레이
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 280, 90), GUI.skin.box);
            GUILayout.Label("🖌 Road Brush", EditorStyles.boldLabel);
            GUILayout.Label($"Spacing: {spacing:F1}m   Width: {roadWidth:F1}m");
            GUILayout.Label($"Snap to Existing: {(snapToExisting ? "ON" : "OFF")} ({snapDistance:F1}m)");
            GUILayout.Label("Drag to paint · Shift=45° · Ctrl=1m grid · Esc=cancel",
                EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();

            sv.Repaint();
        }

        // ─── 스냅 ───
        Vector3 ApplySnaps(Vector3 p, Event e)
        {
            if (e.control)
            {
                p.x = Mathf.Round(p.x);
                p.z = Mathf.Round(p.z);
            }
            // 각도 스냅은 isPainting일 때만 의미 있음
            if (e.shift && isPainting && strokeNodeIds.Count > 0)
            {
                Vector3 from = lastPoint;
                Vector3 v = p - from; v.y = 0;
                float ang = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
                ang = Mathf.Round(ang / 45f) * 45f;
                float r = ang * Mathf.Deg2Rad;
                float len = v.magnitude;
                p = from + new Vector3(Mathf.Cos(r), 0, Mathf.Sin(r)) * len;
            }
            return p;
        }

        // ─── 스트로크 시작 ───
        void BeginStroke(RoadGraphAuthoring auth, Vector3 p)
        {
            Undo.RegisterCompleteObjectUndo(auth, "Road Brush Stroke");

            isPainting = true;
            strokeNodeIds.Clear();

            // 기존 노드/엣지 근처면 그리로 스냅
            int startId = ResolveAttachPoint(auth, p);
            strokeNodeIds.Add(startId);

            var startNode = FindNodeById(auth.graph, startId);
            lastPoint = startNode != null ? startNode.position : p;
        }

        // ─── 드래그 중 새 세그먼트 ───
        void TryAddSegment(RoadGraphAuthoring auth, Vector3 p)
        {
            // spacing이 snap 범위보다 작으면 새 노드가 자기 자신에 흡수되어 엣지 안 생김
            float effectiveSpacing = snapToExisting ? Mathf.Max(spacing, snapDistance * 1.2f) : spacing;
            float dist = Vector3.Distance(lastPoint, p);
            if (dist < effectiveSpacing) return;

            // 한 번 드래그에 spacing이 큰 폭이면 여러 노드 생성
            int steps = Mathf.FloorToInt(dist / effectiveSpacing);
            Vector3 dir = (p - lastPoint).normalized;

            for (int i = 1; i <= steps; i++)
            {
                Vector3 newPos = lastPoint + dir * effectiveSpacing;

                // 기존 노드/엣지 근처면 합치기
                int newId = ResolveAttachPoint(auth, newPos);
                int prevId = strokeNodeIds[strokeNodeIds.Count - 1];

                // 같은 노드면 엣지 만들지 않음
                if (newId != prevId)
                {
                    EnsureEdge(auth, prevId, newId);
                    strokeNodeIds.Add(newId);
                }

                var n = FindNodeById(auth.graph, newId);
                if (n != null) lastPoint = n.position;
                else lastPoint = newPos;
            }

            auth.InvalidateFaceCache();
            EditorUtility.SetDirty(auth);
        }

        // ─── 스트로크 종료 ───
        void EndStroke(RoadGraphAuthoring auth, Vector3 p)
        {
            // 마지막 점 처리 (남은 거리 < spacing이라 안 찍힌 마무리 점)
            float dist = Vector3.Distance(lastPoint, p);
            if (dist > spacing * 0.4f && strokeNodeIds.Count > 0)
            {
                int newId = ResolveAttachPoint(auth, p);
                int prevId = strokeNodeIds[strokeNodeIds.Count - 1];
                if (newId != prevId)
                {
                    EnsureEdge(auth, prevId, newId);
                    strokeNodeIds.Add(newId);
                }
            }

            isPainting = false;
            strokeNodeIds.Clear();
            auth.InvalidateFaceCache();
            EditorUtility.SetDirty(auth);
            SceneView.RepaintAll();
        }

        void CancelStroke()
        {
            // Undo로 처리됨 (BeginStroke에서 Undo 등록)
            Undo.PerformUndo();
            isPainting = false;
            strokeNodeIds.Clear();
        }

        // ─── 위치 → 노드 ID 해결 (기존 노드/엣지 흡수) ───
        int ResolveAttachPoint(RoadGraphAuthoring auth, Vector3 p)
        {
            if (snapToExisting)
            {
                // 1순위: 가까운 기존 노드
                RoadNode nearestNode = null;
                float bestDist = snapDistance;
                foreach (var n in auth.graph.nodes)
                {
                    float d = Vector3.Distance(n.position, p);
                    if (d < bestDist) { bestDist = d; nearestNode = n; }
                }
                if (nearestNode != null) return nearestNode.id;

                // 2순위: 가까운 엣지에 노드 삽입 (분할)
                RoadEdge nearestEdge = null;
                Vector3 nearestPointOnEdge = p;
                float bestEdgeDist = snapDistance;
                foreach (var ed in auth.graph.edges)
                {
                    var nA = FindNodeById(auth.graph, ed.nodeAId);
                    var nB = FindNodeById(auth.graph, ed.nodeBId);
                    if (nA == null || nB == null) continue;
                    Vector3 pt = ClosestPointOnSegment(nA.position, nB.position, p);
                    float d = Vector3.Distance(pt, p);
                    if (d < bestEdgeDist) { bestEdgeDist = d; nearestEdge = ed; nearestPointOnEdge = pt; }
                }
                if (nearestEdge != null)
                {
                    // 기존 SplitEdgeAt 사용
                    var splitNode = auth.graph.SplitEdgeAt(nearestEdge.id, nearestPointOnEdge);
                    if (splitNode != null) return splitNode.id;
                }
            }

            // 새 노드 생성
            var newNode = auth.graph.AddNode(p);
            return newNode.id;
        }

        // ─── 두 노드 간 엣지가 없으면 생성 ───
        void EnsureEdge(RoadGraphAuthoring auth, int aId, int bId)
        {
            // 이미 있는지 검사
            foreach (var e in auth.graph.edges)
            {
                if ((e.nodeAId == aId && e.nodeBId == bId) ||
                    (e.nodeAId == bId && e.nodeBId == aId))
                    return;
            }
            var newEdge = auth.graph.AddEdge(aId, bId);
            if (newEdge != null) newEdge.width = roadWidth;
        }

        // ─── 시각 미리보기 ───
        void DrawHandles(Event e)
        {
            if (hasCurrentMouse)
            {
                Handles.color = isPainting
                    ? new Color(0.4f, 1f, 0.4f, 0.9f)
                    : new Color(1f, 1f, 0.3f, 0.7f);
                Handles.SphereHandleCap(0, currentMouse, Quaternion.identity, 0.5f, EventType.Repaint);

                // snap 영역 원
                Handles.color = new Color(1f, 1f, 1f, 0.15f);
                Handles.DrawWireDisc(currentMouse, Vector3.up, snapDistance);

                if (isPainting)
                {
                    // 마지막 노드 → 현재 마우스 미리보기 선
                    Handles.color = new Color(0.4f, 1f, 0.4f, 0.6f);
                    Handles.DrawDottedLine(lastPoint, currentMouse, 4f);
                }
            }
        }

        // ─── 마우스 → 지면(y=0) 평면 위 점 ───
        static bool RaycastGroundPlane(Vector2 mousePos, out Vector3 worldPoint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
                return true;
            }
            worldPoint = Vector3.zero;
            return false;
        }

        // ─── id로 노드 찾기 (RoadGraph에 메서드 없음) ───
        static RoadNode FindNodeById(RoadGraph g, int id)
        {
            foreach (var n in g.nodes) if (n.id == id) return n;
            return null;
        }

        // ─── 점-선분 최단 거리 ───
        static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }
    }
}