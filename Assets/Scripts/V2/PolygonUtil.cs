using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    public static class PolygonUtil
    {
        public static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);

        /// <summary>
        /// XZ 평면에서 점이 폴리곤 안에 있는지 (ray casting).
        /// </summary>
        public static bool PointInPolygonXZ(Vector3 p, List<Vector3> poly)
        {
            int n = poly.Count;
            bool inside = false;
            float px = p.x, pz = p.z;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = poly[i].x, zi = poly[i].z;
                float xj = poly[j].x, zj = poly[j].z;
                bool intersect = ((zi > pz) != (zj > pz)) &&
                    (px < (xj - xi) * (pz - zi) / ((zj - zi) + 1e-12f) + xi);
                if (intersect) inside = !inside;
            }
            return inside;
        }

        /// <summary>
        /// 폴리곤의 OBB (Oriented Bounding Box). 가장 긴 변 기준으로 축 정렬.
        /// 출력: 중심, 축1(긴 변 방향, 단위벡터), 축2(수직, 단위벡터), size(축1, 축2 길이).
        /// </summary>
        public static void ComputeOBB(List<Vector3> poly,
            out Vector3 center, out Vector2 axis1, out Vector2 axis2, out Vector2 size)
        {
            // 가장 긴 변 찾기
            int n = poly.Count;
            float bestLen = -1f;
            Vector2 bestDir = Vector2.right;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = XZ(poly[i]);
                Vector2 b = XZ(poly[(i + 1) % n]);
                Vector2 d = b - a;
                float l = d.magnitude;
                if (l > bestLen) { bestLen = l; bestDir = d / Mathf.Max(l, 1e-8f); }
            }

            axis1 = bestDir;
            axis2 = new Vector2(-bestDir.y, bestDir.x);

            // 폴리곤을 (axis1, axis2) 좌표계로 투영해서 min/max 구하기
            float min1 = float.MaxValue, max1 = float.MinValue;
            float min2 = float.MaxValue, max2 = float.MinValue;
            foreach (var v in poly)
            {
                Vector2 p = XZ(v);
                float c1 = Vector2.Dot(p, axis1);
                float c2 = Vector2.Dot(p, axis2);
                if (c1 < min1) min1 = c1; if (c1 > max1) max1 = c1;
                if (c2 < min2) min2 = c2; if (c2 > max2) max2 = c2;
            }

            float cx1 = (min1 + max1) * 0.5f;
            float cx2 = (min2 + max2) * 0.5f;
            Vector2 c2d = axis1 * cx1 + axis2 * cx2;
            float y = poly.Count > 0 ? poly[0].y : 0f;
            center = new Vector3(c2d.x, y, c2d.y);
            size = new Vector2(max1 - min1, max2 - min2);
        }
    }
}