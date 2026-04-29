using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// RoadGraph에 대한 편집 헬퍼 (스냅, 교차, 안전한 엣지 추가).
    /// 모든 좌표는 그래프 로컬 좌표(=Authoring 컴포넌트의 transform 로컬).
    /// </summary>
    public static class RoadGraphOps
    {
        // ─────────────────────────────────────────
        // 스냅
        // ─────────────────────────────────────────

        /// <summary> pos에서 가장 가까운 노드 (threshold 안). 없으면 null. </summary>
        public static RoadNode FindNearestNode(RoadGraph g, Vector3 pos, float threshold)
        {
            RoadNode best = null;
            float bestDist = threshold;
            foreach (var n in g.nodes)
            {
                float d = Vector2.Distance(XZ(n.position), XZ(pos));
                if (d < bestDist) { bestDist = d; best = n; }
            }
            return best;
        }

        /// <summary> pos에서 가장 가까운 엣지와 그 위 점. threshold 밖이면 null. </summary>
        public static bool FindNearestEdge(
            RoadGraph g, Vector3 pos, float threshold,
            out RoadEdge edge, out Vector3 pointOnEdge, out float t)
        {
            edge = null; pointOnEdge = pos; t = 0f;
            float bestDist = threshold;
            foreach (var e in g.edges)
            {
                var na = g.GetNode(e.nodeAId);
                var nb = g.GetNode(e.nodeBId);
                if (na == null || nb == null) continue;
                Vector3 p = ClosestPointOnSegmentXZ(na.position, nb.position, pos, out float tt);
                float d = Vector2.Distance(XZ(p), XZ(pos));
                if (d < bestDist)
                {
                    bestDist = d;
                    edge = e;
                    pointOnEdge = p;
                    t = tt;
                }
            }
            return edge != null;
        }

        // ─────────────────────────────────────────
        // "포인트 확보" — 클릭한 위치에 노드를 보장
        // ─────────────────────────────────────────

        public enum SnapKind { CreatedNew, ExistingNode, OnEdgeSplit }

        /// <summary>
        /// pos 위치에 노드를 확보한다.
        /// - 가까운 노드 있으면 그 노드 반환 (스냅)
        /// - 가까운 엣지 있으면 그 위에 노드 삽입 (스플릿)
        /// - 둘 다 없으면 새 노드 생성
        /// </summary>
        public static RoadNode AcquireNodeAt(
            RoadGraph g, Vector3 pos,
            float nodeSnapDist, float edgeSnapDist,
            out SnapKind kind)
        {
            // 1. 노드 스냅 우선
            var near = FindNearestNode(g, pos, nodeSnapDist);
            if (near != null) { kind = SnapKind.ExistingNode; return near; }

            // 2. 엣지 스플릿
            if (FindNearestEdge(g, pos, edgeSnapDist, out var e, out var p, out _))
            {
                kind = SnapKind.OnEdgeSplit;
                return g.SplitEdgeAt(e.id, p);
            }

            // 3. 새 노드
            kind = SnapKind.CreatedNew;
            return g.AddNode(pos);
        }

        // ─────────────────────────────────────────
        // 안전한 엣지 추가 — 기존 엣지와의 교차를 모두 분할
        // ─────────────────────────────────────────

        /// <summary>
        /// fromId → toId 사이에 엣지를 만들되,
        /// 그 선분과 교차하는 모든 기존 엣지를 분할하고
        /// 교차점에 노드를 만들어 차례대로 연결한다.
        /// </summary>
        public static void AddEdgeWithSplits(
            RoadGraph g, int fromId, int toId,
            float coincidenceEps = 0.01f)
        {
            var nFrom = g.GetNode(fromId);
            var nTo = g.GetNode(toId);
            if (nFrom == null || nTo == null || fromId == toId) return;

            Vector3 a = nFrom.position;
            Vector3 b = nTo.position;

            // 새 선분과 교차하는 (기존 엣지, 교차점, t값) 수집
            var crossings = new List<(RoadEdge edge, Vector3 point, float tOnNew)>();
            foreach (var e in g.edges)
            {
                if (e.nodeAId == fromId || e.nodeBId == fromId ||
                    e.nodeAId == toId || e.nodeBId == toId)
                    continue;

                var pa = g.GetNode(e.nodeAId);
                var pb = g.GetNode(e.nodeBId);
                if (pa == null || pb == null) continue;     // 안전장치

                if (SegmentsIntersectXZ(a, b, pa.position, pb.position,
                    out Vector3 hit, out float tNew, out float tOld))
                {
                    if (tNew < coincidenceEps || tNew > 1f - coincidenceEps) continue;
                    if (tOld < coincidenceEps || tOld > 1f - coincidenceEps) continue;
                    crossings.Add((e, hit, tNew));
                }
            }

            // 새 선분 위에서 거리순 정렬
            crossings.Sort((x, y) => x.tOnNew.CompareTo(y.tOnNew));

            // 교차점마다 기존 엣지를 분할하고, 새 노드 ID 누적
            int prevId = fromId;
            foreach (var c in crossings)
            {
                var splitNode = g.SplitEdgeAt(c.edge.id, c.point);
                if (splitNode == null) continue;
                g.AddEdge(prevId, splitNode.id);
                prevId = splitNode.id;
            }
            g.AddEdge(prevId, toId);
        }

        // ─────────────────────────────────────────
        // 기하 유틸 (XZ 평면)
        // ─────────────────────────────────────────

        public static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

        public static Vector3 ClosestPointOnSegmentXZ(
            Vector3 a, Vector3 b, Vector3 p, out float t)
        {
            Vector2 ab = XZ(b) - XZ(a);
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-8f) { t = 0f; return a; }
            t = Mathf.Clamp01(Vector2.Dot(XZ(p) - XZ(a), ab) / len2);
            Vector3 r = a + (b - a) * t;
            r.y = Mathf.Lerp(a.y, b.y, t);
            return r;
        }

        /// <summary>
        /// XZ 평면에서 두 선분 교차. 끝점 접촉 포함.
        /// tA, tB는 각 선분에서 0~1 매개변수.
        /// </summary>
        public static bool SegmentsIntersectXZ(
            Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2,
            out Vector3 hit, out float tA, out float tB)
        {
            hit = default; tA = 0; tB = 0;
            Vector2 p = XZ(a1);
            Vector2 r = XZ(a2) - p;
            Vector2 q = XZ(b1);
            Vector2 s = XZ(b2) - q;

            float rxs = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(rxs) < 1e-8f) return false;   // 평행/공선

            Vector2 qp = q - p;
            tA = (qp.x * s.y - qp.y * s.x) / rxs;
            tB = (qp.x * r.y - qp.y * r.x) / rxs;

            if (tA < 0f || tA > 1f || tB < 0f || tB > 1f) return false;

            Vector2 hp = p + r * tA;
            float y = Mathf.Lerp(a1.y, a2.y, tA);
            hit = new Vector3(hp.x, y, hp.y);
            return true;
        }
        // ─────────────────────────────────────────
        // 노드 병합
        // ─────────────────────────────────────────

        /// <summary>
        /// sourceId 노드를 targetId 노드로 병합.
        /// source의 모든 엣지를 target에 다시 연결한 뒤 source 삭제.
        /// 자가 엣지(self-loop)와 중복 엣지는 자동 제거.
        /// </summary>
        public static void MergeNodes(RoadGraph g, int sourceId, int targetId)
        {
            if (sourceId == targetId) return;
            var src = g.GetNode(sourceId);
            var tgt = g.GetNode(targetId);
            if (src == null || tgt == null) return;

            // src에 연결된 엣지 목록을 복사 (수정 중)
            var edgesToReroute = new List<int>(src.edgeIds);

            foreach (var eid in edgesToReroute)
            {
                var e = g.GetEdge(eid);
                if (e == null) continue;
                int otherId = e.OtherNode(sourceId);

                // 1) self-loop 방지: 다른쪽이 곧 target이면 그냥 삭제
                if (otherId == targetId)
                {
                    g.RemoveEdge(eid);
                    continue;
                }

                // 2) 이미 target ↔ other 엣지가 있으면 중복 → 그냥 삭제
                if (g.FindEdge(targetId, otherId) != null)
                {
                    g.RemoveEdge(eid);
                    continue;
                }

                // 3) 그 외엔 엣지의 source 끝을 target으로 변경
                if (e.nodeAId == sourceId) e.nodeAId = targetId;
                else if (e.nodeBId == sourceId) e.nodeBId = targetId;

                // src에서 떼고 tgt에 붙이기
                src.edgeIds.Remove(eid);
                tgt.edgeIds.Add(eid);
            }

            // src 삭제 (이제 남은 엣지 없음)
            g.RemoveNode(sourceId);
        }
    }
}