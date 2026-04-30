using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 각 노드에 N각형 원반 메쉬 생성. 도로 quad의 트림된 끝을 덮어 깔끔하게 마감.
    /// 반경은 가장 굵은 연결 도로의 halfWidth × padding × extraCoverage.
    /// extraCoverage>1.0이면 도로 경계와 살짝 겹쳐 빈틈 방지.
    /// </summary>
    public static class IntersectionMeshBuilder
    {
        public static Mesh Build(RoadGraph graph, float yOffset = 0.02f,
                                 int segments = 12,
                                 float trimPadding = 1.0f,
                                 float extraCoverage = 1.15f)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 lift = Vector3.up * yOffset;

            if (graph == null || graph.nodes == null) return new Mesh { name = "IntersectionMesh" };

            foreach (var node in graph.nodes)
            {
                if (node.edgeIds == null || node.edgeIds.Count < 2) continue;

                float maxHalfW = 0f;
                foreach (var eid in node.edgeIds)
                {
                    var e = FindEdge(graph, eid);
                    if (e != null) maxHalfW = Mathf.Max(maxHalfW, e.width * 0.5f);
                }
                if (maxHalfW < 0.001f) continue;

                float radius = maxHalfW * trimPadding * extraCoverage;

                int centerIdx = verts.Count;
                verts.Add(node.position + lift);

                int firstIdx = verts.Count;
                for (int i = 0; i < segments; i++)
                {
                    float ang = (i / (float)segments) * Mathf.PI * 2f;
                    Vector3 p = node.position + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
                    verts.Add(p + lift);
                }

                for (int i = 0; i < segments; i++)
                {
                    int a = firstIdx + i;
                    int b = firstIdx + (i + 1) % segments;
                    tris.Add(centerIdx);
                    tris.Add(b);
                    tris.Add(a);
                }
            }

            var mesh = new Mesh { name = "IntersectionMesh" };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static RoadEdge FindEdge(RoadGraph g, int id)
        {
            foreach (var e in g.edges) if (e.id == id) return e;
            return null;
        }
    }
}