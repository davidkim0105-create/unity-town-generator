using UnityEngine;
using System.Collections.Generic;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Enums
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
public enum TownShape
{
    Rectangle,
    Circle,
    Diamond,
    Rounded,
    Irregular
}

public enum BuildingType
{
    Residential,
    Commercial,
    Office,
    Landmark
}

public enum FootprintType
{
    Box,
    LShape,
    TShape,
    UShape
}

public class TownGenerator : MonoBehaviour
{
    [Header("프리셋")]
    [Tooltip("프리셋 에셋을 끌어다 놓고 'Apply Preset' 버튼으로 적용.")]
    public TownPreset preset;

    [Header("기본 설정")]
    [Tooltip("랜덤 시드. 같은 값 = 항상 같은 도시.")]
    public int seed = 42;

    [Header("도시 크기")]
    [Tooltip("도시 한 변의 길이 (미터).")]
    [Range(100f, 600f)] public float townSize = 300f;

    [Tooltip("가로:세로 비율. 1=정사각형")]
    [Range(0.4f, 2.5f)] public float aspectRatio = 1f;

    [Header("도시 외곽 형태")]
    [Tooltip("도시 전체 외곽 모양.")]
    public TownShape townShape = TownShape.Circle;

    [Tooltip("Irregular 형태일 때만 적용. 0=원형, 1=울퉁불퉁")]
    [Range(0f, 1f)] public float shapeNoise = 0.5f;

    [Tooltip("Scene 뷰에 노란 외곽선 표시 ON/OFF.")]
    public bool showOutlineGizmo = true;

    [Tooltip("Scene 뷰에 블록 경계 라인 표시 ON/OFF.")]
    public bool showBlockGridGizmo = false;

    [Tooltip("블록 경계 라인 색상.")]
    public Color blockGridColor = new Color(0.4f, 0.8f, 1f, 0.5f);

    [Header("밀도")]
    [Tooltip("★전체 밀도. 0=시골, 0.5=일반도시, 1=메가시티")]
    [Range(0f, 1f)] public float globalDensity = 0.5f;

    [Tooltip("중심에서 외곽으로 갈수록 밀도 감소량.")]
    [Range(0f, 1f)] public float densityFalloff = 0.5f;

    [Header("도로 그리드")]
    [Tooltip("블록 한 변 크기.")]
    [Range(40f, 120f)] public float blockSize = 70f;

    [Tooltip("도로 휘어짐. 0=격자, 8=구불구불")]
    [Range(0f, 8f)] public float roadDistortion = 3f;

    [Tooltip("간선도로 폭.")]
    [Range(2f, 12f)] public float roadWidth = 6f;

    [Tooltip("도로와 건물 사이 추가 여유.")]
    [Range(0f, 5f)] public float buildingMargin = 1.5f;

    [Header("건물")]
    [Tooltip("최대 층수.")]
    [Range(1, 25)] public int maxFloors = 12;

    [Tooltip("한 층의 높이.")]
    [Range(2.8f, 4.5f)] public float floorHeight = 3.2f;

    [Tooltip("건물의 lot 점유 최대 비율.")]
    [Range(0.5f, 1.0f)] public float buildingMaxCoverage = 0.85f;

    [Tooltip("비정형 풋프린트 비율.")]
    [Range(0f, 1f)] public float complexFootprintRatio = 0.35f;

    [Tooltip("옥상 디테일 비율.")]
    [Range(0f, 1f)] public float rooftopDetailRatio = 0.5f;

    [Tooltip("랜드마크 등장 확률.")]
    [Range(0f, 0.2f)] public float landmarkChance = 0.04f;

    [Tooltip("옥상 디테일 생성 ON/OFF.")]
    public bool generateRooftopDetails = true;

    [Tooltip("비정형 풋프린트 생성 ON/OFF.")]
    public bool generateComplexFootprints = true;

    [Header("골목길")]
    [Tooltip("블록 내부 골목길 생성 ON/OFF.")]
    public bool generateAlleys = true;

    [Tooltip("골목 개수. 0=거의 없음, 1=많음")]
    [Range(0f, 1f)] public float alleyComplexity = 0.6f;

    [Tooltip("한 lot 한 변의 최소 크기.")]
    [Range(4f, 12f)] public float minLotSize = 6f;

    [Header("프롭")]
    [Tooltip("프롭 전체 생성 ON/OFF.")]
    public bool generateProps = true;

    [Tooltip("가로수 생성 ON/OFF.")]
    public bool generateTrees = true;

    [Tooltip("가로등 생성 ON/OFF.")]
    public bool generateLamps = true;

    [Tooltip("횡단보도 생성 ON/OFF.")]
    public bool generateCrosswalks = true;

    [Tooltip("가로수 배치 간격 (미터).")]
    [Range(4f, 20f)] public float treeSpacing = 10f;

    [Tooltip("가로등 배치 간격 (미터).")]
    [Range(8f, 30f)] public float lampSpacing = 20f;

    [Tooltip("도심에서 가로수 줄이는 정도.")]
    [Range(0f, 1f)] public float treeDensityFalloff = 0.5f;

    [Header("영역(Region)")]
    [Tooltip("Scene 뷰에 영역 색상 표시 ON/OFF.")]
    public bool showRegionGizmos = true;

    [Tooltip("Scene 뷰에 영역 이름 라벨 표시 ON/OFF.")]
    public bool showRegionLabels = true;

    [Tooltip("등록된 영역 리스트.")]
    public List<TownRegion> regions = new List<TownRegion>();

    // ─── 내부 ───
    private List<Vector3> gridPoints = new List<Vector3>();
    private bool[,] activeBlocks;
    private int gridX, gridZ;

    // ★ 외부 접근용 (워프 도구가 사용)
    public List<Vector3> GridPoints => gridPoints;
    public int GridX => gridX;
    public int GridZ => gridZ;
    public bool[,] ActiveBlocks => activeBlocks;
    private Dictionary<int, Material> materialCache = new Dictionary<int, Material>();
    public static Dictionary<Material, Color> materialColors = new Dictionary<Material, Color>();
    private Shader cachedShader;

    float TownWidth  => townSize;
    float TownDepth  => townSize / Mathf.Clamp(aspectRatio, 0.4f, 2.5f);

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 색상
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    Color BuildingColor(int floors)
    {
        Color light = new Color(0.92f, 0.89f, 0.84f);
        Color dark  = new Color(0.35f, 0.38f, 0.45f);
        float t = Mathf.Clamp01((float)(floors - 1) / Mathf.Max(1, maxFloors - 1));
        return Color.Lerp(light, dark, t);
    }

