using UnityEditor;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [InitializeOnLoad]
    public static class RoadGraphGizmos
    {
        static RoadGraphGizmos()
        {
            // 매 프레임 SceneView에서 호출
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sv)
        {
            // 모든 RoadGraphAuthoring 찾아서 그리기
            var authorings = Object.FindObjectsByType<RoadGraphAuthoring>(FindObjectsSortMode.None);
            foreach (var a in authorings) DrawAuthoring(a);
        }

        static void DrawAuthoring(RoadGraphAuthoring a)
        {
            if (a == null || a.graph == null) return;
            var origMatrix = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            // 1. Faces
            if (a.showFaces)
            {
                var faces = a.GetFaces();
                foreach (var f in faces)
                {
                    if (f.polygon == null || f.polygon.Count < 3) continue;

                    var verts = new Vector3[f.polygon.Count];
                    for (int i = 0; i < f.polygon.Count; i++)
                        verts[i] = f.polygon[i] + Vector3.up * a.edgeLift * 0.5f;

                    if (f.isOuter)
                    {
                        // 외곽: 흰 엣지선보다 위에, 굵은 빨간 선으로
                        Handles.color = new Color(1f, 0.2f, 0.2f, 0.9f);
                        var loop = new Vector3[verts.Length + 1];
                        for (int i = 0; i < verts.Length; i++)
                            loop[i] = verts[i] + Vector3.up * 0.5f;   // 충분히 위로 띄움
                        loop[verts.Length] = loop[0];
                        Handles.DrawAAPolyLine(5f, loop);              // 굵게
                    }
                    else
                    {
                        // 내부: 채우기 + 외곽선
                        Handles.color = a.innerFaceColor;
                        Handles.DrawAAConvexPolygon(verts);

                        Color outline = new Color(
                            a.innerFaceColor.r, a.innerFaceColor.g, a.innerFaceColor.b,
                            Mathf.Min(1f, a.innerFaceColor.a * 4f));
                        Handles.color = outline;
                        var loop = new Vector3[verts.Length + 1];
                        System.Array.Copy(verts, loop, verts.Length);
                        loop[verts.Length] = verts[0];
                        Handles.DrawAAPolyLine(2f, loop);
                    }
                }
            }

            // 2. Edges
            if (a.showEdges)
            {
                Handles.color = a.edgeColor;
                foreach (var e in a.graph.edges)
                {
                    var na = a.graph.GetNode(e.nodeAId);
                    var nb = a.graph.GetNode(e.nodeBId);
                    if (na == null || nb == null) continue;
                    Vector3 pa = na.position + Vector3.up * a.edgeLift;
                    Vector3 pb = nb.position + Vector3.up * a.edgeLift;
                    Handles.DrawAAPolyLine(Mathf.Max(2f, e.width), pa, pb);
                }
            }

            // 3. Nodes
            if (a.showNodes)
            {
                foreach (var n in a.graph.nodes)
                {
                    Handles.color = n.isFixed ? Color.gray : a.nodeColor;
                    Handles.SphereHandleCap(0, n.position, Quaternion.identity,
                        a.nodeRadius, EventType.Repaint);
                }
            }

            // 4. Labels (선택 시만)
            if (a.showLabels)
            {
                var style = new GUIStyle(EditorStyles.miniLabel);
                style.normal.textColor = Color.white;
                foreach (var n in a.graph.nodes)
                    Handles.Label(n.position + Vector3.up * 0.5f, $"N{n.id}", style);
                foreach (var e in a.graph.edges)
                {
                    var na = a.graph.GetNode(e.nodeAId);
                    var nb = a.graph.GetNode(e.nodeBId);
                    if (na == null || nb == null) continue;
                    Vector3 mid = (na.position + nb.position) * 0.5f + Vector3.up * 0.6f;
                    Handles.Label(mid, $"E{e.id}", style);
                }
            }

            Handles.matrix = origMatrix;
        }
    }
}