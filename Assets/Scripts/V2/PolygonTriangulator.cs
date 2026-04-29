using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 단순 폴리곤(자기교차 없음) 삼각화. Ear-clipping.
    /// 입력은 XZ 평면 폴리곤 (Vector3, Y는 무시).
    /// 결과 인덱스는 입력 순서 기준.
    /// </summary>
    public static class PolygonTriangulator
    {
        public static int[] Triangulate(List<Vector3> polyXZ)
        {
            int n = polyXZ.Count;
            if (n < 3) return new int[0];
            if (n == 3) return new int[] { 0, 1, 2 };

            // 1. CCW 보장 (ear-clipping은 CCW 가정)
            var pts = new List<Vector2>(n);
            for (int i = 0; i < n; i++) pts.Add(new Vector2(polyXZ[i].x, polyXZ[i].z));

            float area = SignedArea(pts);
            bool reversed = false;
            if (area < 0)
            {
                pts.Reverse();
                reversed = true;
            }

            // 인덱스 매핑 (원본 순서 추적)
            var idxMap = new int[n];
            for (int i = 0; i < n; i++) idxMap[i] = reversed ? (n - 1 - i) : i;

            // 2. 정점 인덱스 링크드 리스트 (남은 정점들)
            var remaining = new List<int>(n);
            for (int i = 0; i < n; i++) remaining.Add(i);

            var triangles = new List<int>((n - 2) * 3);
            int safety = 0, maxIter = n * n + 10;

            while (remaining.Count > 3 && safety++ < maxIter)
            {
                bool earFound = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int prev = remaining[(i - 1 + remaining.Count) % remaining.Count];
                    int curr = remaining[i];
                    int next = remaining[(i + 1) % remaining.Count];

                    if (!IsConvex(pts[prev], pts[curr], pts[next])) continue;

                    // 다른 정점이 이 삼각형 안에 있으면 ear 아님
                    bool containsOther = false;
                    for (int k = 0; k < remaining.Count; k++)
                    {
                        int idx = remaining[k];
                        if (idx == prev || idx == curr || idx == next) continue;
                        if (PointInTriangle(pts[idx], pts[prev], pts[curr], pts[next]))
                        {
                            containsOther = true;
                            break;
                        }
                    }
                    if (containsOther) continue;

                    triangles.Add(idxMap[prev]);
                    triangles.Add(idxMap[curr]);
                    triangles.Add(idxMap[next]);
                    remaining.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    // 비정상 폴리곤 → fan으로 fallback
                    Debug.LogWarning("[PolygonTriangulator] no ear found, falling back to fan");
                    triangles.Clear();
                    for (int i = 1; i < n - 1; i++)
                    {
                        triangles.Add(idxMap[0]);
                        triangles.Add(idxMap[i]);
                        triangles.Add(idxMap[i + 1]);
                    }
                    return triangles.ToArray();
                }
            }

            // 마지막 삼각형
            if (remaining.Count == 3)
            {
                triangles.Add(idxMap[remaining[0]]);
                triangles.Add(idxMap[remaining[1]]);
                triangles.Add(idxMap[remaining[2]]);
            }
            return triangles.ToArray();
        }

        // ─────────── helpers ───────────
        static float SignedArea(List<Vector2> pts)
        {
            float s = 0;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % n];
                s += a.x * b.y - b.x * a.y;
            }
            return s * 0.5f;
        }

        static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
        {
            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            return cross > 1e-6f;   // CCW에서 좌회전 = convex
        }

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}