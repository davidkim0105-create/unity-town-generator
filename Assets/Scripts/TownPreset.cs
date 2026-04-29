using UnityEngine;

[CreateAssetMenu(fileName = "NewTownPreset", menuName = "Town Generator/Preset", order = 0)]
public class TownPreset : ScriptableObject
{
    [Header("프리셋 정보")]
    [TextArea(2, 4)]
    public string description = "프리셋 설명을 입력하세요";

    [Header("도시 크기")]
    public float townSize = 300f;
    public float aspectRatio = 1f;

    [Header("도시 외곽 형태")]
    public TownShape townShape = TownShape.Circle;
    public float shapeNoise = 0.5f;

    [Header("밀도")]
    public float globalDensity = 0.5f;
    public float densityFalloff = 0.5f;

    [Header("도로 그리드")]
    public float blockSize = 70f;
    public float roadDistortion = 3f;
    public float roadWidth = 6f;
    public float buildingMargin = 1.5f;

    [Header("건물")]
    public int maxFloors = 12;
    public float floorHeight = 3.2f;
    public float buildingMaxCoverage = 0.85f;

    [Header("골목길")]
    public float alleyComplexity = 0.6f;
    public float minLotSize = 6f;

    // ─── TownGenerator ↔ Preset 데이터 복사 ───
    public void ApplyTo(TownGenerator gen)
    {
        gen.townSize = townSize;
        gen.aspectRatio = aspectRatio;
        gen.townShape = townShape;
        gen.shapeNoise = shapeNoise;
        gen.globalDensity = globalDensity;
        gen.densityFalloff = densityFalloff;
        gen.blockSize = blockSize;
        gen.roadDistortion = roadDistortion;
        gen.roadWidth = roadWidth;
        gen.buildingMargin = buildingMargin;
        gen.maxFloors = maxFloors;
        gen.floorHeight = floorHeight;
        gen.buildingMaxCoverage = buildingMaxCoverage;
        gen.alleyComplexity = alleyComplexity;
        gen.minLotSize = minLotSize;
    }

    public void CopyFrom(TownGenerator gen)
    {
        townSize = gen.townSize;
        aspectRatio = gen.aspectRatio;
        townShape = gen.townShape;
        shapeNoise = gen.shapeNoise;
        globalDensity = gen.globalDensity;
        densityFalloff = gen.densityFalloff;
        blockSize = gen.blockSize;
        roadDistortion = gen.roadDistortion;
        roadWidth = gen.roadWidth;
        buildingMargin = gen.buildingMargin;
        maxFloors = gen.maxFloors;
        floorHeight = gen.floorHeight;
        buildingMaxCoverage = gen.buildingMaxCoverage;
        alleyComplexity = gen.alleyComplexity;
        minLotSize = gen.minLotSize;
    }
}