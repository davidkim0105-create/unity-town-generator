using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// JSON 저장/로드용 통합 도큐먼트.
    /// Graph + Regions + Build settings를 하나로.
    /// </summary>
    [System.Serializable]
    public class TownDocumentV2
    {
        public string version = "v2.0";
        public RoadGraph graph = new RoadGraph();

        // Regions (RegionAuthoring을 그대로 못 직렬화하므로 데이터만 추출)
        public List<TownRegionV2> regions = new List<TownRegionV2>();
        public TownRegionV2 defaultRegion = new TownRegionV2 { name = "Default" };

        // Build settings
        public BuildingFiller.Settings buildSettings = BuildingFiller.DefaultSettings;
        public float blockThickness = 0.1f;
        public float roadInset = 1.5f;
    }
}