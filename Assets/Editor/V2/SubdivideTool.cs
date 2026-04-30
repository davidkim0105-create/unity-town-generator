using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Subdivide", typeof(RoadGraphAuthoring))]
    public class SubdivideTool : EditorTool
    {
        public enum SplitMode { LongestOpposite, TwoLongest }

        public static SplitMode mode = SplitMode.LongestOpposite;
        public static float minEdgeLength = 4f;
        public static float newRoadWidth = 4f;       // 기본 도로와 동일
        public static bool autoRebuild = true;       // 분할 후 자동 재빌드

        Vector3 currentMouse;
        FaceResult hoveredFace;

        public override GUIContent toolbarIcon =>
            new GUIContent("✂", "Subdivide — 블록 클릭으로 자동 분할");

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
            
            hoveredFace = FindContainingFace(auth, currentMouse);

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt && hoveredFace != null)
                    {
                        Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Subdivide Block");
                        bool ok = SubdivideFace(auth, hoveredFace);
                        if (ok)
                        {
                            auth.InvalidateFaceCache();
                            EditorUtility.SetDirty(auth);

                            if (autoRebuild)
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
                        }
                        else
                        {
                            Debug.Log("[Subdivide] 분할 실패: 변이 너무 짧거나 적절한 마주보는 변 없음");
                        }
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    // Ctrl+휠 = Min Edge Length, Shift+휠 = New Road Width
                    if (e.control)
                    {
                        minEdgeLength = Mathf.Clamp(
                            minEdgeLength + (-e.delta.y * 0.5f), 1f, 30f);
                        e.Use();
                    }
                    else if (e.shift)
                    {
                        newRoadWidth = Mathf.Clamp(
                            newRoadWidth + (-e.delta.y * 0.2f), 1f, 10f);
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
            if (hoveredFace == null || hoveredFace.polygon == null) return;

            // 호버된 face 하이라이트
            Handles.color = new Color(1f, 0.6f, 0.2f, 0.25f);
            Handles.DrawAAConvexPolygon(hoveredFace.polygon.ToArray());

            // 분할 미리보기
            var preview = ComputeSplitPreview(hoveredFace);
            if (preview.HasValue)
            {
                var sp = preview.Value;
                Handles.color = new Color(1f, 1f, 0f, 0.9f);
                Handles.DrawDottedLine(sp.midA, sp.midB, 6f);
                Handles.SphereHandleCap(0, sp.midA, Quaternion.identity, 0.4f, EventType.Repaint);
                Handles.SphereHandleCap(0, sp.midB, Quaternion.identity, 0.4f, EventType.Repaint);
            }
        }

        void DrawOverlay()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 320, 130), GUI.skin.box);
            GUILayout.Label("✂ Subdivide", EditorStyles.boldLabel);
            GUILayout.Label($"Mode: {mode}");
            GUILayout.Label($"New Road Width: {newRoadWidth:F1}m   Min Edge: {minEdgeLength:F1}m");
            GUILayout.Label($"Auto Rebuild: {(autoRebuild ? "ON" : "OFF")}");
            GUILayout.Label("Hover block · Click to split", EditorStyles.miniLabel);
            GUILayout.Label("Ctrl+Wheel=MinEdge · Shift+Wheel=Road Width",
                EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // ─── face 찾기 ───
        FaceResult FindContainingFace(RoadGraphAuthoring auth, Vector3 p)
        {
            var faces = auth.GetFaces();
            FaceResult best = null;
            float bestArea = float.MaxValue;
            foreach (var f in faces)
            {
                if (f.isOuter) continue;
                if (f.polygon == null || f.polygon.Count < 3) continue;
                if (PointInPolygon2D(p, f.polygon))
                {
                    float a = Mathf.Abs(f.signedArea);
                    if (a < bestArea) { bestArea = a; best = f; }
                }
            }
            return best;
        }

        // ─── 분할 미리보기 계산 ───
        struct SplitPreview { public Vector3 midA, midB; public int edgeAIdx, edgeBIdx; }

        SplitPreview? ComputeSplitPreview(FaceResult face)
        {
            if (face.nodeIds == null || face.nodeIds.Count < 4) return null;
            if (face.polygon == null || face.polygon.Count != face.nodeIds.Count) return null;

            int n = face.nodeIds.Count;
            float[] lens = new float[n];
            Vector3[] mids = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                Vector3 a = face.polygon[i];
                Vector3 b = face.polygon[(i + 1) % n];
                lens[i] = Vector3.Distance(a, b);
                mids[i] = (a + b) * 0.5f;
            }

            int idxA = -1, idxB = -1;

            if (mode == SplitMode.LongestOpposite)
            {
                float maxLen = 0;
                for (int i = 0; i < n; i++)
                    if (lens[i] > maxLen) { maxLen = lens[i]; idxA = i; }

                if (idxA < 0 || lens[idxA] < minEdgeLength * 2f) return null;

                Vector3 dirA = (face.polygon[(idxA + 1) % n] - face.polygon[idxA]).normalized;
                float bestScore = float.MinValue;
                for (int j = 0; j < n; j++)
                {
                    if (j == idxA) continue;
                    if (lens[j] < minEdgeLength) continue;
                    Vector3 dirB = (face.polygon[(j + 1) % n] - face.polygon[j]).normalized;
                    float parallel = Mathf.Abs(Vector3.Dot(dirA, dirB));
                    int dist = Mathf.Min(Mathf.Abs(j - idxA), n - Mathf.Abs(j - idxA));
                    float score = parallel * 2f + dist * 0.5f;
                    if (score > bestScore) { bestScore = score; idxB = j; }
                }
            }
            else // TwoLongest
            {
                int[] order = new int[n];
                for (int i = 0; i < n; i++) order[i] = i;
                System.Array.Sort(order, (x, y) => lens[y].CompareTo(lens[x]));
                idxA = order[0];
                if (n > 1) idxB = order[1];
                if (idxA < 0 || idxB < 0) return null;
                if (lens[idxA] < minEdgeLength || lens[idxB] < minEdgeLength) return null;
            }

            if (idxA < 0 || idxB < 0 || idxA == idxB) return null;
            return new SplitPreview { midA = mids[idxA], midB = mids[idxB], edgeAIdx = idxA, edgeBIdx = idxB };
        }

        // ─── 실제 분할 ───
        bool SubdivideFace(RoadGraphAuthoring auth, FaceResult face)
        {
            var preview = ComputeSplitPreview(face);
            if (!preview.HasValue) return false;
            var sp = preview.Value;

            int n = face.nodeIds.Count;
            int nodeA1 = face.nodeIds[sp.edgeAIdx];
            int nodeA2 = face.nodeIds[(sp.edgeAIdx + 1) % n];
            int nodeB1 = face.nodeIds[sp.edgeBIdx];
            int nodeB2 = face.nodeIds[(sp.edgeBIdx + 1) % n];

            int edgeAId = FindEdgeBetween(auth.graph, nodeA1, nodeA2);
            int edgeBId = FindEdgeBetween(auth.graph, nodeB1, nodeB2);
            if (edgeAId < 0 || edgeBId < 0) return false;

            // 중요: A 분할 후 B 분할 (B가 A에 영향받지 않음 — 공유 노드가 없는 마주보는 변이라 안전)
            var newNodeA = auth.graph.SplitEdgeAt(edgeAId, sp.midA);
            var newNodeB = auth.graph.SplitEdgeAt(edgeBId, sp.midB);
            if (newNodeA == null || newNodeB == null) return false;

            var newEdge = auth.graph.AddEdge(newNodeA.id, newNodeB.id);
            if (newEdge != null) newEdge.width = newRoadWidth;

            return true;
        }

        // ─── helpers ───
        static int FindEdgeBetween(RoadGraph g, int aId, int bId)
        {
            foreach (var e in g.edges)
            {
                if ((e.nodeAId == aId && e.nodeBId == bId) ||
                    (e.nodeAId == bId && e.nodeBId == aId))
                    return e.id;
            }
            return -1;
        }

        static bool PointInPolygon2D(Vector3 p, List<Vector3> poly)
        {
            int n = poly.Count;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = poly[i].x, zi = poly[i].z;
                float xj = poly[j].x, zj = poly[j].z;
                if (((zi > p.z) != (zj > p.z)) &&
                    (p.x < (xj - xi) * (p.z - zi) / (zj - zi + 1e-9f) + xi))
                    inside = !inside;
            }
            return inside;
        }

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
    }
}