using UnityEngine;
using TownGen.V2.Presets;

namespace TownGen.V2.EditorTools
{
    /// <summary>
    /// Region에 빌딩 프리셋을 자동으로 매핑.
    /// "Cell_0", "Cell_1" 같은 자동 생성 Region에 Bldg 프리셋 다양하게 적용.
    /// </summary>
    public static class RegionAutoAssigner
    {
        // 프리셋 사이클 (Voronoi 결과 region에 순서대로 적용)
        static readonly string[] presetPaths = new[]
        {
            "Presets/Building/Bldg_HighCommercial",
            "Presets/Building/Bldg_MidResi",
            "Presets/Building/Bldg_LowResi",
            "Presets/Building/Bldg_Sparse",
            "Presets/Building/Bldg_Industrial",
            "Presets/Building/Bldg_Megacity"
        };

        static readonly Color[] presetColors = new[]
        {
            new Color(1f, 0.6f, 0.2f),    // 상업 - 주황
            new Color(0.9f, 0.85f, 0.5f), // 중층 - 베이지
            new Color(0.4f, 0.8f, 0.4f),  // 주거 - 초록
            new Color(0.6f, 0.9f, 0.7f),  // 전원 - 연초록
            new Color(0.6f, 0.5f, 0.4f),  // 산업 - 갈색
            new Color(0.5f, 0.4f, 0.7f)   // 메가 - 보라
        };

        static readonly string[] presetNames = new[]
        {
            "Commercial", "MidResi", "LowResi", "Sparse", "Industrial", "Megacity"
        };

        /// <summary>
        /// 모든 Region에 프리셋 사이클로 빌딩 세팅 자동 매핑.
        /// </summary>
        public static int AutoAssign(RegionAuthoring region)
        {
            if (region == null || region.regions == null) return 0;

            int assigned = 0;
            for (int i = 0; i < region.regions.Count; i++)
            {
                var r = region.regions[i];
                int presetIdx = i % presetPaths.Length;
                var preset = Resources.Load<BuildingPresetV2>(presetPaths[presetIdx]);
                if (preset == null) continue;

                var s = preset.GetSettings();
                r.lotSize = s.lotSize;
                r.lotMargin = s.lotMargin;
                r.minHeight = s.minHeight;
                r.maxHeight = s.maxHeight;
                r.density = s.density;

                // 색깔/이름도 자동
                r.color = presetColors[presetIdx];
                r.name = $"{presetNames[presetIdx]}_{i}";

                assigned++;
            }
            return assigned;
        }
    }
}