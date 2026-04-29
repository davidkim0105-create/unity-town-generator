using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    /// <summary>
    /// 자주 쓰는 도시 형태 자동 생성.
    /// </summary>
    public static class AutoGraphPresets
    {
        // ─── 방사형 (Radial) ───
        public static void BuildRadial(RoadGraphAuthoring a,
            int spokes = 8, int rings = 3, float ringStep = 12f)
        {
            a.graph.Clear();
            // 중심 + 동심원 위 노드들
            var center = a.graph.AddNode(Vector3.zero);
            var ringNodes = new RoadNode[rings, spokes];
            for (int r = 0; r < rings; r++)
            {
                float radius = (r + 1) * ringStep;
                for (int s = 0; s < spokes; s++)
                {
                    float ang = s * (360f / spokes) * Mathf.Deg2Rad;
                    var p = new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius);
                    ringNodes[r, s] = a.graph.AddNode(p);
                }
            }
            // 방사 도로 (중심 → 첫 링, 링 → 다음 링)
            for (int s = 0; s < spokes; s++)
            {
                a.graph.AddEdge(center.id, ringNodes[0, s].id);
                for (int r = 0; r < rings - 1; r++)
                    a.graph.AddEdge(ringNodes[r, s].id, ringNodes[r + 1, s].id);
            }
            // 환상 도로
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < spokes; s++)
                    a.graph.AddEdge(ringNodes[r, s].id, ringNodes[r, (s + 1) % spokes].id);

            a.InvalidateFaceCache();
            UnityEditor.EditorUtility.SetDirty(a);
        }

        // ─── 다각형 외곽 (Polygon Loop) ───
        public static void BuildPolygonLoop(RoadGraphAuthoring a,
            int sides = 6, float radius = 25f)
        {
            a.graph.Clear();
            var nodes = new RoadNode[sides];
            for (int i = 0; i < sides; i++)
            {
                float ang = (i / (float)sides) * Mathf.PI * 2f;
                nodes[i] = a.graph.AddNode(new Vector3(Mathf.Cos(ang) * radius, 0, Mathf.Sin(ang) * radius));
            }
            for (int i = 0; i < sides; i++)
                a.graph.AddEdge(nodes[i].id, nodes[(i + 1) % sides].id);

            a.InvalidateFaceCache();
            UnityEditor.EditorUtility.SetDirty(a);
        }

        // ─── Jittered Grid (격자에 노이즈) ───
        public static void BuildJitteredGrid(RoadGraphAuthoring a,
            int n = 5, float step = 10f, float jitter = 2f, int seed = 0)
        {
            a.graph.Clear();
            var rng = new System.Random(seed);
            var nodes = new RoadNode[n, n];
            for (int x = 0; x < n; x++)
                for (int z = 0; z < n; z++)
                {
                    float jx = (float)(rng.NextDouble() - 0.5) * 2f * jitter;
                    float jz = (float)(rng.NextDouble() - 0.5) * 2f * jitter;
                    // 외곽 노드는 jitter 없이 정렬
                    if (x == 0 || x == n - 1) jx = 0;
                    if (z == 0 || z == n - 1) jz = 0;
                    nodes[x, z] = a.graph.AddNode(new Vector3(x * step + jx, 0, z * step + jz));
                }
            for (int x = 0; x < n; x++)
                for (int z = 0; z < n; z++)
                {
                    if (x < n - 1) a.graph.AddEdge(nodes[x, z].id, nodes[x + 1, z].id);
                    if (z < n - 1) a.graph.AddEdge(nodes[x, z].id, nodes[x, z + 1].id);
                }

            a.InvalidateFaceCache();
            UnityEditor.EditorUtility.SetDirty(a);
        }
    }
}