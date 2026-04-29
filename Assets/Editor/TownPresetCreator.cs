using UnityEditor;
using UnityEngine;
using System.IO;

public static class TownPresetCreator
{
    const string PresetFolder = "Assets/Presets";

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 메뉴: Tools > Town Generator > 기본 프리셋 5종 생성
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    [MenuItem("Tools/Town Generator/기본 프리셋 5종 생성")]
    public static void CreateDefaultPresets()
    {
        EnsureFolder();

        CreatePreset("01_시골마을", p =>
        {
            p.description = "🏘️ 작은 시골 마을\n낮은 건물, 큰 마당, 자연스러운 외곽";
            p.townShape = TownShape.Irregular;
            p.shapeNoise = 0.7f;
            p.townSize = 180f;
            p.globalDensity = 0.2f;
            p.densityFalloff = 0.3f;
            p.maxFloors = 3;
            p.blockSize = 55f;
            p.roadDistortion = 4f;
            p.roadWidth = 4f;
            p.alleyComplexity = 0.3f;
            p.minLotSize = 9f;
            p.buildingMaxCoverage = 0.65f;
        });

        CreatePreset("02_주거지", p =>
        {
            p.description = "🏡 일반 주거지\n중층 건물 위주, 정돈된 블록";
            p.townShape = TownShape.Rounded;
            p.townSize = 280f;
            p.globalDensity = 0.5f;
            p.densityFalloff = 0.4f;
            p.maxFloors = 8;
            p.blockSize = 70f;
            p.roadDistortion = 2f;
            p.roadWidth = 6f;
            p.alleyComplexity = 0.5f;
            p.minLotSize = 7f;
        });

        CreatePreset("03_도심", p =>
        {
            p.description = "🏙️ 일반 도심\n중심부 고층 + 외곽 중층";
            p.townShape = TownShape.Circle;
            p.townSize = 350f;
            p.globalDensity = 0.7f;
            p.densityFalloff = 0.5f;
            p.maxFloors = 15;
            p.blockSize = 75f;
            p.roadDistortion = 2f;
            p.roadWidth = 7f;
            p.alleyComplexity = 0.6f;
            p.minLotSize = 6f;
        });

        CreatePreset("04_대도시", p =>
        {
            p.description = "🌆 메가시티 다운타운\n초고층 빌딩 가득";
            p.townShape = TownShape.Rectangle;
            p.aspectRatio = 1.3f;
            p.townSize = 450f;
            p.globalDensity = 0.9f;
            p.densityFalloff = 0.3f;
            p.maxFloors = 22;
            p.blockSize = 85f;
            p.roadDistortion = 1f;
            p.roadWidth = 9f;
            p.alleyComplexity = 0.4f;
            p.minLotSize = 7f;
            p.buildingMaxCoverage = 0.95f;
        });

        CreatePreset("05_해안마을", p =>
        {
            p.description = "🏖️ 해안 마을\n비정형 외곽, 저층 위주, 따뜻한 분위기";
            p.townShape = TownShape.Irregular;
            p.shapeNoise = 0.9f;
            p.townSize = 220f;
            p.globalDensity = 0.4f;
            p.densityFalloff = 0.6f;
            p.maxFloors = 4;
            p.blockSize = 60f;
            p.roadDistortion = 5f;
            p.roadWidth = 4.5f;
            p.alleyComplexity = 0.7f;
            p.minLotSize = 6f;
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ 기본 프리셋 5종 생성 완료! Assets/Presets 폴더 확인");
        EditorUtility.DisplayDialog("프리셋 생성 완료",
            "5종의 기본 프리셋이 Assets/Presets 폴더에 생성되었습니다.",
            "확인");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(PresetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Presets");
        }
    }

    static void CreatePreset(string name, System.Action<TownPreset> setup)
    {
        string path = $"{PresetFolder}/{name}.asset";

        var preset = AssetDatabase.LoadAssetAtPath<TownPreset>(path);
        if (preset == null)
        {
            preset = ScriptableObject.CreateInstance<TownPreset>();
            AssetDatabase.CreateAsset(preset, path);
        }

        setup(preset);
        EditorUtility.SetDirty(preset);
    }
}