using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 입력 좌표를 그리드/각도/노드 등에 스냅.
    /// </summary>
    public static class SnapUtil
    {
        public struct Settings
        {
            public bool gridSnap;       // Ctrl
            public bool angleSnap;      // Shift
            public float gridSize;      // 보통 1m
            public float angleStep;     // 보통 45도
        }

        /// <summary>
        /// pos를 그리드에 스냅.
        /// </summary>
        public static Vector3 SnapToGrid(Vector3 pos, float gridSize)
        {
            if (gridSize <= 0f) return pos;
            float x = Mathf.Round(pos.x / gridSize) * gridSize;
            float z = Mathf.Round(pos.z / gridSize) * gridSize;
            return new Vector3(x, pos.y, z);
        }

        /// <summary>
        /// from에서 to로의 방향을 angleStep도 단위로 스냅.
        /// 거리는 보존.
        /// </summary>
        public static Vector3 SnapAngle(Vector3 from, Vector3 to, float angleStepDeg)
        {
            Vector3 d = to - from;
            float len = new Vector2(d.x, d.z).magnitude;
            if (len < 1e-4f) return to;

            float ang = Mathf.Atan2(d.z, d.x) * Mathf.Rad2Deg;
            float step = angleStepDeg;
            float snappedAng = Mathf.Round(ang / step) * step;
            float rad = snappedAng * Mathf.Deg2Rad;

            return new Vector3(
                from.x + Mathf.Cos(rad) * len,
                to.y,
                from.z + Mathf.Sin(rad) * len);
        }

        /// <summary>
        /// 종합 스냅. anchor는 각도 스냅의 기준점 (있을 때만).
        /// </summary>
        public static Vector3 Apply(Vector3 raw, Vector3? anchor, Settings s)
        {
            Vector3 p = raw;
            // 각도 스냅 먼저 (그래야 그리드 스냅이 직선 방향에 적용됨)
            if (s.angleSnap && anchor.HasValue)
                p = SnapAngle(anchor.Value, p, s.angleStep);
            if (s.gridSnap)
                p = SnapToGrid(p, s.gridSize);
            return p;
        }

        /// <summary>
        /// Event의 modifier 키에서 스냅 세팅 추출.
        /// </summary>
        public static Settings FromEvent(Event e, float gridSize = 1f, float angleStep = 45f)
        {
            return new Settings
            {
                gridSnap = e.control || e.command,
                angleSnap = e.shift,
                gridSize = gridSize,
                angleStep = angleStep,
            };
        }
    }
}