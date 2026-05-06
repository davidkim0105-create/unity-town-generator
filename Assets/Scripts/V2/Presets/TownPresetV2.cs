using UnityEngine;

namespace TownGen.V2.Presets
{
    [CreateAssetMenu(menuName = "TownGen V2/Town Preset", fileName = "Town_NewPreset")]
    public class TownPresetV2 : ScriptableObject
    {
        [Tooltip("이 프리셋의 짧은 설명")]
        [TextArea(1, 3)] public string description;

        [Header("Build Settings")]
        public float roadInset = 1.5f;
        [Range(0f, 5f)] public float blockThickness = 0.1f;
        public bool useEdgeWidthForInset = true;
        [Range(0f, 5f)] public float insetExtraMargin = 0.5f;

        public void ApplyTo(RoadGraphAuthoring auth)
        {
            if (auth == null) return;
            auth.roadInset = roadInset;
            auth.blockThickness = blockThickness;
            auth.useEdgeWidthForInset = useEdgeWidthForInset;
            auth.insetExtraMargin = insetExtraMargin;
        }

        public void CopyFrom(RoadGraphAuthoring auth)
        {
            if (auth == null) return;
            roadInset = auth.roadInset;
            blockThickness = auth.blockThickness;
            useEdgeWidthForInset = auth.useEdgeWidthForInset;
            insetExtraMargin = auth.insetExtraMargin;
        }
    }
}