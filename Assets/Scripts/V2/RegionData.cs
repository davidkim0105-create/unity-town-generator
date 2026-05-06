using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    [System.Serializable]
    public class TownRegionV2
    {
        [Tooltip("영역 이름 (인스펙터 표시용). 예: Residential, Downtown, Park")]
        public string name = "Region";

        [Tooltip("영역 색깔. 블록 메쉬 색 + Scene 가시화에 사용")]
        public Color color = new Color(0.7f, 0.85f, 0.7f);

        [Tooltip("영역 폴리곤 (XZ 평면, 로컬 좌표). Region Paint 도구로 그리세요. 직접 편집 비추천.")]
        public List<Vector3> polygon = new List<Vector3>();

        [Header("Building Settings")]
        [Tooltip("이 영역의 빌딩 한 칸 폭 (m, 도로변 따라)\n• 작게(2~3): 빌딩 많음 (시장, 골목)\n• 보통(4~6): 일반 주거/상업\n• 크게(8~12): 큰 빌딩 (산업단지)")]
        public float lotSize = 4f;

        [Tooltip("빌딩 사이 간격 비율 (0~0.6)\n• 0: 다닥다닥 붙음\n• 0.15 (기본): 살짝 간격\n• 0.5: 빌딩 사이가 절반 (느슨)")]
        public float lotMargin = 0.15f;

        [Tooltip("이 영역 빌딩 최소 높이 (m)")]
        public float minHeight = 3f;

        [Tooltip("이 영역 빌딩 최대 높이 (m). 빌딩 키는 min~max 사이 랜덤")]
        public float maxHeight = 12f;

        [Tooltip("빌딩 생성 확률 (0~1)\n• 1: 모든 칸에 빌딩\n• 0.85 (기본): 약간 듬성듬성\n• 0.5: 절반만 (광장/공터 많은 마을)")]
        public float density = 0.85f;

        [Header("Block Pattern")]
        [Tooltip("이 영역의 블록 채우기 패턴")]
        public BlockPatternFiller.BlockPattern pattern = BlockPatternFiller.BlockPattern.Solid;

        [Tooltip("Perimeter/U/L/Courtyard 띠 두께(m)")]
        public float perimeterDepth = 6f;

        public BlockPatternFiller.PatternSettings GetPatternSettings()
        {
            return new BlockPatternFiller.PatternSettings
            {
                pattern = pattern,
                perimeterDepth = perimeterDepth
            };
        }

        public BuildingFiller.Settings ToFillerSettings(int seed,
            BuildingFiller.FillMode mode = BuildingFiller.FillMode.RoadFacing,
            float lotDepth = 6f,
            bool fillInteriorRows = false)
        {
            return new BuildingFiller.Settings
            {
                mode = mode,
                lotSize = lotSize,
                lotDepth = lotDepth,
                lotMargin = lotMargin,
                minHeight = minHeight,
                maxHeight = maxHeight,
                density = density,
                seed = seed,
                material = null,
                fillInteriorRows = fillInteriorRows,
            };
        }
    }
}