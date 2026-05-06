using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    public static class VoronoiRegionGenerator
    {
        public class Result
        {
            public List<List<Vector3>> polygons = new List<List<Vector3>>();
            public List<int> seedIndices = new List<int>(); // 각 폴리곤이 속한 시드 인덱스
        }

        /// <summary>
        /// 시드 점들로부터 Voronoi 셀 폴리곤들을 추출.
        /// 옵션 B: 격자 샘플링 + 마칭 스퀘어 + 단순화.
        /// </summary>
        public static Result Generate(List<Vector3> seeds,
            Vector3 boundsMin, Vector3 boundsMax,
            float gridResolution = 1f,
            int smoothingPasses = 1,
            float simplifyTolerance = 0.5f)
        {
            var result = new Result();
            if (seeds == null || seeds.Count == 0) return result;

            float w = boundsMax.x - boundsMin.x;
            float h = boundsMax.z - boundsMin.z;
            int nx = Mathf.Max(2, Mathf.CeilToInt(w / gridResolution));
            int nz = Mathf.Max(2, Mathf.CeilToInt(h / gridResolution));
            float dx = w / nx;
            float dz = h / nz;

            // 1) 각 격자 셀에 가장 가까운 시드 인덱스 할당
            int[,] owners = new int[nx + 1, nz + 1];
            for (int xi = 0; xi <= nx; xi++)
            {
                for (int zi = 0; zi <= nz; zi++)
                {
                    Vector2 p = new Vector2(boundsMin.x + xi * dx, boundsMin.z + zi * dz);
                    int best = 0;
                    float bestDist = float.MaxValue;
                    for (int s = 0; s < seeds.Count; s++)
                    {
                        Vector2 sp = new Vector2(seeds[s].x, seeds[s].z);
                        float d = (p - sp).sqrMagnitude;
                        if (d < bestDist) { bestDist = d; best = s; }
                    }
                    owners[xi, zi] = best;
                }
            }

            // 2) Smoothing (다수결 평활화)
            for (int pass = 0; pass < smoothingPasses; pass++)
                SmoothMajority(owners, nx, nz);

            // 3) 시드별로 외곽 폴리곤 추출
            float yRef = boundsMin.y;
            for (int s = 0; s < seeds.Count; s++)
            {
                var poly = ExtractContour(owners, s, nx, nz, boundsMin, dx, dz, yRef);
                if (poly == null || poly.Count < 3) continue;
                if (simplifyTolerance > 0f)
                    poly = DouglasPeucker(poly, simplifyTolerance);
                if (poly.Count < 3) continue;
                result.polygons.Add(poly);
                result.seedIndices.Add(s);
            }

            return result;
        }

        // ─── 평활화: 셀 주변 4방향 다수결 ───
        static void SmoothMajority(int[,] owners, int nx, int nz)
        {
            int[,] copy = (int[,])owners.Clone();
            for (int xi = 1; xi < nx; xi++)
            {
                for (int zi = 1; zi < nz; zi++)
                {
                    int self = copy[xi, zi];
                    int n = copy[xi, zi + 1], s = copy[xi, zi - 1];
                    int e = copy[xi + 1, zi], w = copy[xi - 1, zi];
                    // 주위 4개 중 같은 값이 3개 이상 있으면 그걸로
                    var counts = new Dictionary<int, int>();
                    Inc(counts, self); Inc(counts, n); Inc(counts, s); Inc(counts, e); Inc(counts, w);
                    int bestId = self, bestCount = 0;
                    foreach (var kv in counts)
                        if (kv.Value > bestCount) { bestCount = kv.Value; bestId = kv.Key; }
                    if (bestCount >= 3) owners[xi, zi] = bestId;
                }
            }
        }

        static void Inc(Dictionary<int, int> d, int k)
        {
            if (!d.ContainsKey(k)) d[k] = 0;
            d[k]++;
        }

        // ─── 시드 s의 영역 외곽선 추출 (단순 경계 추적) ───
        static List<Vector3> ExtractContour(int[,] owners, int targetId, int nx, int nz,
            Vector3 boundsMin, float dx, float dz, float yRef)
        {
            // 경계 셀 찾기: targetId 셀이지만 4방향 중 하나라도 다른 ID이거나 격자 끝
            // 그 셀들의 중심을 모아 시계방향으로 정렬
            var boundary = new List<Vector2>();
            for (int xi = 0; xi <= nx; xi++)
            {
                for (int zi = 0; zi <= nz; zi++)
                {
                    if (owners[xi, zi] != targetId) continue;
                    bool isBoundary = false;
                    if (xi == 0 || xi == nx || zi == 0 || zi == nz) isBoundary = true;
                    else if (owners[xi + 1, zi] != targetId ||
                             owners[xi - 1, zi] != targetId ||
                             owners[xi, zi + 1] != targetId ||
                             owners[xi, zi - 1] != targetId) isBoundary = true;
                    if (isBoundary)
                        boundary.Add(new Vector2(boundsMin.x + xi * dx, boundsMin.z + zi * dz));
                }
            }

            if (boundary.Count < 3) return null;

            // 중심으로부터 각도순 정렬 (볼록 영역 가정 — 거친 근사)
            Vector2 c = Vector2.zero;
            foreach (var p in boundary) c += p;
            c /= boundary.Count;

            boundary.Sort((a, b) =>
            {
                float angA = Mathf.Atan2(a.y - c.y, a.x - c.x);
                float angB = Mathf.Atan2(b.y - c.y, b.x - c.x);
                return angA.CompareTo(angB);
            });

            // Vector2 → Vector3 변환
            var result = new List<Vector3>(boundary.Count);
            foreach (var p in boundary)
                result.Add(new Vector3(p.x, yRef, p.y));
            return result;
        }

        // ─── Douglas-Peucker 폴리곤 단순화 ───
        public static List<Vector3> DouglasPeucker(List<Vector3> pts, float tolerance)
        {
            if (pts.Count < 3) return new List<Vector3>(pts);
            int n = pts.Count;
            bool[] keep = new bool[n];
            keep[0] = true;
            keep[n - 1] = true;
            DPRecursive(pts, 0, n - 1, tolerance, keep);
            var result = new List<Vector3>();
            for (int i = 0; i < n; i++) if (keep[i]) result.Add(pts[i]);
            return result;
        }

        static void DPRecursive(List<Vector3> pts, int first, int last, float tol, bool[] keep)
        {
            float maxDist = 0;
            int idx = -1;
            Vector2 a = new Vector2(pts[first].x, pts[first].z);
            Vector2 b = new Vector2(pts[last].x, pts[last].z);
            for (int i = first + 1; i < last; i++)
            {
                Vector2 p = new Vector2(pts[i].x, pts[i].z);
                float d = PerpDist(a, b, p);
                if (d > maxDist) { maxDist = d; idx = i; }
            }
            if (maxDist > tol && idx > 0)
            {
                keep[idx] = true;
                DPRecursive(pts, first, idx, tol, keep);
                DPRecursive(pts, idx, last, tol, keep);
            }
        }

        static float PerpDist(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-9f) return Vector2.Distance(a, p);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }
    }
}