using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 블록 면적/비율에 따라 자동으로 적절한 패턴과 높이를 결정.
    /// QuickOSMCity 등에서 사용.
    /// </summary>
    public static class AdaptiveBuildingFiller
    {
        /// <summary>
        /// 모든 블록을 분석해서 면적별 패턴 + 높이를 적용.
        /// </summary>
        public static void ApplyAdaptive(RoadGraphAuthoring auth,
            BuildingFiller.Settings baseSettings,
            RegionAuthoring region = null)
        {
            if (auth == null) return;
            var blocksRoot = auth.transform.Find("_Blocks");
            if (blocksRoot == null) return;

            // 1) 블록 + 면적 수집
            var blockList = new List<(TownBlockV2 block, float area)>();
            foreach (Transform child in blocksRoot)
            {
                var b = child.GetComponent<TownBlockV2>();
                if (b == null || b.polygon == null) continue;
                float a = Mathf.Abs(SignedArea2D(b.polygon));
                blockList.Add((b, a));
            }
            if (blockList.Count == 0) return;

            // 2) 면적 percentile 임계값 계산
            var sortedAreas = new List<float>();
            foreach (var bl in blockList) sortedAreas.Add(bl.area);
            sortedAreas.Sort();

            float p30 = Percentile(sortedAreas, 0.30f);
            float p70 = Percentile(sortedAreas, 0.70f);
            float p90 = Percentile(sortedAreas, 0.90f);
            float medianArea = Percentile(sortedAreas, 0.50f);
            float maxArea = sortedAreas[sortedAreas.Count - 1];

            // 3) 기존 빌딩 제거
            foreach (Transform child in blocksRoot)
            {
                for (int i = child.childCount - 1; i >= 0; i--)
                {
                    var c = child.GetChild(i).gameObject;
                    if (Application.isPlaying) Object.Destroy(c);
                    else Object.DestroyImmediate(c);
                }
            }

            // 4) 블록마다 적용
            int idx = 0;
            int[] patternCounts = new int[6];  // Solid, Perimeter, UShape, LShape, Courtyard, SingleTower
            foreach (var (block, area) in blockList)
            {
                float aspect = ComputeAspectRatio(block.polygon);

                // 패턴: percentile 기반
                var pattern = PickPatternByPercentile(area, aspect, p30, p70, p90);
                var ps = new BlockPatternFiller.PatternSettings
                {
                    pattern = pattern,
                    perimeterDepth = PickDepth(area, pattern)
                };
                patternCounts[(int)pattern]++;

                // 빌딩 세팅
                BuildingFiller.Settings s;
                if (region != null)
                {
                    var center = ComputeCentroid(block.polygon);
                    var r = region.ResolveRegionAt(center);
                    if (r != null && r != region.defaultRegion)
                    {
                        s = r.ToFillerSettings(
                            baseSettings.seed + idx * 7919,
                            baseSettings.mode,
                            baseSettings.lotDepth,
                            baseSettings.fillInteriorRows);
                    }
                    else
                    {
                        s = AdaptHeightByArea(baseSettings, area, medianArea, maxArea);
                        s.seed = baseSettings.seed + idx * 7919;
                    }
                }
                else
                {
                    s = AdaptHeightByArea(baseSettings, area, medianArea, maxArea);
                    s.seed = baseSettings.seed + idx * 7919;
                }

                BlockPatternFiller.Fill(block, s, ps);
                idx++;
            }

            Debug.Log($"[AdaptiveBuildingFiller] {idx} blocks. Patterns: " +
                      $"Solid={patternCounts[0]}, Perimeter={patternCounts[1]}, " +
                      $"UShape={patternCounts[2]}, LShape={patternCounts[3]}, " +
                      $"Courtyard={patternCounts[4]}, Tower={patternCounts[5]}. " +
                      $"Thresholds: p30={p30:F0}, p70={p70:F0}, p90={p90:F0}");
        }

        // ─── Percentile 기반 패턴 선택 ───
        public static BlockPatternFiller.BlockPattern PickPatternByPercentile(
            float area, float aspect, float p30, float p70, float p90)
        {
            // 매우 작은 블록만 SingleTower (랜드마크)
            if (area < 30f)
                return BlockPatternFiller.BlockPattern.SingleTower;

            // 진짜 거대(상위 5%)만 Courtyard로 시각적 강조
            if (area > p90 * 1.5f)
                return BlockPatternFiller.BlockPattern.Courtyard;

            // 나머지 95%는 모두 Solid (빌딩 다수 유지)
            // 높이 다양성은 AdaptHeightByArea가 처리
            return BlockPatternFiller.BlockPattern.Solid;
        }

        // ─── Percentile 계산 ───
        static float Percentile(List<float> sorted, float p)
        {
            if (sorted.Count == 0) return 0;
            int idx = Mathf.Clamp(Mathf.RoundToInt(sorted.Count * p), 0, sorted.Count - 1);
            return sorted[idx];
        }

        // ─── 패턴 결정 ───
        public static BlockPatternFiller.BlockPattern PickPattern(float area, float aspectRatio)
        {
            if (area < 50f)            return BlockPatternFiller.BlockPattern.SingleTower;
            if (area < 1500f)          return BlockPatternFiller.BlockPattern.Solid;  // 대부분 Solid
            if (area < 4000f)
            {
                // 정사각형 큰 블록만 Perimeter, 긴 블록도 Solid
                return aspectRatio < 1.5f
                    ? BlockPatternFiller.BlockPattern.Perimeter
                    : BlockPatternFiller.BlockPattern.Solid;
            }
            return BlockPatternFiller.BlockPattern.Courtyard;
        }

        // ─── 띠 두께 ───
        static float PickDepth(float area, BlockPatternFiller.BlockPattern pattern)
        {
            if (pattern == BlockPatternFiller.BlockPattern.SingleTower) return 0f;
            if (pattern == BlockPatternFiller.BlockPattern.Solid) return 0f;

            // 면적 따라 5~10m
            float t = Mathf.Clamp01((area - 300f) / 2700f);
            return Mathf.Lerp(5f, 10f, t);
        }

        // ─── 면적 따라 높이 보정 ───
        static BuildingFiller.Settings AdaptHeightByArea(
            BuildingFiller.Settings src, float area, float medianArea, float maxArea)
        {
            var s = src;
            // 정규화: median 기준 0.7~1.5 배수
            float ratio = area / Mathf.Max(medianArea, 1f);
            float scale = Mathf.Lerp(0.7f, 1.5f, Mathf.Clamp01((ratio - 0.5f) / 2f));
            s.minHeight = src.minHeight * scale;
            s.maxHeight = src.maxHeight * scale;
            return s;
        }

        // ─── helpers ───
        static float SignedArea2D(List<Vector3> poly)
        {
            float s = 0; int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % n];
                s += a.x * b.z - b.x * a.z;
            }
            return s * 0.5f;
        }

        static float ComputeAspectRatio(List<Vector3> poly)
        {
            if (poly == null || poly.Count < 3) return 1f;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in poly)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }
            float w = maxX - minX, h = maxZ - minZ;
            if (w < 0.01f || h < 0.01f) return 1f;
            return Mathf.Max(w, h) / Mathf.Min(w, h);
        }

        static Vector3 ComputeCentroid(List<Vector3> poly)
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in poly) sum += p;
            return sum / poly.Count;
        }
    }
}