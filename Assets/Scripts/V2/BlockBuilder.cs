using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    public static class BlockBuilder
    {
        // ─────────────────────────────────────────
        // Blocks
        // ─────────────────────────────────────────

        public static void RebuildAllBlocks(RoadGraphAuthoring a,
            float roadInset = 1.5f,
            Material material = null,
            RegionAuthoring regions = null,
            float blockThickness = 0.1f)     // ★ 추가
        {
            if (a == null) return;
            var parent = GetOrCreateBlocksRoot(a);
            ClearChildren(parent);

            var faces = a.GetFaces();
            int idx = 0;
            foreach (var f in faces)
            {
                if (f.isOuter) continue;
                if (f.polygon == null || f.polygon.Count < 3) continue;

                var inset = InsetPolygon(f.polygon, roadInset);
                if (inset.Count < 3) continue;

                var go = new GameObject($"Block_{idx}");
                go.transform.SetParent(parent, false);

                var block = go.AddComponent<TownBlockV2>();
                block.polygon = inset;
                block.faceIndex = idx;
                block.signedArea = f.signedArea;

                Material useMat = material;
                if (regions != null)
                {
                    var center = ComputeCentroid(inset);
                    var r = regions.ResolveRegionAt(center);
                    if (r != null && r != regions.defaultRegion)
                    {
                        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                        useMat = new Material(sh) { color = r.color };
                    }
                }

                BuildMesh(go, inset, useMat, blockThickness);    // ★ 두께 전달
                idx++;
            }
        }

        public static void ClearAllBlocks(RoadGraphAuthoring a)
        {
            if (a == null) return;
            var parent = GetOrCreateBlocksRoot(a);
            ClearChildren(parent);
        }

        // ─────────────────────────────────────────
        // Buildings
        // ─────────────────────────────────────────

        public static void FillAllBlocksWithBuildings(RoadGraphAuthoring a,
            BuildingFiller.Settings fallback,
            RegionAuthoring regions = null)
        {
            if (a == null) return;
            var parent = GetOrCreateBlocksRoot(a);
            int idx = 0;
            foreach (Transform child in parent)
            {
                var block = child.GetComponent<TownBlockV2>();
                if (block == null) continue;

                BuildingFiller.Settings s = fallback;
                s.seed = fallback.seed + idx * 7919;

                if (regions != null)
                {
                    var center = ComputeCentroid(block.polygon);
                    var r = regions.ResolveRegionAt(center);
                    // ★ 명시적 Region (defaultRegion이 아닌 경우)일 때만 region 세팅 사용
                    if (r != null && r != regions.defaultRegion)
                    {
                        s = r.ToFillerSettings(
                            fallback.seed + idx * 7919,
                            fallback.mode,
                            fallback.lotDepth,
                            fallback.fillInteriorRows);
                    }
                    // 그 외 (Region 폴리곤 밖, 또는 default)는 인스펙터 fallback 사용
                }

                BuildingFiller.FillBlock(block, s);
                idx++;
            }
        }

        public static void ClearAllBuildings(RoadGraphAuthoring a)
        {
            if (a == null) return;
            var parent = GetOrCreateBlocksRoot(a);
            foreach (Transform child in parent)
            {
                var block = child.GetComponent<TownBlockV2>();
                if (block == null) continue;
                for (int i = child.childCount - 1; i >= 0; i--)
                {
                    var c = child.GetChild(i).gameObject;
#if UNITY_EDITOR
                    if (Application.isPlaying) Object.Destroy(c);
                    else Object.DestroyImmediate(c);
#else
                    Object.Destroy(c);
#endif
                }
            }
        }

        // ─────────────────────────────────────────
        // Inset (정확한 edge offset + line intersection)
        // ─────────────────────────────────────────

        static List<Vector3> InsetPolygon(List<Vector3> poly, float inset)
        {
            int n = poly.Count;
            if (n < 3 || inset <= 0f) return new List<Vector3>(poly);

            // ★ 안쪽 방향 판정용 centroid
            Vector2 centroid = Vector2.zero;
            foreach (var p in poly) centroid += new Vector2(p.x, p.z);
            centroid /= n;

            // 1. 각 변마다 안쪽으로 평행이동한 직선 계산
            //    선분: a → b
            //    안쪽 법선 방향 = centroid 쪽
            var lineP = new Vector2[n];   // 평행이동된 선의 한 점
            var lineD = new Vector2[n];   // 그 선의 방향벡터 (단위)

            for (int i = 0; i < n; i++)
            {
                Vector2 a = new Vector2(poly[i].x, poly[i].z);
                Vector2 b = new Vector2(poly[(i + 1) % n].x, poly[(i + 1) % n].z);
                Vector2 d = b - a;
                float len = d.magnitude;
                if (len < 1e-6f) { lineP[i] = a; lineD[i] = Vector2.right; continue; }
                d /= len;

                // 두 후보 법선 중 centroid 쪽
                Vector2 normal = new Vector2(-d.y, d.x);
                Vector2 mid = (a + b) * 0.5f;
                if (Vector2.Dot(centroid - mid, normal) < 0f) normal = -normal;

                lineP[i] = a + normal * inset;
                lineD[i] = d;
            }

            // 2. 이웃한 두 선의 교점 계산
            var result = new List<Vector3>(n);
            float yRef = poly[0].y;
            for (int i = 0; i < n; i++)
            {
                int prev = (i - 1 + n) % n;
                // prev 선과 i 선의 교점이 새 정점 i
                Vector2 p1 = lineP[prev], d1 = lineD[prev];
                Vector2 p2 = lineP[i], d2 = lineD[i];

                if (TryIntersect(p1, d1, p2, d2, out Vector2 hit))
                {
                    result.Add(new Vector3(hit.x, yRef, hit.y));
                }
                else
                {
                    // 평행 → 그냥 평행이동된 점 사용 (fallback)
                    Vector2 fallback = lineP[i];
                    result.Add(new Vector3(fallback.x, yRef, fallback.y));
                }
            }

            // 3. 결과 검증: 면적이 너무 작거나 부호가 뒤집히면 폐기
            float origArea = ComputeSignedArea2D(poly);
            float newArea = ComputeSignedArea2D(result);

            // 부호가 다르면 inset이 너무 커서 폴리곤이 뒤집힘
            if (Mathf.Sign(origArea) != Mathf.Sign(newArea)) return new List<Vector3>();
            // 너무 작으면 의미 없음
            if (Mathf.Abs(newArea) < 0.5f) return new List<Vector3>();

            // 4. 자기교차 검사 (간단: 인접하지 않은 변끼리 교차 있으면 폐기)
            if (HasSelfIntersection(result)) return new List<Vector3>();

            return result;
        }

        // ─── helpers ───
        static bool TryIntersect(Vector2 p1, Vector2 d1, Vector2 p2, Vector2 d2, out Vector2 hit)
        {
            hit = default;
            float cross = d1.x * d2.y - d1.y * d2.x;
            if (Mathf.Abs(cross) < 1e-6f) return false;     // 평행
            Vector2 dp = p2 - p1;
            float t = (dp.x * d2.y - dp.y * d2.x) / cross;
            hit = p1 + d1 * t;
            return true;
        }

        static float ComputeSignedArea2D(List<Vector3> poly)
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

        static bool HasSelfIntersection(List<Vector3> poly)
        {
            int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 a1 = new Vector2(poly[i].x, poly[i].z);
                Vector2 a2 = new Vector2(poly[(i + 1) % n].x, poly[(i + 1) % n].z);
                for (int j = i + 2; j < n; j++)
                {
                    // 인접 변 제외 (i와 i+1, 그리고 마지막-첫 번째도 제외)
                    if (i == 0 && j == n - 1) continue;
                    Vector2 b1 = new Vector2(poly[j].x, poly[j].z);
                    Vector2 b2 = new Vector2(poly[(j + 1) % n].x, poly[(j + 1) % n].z);
                    if (SegmentsIntersect(a1, a2, b1, b2)) return true;
                }
            }
            return false;
        }

        static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            Vector2 r = p2 - p1;
            Vector2 s = q2 - q1;
            float rxs = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(rxs) < 1e-8f) return false;
            Vector2 qp = q1 - p1;
            float t = (qp.x * s.y - qp.y * s.x) / rxs;
            float u = (qp.x * r.y - qp.y * r.x) / rxs;
            return t > 1e-4f && t < 1f - 1e-4f && u > 1e-4f && u < 1f - 1e-4f;
        }
        // ─────────────────────────────────────────
        // Mesh
        // ─────────────────────────────────────────

        static void BuildMesh(GameObject go, List<Vector3> poly, Material mat, float thickness = 0f)
        {
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();

            var mesh = new Mesh();
            mesh.name = "BlockPolygon";

            if (thickness <= 0.001f)
            {
                // 평면 (기존 동작)
                var verts = new Vector3[poly.Count];
                for (int i = 0; i < poly.Count; i++) verts[i] = poly[i];
                mesh.vertices = verts;
                mesh.triangles = PolygonTriangulator.Triangulate(poly);
            }
            else
            {
                // 두께 있는 prism (윗면 + 아랫면 + 옆면)
                int n = poly.Count;
                var verts = new List<Vector3>(n * 2);
                for (int i = 0; i < n; i++) verts.Add(new Vector3(poly[i].x, poly[i].y + thickness, poly[i].z));   // 0..n-1: top
                for (int i = 0; i < n; i++) verts.Add(new Vector3(poly[i].x, poly[i].y, poly[i].z));               // n..2n-1: bottom

                var tris = new List<int>();
                // 윗면
                var topTris = PolygonTriangulator.Triangulate(poly);
                tris.AddRange(topTris);
                // 아랫면 (반대 방향)
                for (int i = 0; i < topTris.Length; i += 3)
                {
                    tris.Add(topTris[i] + n);
                    tris.Add(topTris[i + 2] + n);
                    tris.Add(topTris[i + 1] + n);
                }
                // 옆면 (각 변마다 quad 두 삼각형)
                for (int i = 0; i < n; i++)
                {
                    int i2 = (i + 1) % n;
                    int tA = i, tB = i2, bA = i + n, bB = i2 + n;
                    // top facing outward — 두 삼각형
                    tris.Add(tA); tris.Add(bA); tris.Add(bB);
                    tris.Add(tA); tris.Add(bB); tris.Add(tB);
                }

                mesh.vertices = verts.ToArray();
                mesh.triangles = tris.ToArray();
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = new Color(0.7f, 0.85f, 0.7f);
            }
            mr.sharedMaterial = mat;
        }

        // ─────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────

        static Transform GetOrCreateBlocksRoot(RoadGraphAuthoring a)
        {
            const string NAME = "_Blocks";
            var t = a.transform.Find(NAME);
            if (t == null)
            {
                var go = new GameObject(NAME);
                go.transform.SetParent(a.transform, false);
                t = go.transform;
            }
            return t;
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

        static Vector3 ComputeCentroid(List<Vector3> poly)
        {
            if (poly == null || poly.Count == 0) return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var p in poly) sum += p;
            return sum / poly.Count;
        }

        static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
        static Vector2 Norm(Vector2 v) { float l = v.magnitude; return l < 1e-8f ? Vector2.zero : v / l; }
    }
}