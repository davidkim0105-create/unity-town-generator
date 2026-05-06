using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace TownGen.V2.OSM
{
    /// <summary>
    /// OpenStreetMap XML(.osm) 파싱.
    /// 위경도 → Unity 미터 좌표 변환 (간단한 등거리 투영, 작은 영역에 적합).
    /// </summary>
    public static class OSMParser
    {
        public class OSMData
        {
            public Dictionary<long, Vector3> nodes = new Dictionary<long, Vector3>();
            public List<OSMWay> ways = new List<OSMWay>();
            public double minLat, minLon, maxLat, maxLon;
            public Vector3 unityCenter;  // 변환된 영역 중심 (0,0,0 기준)
        }

        public class OSMWay
        {
            public long id;
            public List<long> nodeIds = new List<long>();
            public string highwayType;       // "primary", "residential", null 등
            public Dictionary<string, string> tags = new Dictionary<string, string>();
        }

        /// <summary>지구 반지름 (m)</summary>
        const double EARTH_RADIUS = 6378137.0;

        public static OSMData Parse(string xmlContent)
        {
            var data = new OSMData();
            var doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            // 1) bounds 추출
            var boundsNode = doc.SelectSingleNode("//bounds");
            if (boundsNode != null && boundsNode.Attributes != null)
            {
                data.minLat = ParseD(boundsNode.Attributes["minlat"]?.Value);
                data.minLon = ParseD(boundsNode.Attributes["minlon"]?.Value);
                data.maxLat = ParseD(boundsNode.Attributes["maxlat"]?.Value);
                data.maxLon = ParseD(boundsNode.Attributes["maxlon"]?.Value);
            }

            // bounds가 없으면 노드들 스캔으로 계산
            if (data.minLat == 0 && data.maxLat == 0)
            {
                ComputeBoundsFromNodes(doc, data);
            }

            double centerLat = (data.minLat + data.maxLat) * 0.5;
            double centerLon = (data.minLon + data.maxLon) * 0.5;

            // 2) 노드 파싱 (좌표 변환)
            var rawNodes = doc.SelectNodes("//node");
            if (rawNodes != null)
            {
                foreach (XmlNode n in rawNodes)
                {
                    if (n.Attributes == null) continue;
                    long id = ParseL(n.Attributes["id"]?.Value);
                    double lat = ParseD(n.Attributes["lat"]?.Value);
                    double lon = ParseD(n.Attributes["lon"]?.Value);
                    Vector3 pos = LatLonToUnity(lat, lon, centerLat, centerLon);
                    data.nodes[id] = pos;
                }
            }

            // 3) way 파싱 (highway 태그 있는 것만)
            var rawWays = doc.SelectNodes("//way");
            if (rawWays != null)
            {
                foreach (XmlNode w in rawWays)
                {
                    if (w.Attributes == null) continue;
                    var way = new OSMWay
                    {
                        id = ParseL(w.Attributes["id"]?.Value)
                    };

                    // 태그 수집
                    var tagNodes = w.SelectNodes("tag");
                    if (tagNodes != null)
                    {
                        foreach (XmlNode t in tagNodes)
                        {
                            if (t.Attributes == null) continue;
                            string k = t.Attributes["k"]?.Value;
                            string v = t.Attributes["v"]?.Value;
                            if (!string.IsNullOrEmpty(k))
                                way.tags[k] = v ?? "";
                        }
                    }

                    if (!way.tags.TryGetValue("highway", out way.highwayType))
                        continue; // 도로 아니면 스킵

                    // nd refs
                    var ndNodes = w.SelectNodes("nd");
                    if (ndNodes != null)
                    {
                        foreach (XmlNode nd in ndNodes)
                        {
                            if (nd.Attributes == null) continue;
                            long refId = ParseL(nd.Attributes["ref"]?.Value);
                            way.nodeIds.Add(refId);
                        }
                    }

                    if (way.nodeIds.Count >= 2)
                        data.ways.Add(way);
                }
            }

            return data;
        }

        static void ComputeBoundsFromNodes(XmlDocument doc, OSMData data)
        {
            var nodes = doc.SelectNodes("//node");
            if (nodes == null || nodes.Count == 0) return;

            double minLat = double.MaxValue, minLon = double.MaxValue;
            double maxLat = double.MinValue, maxLon = double.MinValue;
            foreach (XmlNode n in nodes)
            {
                if (n.Attributes == null) continue;
                double lat = ParseD(n.Attributes["lat"]?.Value);
                double lon = ParseD(n.Attributes["lon"]?.Value);
                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lon < minLon) minLon = lon;
                if (lon > maxLon) maxLon = lon;
            }
            data.minLat = minLat; data.maxLat = maxLat;
            data.minLon = minLon; data.maxLon = maxLon;
        }

        /// <summary>
        /// 위경도 → 로컬 미터 변환 (등거리 평면 투영, 작은 영역에 정확).
        /// 중심점 기준 X=동, Z=북.
        /// </summary>
        public static Vector3 LatLonToUnity(double lat, double lon, double centerLat, double centerLon)
        {
            double latRad = lat * System.Math.PI / 180.0;
            double centerLatRad = centerLat * System.Math.PI / 180.0;
            double dLat = (lat - centerLat) * System.Math.PI / 180.0;
            double dLon = (lon - centerLon) * System.Math.PI / 180.0;

            // 평면 근사 (영역 지름 < 수십 km에서 cm 단위 정확)
            double x = dLon * EARTH_RADIUS * System.Math.Cos(centerLatRad);
            double z = dLat * EARTH_RADIUS;
            return new Vector3((float)x, 0f, (float)z);
        }

        /// <summary>highway 종류 → 도로 폭(m) 매핑</summary>
        public static float HighwayToWidth(string highway)
        {
            if (string.IsNullOrEmpty(highway)) return 3f;
            switch (highway)
            {
                case "motorway": case "motorway_link": return 10f;
                case "trunk": case "trunk_link":       return 8f;
                case "primary": case "primary_link":   return 6f;
                case "secondary":                       return 5f;
                case "tertiary":                        return 4f;
                case "residential": case "unclassified": return 3.5f;
                case "service":                         return 2.5f;
                case "living_street":                   return 3f;
                case "pedestrian":                      return 2f;
                case "footway": case "path": case "cycleway": return 1.5f;
                default:                                return 3f;
            }
        }

        static double ParseD(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }
        static long ParseL(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            return long.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}