    BuildingType DecideType(int floors, float density)
    {
        if (density >= 0.7f && Random.value < landmarkChance)
            return BuildingType.Landmark;
        if (floors <= 3) return BuildingType.Residential;
        if (floors <= 7) return BuildingType.Commercial;
        return BuildingType.Office;
    }

    Color BuildingColorByType(BuildingType type, int floors)
    {
        float t = Mathf.Clamp01((float)(floors - 1) / Mathf.Max(1, maxFloors - 1));
        switch (type)
        {
            case BuildingType.Residential:
                return Color.Lerp(
                    new Color(0.93f, 0.88f, 0.78f),
                    new Color(0.72f, 0.62f, 0.50f), t);
            case BuildingType.Commercial:
                return Color.Lerp(
                    new Color(0.78f, 0.82f, 0.85f),
                    new Color(0.45f, 0.55f, 0.65f), t);
            case BuildingType.Office:
                return Color.Lerp(
                    new Color(0.65f, 0.68f, 0.72f),
                    new Color(0.25f, 0.28f, 0.32f), t);
            case BuildingType.Landmark:
                float h = Random.Range(0f, 1f);
                return Color.HSVToRGB(h, 0.45f, 0.85f);
        }
        return Color.white;
    }

    Color RoadColor(float width)
    {
        Color light = new Color(0.75f, 0.62f, 0.45f);
        Color dark  = new Color(0.18f, 0.18f, 0.22f);
        float t = Mathf.Clamp01((width - 1f) / 9f);
        return Color.Lerp(light, dark, t);
    }

    Color GroundColor(float density)
    {
        Color grass = new Color(0.42f, 0.55f, 0.35f);
        Color paved = new Color(0.52f, 0.50f, 0.47f);
        return Color.Lerp(grass, paved, density);
    }

    Color TreeTrunkColor() => new Color(0.35f, 0.25f, 0.15f);
    Color TreeLeafColor()  => new Color(0.30f, 0.55f, 0.25f);
    Color LampPoleColor()  => new Color(0.25f, 0.25f, 0.28f);
    Color LampHeadColor()  => new Color(0.95f, 0.92f, 0.70f);
    Color CrosswalkColor() => new Color(0.92f, 0.92f, 0.88f);

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 메인 흐름 (잠금 영역 보존 포함)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public void Generate()
    {
        var lockedSnapshots = SnapshotLockedBlocks();

        Clear();
        Random.InitState(seed);
        ComputeGrid();
        ComputeActiveBlocks();
        MakeGround();
        MakeRoads();
        MakeAllBlocks();
        if (generateProps) MakeProps();

        RestoreLockedBlocks(lockedSnapshots);

        Debug.Log($"도시 생성 완료! 형태: {townShape}, 밀도: {globalDensity:F2}");
    }

    public void Clear()
    {
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
        gridPoints.Clear();
        materialCache.Clear();
    }

    public bool IsInsideShape(Vector3 point)
    {
        float nx = (point.x - TownWidth * 0.5f) / (TownWidth * 0.48f);
        float nz = (point.z - TownDepth * 0.5f) / (TownDepth * 0.48f);
        switch (townShape)
        {
            case TownShape.Rectangle: return Mathf.Abs(nx) <= 1f && Mathf.Abs(nz) <= 1f;
            case TownShape.Circle:    return nx * nx + nz * nz <= 1f;
            case TownShape.Diamond:   return Mathf.Abs(nx) + Mathf.Abs(nz) <= 1f;
            case TownShape.Rounded:   return Mathf.Pow(Mathf.Abs(nx), 4f) + Mathf.Pow(Mathf.Abs(nz), 4f) <= 1f;
            case TownShape.Irregular:
                float angle = Mathf.Atan2(nz, nx);
                float n = Mathf.PerlinNoise(angle * 1.5f + seed * 0.1f, seed * 0.05f);
                float radius = 0.55f + n * shapeNoise * 0.8f;
                return nx * nx + nz * nz <= radius * radius;
        }
        return true;
    }

    void ComputeGrid()
    {
        gridX = Mathf.CeilToInt(TownWidth / blockSize) + 1;
        gridZ = Mathf.CeilToInt(TownDepth / blockSize) + 1;
        gridPoints.Clear();
        for (int z = 0; z < gridZ; z++)
        for (int x = 0; x < gridX; x++)
        {
            float px = x * blockSize;
            float pz = z * blockSize;
            bool isEdge = (x == 0 || x == gridX - 1 || z == 0 || z == gridZ - 1);
            if (!isEdge)
            {
                float n1 = Mathf.PerlinNoise(x * 0.4f + seed * 0.1f, z * 0.4f) - 0.5f;
                float n2 = Mathf.PerlinNoise(x * 0.4f, z * 0.4f + seed * 0.1f) - 0.5f;
                px += n1 * roadDistortion * blockSize * 0.15f;
                pz += n2 * roadDistortion * blockSize * 0.15f;
            }
            gridPoints.Add(new Vector3(px, 0, pz));
        }
    }

    void ComputeActiveBlocks()
    {
        activeBlocks = new bool[gridX - 1, gridZ - 1];
        for (int z = 0; z < gridZ - 1; z++)
        for (int x = 0; x < gridX - 1; x++)
            activeBlocks[x, z] = IsInsideShape(GetBlockCenter(x, z));
    }

    Vector3 GetBlockCenter(int x, int z)
    {
        Vector3 p00 = gridPoints[z * gridX + x];
        Vector3 p10 = gridPoints[z * gridX + x + 1];
        Vector3 p01 = gridPoints[(z + 1) * gridX + x];
        Vector3 p11 = gridPoints[(z + 1) * gridX + x + 1];
        return (p00 + p10 + p01 + p11) * 0.25f;
    }

