using UnityEngine;

namespace TownGen.V2.Presets
{
    [CreateAssetMenu(menuName = "TownGen V2/Building Preset", fileName = "Bldg_NewPreset")]
    public class BuildingPresetV2 : ScriptableObject
    {
        [Tooltip("이 프리셋의 짧은 설명")]
        [TextArea(1, 3)] public string description;

        public BuildingFiller.Settings settings = BuildingFiller.DefaultSettings;

        public BuildingFiller.Settings GetSettings() => settings;

        public void CopyFrom(BuildingFiller.Settings src) => settings = src;
    }
}