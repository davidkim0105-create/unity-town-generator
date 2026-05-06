using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TownGen.V2.OSM
{
    /// <summary>
    /// OSMParser 결과를 RoadGraph로 변환.
    /// </summary>
    public static class OSMImporter
    {
        [System.Serializable]
        public class ImportOptions
        {
            public bool clearGraphFirst = true;
            public bool useOSMRoadWidths = true;
            public float defaultWidth = 3f;
            public float scaleFactor = 1f;       // 1 = 실제 크기, 0.1 = 10배 작게
            public bool excludeFootways = false; // footway/path/cycleway 제외
        }

        public class ImportResult
        {
            public int nodesAdded;
            public int edgesAdded;
            public int waysSkipped;
            public Vector3 boundsMin, boundsMax;
            public string summary;
        }

        public static ImportResult Import(string osmFilePath, RoadGraphAuthoring auth, ImportOptions opt)
        {
            var result = new ImportResult();
            if (!File.Exists(osmFilePath))
            {
                result.summary = $"File not found: {osmFilePath}";
                return result;
            }
            if (auth == null)
            {
                result.summary = "RoadGraphAuthoring is null";
                return result;
            }

            string xml = File.ReadAllText(osmFilePath);
            var data = OSMParser.Parse(xml);

            if (data.nodes.Count == 0)
            {
                result.summary = "No nodes in OSM data";
                return result;
            }

            if (opt.clearGraphFirst) auth.graph.Clear();

            // OSM nodeId → RoadNode.id 매핑
            var idMap = new Dictionary<long, int>();

            int waysSkipped = 0;
            foreach (var way in data.ways)
            {
                if (opt.excludeFootways && IsFootway(way.highwayType))
                {
                    waysSkipped++;
                    continue;
                }

                float width = opt.useOSMRoadWidths
                    ? OSMParser.HighwayToWidth(way.highwayType)
                    : opt.defaultWidth;

                // way의 노드들을 순서대로 그래프에 추가하고 연속으로 엣지 생성
                int prevRoadNodeId = -1;
                for (int i = 0; i < way.nodeIds.Count; i++)
                {
                    long osmId = way.nodeIds[i];
                    if (!data.nodes.TryGetValue(osmId, out Vector3 pos)) continue;

                    pos *= opt.scaleFactor;

                    int roadNodeId;
                    if (!idMap.TryGetValue(osmId, out roadNodeId))
                    {
                        var n = auth.graph.AddNode(pos);
                        roadNodeId = n.id;
                        idMap[osmId] = roadNodeId;
                        result.nodesAdded++;
                    }

                    if (prevRoadNodeId >= 0 && prevRoadNodeId != roadNodeId)
                    {
                        // 중복 엣지 방지
                        if (!HasEdge(auth.graph, prevRoadNodeId, roadNodeId))
                        {
                            var e = auth.graph.AddEdge(prevRoadNodeId, roadNodeId);
                            if (e != null)
                            {
                                e.width = width;
                                result.edgesAdded++;
                            }
                        }
                    }
                    prevRoadNodeId = roadNodeId;
                }
            }

            result.waysSkipped = waysSkipped;

            // bounds 계산
            if (auth.graph.nodes.Count > 0)
            {
                Vector3 mn = auth.graph.nodes[0].position;
                Vector3 mx = mn;
                foreach (var n in auth.graph.nodes)
                {
                    mn = Vector3.Min(mn, n.position);
                    mx = Vector3.Max(mx, n.position);
                }
                result.boundsMin = mn;
                result.boundsMax = mx;
            }

            auth.graph.InvalidateCache();
            auth.InvalidateFaceCache();

            float w = result.boundsMax.x - result.boundsMin.x;
            float h = result.boundsMax.z - result.boundsMin.z;
            result.summary = $"OSM Import: {result.nodesAdded} nodes, {result.edgesAdded} edges, " +
                             $"{waysSkipped} ways skipped. Size: {w:F0}×{h:F0}m";
            return result;
        }

        static bool HasEdge(RoadGraph g, int aId, int bId)
        {
            foreach (var e in g.edges)
                if ((e.nodeAId == aId && e.nodeBId == bId) ||
                    (e.nodeAId == bId && e.nodeBId == aId)) return true;
            return false;
        }

        static bool IsFootway(string highway)
        {
            return highway == "footway" || highway == "path" ||
                   highway == "cycleway" || highway == "pedestrian";
        }
    }
}