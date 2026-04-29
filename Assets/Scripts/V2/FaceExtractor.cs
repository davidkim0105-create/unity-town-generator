using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// face 추출 결과. 폴리곤 = 노드 ID들의 시계방향 순서.
    /// </summary>
    public class FaceResult
    {
        public List<int> nodeIds = new List<int>();
        public List<Vector3> polygon = new List<Vector3>();
        public bool isOuter;          // 외곽(무한) face
        public float signedArea;      // 음수면 CCW(외곽), 양수면 CW(내부)
    }

    /// <summary>
    /// Planar graph face 추출.
    /// 알고리즘: Half-edge + 각도 정렬 + "가장 오른쪽 회전".
    /// </summary>
    public static class FaceExtractor
    {
        // 내부용 half-edge
        class HE
        {
            public int id;
            public int fromId;
            public int toId;
            public int twinId;
            public int nextId = -1;
            public int faceId = -1;
            public float angleFromOrigin;   // from 노드에서 to 노드로 가는 각도
        }

        /// <summary>
        /// face 추출 메인.
        /// pruneDeadEnds = true면 degree-1 노드를 미리 가지치기 (반복).
        /// </summary>
        public static List<FaceResult> Extract(RoadGraph graph, bool pruneDeadEnds = true)
        {
            // 0. 작업용 그래프 복사 (원본 보존)
            var work = CloneGraph(graph);

            // 1. 막다른 가지 가지치기
            if (pruneDeadEnds) PruneDeadEnds(work);

            if (work.edges.Count == 0) return new List<FaceResult>();

            // 2. half-edge 생성
            var halfEdges = new List<HE>();
            var heByFromTo = new Dictionary<(int, int), HE>();
            int heId = 0;
            foreach (var e in work.edges)
            {
                var na = work.GetNode(e.nodeAId);
                var nb = work.GetNode(e.nodeBId);
                if (na == null || nb == null) continue;

                var he1 = new HE
                {
                    id = heId++, fromId = e.nodeAId, toId = e.nodeBId,
                    angleFromOrigin = AngleXZ(na.position, nb.position)
                };
                var he2 = new HE
                {
                    id = heId++, fromId = e.nodeBId, toId = e.nodeAId,
                    angleFromOrigin = AngleXZ(nb.position, na.position)
                };
                he1.twinId = he2.id;
                he2.twinId = he1.id;
                halfEdges.Add(he1);
                halfEdges.Add(he2);
                heByFromTo[(he1.fromId, he1.toId)] = he1;
                heByFromTo[(he2.fromId, he2.toId)] = he2;
            }

            // 3. 노드별 outgoing half-edge를 각도순 정렬
            var outgoingByNode = new Dictionary<int, List<HE>>();
            foreach (var he in halfEdges)
            {
                if (!outgoingByNode.TryGetValue(he.fromId, out var list))
                {
                    list = new List<HE>();
                    outgoingByNode[he.fromId] = list;
                }
                list.Add(he);
            }
            foreach (var kv in outgoingByNode)
                kv.Value.Sort((x, y) => x.angleFromOrigin.CompareTo(y.angleFromOrigin));

            // 4. 각 half-edge의 next 결정
            //    he의 next = (he의 도착 노드에서 twin의 바로 이전 outgoing)
            foreach (var he in halfEdges)
            {
                var arriveNode = he.toId;
                var twin = halfEdges[he.twinId];
                var sorted = outgoingByNode[arriveNode];
                int idx = sorted.IndexOf(twin);
                int prevIdx = (idx - 1 + sorted.Count) % sorted.Count;
                he.nextId = sorted[prevIdx].id;
            }

            // 5. face 추적 (사이클 발견)
            var faces = new List<FaceResult>();
            int faceId = 0;
            const int SAFETY = 100000;
            foreach (var seed in halfEdges)
            {
                if (seed.faceId != -1) continue;
                var face = new FaceResult();
                int safety = 0;
                var cur = seed;
                while (cur.faceId == -1 && safety++ < SAFETY)
                {
                    cur.faceId = faceId;
                    face.nodeIds.Add(cur.fromId);
                    face.polygon.Add(work.GetNode(cur.fromId).position);
                    cur = halfEdges[cur.nextId];
                }
                if (safety >= SAFETY)
                {
                    Debug.LogError("[FaceExtractor] cycle traversal overflow");
                    break;
                }
                face.signedArea = SignedAreaXZ(face.polygon);
                face.isOuter = face.signedArea < 0;     // CCW = 외곽
                faces.Add(face);
                faceId++;
            }

            return faces;
        }

        // ─────────── 유틸 ───────────
        /// <summary> XZ 평면 각도 (radian, 0~2π). </summary>
        static float AngleXZ(Vector3 from, Vector3 to)
        {
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            float a = Mathf.Atan2(dz, dx);
            if (a < 0) a += Mathf.PI * 2f;
            return a;
        }

        /// <summary>
        /// 부호 있는 폴리곤 면적 (XZ 평면, Shoelace).
        /// 좌표계: Y=up, X→오른쪽, Z→앞. Y+에서 내려다본 기준.
        /// CCW(반시계) → 양수 → 유한(내부) face
        /// CW(시계)   → 음수 → 무한(외곽) face
        /// </summary>
        public static float SignedAreaXZ(List<Vector3> poly)
        {
            float sum = 0f;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % n];
                sum += (a.x * b.z - b.x * a.z);
            }
            return sum * 0.5f;   // 표준 Shoelace (부호 반전 X)
        }

        // ─────────── 가지치기 ───────────
        static void PruneDeadEnds(RoadGraph g)
        {
            bool changed = true;
            int safety = 0;
            while (changed && safety++ < 10000)
            {
                changed = false;
                var deadNodes = g.nodes.Where(n => n.edgeIds.Count <= 1).Select(n => n.id).ToList();
                foreach (var nid in deadNodes)
                {
                    g.RemoveNode(nid);
                    changed = true;
                }
            }
        }

        // ─────────── 그래프 복사 (원본 보존) ───────────
        static RoadGraph CloneGraph(RoadGraph src)
        {
            var dst = new RoadGraph();
            foreach (var n in src.nodes)
            {
                var nn = new RoadNode(n.id, n.position) { isFixed = n.isFixed };
                dst.nodes.Add(nn);
            }
            foreach (var e in src.edges)
            {
                var ne = new RoadEdge(e.id, e.nodeAId, e.nodeBId) { width = e.width, type = e.type };
                dst.edges.Add(ne);
            }
            // edgeIds 재구성
            foreach (var e in dst.edges)
            {
                dst.nodes.First(x => x.id == e.nodeAId).edgeIds.Add(e.id);
                dst.nodes.First(x => x.id == e.nodeBId).edgeIds.Add(e.id);
            }
            dst.nextNodeId = src.nextNodeId;
            dst.nextEdgeId = src.nextEdgeId;
            dst.InvalidateCache();
            return dst;
        }
    }
}