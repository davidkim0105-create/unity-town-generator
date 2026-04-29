using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class TownAutoClassify
{
    public class Settings
    {
        public int tierCount = 4;
        public int minRegionSize = 3;
        public bool clearExisting = true;
        public bool protectLocked = true;
    }

    // 티어별 기본 색상 (저밀도 청색 → 고밀도 빨강)
    static readonly Color[] TIER_COLORS_LOW_TO_HIGH = new Color[]
    {
        new Color(0.4f, 0.7f, 0.95f, 0.35f),  // 청색
        new Color(0.5f, 0.85f, 0.6f, 0.35f),  // 청록
        new Color(0.6f, 0.85f, 0.4f, 0.35f),  // 연두
        new Color(0.95f, 0.85f, 0.3f, 0.35f), // 노랑
        new Color(1f, 0.6f, 0.2f, 0.35f),     // 주황
        new Color(0.95f, 0.3f, 0.3f, 0.35f),  // 빨강
    };

    public static int Classify(TownGenerator gen, Settings settings)
    {
        Transform root = gen.transform.Find("Blocks");
        if (root == null) return 0;

        var blocks = new List<TownBlock>();
        foreach (Transform t in root)
        {
            var b = t.GetComponent<TownBlock>();
            if (b != null) blocks.Add(b);
        }
        if (blocks.Count == 0) return 0;

        Undo.RecordObject(gen, "Auto Classify Regions");

        // 1. 잠긴 영역 블록 ID 수집
        var lockedBlockIds = new HashSet<long>();
        if (settings.protectLocked)
        {
            foreach (var region in gen.regions)
            {
                if (region.isLocked)
                {
                    foreach (var v in region.blockIds)
                        lockedBlockIds.Add(Key(v.x, v.y));
                }
            }
        }

        // 2. 기존 영역 삭제 (잠긴 건 보존)
        if (settings.clearExisting)
        {
            gen.regions.RemoveAll(r => !r.isLocked);
        }

        // 3. 분류 대상 블록 (잠긴 거 제외)
        var targetBlocks = blocks
            .Where(b => !lockedBlockIds.Contains(Key(b.gridX, b.gridZ)))
            .ToList();
        if (targetBlocks.Count == 0)
        {
            Debug.LogWarning("자동 분류: 분류 대상 블록이 없습니다.");
            return 0;
        }

        // 4. 티어 계산
        var blockTiers = new Dictionary<long, int>();
        var blockMap = new Dictionary<long, TownBlock>();
        foreach (var b in targetBlocks)
        {
            float density = b.EffectiveDensity;
            int tier = Mathf.Clamp(
                Mathf.FloorToInt(density * settings.tierCount),
                0, settings.tierCount - 1);
            long k = Key(b.gridX, b.gridZ);
            blockTiers[k] = tier;
            blockMap[k] = b;
        }

        // 5. Flood Fill 클러스터링
        var visited = new HashSet<long>();
        var clusters = new List<ClusterInfo>();

        foreach (var b in targetBlocks)
        {
            long key = Key(b.gridX, b.gridZ);
            if (visited.Contains(key)) continue;

            int tier = blockTiers[key];
            var cluster = new List<TownBlock>();
            FloodFill(b, tier, blockMap, blockTiers, visited, cluster);

            if (cluster.Count > 0)
            {
                clusters.Add(new ClusterInfo { blocks = cluster, tier = tier });
            }
        }

        // 6. 영역 생성 (큰 것부터, 같은 티어 내 A/B/C 부여)
        var sorted = clusters
            .OrderByDescending(c => c.blocks.Count)
            .ToList();

        var tierUsedCount = new Dictionary<int, int>();
        var newRegions = new List<TownRegion>();

        foreach (var c in sorted)
        {
            if (c.blocks.Count < settings.minRegionSize) continue;

            int tier = c.tier;
            if (!tierUsedCount.ContainsKey(tier))
                tierUsedCount[tier] = 0;
            int variant = tierUsedCount[tier];
            tierUsedCount[tier]++;

            string baseName = GetTierName(tier, settings.tierCount);
            string suffix = (variant > 0) ? " " + ((char)('A' + variant)).ToString() : "";
            string name = baseName + suffix;

            Color baseColor = GetTierColor(tier, settings.tierCount);
            Color color = ShiftColor(baseColor, variant);

            var region = new TownRegion
            {
                name = name,
                color = color,
                isLocked = false,
                showLabel = true,
            };
            foreach (var b in c.blocks)
                region.blockIds.Add(new Vector2Int(b.gridX, b.gridZ));

            newRegions.Add(region);
        }

        gen.regions.AddRange(newRegions);
        EditorUtility.SetDirty(gen);
        SceneView.RepaintAll();

        Debug.Log($"✅ 자동 분류 완료: {newRegions.Count}개 영역 생성 (대상 블록 {targetBlocks.Count}개)");
        return newRegions.Count;
    }

    // 클러스터 정보 임시 클래스
    class ClusterInfo
    {
        public List<TownBlock> blocks;
        public int tier;
    }

    static void FloodFill(
        TownBlock start, int targetTier,
        Dictionary<long, TownBlock> blockMap,
        Dictionary<long, int> blockTiers,
        HashSet<long> visited,
        List<TownBlock> result)
    {
        var queue = new Queue<TownBlock>();
        queue.Enqueue(start);
        visited.Add(Key(start.gridX, start.gridZ));

        int[] dx = { -1, 1, 0, 0 };
        int[] dz = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            var b = queue.Dequeue();
            result.Add(b);

            for (int i = 0; i < 4; i++)
            {
                int nx = b.gridX + dx[i];
                int nz = b.gridZ + dz[i];
                long nkey = Key(nx, nz);
                if (visited.Contains(nkey)) continue;
                if (!blockMap.ContainsKey(nkey)) continue;
                if (blockTiers[nkey] != targetTier) continue;
                visited.Add(nkey);
                queue.Enqueue(blockMap[nkey]);
            }
        }
    }

    static string GetTierName(int tier, int totalTiers)
    {
        switch (totalTiers)
        {
            case 2:
                return new string[] { "외곽", "도심" }[Mathf.Clamp(tier, 0, 1)];
            case 3:
                return new string[] { "외곽", "주거지", "도심" }[Mathf.Clamp(tier, 0, 2)];
            case 4:
                return new string[] { "외곽", "주거지", "오피스지구", "다운타운" }[Mathf.Clamp(tier, 0, 3)];
            case 5:
                return new string[] { "외곽", "주거지", "혼합지구", "오피스지구", "다운타운" }[Mathf.Clamp(tier, 0, 4)];
            case 6:
                return new string[] { "외곽", "전원", "주거지", "혼합지구", "오피스지구", "다운타운" }[Mathf.Clamp(tier, 0, 5)];
            default:
                return "Tier " + tier;
        }
    }

    static Color GetTierColor(int tier, int totalTiers)
    {
        float t = (float)tier / Mathf.Max(1, totalTiers - 1);
        int idx = Mathf.Clamp(
            Mathf.RoundToInt(t * (TIER_COLORS_LOW_TO_HIGH.Length - 1)),
            0, TIER_COLORS_LOW_TO_HIGH.Length - 1);
        return TIER_COLORS_LOW_TO_HIGH[idx];
    }

    static Color ShiftColor(Color baseColor, int variant)
    {
        if (variant == 0) return baseColor;
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        h = (h + variant * 0.07f) % 1f;
        Color shifted = Color.HSVToRGB(h, s, v);
        shifted.a = baseColor.a;
        return shifted;
    }

    static long Key(int x, int z) => ((long)x << 32) | (uint)z;
}