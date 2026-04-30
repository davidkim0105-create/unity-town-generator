using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 도로 그래프의 각 엣지를 width 만큼 양쪽으로 offset한 quad로 변환.
    /// trimEnds=true면 노드 근처는 잘라내어 교차로 메쉬가 깔끔하게 덮을 공간 확보.
    /// </summary>
    public static class RoadMeshBuilder
    {
        public static Mesh Build(RoadGraph graph, float yOffset = 0.01f,
                                 bool trimEnds = true, float trimPadding = 1.0f)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            Vector3 lift = Vector3.up * yOffset;

            if (graph == null || graph.edges == null) return new Mesh { name = "RoadMesh" };

            // 노드별 트림 반경 = (그 노드에 연결된 엣지들 중 가장 굵은 것의 halfWidth) × padding
            // degree<2(막다른길)는 트림 안 함 (끝이 노출되어도 디자인 의도일 수 있음)
            var trimRadius = new Dictionary<int, float>();
            foreach (var node in graph.nodes)
            {
                if (!trimEnds || node.edgeIds == null || node.edgeIds.Count < 2)
                {
                    trimRadius[node.id] = 0f;
                    continue;
                }
                float maxHalf = 0f;
                foreach (var eid in node.edgeIds)
                {
                    var e = FindEdge(graph, eid);
                    if (e != null) maxHalf = Mathf.Max(maxHalf, e.width * 0.5f);
                }
                trimRadius[node.id] = maxHalf * trimPadding;
            }

            foreach (var edge in graph.edges)
            {
                var nA = FindNode(graph, edge.nodeAId);
                var nB = FindNode(graph, edge.nodeBId);
                if (nA == null || nB == null) continue;

                Vector3 a = nA.position;
                Vector3 b = nB.position;
                Vector3 ab = b - a; ab.y = 0f;
                float len = ab.magnitude;
                if (len < 0.001f) continue;

                Vector3 dir = ab / len;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                float halfW = Mathf.Max(0.05f, edge.width * 0.5f);

                // 양쪽 끝 트림 (너무 짧은 엣지는 비율로 제한)
                float trA = trimRadius.TryGetValue(edge.nodeAId, out var ta) ? ta : 0f;
                float trB = trimRadius.TryGetValue(edge.nodeBId, out var tb) ? tb : 0f;
                float maxTrim = len * 0.45f;
                trA = Mathf.Min(trA, maxTrim);
                trB = Mathf.Min(trB, maxTrim);

                Vector3 a2 = a + dir * trA;
                Vector3 b2 = b - dir * trB;
                float effLen = (b2 - a2).magnitude;

                int baseIdx = verts.Count;
                verts.Add(a2 - right * halfW + lift);
                verts.Add(a2 + right * halfW + lift);
                verts.Add(b2 - right * halfW + lift);
                verts.Add(b2 + right * halfW + lift);

                float vScale = effLen / Mathf.Max(0.01f, edge.width);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, vScale));
                uvs.Add(new Vector2(1f, vScale));

                tris.Add(baseIdx + 0); tris.Add(baseIdx + 2); tris.Add(baseIdx + 1);
                tris.Add(baseIdx + 1); tris.Add(baseIdx + 2); tris.Add(baseIdx + 3);
            }

            var mesh = new Mesh { name = "RoadMesh" };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static RoadNode FindNode(RoadGraph g, int id)
        {
            foreach (var n in g.nodes) if (n.id == id) return n;
            return null;
        }
        static RoadEdge FindEdge(RoadGraph g, int id)
        {
            foreach (var e in g.edges) if (e.id == id) return e;
            return null;
        }
    }
}