    void MakeGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "_Ground";
        ground.transform.SetParent(transform);
        float maxSide = Mathf.Max(TownWidth, TownDepth);
        ground.transform.localPosition = new Vector3(TownWidth / 2, -0.1f, TownDepth / 2);
        ground.transform.localScale = new Vector3(maxSide + 80, 0.1f, maxSide + 80);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.35f, 0.50f, 0.30f));
    }

    void MakeRoads()
    {
        var roadParent = new GameObject("Roads");
        roadParent.transform.SetParent(transform);

        for (int z = 0; z < gridZ; z++)
        for (int x = 0; x < gridX; x++)
        {
            int i = z * gridX + x;
            if (x < gridX - 1)
            {
                bool needRoad = false;
                if (z > 0 && activeBlocks[x, z - 1]) needRoad = true;
                if (z < gridZ - 1 && activeBlocks[x, z]) needRoad = true;
                if (needRoad) MakeRoadSegment(gridPoints[i], gridPoints[i + 1], roadParent.transform);
            }
            if (z < gridZ - 1)
            {
                bool needRoad = false;
                if (x > 0 && activeBlocks[x - 1, z]) needRoad = true;
                if (x < gridX - 1 && activeBlocks[x, z]) needRoad = true;
                if (needRoad) MakeRoadSegment(gridPoints[i], gridPoints[i + gridX], roadParent.transform);
            }
        }
    }

    void MakeRoadSegment(Vector3 a, Vector3 b, Transform parent)
    {
        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.SetParent(parent);
        Vector3 mid = (a + b) * 0.5f;
        float length = Vector3.Distance(a, b);
        Vector3 dir = (b - a).normalized;
        road.transform.position = new Vector3(mid.x, 0.02f, mid.z);
        if (dir != Vector3.zero) road.transform.rotation = Quaternion.LookRotation(dir);
        road.transform.localScale = new Vector3(roadWidth, 0.05f, length + roadWidth);
        road.GetComponent<Renderer>().sharedMaterial = MakeMaterial(RoadColor(roadWidth));
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 블록 생성
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void MakeAllBlocks()
    {
        var blocksParent = new GameObject("Blocks");
        blocksParent.transform.SetParent(transform);

        Vector3 cityCenter = new Vector3(TownWidth / 2, 0, TownDepth / 2);
        float maxDist = Mathf.Max(TownWidth, TownDepth);

        for (int z = 0; z < gridZ - 1; z++)
        for (int x = 0; x < gridX - 1; x++)
        {
            if (!activeBlocks[x, z]) continue;

            Vector3 p00 = gridPoints[z * gridX + x];
            Vector3 p10 = gridPoints[z * gridX + x + 1];
            Vector3 p01 = gridPoints[(z + 1) * gridX + x];
            Vector3 p11 = gridPoints[(z + 1) * gridX + x + 1];
            Vector3 blockCenter = (p00 + p10 + p01 + p11) * 0.25f;

            float distNorm = Vector3.Distance(blockCenter, cityCenter) / (maxDist * 0.55f);
            float noise = Mathf.PerlinNoise(blockCenter.x * 0.015f + seed, blockCenter.z * 0.015f);
            float density = Mathf.Clamp01(globalDensity - distNorm * densityFalloff + (noise - 0.5f) * 0.25f);

            Vector3 rightVec = ((p10 - p00) + (p11 - p01)) * 0.5f;
            float angleY = -Mathf.Atan2(rightVec.z, rightVec.x) * Mathf.Rad2Deg;

            float localWidth = Mathf.Min(Vector3.Distance(p00, p10), Vector3.Distance(p01, p11));
            float localDepth = Mathf.Min(Vector3.Distance(p00, p01), Vector3.Distance(p10, p11));

            float roadHalf = roadWidth * 0.5f;
            float outerHalfW = localWidth * 0.5f - roadHalf;
            float outerHalfD = localDepth * 0.5f - roadHalf;
            float innerHalfW = outerHalfW - buildingMargin;
            float innerHalfD = outerHalfD - buildingMargin;

            if (innerHalfW < 4f || innerHalfD < 4f) continue;

            Rect outerRect = new Rect(-outerHalfW, -outerHalfD, outerHalfW * 2, outerHalfD * 2);
            Rect innerRect = new Rect(-innerHalfW, -innerHalfD, innerHalfW * 2, innerHalfD * 2);

            var blockGo = new GameObject($"Block_{x}_{z}_d{density:F2}");
            blockGo.transform.SetParent(blocksParent.transform);
            blockGo.transform.position = new Vector3(blockCenter.x, 0, blockCenter.z);
            blockGo.transform.rotation = Quaternion.Euler(0, angleY, 0);

            var blk = blockGo.AddComponent<TownBlock>();
            blk.gridX = x;
            blk.gridZ = z;
            blk.outerRect = outerRect;
            blk.innerRect = innerRect;
            blk.autoDensity = density;
            blk.generator = this;
            blk.seedOffset = 0;

            Random.InitState(GetBlockSeed(x, z, 0));
            FillBlock(blockGo.transform, outerRect, innerRect, density);
        }
    }

    int GetBlockSeed(int x, int z, int offset)
    {
        return seed * 1000 + x * 37 + z * 53 + offset;
    }

    public void RegenerateBlock(TownBlock block)
    {
        Random.InitState(GetBlockSeed(block.gridX, block.gridZ, block.seedOffset));
        FillBlock(block.transform, block.outerRect, block.innerRect, block.EffectiveDensity);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 그리드 포인트 워프 (옵션 A + B용)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void SetGridPoint(int gx, int gz, Vector3 newPos)
    {
        if (gx < 0 || gx >= gridX || gz < 0 || gz >= gridZ) return;
        int idx = gz * gridX + gx;
        if (idx < 0 || idx >= gridPoints.Count) return;
        gridPoints[idx] = new Vector3(newPos.x, 0, newPos.z);
    }

    public Vector3 GetGridPoint(int gx, int gz)
    {
        if (gx < 0 || gx >= gridX || gz < 0 || gz >= gridZ) return Vector3.zero;
        return gridPoints[gz * gridX + gx];
    }

    public void RebuildAfterWarp()
    {
        var lockedSnapshots = SnapshotLockedBlocks();

        DestroyChildIfExists("Roads");
        DestroyChildIfExists("Blocks");
        DestroyChildIfExists("Props");

        Random.InitState(seed);
        ComputeActiveBlocks();
        MakeRoads();
        MakeAllBlocks();
        if (generateProps) MakeProps();

        RestoreLockedBlocks(lockedSnapshots);
    }

    void DestroyChildIfExists(string name)
    {
        Transform t = transform.Find(name);
        if (t != null) DestroyImmediate(t.gameObject);
    }

    public void RebuildAffectedBlocks(HashSet<Vector2Int> affectedGridPoints)
    {
        if (affectedGridPoints.Count == 0) return;

        var affectedBlocks = new HashSet<Vector2Int>();
        foreach (var p in affectedGridPoints)
        {
            for (int dx = -1; dx <= 0; dx++)
            for (int dz = -1; dz <= 0; dz++)
            {
                int bx = p.x + dx;
                int bz = p.y + dz;
                if (bx < 0 || bx >= gridX - 1) continue;
                if (bz < 0 || bz >= gridZ - 1) continue;
                affectedBlocks.Add(new Vector2Int(bx, bz));
            }
        }

        Transform blocksRoot = transform.Find("Blocks");
        if (blocksRoot == null)
        {
            RebuildAfterWarp();
            return;
        }

        var toDestroy = new List<GameObject>();
        foreach (Transform t in blocksRoot)
        {
            var blk = t.GetComponent<TownBlock>();
            if (blk != null && affectedBlocks.Contains(new Vector2Int(blk.gridX, blk.gridZ)))
                toDestroy.Add(t.gameObject);
        }
        foreach (var go in toDestroy) DestroyImmediate(go);

        DestroyChildIfExists("Roads");
        DestroyChildIfExists("Props");
        Random.InitState(seed);
        ComputeActiveBlocks();
        MakeRoads();

        Vector3 cityCenter = new Vector3(TownWidth / 2, 0, TownDepth / 2);
        float maxDist = Mathf.Max(TownWidth, TownDepth);

        Transform blocksParent = transform.Find("Blocks");
        if (blocksParent == null)
        {
            var go = new GameObject("Blocks");
            go.transform.SetParent(transform);
            blocksParent = go.transform;
        }

        foreach (var blockId in affectedBlocks)
        {
            int x = blockId.x, z = blockId.y;
            if (!activeBlocks[x, z]) continue;

            Vector3 p00 = gridPoints[z * gridX + x];
            Vector3 p10 = gridPoints[z * gridX + x + 1];
            Vector3 p01 = gridPoints[(z + 1) * gridX + x];
            Vector3 p11 = gridPoints[(z + 1) * gridX + x + 1];
            Vector3 blockCenter = (p00 + p10 + p01 + p11) * 0.25f;

            float distNorm = Vector3.Distance(blockCenter, cityCenter) / (maxDist * 0.55f);
            float noise = Mathf.PerlinNoise(blockCenter.x * 0.015f + seed, blockCenter.z * 0.015f);
            float density = Mathf.Clamp01(globalDensity - distNorm * densityFalloff + (noise - 0.5f) * 0.25f);

            Vector3 rightVec = ((p10 - p00) + (p11 - p01)) * 0.5f;
            float angleY = -Mathf.Atan2(rightVec.z, rightVec.x) * Mathf.Rad2Deg;

            float localWidth = Mathf.Min(Vector3.Distance(p00, p10), Vector3.Distance(p01, p11));
            float localDepth = Mathf.Min(Vector3.Distance(p00, p01), Vector3.Distance(p10, p11));

            float roadHalf = roadWidth * 0.5f;
            float outerHalfW = localWidth * 0.5f - roadHalf;
            float outerHalfD = localDepth * 0.5f - roadHalf;
            float innerHalfW = outerHalfW - buildingMargin;
            float innerHalfD = outerHalfD - buildingMargin;

            if (innerHalfW < 4f || innerHalfD < 4f) continue;

            Rect outerRect = new Rect(-outerHalfW, -outerHalfD, outerHalfW * 2, outerHalfD * 2);
            Rect innerRect = new Rect(-innerHalfW, -innerHalfD, innerHalfW * 2, innerHalfD * 2);

            var blockGo = new GameObject($"Block_{x}_{z}_d{density:F2}");
            blockGo.transform.SetParent(blocksParent);
            blockGo.transform.position = new Vector3(blockCenter.x, 0, blockCenter.z);
            blockGo.transform.rotation = Quaternion.Euler(0, angleY, 0);

            var blk = blockGo.AddComponent<TownBlock>();
            blk.gridX = x;
            blk.gridZ = z;
            blk.outerRect = outerRect;
            blk.innerRect = innerRect;
            blk.autoDensity = density;
            blk.generator = this;
            blk.seedOffset = 0;

            Random.InitState(GetBlockSeed(x, z, 0));
            FillBlock(blockGo.transform, outerRect, innerRect, density);
        }

        if (generateProps) MakeProps();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 영역(Region) 관리
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public TownRegion FindRegionFor(int gx, int gz)
    {
        for (int i = 0; i < regions.Count; i++)
            if (regions[i].ContainsBlock(gx, gz))
                return regions[i];
        return null;
    }

    public TownRegion FindRegionFor(TownBlock b) => FindRegionFor(b.gridX, b.gridZ);

    public void AddBlockToRegion(TownRegion region, int gx, int gz)
    {
        if (region == null) return;
        RemoveBlockFromAllRegions(gx, gz);
        var v = new Vector2Int(gx, gz);
        if (!region.blockIds.Contains(v))
            region.blockIds.Add(v);
    }

    public void RemoveBlockFromAllRegions(int gx, int gz)
    {
        foreach (var r in regions)
            r.blockIds.RemoveAll(v => v.x == gx && v.y == gz);
    }

    public List<TownBlock> GetBlocksInRegion(TownRegion region)
    {
        var list = new List<TownBlock>();
        if (region == null) return list;

        Transform root = transform.Find("Blocks");
        if (root == null) return list;

        foreach (Transform t in root)
        {
            var b = t.GetComponent<TownBlock>();
            if (b != null && region.ContainsBlock(b))
                list.Add(b);
        }
        return list;
    }

    public void CleanupEmptyRegions()
    {
        regions.RemoveAll(r => r.blockIds.Count == 0);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 잠금 영역 보존
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    class LockedBlockSnapshot
    {
        public int gridX;
        public int gridZ;
        public float densityOverride;
        public int seedOffset;
    }

    List<LockedBlockSnapshot> SnapshotLockedBlocks()
    {
        var snapshots = new List<LockedBlockSnapshot>();
        if (regions == null || regions.Count == 0) return snapshots;

        Transform root = transform.Find("Blocks");
        if (root == null) return snapshots;

        foreach (Transform t in root)
        {
            var b = t.GetComponent<TownBlock>();
            if (b == null) continue;
            var region = FindRegionFor(b);
            if (region != null && region.isLocked)
            {
                snapshots.Add(new LockedBlockSnapshot
                {
                    gridX = b.gridX,
                    gridZ = b.gridZ,
                    densityOverride = b.densityOverride,
                    seedOffset = b.seedOffset
                });
            }
        }
        return snapshots;
    }

    void RestoreLockedBlocks(List<LockedBlockSnapshot> snapshots)
    {
        if (snapshots.Count == 0) return;

        Transform root = transform.Find("Blocks");
        if (root == null) return;

        foreach (Transform t in root)
        {
            var b = t.GetComponent<TownBlock>();
            if (b == null) continue;
            foreach (var snap in snapshots)
            {
                if (snap.gridX == b.gridX && snap.gridZ == b.gridZ)
                {
                    b.densityOverride = snap.densityOverride;
                    b.seedOffset = snap.seedOffset;
                    // 직접 호출 (Regenerate는 잠금 검사로 막힘)
                    Random.InitState(GetBlockSeed(b.gridX, b.gridZ, b.seedOffset));
                    while (b.transform.childCount > 0)
                        DestroyImmediate(b.transform.GetChild(0).gameObject);
                    FillBlock(b.transform, b.outerRect, b.innerRect, b.EffectiveDensity);
                    break;
                }
            }
        }
    }

    public bool IsBlockLocked(TownBlock b)
    {
        var region = FindRegionFor(b);
        return region != null && region.isLocked;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // FillBlock / Subdivide
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void FillBlock(Transform parent, Rect outerRect, Rect innerRect, float density)
    {
        var blockGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockGround.name = "BlockGround";
        blockGround.transform.SetParent(parent);
        blockGround.transform.localPosition = new Vector3(0, -0.01f, 0);
        blockGround.transform.localRotation = Quaternion.identity;
        blockGround.transform.localScale = new Vector3(outerRect.width, 0.04f, outerRect.height);
        blockGround.GetComponent<Renderer>().sharedMaterial = MakeMaterial(GroundColor(density));

        var lots = new List<Rect>();
        var alleys = new List<Rect>();

        float gap = (density > 0.3f && density < 0.75f)
            ? Random.Range(1.8f, 2.5f)
            : Random.Range(1.2f, 2f);

        Subdivide(outerRect, density, gap, lots, alleys, 0);

        if (generateAlleys)
        {
            foreach (var alley in alleys)
            {
                float aw = Mathf.Min(alley.width, alley.height);
                float al = Mathf.Max(alley.width, alley.height);
                if (aw < 0.4f || al < 6f) continue;

                var ag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ag.name = "Alley";
                ag.transform.SetParent(parent);
                ag.transform.localPosition = new Vector3(alley.center.x, 0.02f, alley.center.y);
                ag.transform.localRotation = Quaternion.identity;
                ag.transform.localScale = new Vector3(alley.width, 0.04f, alley.height);
                ag.GetComponent<Renderer>().sharedMaterial = MakeMaterial(RoadColor(aw));
            }
        }

        foreach (var lot in lots)
        {
            Rect clipped = ClipRect(lot, innerRect);
            if (clipped.width < 3f || clipped.height < 3f) continue;
            SpawnBuilding(clipped, density, parent);
        }
    }

    Rect ClipRect(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        if (xMax <= xMin || yMax <= yMin) return new Rect(0, 0, 0, 0);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    void Subdivide(Rect r, float density, float gap, List<Rect> lots, List<Rect> alleys, int depth)
    {
        float targetSize = Mathf.Lerp(40f, 10f, alleyComplexity);
        int maxDepth = Mathf.RoundToInt(Mathf.Lerp(2f, 6f, alleyComplexity));

        bool canSplitH = r.width  >= minLotSize * 2 + gap;
        bool canSplitV = r.height >= minLotSize * 2 + gap;

        bool reachedTarget = r.width < targetSize && r.height < targetSize;
        if ((!canSplitH && !canSplitV) || reachedTarget || depth >= maxDepth)
        {
            if (density < 0.25f && Random.value > density * 3f) return;
            lots.Add(r);
            return;
        }

        bool splitVertical = r.width >= r.height;
        if (splitVertical && !canSplitH) splitVertical = false;
        if (!splitVertical && !canSplitV) splitVertical = true;

        if (canSplitH && canSplitV && Random.value < 0.3f)
            splitVertical = Random.value > 0.5f;

        float available = splitVertical ? r.width : r.height;
        float minRatio = (minLotSize + gap * 0.5f) / available;
        float maxRatio = 1f - minRatio;
        if (minRatio >= maxRatio) { lots.Add(r); return; }

        float ratio = Random.Range(minRatio, maxRatio);
        float g = gap * 0.5f;

        if (splitVertical)
        {
            float cut = r.x + r.width * ratio;
            alleys.Add(new Rect(cut - g, r.yMin, gap, r.height));
            Rect L = Rect.MinMaxRect(r.xMin, r.yMin, cut - g, r.yMax);
            Rect R = Rect.MinMaxRect(cut + g, r.yMin, r.xMax, r.yMax);
            Subdivide(L, density, gap, lots, alleys, depth + 1);
            Subdivide(R, density, gap, lots, alleys, depth + 1);
        }
        else
        {
            float cut = r.y + r.height * ratio;
            alleys.Add(new Rect(r.xMin, cut - g, r.width, gap));
            Rect B = Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, cut - g);
            Rect T = Rect.MinMaxRect(r.xMin, cut + g, r.xMax, r.yMax);
            Subdivide(B, density, gap, lots, alleys, depth + 1);
            Subdivide(T, density, gap, lots, alleys, depth + 1);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 건물 생성
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void SpawnBuilding(Rect lot, float density, Transform parent)
    {
        int floors;
        if (density >= 0.75f)
            floors = Random.Range(Mathf.Max(4, maxFloors / 2), maxFloors + 1);
        else if (density >= 0.45f)
            floors = Random.Range(2, Mathf.Max(3, maxFloors / 3) + 1);
        else if (density >= 0.25f)
            floors = Random.Range(1, 3);
        else
            floors = 1;

        BuildingType type = DecideType(floors, density);
        Color color = BuildingColorByType(type, floors);

        FootprintType fp = FootprintType.Box;
        if (generateComplexFootprints &&
            lot.width >= 12f && lot.height >= 12f &&
            Random.value < complexFootprintRatio)
        {
            float r = Random.value;
            if (r < 0.5f)       fp = FootprintType.LShape;
            else if (r < 0.85f) fp = FootprintType.TShape;
            else                fp = FootprintType.UShape;
        }

        var bGo = new GameObject($"Building_{floors}F_{type}");
        bGo.transform.SetParent(parent);

        float coverage = Mathf.Lerp(0.5f, buildingMaxCoverage, density);
        float w = lot.width * coverage * Random.Range(0.85f, 1f);
        float d = lot.height * coverage * Random.Range(0.85f, 1f);
        float ox = (lot.width - w) * Random.Range(0.1f, 0.5f);
        float oz = (lot.height - d) * Random.Range(0.1f, 0.5f);

        Vector3 footprintCenter = new Vector3(lot.x + w / 2 + ox, 0, lot.y + d / 2 + oz);
        bGo.transform.localPosition = footprintCenter;

        if (density > 0.25f && density < 0.7f)
            bGo.transform.localRotation = Quaternion.Euler(0, Random.Range(-2f, 2f), 0);

        float h = floors * floorHeight;
        BuildFootprint(bGo.transform, fp, w, d, h, color);

        if (generateRooftopDetails &&
            Random.value < rooftopDetailRatio && floors >= 2)
        {
            AddRooftopDetail(bGo.transform, w, d, h, color, floors);
        }
    }

    void BuildFootprint(Transform parent, FootprintType fp, float w, float d, float h, Color color)
    {
        Material mat = MakeMaterial(color);

        switch (fp)
        {
            case FootprintType.Box:
                CreatePart(parent, "Main", new Vector3(0, h / 2, 0), new Vector3(w, h, d), mat);
                break;

            case FootprintType.LShape:
            {
                float cutW = w * Random.Range(0.4f, 0.55f);
                float cutD = d * Random.Range(0.4f, 0.55f);
                CreatePart(parent, "Part1",
                    new Vector3(0, h / 2, -d / 2 + cutD / 2),
                    new Vector3(w, h, cutD), mat);
                CreatePart(parent, "Part2",
                    new Vector3(-w / 2 + cutW / 2, h / 2, cutD / 2),
                    new Vector3(cutW, h, d - cutD), mat);
                break;
            }

            case FootprintType.TShape:
            {
                float armD = d * 0.5f;
                float stemW = w * Random.Range(0.35f, 0.5f);
                CreatePart(parent, "Arm",
                    new Vector3(0, h / 2, d / 2 - armD / 2),
                    new Vector3(w, h, armD), mat);
                CreatePart(parent, "Stem",
                    new Vector3(0, h / 2, -armD / 2),
                    new Vector3(stemW, h, d - armD), mat);
                break;
            }

            case FootprintType.UShape:
            {
                float wingW = w * 0.3f;
                float baseD = d * 0.4f;
                CreatePart(parent, "Base",
                    new Vector3(0, h / 2, -d / 2 + baseD / 2),
                    new Vector3(w, h, baseD), mat);
                CreatePart(parent, "WingL",
                    new Vector3(-w / 2 + wingW / 2, h / 2, baseD / 2),
                    new Vector3(wingW, h, d - baseD), mat);
                CreatePart(parent, "WingR",
                    new Vector3(w / 2 - wingW / 2, h / 2, baseD / 2),
                    new Vector3(wingW, h, d - baseD), mat);
                break;
            }
        }
    }

    void CreatePart(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPos;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void AddRooftopDetail(Transform parent, float w, float d, float h, Color baseColor, int floors)
    {
        Color rooftopColor = Color.Lerp(baseColor, new Color(0.3f, 0.3f, 0.32f), 0.7f);
        Material mat = MakeMaterial(rooftopColor);

        int detailType = Random.Range(0, 3);

        switch (detailType)
        {
            case 0:
            {
                float pw = w * Random.Range(0.4f, 0.7f);
                float pd = d * Random.Range(0.4f, 0.7f);
                float ph = floorHeight * Random.Range(0.6f, 1.1f);
                float ox = Random.Range(-(w - pw) / 2, (w - pw) / 2);
                float oz = Random.Range(-(d - pd) / 2, (d - pd) / 2);
                CreatePart(parent, "Penthouse",
                    new Vector3(ox, h + ph / 2, oz),
                    new Vector3(pw, ph, pd), mat);
                break;
            }
            case 1:
            {
                for (int i = 0; i < 2; i++)
                {
                    float ux = Random.Range(-w / 3, w / 3);
                    float uz = Random.Range(-d / 3, d / 3);
                    float uw = Random.Range(1.5f, 3f);
                    float uh = Random.Range(0.8f, 1.5f);
                    CreatePart(parent, "AC",
                        new Vector3(ux, h + uh / 2, uz),
                        new Vector3(uw, uh, uw), mat);
                }
                break;
            }
            case 2:
            {
                float pw = w * 0.3f;
                float pd = d * 0.3f;
                float ph = floorHeight * 0.5f;
                CreatePart(parent, "RoofBox",
                    new Vector3(0, h + ph / 2, 0),
                    new Vector3(pw, ph, pd), mat);
                break;
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 프롭
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void MakeProps()
    {
        var propsParent = new GameObject("Props");
        propsParent.transform.SetParent(transform);

        var treeParent = new GameObject("Trees");
        treeParent.transform.SetParent(propsParent.transform);
        var lampParent = new GameObject("Lamps");
        lampParent.transform.SetParent(propsParent.transform);
        var crossParent = new GameObject("Crosswalks");
        crossParent.transform.SetParent(propsParent.transform);

        Vector3 cityCenter = new Vector3(TownWidth / 2, 0, TownDepth / 2);
        float maxDist = Mathf.Max(TownWidth, TownDepth);

        Random.InitState(seed + 12345);

        for (int z = 0; z < gridZ; z++)
        for (int x = 0; x < gridX; x++)
        {
            int i = z * gridX + x;

            if (x < gridX - 1)
            {
                bool needRoad = false;
                if (z > 0 && activeBlocks[x, z - 1]) needRoad = true;
                if (z < gridZ - 1 && activeBlocks[x, z]) needRoad = true;
                if (needRoad)
                {
                    Vector3 a = gridPoints[i];
                    Vector3 b = gridPoints[i + 1];
                    PlacePropsAlongRoad(a, b, cityCenter, maxDist,
                        treeParent.transform, lampParent.transform);
                }
            }
            if (z < gridZ - 1)
            {
                bool needRoad = false;
                if (x > 0 && activeBlocks[x - 1, z]) needRoad = true;
                if (x < gridX - 1 && activeBlocks[x, z]) needRoad = true;
                if (needRoad)
                {
                    Vector3 a = gridPoints[i];
                    Vector3 b = gridPoints[i + gridX];
                    PlacePropsAlongRoad(a, b, cityCenter, maxDist,
                        treeParent.transform, lampParent.transform);
                }
            }
        }

        if (generateCrosswalks)
        {
            for (int z = 0; z < gridZ; z++)
            for (int x = 0; x < gridX; x++)
            {
                bool hasActiveNeighbor = false;
                if (x > 0 && z > 0 && activeBlocks[x - 1, z - 1]) hasActiveNeighbor = true;
                if (x < gridX - 1 && z > 0 && activeBlocks[x, z - 1]) hasActiveNeighbor = true;
                if (x > 0 && z < gridZ - 1 && activeBlocks[x - 1, z]) hasActiveNeighbor = true;
                if (x < gridX - 1 && z < gridZ - 1 && activeBlocks[x, z]) hasActiveNeighbor = true;
                if (!hasActiveNeighbor) continue;

                Vector3 cross = gridPoints[z * gridX + x];

                if (x < gridX - 1)
                {
                    bool roadOK = false;
                    if (z > 0 && activeBlocks[x, z - 1]) roadOK = true;
                    if (z < gridZ - 1 && activeBlocks[x, z]) roadOK = true;
                    if (roadOK)
                        PlaceCrosswalk(cross, gridPoints[z * gridX + x + 1], crossParent.transform);
                }
                if (z < gridZ - 1)
                {
                    bool roadOK = false;
                    if (x > 0 && activeBlocks[x - 1, z]) roadOK = true;
                    if (x < gridX - 1 && activeBlocks[x, z]) roadOK = true;
                    if (roadOK)
                        PlaceCrosswalk(cross, gridPoints[(z + 1) * gridX + x], crossParent.transform);
                }
            }
        }
    }

    void PlacePropsAlongRoad(Vector3 a, Vector3 b, Vector3 cityCenter, float maxDist,
                              Transform treeParent, Transform lampParent)
    {
        float length = Vector3.Distance(a, b);
        Vector3 dir = (b - a).normalized;
        Vector3 normal = new Vector3(-dir.z, 0, dir.x);

        float roadHalf = roadWidth * 0.5f;
        float sidewalkOffset = roadHalf + 1.2f;

        Vector3 roadMid = (a + b) * 0.5f;
        float distNorm = Vector3.Distance(roadMid, cityCenter) / (maxDist * 0.55f);
        float localDensity = Mathf.Clamp01(globalDensity - distNorm * densityFalloff);

        float effectiveTreeSpacing = treeSpacing * (1f + localDensity * treeDensityFalloff);

        if (generateTrees)
        {
            int treeCount = Mathf.FloorToInt(length / effectiveTreeSpacing);
            for (int i = 1; i < treeCount; i++)
            {
                float t = (float)i / treeCount;
                Vector3 along = Vector3.Lerp(a, b, t);
                Vector3 jitter = dir * Random.Range(-0.5f, 0.5f);

                for (int side = -1; side <= 1; side += 2)
                {
                    if (Random.value < localDensity * 0.6f) continue;
                    Vector3 pos = along + normal * sidewalkOffset * side + jitter;
                    CreateTree(pos, treeParent);
                }
            }
        }

        if (generateLamps)
        {
            int lampCount = Mathf.FloorToInt(length / lampSpacing);
            for (int i = 1; i < lampCount + 1; i++)
            {
                float t = (float)i / (lampCount + 1);
                Vector3 along = Vector3.Lerp(a, b, t);

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 pos = along + normal * (sidewalkOffset - 0.3f) * side;
                    CreateLamp(pos, lampParent);
                }
            }
        }
    }

    void PlaceCrosswalk(Vector3 a, Vector3 b, Transform parent)
    {
        float length = Vector3.Distance(a, b);
        if (length < 8f) return;

        Vector3 dir = (b - a).normalized;
        float offset = roadWidth * 0.5f + 2f;
        Vector3 cwCenter = a + dir * offset;

        int stripeCount = 5;
        float stripeWidth = 0.4f;
        float stripeGap = 0.4f;
        float totalSpan = stripeCount * (stripeWidth + stripeGap);
        float startOffset = -totalSpan * 0.5f;

        Vector3 normal = new Vector3(-dir.z, 0, dir.x);

        for (int i = 0; i < stripeCount; i++)
        {
            Vector3 stripePos = cwCenter + dir * (startOffset + i * (stripeWidth + stripeGap));
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            s.name = "Crosswalk";
            s.transform.SetParent(parent);
            s.transform.position = new Vector3(stripePos.x, 0.04f, stripePos.z);
            s.transform.rotation = Quaternion.LookRotation(normal);
            s.transform.localScale = new Vector3(roadWidth - 0.5f, 0.02f, stripeWidth);
            s.GetComponent<Renderer>().sharedMaterial = MakeMaterial(CrosswalkColor());
        }
    }

    void CreateTree(Vector3 pos, Transform parent)
    {
        var tree = new GameObject("Tree");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;

        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trunk.name = "Trunk";
        trunk.transform.SetParent(tree.transform);
        float trunkH = Random.Range(1.8f, 2.5f);
        trunk.transform.localPosition = new Vector3(0, trunkH * 0.5f, 0);
        trunk.transform.localScale = new Vector3(0.35f, trunkH, 0.35f);
        trunk.GetComponent<Renderer>().sharedMaterial = MakeMaterial(TreeTrunkColor());

        var leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Leaves";
        leaves.transform.SetParent(tree.transform);
        float leavesSize = Random.Range(2.0f, 3.0f);
        leaves.transform.localPosition = new Vector3(0, trunkH + leavesSize * 0.3f, 0);
        leaves.transform.localScale = Vector3.one * leavesSize;
        leaves.GetComponent<Renderer>().sharedMaterial = MakeMaterial(TreeLeafColor());
    }

    void CreateLamp(Vector3 pos, Transform parent)
    {
        var lamp = new GameObject("Lamp");
        lamp.transform.SetParent(parent);
        lamp.transform.position = pos;

        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        pole.transform.SetParent(lamp.transform);
        float poleH = 4.5f;
        pole.transform.localPosition = new Vector3(0, poleH * 0.5f, 0);
        pole.transform.localScale = new Vector3(0.2f, poleH, 0.2f);
        pole.GetComponent<Renderer>().sharedMaterial = MakeMaterial(LampPoleColor());

        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "Head";
        head.transform.SetParent(lamp.transform);
        head.transform.localPosition = new Vector3(0, poleH + 0.2f, 0);
        head.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);
        head.GetComponent<Renderer>().sharedMaterial = MakeMaterial(LampHeadColor());
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 머티리얼
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    Shader GetShader()
    {
        if (cachedShader != null) return cachedShader;
        cachedShader = Shader.Find("Universal Render Pipeline/Lit");
        if (cachedShader == null) cachedShader = Shader.Find("Standard");
        if (cachedShader == null) cachedShader = Shader.Find("HDRP/Lit");
        return cachedShader;
    }

    Material MakeMaterial(Color color)
    {
        int key = (Mathf.RoundToInt(color.r * 255) << 16)
                | (Mathf.RoundToInt(color.g * 255) << 8)
                |  Mathf.RoundToInt(color.b * 255);
        if (materialCache.TryGetValue(key, out var existing) && existing != null)
            return existing;
        var mat = new Material(GetShader());
        mat.color = color;
        mat.SetColor("_BaseColor", color);
        materialCache[key] = mat;
        materialColors[mat] = color;
        return mat;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Gizmos
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void OnDrawGizmos()
    {
        // 외곽선
        if (showOutlineGizmo)
        {
            Gizmos.color = Color.yellow;
            int segments = 72;
            Vector3 prev = Vector3.zero;
            float cx = TownWidth / 2f, cz = TownDepth / 2f;
            for (int i = 0; i <= segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                float lo = 0f, hi = Mathf.Max(TownWidth, TownDepth);
                for (int j = 0; j < 16; j++)
                {
                    float mid = (lo + hi) * 0.5f;
                    Vector3 tp = new Vector3(cx + Mathf.Cos(a) * mid, 0, cz + Mathf.Sin(a) * mid);
                    if (IsInsideShape(tp)) lo = mid; else hi = mid;
                }
                Vector3 pt = new Vector3(cx + Mathf.Cos(a) * lo, 0.5f, cz + Mathf.Sin(a) * lo);
                if (i > 0) Gizmos.DrawLine(prev, pt);
                prev = pt;
            }
        }

        // 블록 경계
        if (showBlockGridGizmo)
        {
            Transform blocksRoot = transform.Find("Blocks");
            if (blocksRoot != null)
            {
                Gizmos.color = blockGridColor;
                foreach (Transform blockTr in blocksRoot)
                {
                    var blk = blockTr.GetComponent<TownBlock>();
                    if (blk == null) continue;
                    float halfW = blk.outerRect.width * 0.5f;
                    float halfD = blk.outerRect.height * 0.5f;
                    float y = 0.5f;
                    Vector3 c1 = blockTr.TransformPoint(new Vector3(-halfW, y, -halfD));
                    Vector3 c2 = blockTr.TransformPoint(new Vector3( halfW, y, -halfD));
                    Vector3 c3 = blockTr.TransformPoint(new Vector3( halfW, y,  halfD));
                    Vector3 c4 = blockTr.TransformPoint(new Vector3(-halfW, y,  halfD));
                    Gizmos.DrawLine(c1, c2);
                    Gizmos.DrawLine(c2, c3);
                    Gizmos.DrawLine(c3, c4);
                    Gizmos.DrawLine(c4, c1);
                }
            }
        }

        // 영역
        if (showRegionGizmos && regions != null && regions.Count > 0)
        {
            Transform blocksRoot = transform.Find("Blocks");
            if (blocksRoot == null) return;

            var blockMap = new Dictionary<long, Transform>();
            foreach (Transform blockTr in blocksRoot)
            {
                var blk = blockTr.GetComponent<TownBlock>();
                if (blk == null) continue;
                blockMap[Key(blk.gridX, blk.gridZ)] = blockTr;
            }

            foreach (var region in regions)
            {
                if (region == null) continue;
                Color fill = region.color;
                Color line = new Color(region.color.r, region.color.g, region.color.b, 0.95f);

                Vector3 centerSum = Vector3.zero;
                int centerCount = 0;

                foreach (var v in region.blockIds)
                {
                    if (!blockMap.TryGetValue(Key(v.x, v.y), out Transform blockTr)) continue;
                    var blk = blockTr.GetComponent<TownBlock>();
                    if (blk == null) continue;

                    float halfW = blk.outerRect.width * 0.5f;
                    float halfD = blk.outerRect.height * 0.5f;

                    Matrix4x4 prevM = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(
                        blockTr.position + Vector3.up * 0.6f,
                        blockTr.rotation,
                        Vector3.one);
                    Gizmos.color = fill;
                    Gizmos.DrawCube(Vector3.zero, new Vector3(halfW * 2, 1.2f, halfD * 2));
                    Gizmos.color = line;
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(halfW * 2, 1.2f, halfD * 2));
                    Gizmos.matrix = prevM;

                    centerSum += blockTr.position;
                    centerCount++;
                }

                #if UNITY_EDITOR
                if (showRegionLabels && region.showLabel && centerCount > 0)
                {
                    Vector3 labelPos = centerSum / centerCount;
                    labelPos.y += 8f;

                    string text = region.name;
                    if (region.isLocked) text = "[LOCK] " + text;
                    text += $"  ({region.blockIds.Count})";

                    Color bgColor = region.isLocked
                        ? new Color(0.6f, 0.1f, 0.1f, 0.85f)
                        : new Color(0, 0, 0, 0.6f);

                    var style = new GUIStyle();
                    style.fontSize = 13;
                    style.fontStyle = FontStyle.Bold;
                    style.alignment = TextAnchor.MiddleCenter;
                    style.normal.textColor = region.isLocked ? new Color(1f, 0.9f, 0.6f) : Color.white;
                    style.normal.background = MakeLabelBg(bgColor);
                    style.padding = new RectOffset(8, 8, 4, 4);

                    UnityEditor.Handles.Label(labelPos, text, style);
                }
                #endif
            }
        }
    }

    #if UNITY_EDITOR
    static Texture2D _labelBgTex;
    static Color _labelBgLastColor;

    static Texture2D MakeLabelBg(Color color)
    {
        if (_labelBgTex != null && _labelBgLastColor == color) return _labelBgTex;
        _labelBgTex = new Texture2D(1, 1);
        _labelBgTex.SetPixel(0, 0, color);
        _labelBgTex.Apply();
        _labelBgTex.hideFlags = HideFlags.HideAndDontSave;
        _labelBgLastColor = color;
        return _labelBgTex;
    }
    #endif

    static long Key(int x, int z) => ((long)x << 32) | (uint)z;
}