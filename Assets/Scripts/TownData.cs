using System;
using System.Collections.Generic;
using UnityEngine;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// JSON 익스포트용 데이터 구조
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[Serializable]
public class TownData
{
    public string version = "1.0";
    public TownMetadata metadata = new TownMetadata();
    public TownParams parameters = new TownParams();
    public List<BlockData> blocks = new List<BlockData>();
    public List<RoadData> roads = new List<RoadData>();
    public string groundColor = "#598133";
}

[Serializable]
public class TownMetadata
{
    public string createdAt;
    public int seed;
    public int blockCount;
    public int buildingCount;
    public int roadCount;
}

[Serializable]
public class TownParams
{
    public float townSize;
    public float aspectRatio;
    public string townShape;
    public float shapeNoise;
    public float globalDensity;
    public float densityFalloff;
    public float blockSize;
    public float roadDistortion;
    public float roadWidth;
    public float buildingMargin;
    public int maxFloors;
    public float floorHeight;
    public float buildingMaxCoverage;
    public float alleyComplexity;
    public float minLotSize;
}

[Serializable]
public class BlockData
{
    public int gridX;
    public int gridZ;
    public Vec3 position;
    public float rotationY;
    public Vec4 outerRect;     // x, y, w, h
    public Vec4 innerRect;
    public float autoDensity;
    public float densityOverride;
    public int seedOffset;
    public string groundColor;
    public List<BuildingData> buildings = new List<BuildingData>();
    public List<AlleyData> alleys = new List<AlleyData>();
}

[Serializable]
public class BuildingData
{
    public Vec3 localPosition;
    public Vec3 localScale;
    public float localRotationY;
    public string color;
    public int floors;
}

[Serializable]
public class AlleyData
{
    public Vec3 localPosition;
    public Vec3 localScale;
    public string color;
}

[Serializable]
public class RoadData
{
    public Vec3 position;
    public float rotationY;
    public Vec3 scale;
    public string color;
}

// JsonUtility는 Vector3를 잘 지원하지 못하므로 직렬화 전용 구조체
[Serializable]
public struct Vec3
{
    public float x, y, z;
    public Vec3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public struct Vec4
{
    public float x, y, w, h;
    public Vec4(Rect r) { x = r.x; y = r.y; w = r.width; h = r.height; }
    public Rect ToRect() => new Rect(x, y, w, h);
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 색상 ↔ Hex 문자열 변환
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public static class ColorHex
{
    public static string ToHex(Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    public static Color FromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c))
            return c;
        return Color.magenta;
    }
}