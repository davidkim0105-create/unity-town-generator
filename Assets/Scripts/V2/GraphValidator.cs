using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TownGen.V2
{
    public static class GraphValidator
    {
        public class Issue
        {
            public string severity;   // "INFO", "WARN", "ERROR"
            public string message;
            public int? nodeId;
            public int? edgeId;
        }

        public static List<Issue> Validate(RoadGraph g)
        {
            var issues = new List<Issue>();
            if (g == null) { issues.Add(new Issue { severity = "ERROR", message = "Graph is null" }); return issues; }

            // 1. 고립 노드
            foreach (var n in g.nodes)
            {
                if (n.edgeIds.Count == 0)
                    issues.Add(new Issue { severity = "INFO", message = $"Isolated node N{n.id}", nodeId = n.id });
            }

            // 2. degree-1 노드 (막다른 가지)
            int deadEnds = 0;
            foreach (var n in g.nodes)
                if (n.edgeIds.Count == 1) deadEnds++;
            if (deadEnds > 0)
                issues.Add(new Issue { severity = "INFO", message = $"{deadEnds} dead-end node(s) (will be pruned for face extraction)" });

            // 3. 자기 루프 (a==b)
            foreach (var e in g.edges)
                if (e.nodeAId == e.nodeBId)
                    issues.Add(new Issue { severity = "ERROR", message = $"Self-loop edge E{e.id} (node N{e.nodeAId} → itself)", edgeId = e.id });

            // 4. 중복 엣지
            var seen = new HashSet<(int, int)>();
            foreach (var e in g.edges)
            {
                int a = Mathf.Min(e.nodeAId, e.nodeBId);
                int b = Mathf.Max(e.nodeAId, e.nodeBId);
                if (!seen.Add((a, b)))
                    issues.Add(new Issue { severity = "WARN", message = $"Duplicate edge between N{a} and N{b}", edgeId = e.id });
            }

            // 5. 끊긴 엣지 (참조 노드 없음)
            foreach (var e in g.edges)
            {
                if (g.GetNode(e.nodeAId) == null || g.GetNode(e.nodeBId) == null)
                    issues.Add(new Issue { severity = "ERROR", message = $"Edge E{e.id} references missing node", edgeId = e.id });
            }

            // 6. 매우 짧은 엣지 (< 0.01m)
            foreach (var e in g.edges)
            {
                var na = g.GetNode(e.nodeAId);
                var nb = g.GetNode(e.nodeBId);
                if (na == null || nb == null) continue;
                if (Vector3.Distance(na.position, nb.position) < 0.01f)
                    issues.Add(new Issue { severity = "WARN", message = $"Very short edge E{e.id} (< 0.01m)", edgeId = e.id });
            }

            // 7. 엣지 자기교차 (인접 아닌 것들끼리)
            int crossCount = 0;
            for (int i = 0; i < g.edges.Count; i++)
            {
                var e1 = g.edges[i];
                var a1 = g.GetNode(e1.nodeAId); var b1 = g.GetNode(e1.nodeBId);
                if (a1 == null || b1 == null) continue;

                for (int j = i + 1; j < g.edges.Count; j++)
                {
                    var e2 = g.edges[j];
                    if (e2.nodeAId == e1.nodeAId || e2.nodeAId == e1.nodeBId ||
                        e2.nodeBId == e1.nodeAId || e2.nodeBId == e1.nodeBId)
                        continue; // 끝점 공유

                    var a2 = g.GetNode(e2.nodeAId); var b2 = g.GetNode(e2.nodeBId);
                    if (a2 == null || b2 == null) continue;

                    if (RoadGraphOps.SegmentsIntersectXZ(a1.position, b1.position,
                        a2.position, b2.position, out _, out _, out _))
                        crossCount++;
                }
            }
            if (crossCount > 0)
                issues.Add(new Issue { severity = "WARN", message = $"{crossCount} edge intersection(s) without node — use Road Draw which auto-splits to fix" });

            return issues;
        }

        public static string FormatReport(List<Issue> issues)
        {
            var sb = new StringBuilder();
            int errors = 0, warns = 0, infos = 0;
            foreach (var i in issues)
            {
                if (i.severity == "ERROR") errors++;
                else if (i.severity == "WARN") warns++;
                else infos++;
            }
            sb.AppendLine($"[GraphValidator] Errors: {errors}, Warnings: {warns}, Info: {infos}");
            foreach (var i in issues)
                sb.AppendLine($"  [{i.severity}] {i.message}");
            return sb.ToString();
        }
    }
}