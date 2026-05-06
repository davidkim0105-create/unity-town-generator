using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 블록 폴리곤 안에 빌딩(박스)을 배치.
    /// 모드: GridOBB (기존), RoadFacing (신규).
    /// </summary>
    public static class BuildingFiller
    {
        public enum FillMode
        {
            GridOBB,        // OBB 격자 (기존)
            RoadFacing,     // 도로변 따라 정렬 (신규)
        }

        [System.Serializable]
        public struct Settings
        {
            [Tooltip("배치 알고리즘\n• GridOBB: 블록 전체에 격자로 배치 (큰 블록 빽빽이 채움)\n• RoadFacing: 도로변에 줄지어 정렬 (도시형, 안마당 빔)")]
            public FillMode mode;

            [Tooltip("빌딩 한 칸 폭 (m, 도로변 따라)\n• 작게(2~3): 빌딩 개수 많음\n• 크게(8~12): 빌딩 개수 적고 큼\n• 한 변 길이가 짧으면 1개로 고정")]
            public float lotSize;

            [Tooltip("(RoadFacing 전용) 빌딩 깊이 (도로 → 안쪽 m)\n• 작게(2~4): 얇은 빌딩 (상가, 가게)\n• 크게(6~10): 두꺼운 빌딩 (저택, 사무실)")]
            public float lotDepth;

            [Tooltip("칸 안에서 빌딩이 차지하는 비율 (0~0.6)\n• 0: 칸 가득 채움 (다닥다닥)\n• 0.15 (기본): 살짝 간격\n• 0.5: 빌딩 사이 간격 큼 (전원)")]
            public float lotMargin;

            [Tooltip("빌딩 최소 높이 (m). 한국 1층 ≈ 3m")]
            public float minHeight;

            [Tooltip("빌딩 최대 높이 (m). 빌딩 키는 min~max 사이 랜덤")]
            public float maxHeight;

            [Tooltip("각 칸에 빌딩이 생길 확률 (0~1)\n• 1: 모든 칸에 빌딩\n• 0.85 (기본): 약간 빈자리\n• 0.5: 듬성듬성 (전원/마을)")]
            public float density;

            [Tooltip("랜덤 시드 (정수). 같은 값이면 항상 같은 결과")]
            public int seed;

            public Material material;

            [Tooltip("(RoadFacing 전용) 도로변 1줄만 vs 안쪽 3줄까지 채우기\n• OFF: 외곽 한 줄만 (블록 가운데 빔, 자연스러움)\n• ON: 안쪽까지 빽빽 (밀집 도시)")]
            public bool fillInteriorRows;
        }

        public static Settings DefaultSettings => new Settings
        {
            mode = FillMode.RoadFacing,
            lotSize = 4f,
            lotDepth = 6f,
            lotMargin = 0.15f,
            minHeight = 3f,
            maxHeight = 12f,
            density = 0.85f,
            seed = 0,
            material = null,
            fillInteriorRows = false,
        };

        // ─────────────────────────────────────────
        // Entry
        // ─────────────────────────────────────────

        public static void FillBlock(TownBlockV2 block, Settings s)
        {
            if (block == null || block.polygon == null || block.polygon.Count < 3) return;
            ClearChildren(block.transform);

            switch (s.mode)
            {
                case FillMode.GridOBB:
                    FillGridOBB(block, s);
                    break;
                case FillMode.RoadFacing:
                    FillRoadFacing(block, s);
                    break;
            }
        }

        // ─────────────────────────────────────────
        // Mode 1: GridOBB (기존)
        // ─────────────────────────────────────────

        static void FillGridOBB(TownBlockV2 block, Settings s)
        {
            var rng = new System.Random(s.seed ^ block.faceIndex * 9176);
            PolygonUtil.ComputeOBB(block.polygon, out var center, out var axis1, out var axis2, out var size);

            int n1 = Mathf.Max(1, Mathf.FloorToInt(size.x / s.lotSize));
            int n2 = Mathf.Max(1, Mathf.FloorToInt(size.y / s.lotSize));
            float step1 = size.x / n1;
            float step2 = size.y / n2;

            float originY = center.y;
            Vector2 origin2D = new Vector2(center.x, center.z) - axis1 * (size.x * 0.5f) - axis2 * (size.y * 0.5f);

            Material mat = EnsureMaterial(s.material);
            int spawned = 0;

            for (int i = 0; i < n1; i++)
            for (int j = 0; j < n2; j++)
            {
                if (rng.NextDouble() > s.density) continue;
                Vector2 cell2D = origin2D + axis1 * ((i + 0.5f) * step1) + axis2 * ((j + 0.5f) * step2);
                Vector3 cellCenter = new Vector3(cell2D.x, originY, cell2D.y);
                if (!PolygonUtil.PointInPolygonXZ(cellCenter, block.polygon)) continue;

                float w = step1 * (1f - s.lotMargin);
                float d = step2 * (1f - s.lotMargin);
                float h = Mathf.Lerp(s.minHeight, s.maxHeight, (float)rng.NextDouble());
                SpawnBox(block.transform, cellCenter, axis1, w, d, h, mat, spawned++);
            }
        }

        // ─────────────────────────────────────────
        // Mode 2: RoadFacing (신규)
        // ─────────────────────────────────────────

        static void FillRoadFacing(TownBlockV2 block, Settings s)
        {
            var rng = new System.Random(s.seed ^ block.faceIndex * 9176);
            Material mat = EnsureMaterial(s.material);
            int n = block.polygon.Count;
            int spawned = 0;

            Vector3 centroid3 = Vector3.zero;
            foreach (var p in block.polygon) centroid3 += p;
            centroid3 /= n;
            Vector2 centroid = new Vector2(centroid3.x, centroid3.z);

            int rowsToFill = s.fillInteriorRows ? 3 : 1;

            // 이미 놓인 빌딩들의 OBB 정보 (정확한 회전 충돌 검사용)
            var pCenters = new List<Vector2>();
            var pAlongs = new List<Vector2>();
            var pWs = new List<float>();
            var pDs = new List<float>();

            for (int rowIdx = 0; rowIdx < rowsToFill; rowIdx++)
            {
                float rowOffset = rowIdx * s.lotDepth;

                for (int i = 0; i < n; i++)
                {
                    Vector3 a = block.polygon[i];
                    Vector3 b = block.polygon[(i + 1) % n];
                    Vector2 a2 = new Vector2(a.x, a.z);
                    Vector2 b2 = new Vector2(b.x, b.z);
                    Vector2 along = b2 - a2;
                    float edgeLen = along.magnitude;
                    if (edgeLen < s.lotSize * 0.5f) continue;
                    Vector2 alongDir = along / edgeLen;

                    Vector2 candidate = new Vector2(-alongDir.y, alongDir.x);
                    Vector2 midEdge = (a2 + b2) * 0.5f;
                    if (Vector2.Dot(centroid - midEdge, candidate) < 0f)
                        candidate = -candidate;
                    Vector2 inward = candidate;

                    int count = Mathf.Max(1, Mathf.FloorToInt(edgeLen / s.lotSize));
                    float step = edgeLen / count;
                    float w = step * (1f - s.lotMargin);
                    float d = s.lotDepth * (1f - s.lotMargin);

                    for (int k = 0; k < count; k++)
                    {
                        if (rng.NextDouble() > s.density) continue;

                        Vector2 onEdge = a2 + alongDir * ((k + 0.5f) * step);
                        Vector2 center2 = onEdge + inward * (rowOffset + s.lotDepth * 0.5f);

                        Vector3 center3 = new Vector3(center2.x, a.y, center2.y);
                        if (!PolygonUtil.PointInPolygonXZ(center3, block.polygon)) continue;

                        // OBB 회전 충돌 검사
                        bool overlap = false;
                        for (int p = 0; p < pCenters.Count; p++)
                        {
                            if (ObbOverlap(center2, alongDir, w, d,
                                        pCenters[p], pAlongs[p], pWs[p], pDs[p]))
                            {
                                overlap = true;
                                break;
                            }
                        }
                        if (overlap) continue;

                        pCenters.Add(center2);
                        pAlongs.Add(alongDir);
                        pWs.Add(w);
                        pDs.Add(d);

                        float h = Mathf.Lerp(s.minHeight, s.maxHeight, (float)rng.NextDouble());
                        SpawnBox(block.transform, center3, alongDir, w, d, h, mat, spawned++);
                    }
                }
            }
        }
        // ─────────────────────────────────────────
        // Spawn
        // ─────────────────────────────────────────

        static void SpawnBox(Transform parent, Vector3 baseCenter, Vector2 facingAxis,
            float w, float d, float h, Material mat, int idx)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Building_{idx}";
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            go.transform.SetParent(parent, false);
            float angle = Mathf.Atan2(facingAxis.y, facingAxis.x) * Mathf.Rad2Deg;
            go.transform.localPosition = baseCenter + Vector3.up * (h * 0.5f);
            go.transform.localRotation = Quaternion.Euler(0, -angle, 0);
            go.transform.localScale = new Vector3(w, h, d);

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
        }

        /// <summary>
        /// 두 OBB(회전 사각형) 사이의 빠른 근사 충돌 검사.
        /// 두 중심을 잇는 방향에서 각 OBB가 차지하는 반지름의 합이 거리보다 크면 겹침.
        /// </summary>
        static bool ObbOverlap(Vector2 c1, Vector2 along1, float w1, float d1,
                                Vector2 c2, Vector2 along2, float w2, float d2)
        {
            Vector2 diff = c2 - c1;
            float dist = diff.magnitude;
            if (dist < 1e-6f) return true;
            Vector2 dir = diff / dist;

            Vector2 in1 = new Vector2(-along1.y, along1.x);
            Vector2 in2 = new Vector2(-along2.y, along2.x);

            float r1 = Mathf.Abs(Vector2.Dot(dir, along1)) * w1 * 0.5f
                    + Mathf.Abs(Vector2.Dot(dir, in1)) * d1 * 0.5f;
            float r2 = Mathf.Abs(Vector2.Dot(dir, along2)) * w2 * 0.5f
                    + Mathf.Abs(Vector2.Dot(dir, in2)) * d2 * 0.5f;

            return dist < (r1 + r2) * 0.92f;
        }


        // ─────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────

        static Material EnsureMaterial(Material mat)
        {
            if (mat != null) return mat;
            var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(sh) { color = new Color(0.85f, 0.8f, 0.7f) };
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var c = parent.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (Application.isPlaying) Object.Destroy(c);
                else Object.DestroyImmediate(c);
#else
                Object.Destroy(c);
#endif
            }
        }

        static float ComputeSignedAreaXZ(List<Vector3> poly)
        {
            float s = 0;
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % n];
                s += a.x * b.z - b.x * a.z;
            }
            return s * 0.5f;
        }
    }
}