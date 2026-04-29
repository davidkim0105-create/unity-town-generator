using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    public enum RoadType { Main, Side, Alley, Pedestrian }

    [System.Serializable]
    public class RoadNode
    {
        public int id;
        public Vector3 position;          // 월드 좌표 (Y는 높이)
        public bool isFixed;              // true면 편집 잠금
        public List<int> edgeIds = new List<int>();   // 연결된 엣지 ID

        public RoadNode(int id, Vector3 pos)
        {
            this.id = id;
            this.position = pos;
        }
    }

    [System.Serializable]
    public class RoadEdge
    {
        public int id;
        public int nodeAId;
        public int nodeBId;
        public float width = 4f;
        public RoadType type = RoadType.Side;

        public RoadEdge(int id, int a, int b)
        {
            this.id = id;
            this.nodeAId = a;
            this.nodeBId = b;
        }

        public int OtherNode(int nodeId)
        {
            if (nodeId == nodeAId) return nodeBId;
            if (nodeId == nodeBId) return nodeAId;
            return -1;
        }
    }

    /// <summary>
    /// 도로 그래프. 노드와 엣지의 컨테이너.
    /// ID 기반 안전 참조. 삭제는 사전(Dictionary)으로 O(1).
    /// </summary>
    [System.Serializable]
    public class RoadGraph
    {
        // 직렬화 위해 List, 빠른 조회 위해 Dictionary 캐시 (런타임만)
        public List<RoadNode> nodes = new List<RoadNode>();
        public List<RoadEdge> edges = new List<RoadEdge>();
        public int nextNodeId = 0;
        public int nextEdgeId = 0;

        // 캐시 (직렬화 X)
        [System.NonSerialized] private Dictionary<int, RoadNode> nodeMap;
        [System.NonSerialized] private Dictionary<int, RoadEdge> edgeMap;
        [System.NonSerialized] private bool cacheBuilt = false;

        // ─────────── 캐시 관리 ───────────
        void EnsureCache()
        {
            if (cacheBuilt) return;
            nodeMap = new Dictionary<int, RoadNode>(nodes.Count);
            edgeMap = new Dictionary<int, RoadEdge>(edges.Count);
            foreach (var n in nodes) nodeMap[n.id] = n;
            foreach (var e in edges) edgeMap[e.id] = e;
            cacheBuilt = true;
        }
        public void InvalidateCache() { cacheBuilt = false; }

        public RoadNode GetNode(int id)
        {
            EnsureCache();
            return nodeMap.TryGetValue(id, out var n) ? n : null;
        }
        public RoadEdge GetEdge(int id)
        {
            EnsureCache();
            return edgeMap.TryGetValue(id, out var e) ? e : null;
        }

        // ─────────── 추가 ───────────
        public RoadNode AddNode(Vector3 pos)
        {
            var n = new RoadNode(nextNodeId++, pos);
            nodes.Add(n);
            if (cacheBuilt) nodeMap[n.id] = n;
            return n;
        }

        /// <summary> 두 노드 사이 엣지. 이미 있으면 그것 반환. </summary>
        public RoadEdge AddEdge(int nodeAId, int nodeBId)
        {
            if (nodeAId == nodeBId) return null;     // self-loop 금지
            var existing = FindEdge(nodeAId, nodeBId);
            if (existing != null) return existing;

            var e = new RoadEdge(nextEdgeId++, nodeAId, nodeBId);
            edges.Add(e);
            if (cacheBuilt) edgeMap[e.id] = e;

            GetNode(nodeAId)?.edgeIds.Add(e.id);
            GetNode(nodeBId)?.edgeIds.Add(e.id);
            return e;
        }

        public RoadEdge FindEdge(int nodeAId, int nodeBId)
        {
            var na = GetNode(nodeAId);
            if (na == null) return null;
            foreach (var eid in na.edgeIds)
            {
                var e = GetEdge(eid);
                if (e != null && (e.nodeAId == nodeBId || e.nodeBId == nodeBId))
                    return e;
            }
            return null;
        }

        // ─────────── 삭제 ───────────
        public void RemoveEdge(int edgeId)
        {
            var e = GetEdge(edgeId);
            if (e == null) return;
            GetNode(e.nodeAId)?.edgeIds.Remove(edgeId);
            GetNode(e.nodeBId)?.edgeIds.Remove(edgeId);
            edges.RemoveAll(x => x.id == edgeId);
            if (cacheBuilt) edgeMap.Remove(edgeId);
        }

        /// <summary> 노드 삭제 시 연결된 엣지도 모두 삭제. </summary>
        public void RemoveNode(int nodeId)
        {
            var n = GetNode(nodeId);
            if (n == null) return;
            // 복사본으로 순회 (수정 중)
            var toRemove = new List<int>(n.edgeIds);
            foreach (var eid in toRemove) RemoveEdge(eid);

            nodes.RemoveAll(x => x.id == nodeId);
            if (cacheBuilt) nodeMap.Remove(nodeId);
        }

        // ─────────── 편집 헬퍼 ───────────
        /// <summary>
        /// 엣지 중간에 노드 삽입. 기존 엣지는 두 개로 분할.
        /// 반환: 새로 생성된 노드.
        /// </summary>
        public RoadNode SplitEdgeAt(int edgeId, Vector3 splitPos)
        {
            var e = GetEdge(edgeId);
            if (e == null) return null;
            int aId = e.nodeAId, bId = e.nodeBId;
            float width = e.width;
            RoadType type = e.type;

            RemoveEdge(edgeId);
            var newNode = AddNode(splitPos);
            var e1 = AddEdge(aId, newNode.id);
            var e2 = AddEdge(newNode.id, bId);
            if (e1 != null) { e1.width = width; e1.type = type; }
            if (e2 != null) { e2.width = width; e2.type = type; }
            return newNode;
        }

        /// <summary> 모든 노드/엣지 제거. </summary>
        public void Clear()
        {
            nodes.Clear();
            edges.Clear();
            nextNodeId = 0;
            nextEdgeId = 0;
            InvalidateCache();
        }

        // ─────────── 조회 ───────────
        public IEnumerable<RoadEdge> EdgesOf(int nodeId)
        {
            var n = GetNode(nodeId);
            if (n == null) yield break;
            foreach (var eid in n.edgeIds)
            {
                var e = GetEdge(eid);
                if (e != null) yield return e;
            }
        }

        public int Degree(int nodeId)
        {
            var n = GetNode(nodeId);
            return n?.edgeIds.Count ?? 0;
        }
    }
}