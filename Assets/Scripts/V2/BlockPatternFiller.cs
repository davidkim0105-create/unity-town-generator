using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    public static class BlockPatternFiller
    {
        public enum BlockPattern
        {
            Solid,
            Perimeter,
            UShape,
            LShape,
            Courtyard,
            SingleTower
        }

        [System.Serializable]
        public struct PatternSettings
        {
            public BlockPattern pattern;
            [Tooltip("Perimeter/U/L/Courtyard 띠 두께(m)")]
            public float perimeterDepth;

            public static PatternSettings Default => new PatternSettings
            {
                pattern = BlockPattern.Solid,
                perimeterDepth = 6f
            };
        }

        public static void Fill(TownBlockV2 block, BuildingFiller.Settings s, PatternSettings ps)
        {
            switch (ps.pattern)
            {
                case BlockPattern.Solid:
                    BuildingFiller.FillBlock(block, s);
                    break;
                case BlockPattern.Perimeter:
                    FillPerimeter(block, s, ps.perimeterDepth, SkipMode.None, uniformHeight: false);
                    break;
                case BlockPattern.UShape:
                    FillPerimeter(block, s, ps.perimeterDepth, SkipMode.LongestEdge, uniformHeight: false);
                    break;
                case BlockPattern.LShape:
                    FillPerimeter(block, s, ps.perimeterDepth, SkipMode.LongestPlusAdjacent, uniformHeight: false);
                    break;
                case BlockPattern.Courtyard:
                    FillPerimeter(block, s, ps.perimeterDepth * 1.4f, SkipMode.None, uniformHeight: true);
                    break;
                case BlockPattern.SingleTower:
                    FillSingleTower(block, s);
                    break;
            }
        }

        enum SkipMode { None, LongestEdge, LongestPlusAdjacent }

        static void FillPerimeter(TownBlockV2 block, BuildingFiller.Settings s,
            float depth, SkipMode skip, bool uniformHeight)
        {
            if (block.polygon == null || block.polygon.Count < 3) return;
            int n = block.polygon.Count;

            // 블록 크기 계산 → 띠 두께 자동 제한
            float minEdgeLen = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                float len = Vector3.Distance(block.polygon[i], block.polygon[(i + 1) % n]);
                if (len < minEdgeLen) minEdgeLen = len;
            }
            // 띠 두께를 블록 짧은 변의 35%로 제한 (너무 두꺼우면 안마당 사라짐)
            float effectiveDepth = Mathf.Min(depth, minEdgeLen * 0.35f);
            if (effectiveDepth < 0.5f)
            {
                // 블록이 너무 작으면 SingleTower로 fallback
                FillSingleTower(block, s);
                return;
            }

            // skip 변 결정
            var skipIdx = new HashSet<int>();
            if (skip != SkipMode.None)
            {
                int longest = 0; float maxLen = 0;
                for (int i = 0; i < n; i++)
                {
                    float len = Vector3.Distance(block.polygon[i], block.polygon[(i + 1) % n]);
                    if (len > maxLen) { maxLen = len; longest = i; }
                }
                skipIdx.Add(longest);
                if (skip == SkipMode.LongestPlusAdjacent)
                    skipIdx.Add((longest + 1) % n);
            }

            // 안쪽 방향용 centroid
            Vector3 centroid = Vector3.zero;
            foreach (var p in block.polygon) centroid += p;
            centroid /= n;

            var rng = new System.Random(s.seed);
            float uniformH = s.maxHeight;

            for (int i = 0; i < n; i++)
            {
                if (skipIdx.Contains(i)) continue;

                Vector3 a = block.polygon[i];
                Vector3 b = block.polygon[(i + 1) % n];
                Vector3 ab = b - a; ab.y = 0;
                float len = ab.magnitude;
                if (len < 1f) continue;
                Vector3 dir = ab / len;
                Vector3 normal = new Vector3(-dir.z, 0, dir.x);
                Vector3 mid = (a + b) * 0.5f;
                if (Vector3.Dot(centroid - mid, normal) < 0) normal = -normal;

                // 변별로 한 박스 (변 길이 - 양 끝 모서리 여유)
                float boxLen = len - effectiveDepth * 0.6f;
                if (boxLen < 1.5f) continue;

                if ((float)rng.NextDouble() > s.density) continue;

                float h = uniformHeight
                    ? uniformH
                    : (float)(s.minHeight + rng.NextDouble() * (s.maxHeight - s.minHeight));

                Vector3 center = mid + normal * effectiveDepth * 0.5f;
                CreateBox(block, center, dir, boxLen, effectiveDepth, h, s.material);
            }
        }

        static void FillSingleTower(TownBlockV2 block, BuildingFiller.Settings s)
        {
            if (block.polygon == null || block.polygon.Count < 3) return;

            PolygonUtil.ComputeOBB(block.polygon, out Vector3 center, out Vector2 axis1, out Vector2 axis2, out Vector2 size);

            float scale = 0.6f;
            Vector3 dir = new Vector3(axis1.x, 0, axis1.y);
            float boxLen = size.x * scale;
            float boxDepth = size.y * scale;
            if (boxLen < 1.5f || boxDepth < 1.5f) return;

            var rng = new System.Random(s.seed);
            // 단독 타워는 평균보다 키움 (랜드마크 느낌)
            float h = (s.minHeight + s.maxHeight) * 0.5f
                      + (float)rng.NextDouble() * (s.maxHeight - s.minHeight) * 0.5f;

            CreateBox(block, center, dir, boxLen, boxDepth, h, s.material);
        }

        static void CreateBox(TownBlockV2 block, Vector3 center, Vector3 forward,
            float length, float depth, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Building_Pattern";
            go.transform.SetParent(block.transform, false);
            // forward = 변 방향 (length 축), Cube의 local Z가 forward와 정렬되게
            go.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            go.transform.position = center + Vector3.up * (height * 0.5f);
            // local: x=depth(width), y=height, z=length(forward)
            go.transform.localScale = new Vector3(depth, height, length);

            if (mat != null)
            {
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }
            // 기본 충돌체 비활성 (씬 무거워짐 방지)
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
    }
}