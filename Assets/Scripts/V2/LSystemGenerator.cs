using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    public static class LSystemGenerator
    {
        /// <summary>
        /// L-System이 기존 엣지를 분할/흡수할지 여부.
        /// false(기본): 기존 도로 폭 100% 보존, 새 노드만 생성
        /// true: 기존 엣지를 분할해서 자연스럽게 합침 (폭 복원 시도함)
        /// </summary>
        public static bool absorbExistingEdges = false;

        [System.Serializable]
        public struct Settings
        {
            public int iterations;
            public float segmentLength;
            public float segmentLengthJitter; // 0~1 (±%)
            public float angleJitter;          // degrees
            public float branchProbability;    // 0~1
            public float roadWidth;
            public float maxRadius;
            public float snapDistance;
            public int seed;
            public int initialBranches;        // 시드에서 시작 방향 수 (1=일자, 2=양쪽, 4=십자)
        }

        public static Settings Default => new Settings
        {
            iterations = 4,
            segmentLength = 10f,
            segmentLengthJitter = 0.3f,
            angleJitter = 20f,
            branchProbability = 0.18f,
            roadWidth = 2.5f,
            maxRadius = 60f,
            snapDistance = 3f,
            seed = 0,
            initialBranches = 4
        };

        struct Frontier { public int nodeId; public float angle; }

        public static int Grow(RoadGraph graph, Vector3 origin, Settings s)
        {
            if (graph == null) return 0;
            var rng = new System.Random(s.seed);
            int addedNodes = 0, addedEdges = 0;

            int startId = ResolveAttachPoint(graph, origin, s.snapDistance, ref addedNodes);
            if (startId < 0) return 0;

            var frontier = new List<Frontier>();
            int branches = Mathf.Clamp(s.initialBranches, 1, 8);
            float step = 360f / branches;
            for (int i = 0; i < branches; i++)
                frontier.Add(new Frontier { nodeId = startId, angle = i * step });

            for (int iter = 0; iter < s.iterations; iter++)
            {
                var next = new List<Frontier>();
                foreach (var f in frontier)
                {
                    var node = FindNodeById(graph, f.nodeId);
                    if (node == null) continue;
                    if (Vector3.Distance(node.position, origin) > s.maxRadius) continue;

                    // 1) 전진
                    TryExtendAngle(graph, origin, node,
                        f.angle + RangeF(rng, -s.angleJitter, s.angleJitter),
                        s, rng, next, ref addedNodes, ref addedEdges);

                    // 2) 좌 분기
                    if ((float)rng.NextDouble() < s.branchProbability)
                    {
                        float ang = f.angle + 90f + RangeF(rng, -s.angleJitter, s.angleJitter);
                        TryExtendAngle(graph, origin, node, ang, s, rng, next, ref addedNodes, ref addedEdges);
                    }
                    // 3) 우 분기
                    if ((float)rng.NextDouble() < s.branchProbability)
                    {
                        float ang = f.angle - 90f + RangeF(rng, -s.angleJitter, s.angleJitter);
                        TryExtendAngle(graph, origin, node, ang, s, rng, next, ref addedNodes, ref addedEdges);
                    }
                }
                frontier = next;
                if (frontier.Count == 0) break;
            }

            graph.InvalidateCache();
            return addedEdges;
        }

        static void TryExtendAngle(RoadGraph g, Vector3 origin, RoadNode from, float angle,
                                   Settings s, System.Random rng, List<Frontier> next,
                                   ref int addedN, ref int addedE)
        {
            float r = angle * Mathf.Deg2Rad;
            float lenJ = s.segmentLength *
                         (1f + RangeF(rng, -s.segmentLengthJitter, s.segmentLengthJitter));
            Vector3 dir = new Vector3(Mathf.Cos(r), 0, Mathf.Sin(r));
            Vector3 to = from.position + dir * lenJ;

            if (Vector3.Distance(to, origin) > s.maxRadius) return;

            int targetId = ResolveAttachPoint(g, to, s.snapDistance, ref addedN);
            if (targetId == from.id) return;

            if (HasEdge(g, from.id, targetId))
            {
                next.Add(new Frontier { nodeId = targetId, angle = angle });
                return;
            }

            var edge = g.AddEdge(from.id, targetId);
            if (edge != null)
            {
                edge.width = s.roadWidth;
                addedE++;
                next.Add(new Frontier { nodeId = targetId, angle = angle });
            }
        }

        static int ResolveAttachPoint(RoadGraph g, Vector3 p, float snap, ref int addedN)
        {
            // 1) 가까운 노드 (폭 정보와 무관하므로 항상 시도)
            RoadNode nearest = null;
            float bestD = snap;
            foreach (var n in g.nodes)
            {
                float d = Vector3.Distance(n.position, p);
                if (d < bestD) { bestD = d; nearest = n; }
            }
            if (nearest != null) return nearest.id;

            // 2) 가까운 엣지에 분할 삽입 (기존 도로 폭 보존을 위해 옵션화)
            if (absorbExistingEdges)
            {
                RoadEdge bestEdge = null;
                Vector3 bestPt = p;
                float bestED = snap;
                foreach (var e in g.edges)
                {
                    var nA = FindNodeById(g, e.nodeAId);
                    var nB = FindNodeById(g, e.nodeBId);
                    if (nA == null || nB == null) continue;
                    Vector3 pt = ClosestPointOnSegment(nA.position, nB.position, p);
                    float d = Vector3.Distance(pt, p);
                    if (d < bestED) { bestED = d; bestEdge = e; bestPt = pt; }
                }
                if (bestEdge != null)
                {
                    // 원래 엣지의 폭을 기억해두고
                    float origWidth = bestEdge.width;
                    var splitNode = g.SplitEdgeAt(bestEdge.id, bestPt);
                    if (splitNode != null)
                    {
                        // 분할된 두 새 엣지에 원래 폭 복원 (SplitEdgeAt이 어떻게 처리하든)
                        if (splitNode.edgeIds != null)
                        {
                            foreach (var eid in splitNode.edgeIds)
                            {
                                foreach (var e2 in g.edges)
                                {
                                    if (e2.id == eid) { e2.width = origWidth; break; }
                                }
                            }
                        }
                        return splitNode.id;
                    }
                }
            }

            // 3) 새 노드
            var newN = g.AddNode(p);
            addedN++;
            return newN.id;
        }

        static bool HasEdge(RoadGraph g, int aId, int bId)
        {
            foreach (var e in g.edges)
                if ((e.nodeAId == aId && e.nodeBId == bId) ||
                    (e.nodeAId == bId && e.nodeBId == aId)) return true;
            return false;
        }

        static RoadNode FindNodeById(RoadGraph g, int id)
        {
            foreach (var n in g.nodes) if (n.id == id) return n;
            return null;
        }

        static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Mathf.Max(Vector3.Dot(ab, ab), 1e-9f);
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }

        static float RangeF(System.Random r, float min, float max) =>
            min + (float)r.NextDouble() * (max - min);
    }
}