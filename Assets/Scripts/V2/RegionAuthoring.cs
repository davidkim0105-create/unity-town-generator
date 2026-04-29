using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class RegionAuthoring : MonoBehaviour
    {
        [Tooltip("정의된 영역 목록. Quick Actions 버튼으로 추가하거나 Region Paint 도구로 폴리곤을 그리세요.")]
        public List<TownRegionV2> regions = new List<TownRegionV2>();

        [Header("Default (no region matched)")]
        [Tooltip("어떤 Region 폴리곤에도 안 들어간 블록 처리.\n참고: 인스펙터 fallback 세팅이 우선 사용되며 이 default는 색 표시용입니다.")]
        public TownRegionV2 defaultRegion = new TownRegionV2
        {
            name = "Default",
            color = new Color(0.75f, 0.78f, 0.75f),
        };

        [Header("Visualization")]
        [Tooltip("Scene에 영역 폴리곤을 색칠 표시\n• ON: 영역 외곽선 + 채움 표시\n• OFF: 안 보임 (최종 결과 보기)")]
        public bool showPolygons = true;

        [Tooltip("Region 폴리곤을 지면에서 띄우는 높이 (m, z-fighting 방지)")]
        public float lift = 0.05f;

        public TownRegionV2 ResolveRegionAt(Vector3 localPoint)
        {
            foreach (var r in regions)
            {
                if (r.polygon != null && r.polygon.Count >= 3 &&
                    PolygonUtil.PointInPolygonXZ(localPoint, r.polygon))
                    return r;
            }
            return defaultRegion;
        }
    }
}