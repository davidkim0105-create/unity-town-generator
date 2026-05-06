using UnityEngine;

namespace TownGen.V2.Presets
{
    [CreateAssetMenu(menuName = "TownGen V2/L-System Preset", fileName = "LS_NewPreset")]
    public class LSystemPresetV2 : ScriptableObject
    {
        [Tooltip("이 프리셋의 짧은 설명")]
        [TextArea(1, 3)] public string description;

        public LSystemGenerator.Settings settings = LSystemGenerator.Default;

        public LSystemGenerator.Settings GetSettings() => settings;

        public void CopyFrom(LSystemGenerator.Settings src) => settings = src;
    }
}