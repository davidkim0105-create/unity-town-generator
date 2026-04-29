using System.IO;
using UnityEngine;

public static class TownJsonIO
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // EXPORT: TownGenerator → JSON 파일
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static string Export(TownGenerator gen, string filePath)
    {
        var data = new TownData();

        // 메타데이터
        data.metadata.createdAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.metadata.seed = gen.seed;

        // 파라미터
        data.parameters = new TownParams
        {
            townSize = gen.townSize,
            aspectRatio = gen.aspectRatio,
            townShape = gen.townShape.ToString(),
            shapeNoise = gen.shapeNoise,
            globalDensity = gen.globalDensity,
            densityFalloff = gen.densityFalloff,
            blockSize = gen.blockSize,
            roadDistortion = gen.roadDistortion,
            roadWidth = gen.roadWidth,
            buildingMargin = gen.buildingMargin,
            maxFloors = gen.maxFloors,
            floorHeight = gen.floorHeight,
            buildingMaxCoverage = gen.buildingMaxCoverage,
            alleyComplexity = gen.alleyComplexity,
            minLotSize = gen.minLotSize
        };

        int buildingCount = 0;
        int roadCount = 0;

        // 도로 + 블록 수집
        foreach (Transform child in gen.transform)
        {
            if (child.name == "Roads")
            {
                foreach (Transform r in child)
                {
                    var rd = new RoadData
                    {
                        position = new Vec3(r.position),
                        rotationY = r.eulerAngles.y,
                        scale = new Vec3(r.localScale),
                        color = GetColorHex(r)
                    };
                    data.roads.Add(rd);
                    roadCount++;
                }
            }
            else if (child.name == "Blocks")
            {
                foreach (Transform blockTr in child)
                {
                    var blk = blockTr.GetComponent<TownBlock>();
                    if (blk == null) continue;

                    var bd = new BlockData
                    {
                        gridX = blk.gridX,
                        gridZ = blk.gridZ,
                        position = new Vec3(blockTr.position),
                        rotationY = blockTr.eulerAngles.y,
                        outerRect = new Vec4(blk.outerRect),
                        innerRect = new Vec4(blk.innerRect),
                        autoDensity = blk.autoDensity,
                        densityOverride = blk.densityOverride,
                        seedOffset = blk.seedOffset,
                    };

                    foreach (Transform sub in blockTr)
                    {
                        if (sub.name == "BlockGround")
                        {
                            bd.groundColor = GetColorHex(sub);
                        }
                        else if (sub.name.StartsWith("Building"))
                        {
                            int floors = ParseFloors(sub.name);
                            bd.buildings.Add(new BuildingData
                            {
                                localPosition = new Vec3(sub.localPosition),
                                localScale = new Vec3(sub.localScale),
                                localRotationY = sub.localEulerAngles.y,
                                color = GetColorHex(sub),
                                floors = floors
                            });
                            buildingCount++;
                        }
                        else if (sub.name == "Alley")
                        {
                            bd.alleys.Add(new AlleyData
                            {
                                localPosition = new Vec3(sub.localPosition),
                                localScale = new Vec3(sub.localScale),
                                color = GetColorHex(sub)
                            });
                        }
                    }

                    data.blocks.Add(bd);
                }
            }
            else if (child.name == "_Ground")
            {
                data.groundColor = GetColorHex(child);
            }
        }

        data.metadata.blockCount = data.blocks.Count;
        data.metadata.buildingCount = buildingCount;
        data.metadata.roadCount = roadCount;

        // JSON 변환 + 파일 저장
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"✅ JSON 익스포트 완료: {filePath}\n블록 {data.blocks.Count}개, 건물 {buildingCount}개, 도로 {roadCount}개");
        return json;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // IMPORT: JSON 파일 → TownGenerator 씬에 복원
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static void Import(TownGenerator gen, string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"파일 없음: {filePath}");
            return;
        }

        string json = File.ReadAllText(filePath);
        var data = JsonUtility.FromJson<TownData>(json);
        if (data == null)
        {
            Debug.LogError("JSON 파싱 실패");
            return;
        }

        // 기존 씬 정리
        gen.Clear();

        // 파라미터 복원
        ApplyParams(gen, data.parameters);
        gen.seed = data.metadata.seed;

        // 바닥 복원
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "_Ground";
        ground.transform.SetParent(gen.transform);
        float maxSide = Mathf.Max(gen.townSize, gen.townSize / Mathf.Clamp(gen.aspectRatio, 0.4f, 2.5f));
        ground.transform.localPosition = new Vector3(gen.townSize / 2, -0.1f, gen.townSize / Mathf.Clamp(gen.aspectRatio, 0.4f, 2.5f) / 2);
        ground.transform.localScale = new Vector3(maxSide + 80, 0.1f, maxSide + 80);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMat(gen, ColorHex.FromHex(data.groundColor));

        // 도로 복원
        var roadParent = new GameObject("Roads");
        roadParent.transform.SetParent(gen.transform);
        foreach (var rd in data.roads)
        {
            var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "Road";
            r.transform.SetParent(roadParent.transform);
            r.transform.position = rd.position.ToVector3();
            r.transform.rotation = Quaternion.Euler(0, rd.rotationY, 0);
            r.transform.localScale = rd.scale.ToVector3();
            r.GetComponent<Renderer>().sharedMaterial = MakeMat(gen, ColorHex.FromHex(rd.color));
        }

        // 블록 복원
        var blocksParent = new GameObject("Blocks");
        blocksParent.transform.SetParent(gen.transform);
        foreach (var bd in data.blocks)
        {
            var blockGo = new GameObject($"Block_{bd.gridX}_{bd.gridZ}_d{bd.autoDensity:F2}");
            blockGo.transform.SetParent(blocksParent.transform);
            blockGo.transform.position = bd.position.ToVector3();
            blockGo.transform.rotation = Quaternion.Euler(0, bd.rotationY, 0);

            var blk = blockGo.AddComponent<TownBlock>();
            blk.gridX = bd.gridX;
            blk.gridZ = bd.gridZ;
            blk.outerRect = bd.outerRect.ToRect();
            blk.innerRect = bd.innerRect.ToRect();
            blk.autoDensity = bd.autoDensity;
            blk.densityOverride = bd.densityOverride;
            blk.seedOffset = bd.seedOffset;
            blk.generator = gen;

            // 블록 바닥
            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "BlockGround";
            bg.transform.SetParent(blockGo.transform);
            bg.transform.localPosition = new Vector3(0, -0.01f, 0);
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(blk.outerRect.width, 0.04f, blk.outerRect.height);
            bg.GetComponent<Renderer>().sharedMaterial = MakeMat(gen, ColorHex.FromHex(bd.groundColor));

            // 골목
            foreach (var ad in bd.alleys)
            {
                var ag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ag.name = "Alley";
                ag.transform.SetParent(blockGo.transform);
                ag.transform.localPosition = ad.localPosition.ToVector3();
                ag.transform.localRotation = Quaternion.identity;
                ag.transform.localScale = ad.localScale.ToVector3();
                ag.GetComponent<Renderer>().sharedMaterial = MakeMat(gen, ColorHex.FromHex(ad.color));
            }

            // 건물
            foreach (var bld in bd.buildings)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = $"Building_{bld.floors}F";
                b.transform.SetParent(blockGo.transform);
                b.transform.localPosition = bld.localPosition.ToVector3();
                b.transform.localRotation = Quaternion.Euler(0, bld.localRotationY, 0);
                b.transform.localScale = bld.localScale.ToVector3();
                b.GetComponent<Renderer>().sharedMaterial = MakeMat(gen, ColorHex.FromHex(bld.color));
            }
        }

        Debug.Log($"✅ JSON 임포트 완료: 블록 {data.blocks.Count}개, 도로 {data.roads.Count}개");
    }

    // ─── 헬퍼 ───
    static void ApplyParams(TownGenerator gen, TownParams p)
    {
        gen.townSize = p.townSize;
        gen.aspectRatio = p.aspectRatio;
        if (System.Enum.TryParse<TownShape>(p.townShape, out var shape))
            gen.townShape = shape;
        gen.shapeNoise = p.shapeNoise;
        gen.globalDensity = p.globalDensity;
        gen.densityFalloff = p.densityFalloff;
        gen.blockSize = p.blockSize;
        gen.roadDistortion = p.roadDistortion;
        gen.roadWidth = p.roadWidth;
        gen.buildingMargin = p.buildingMargin;
        gen.maxFloors = p.maxFloors;
        gen.floorHeight = p.floorHeight;
        gen.buildingMaxCoverage = p.buildingMaxCoverage;
        gen.alleyComplexity = p.alleyComplexity;
        gen.minLotSize = p.minLotSize;
    }

    static string GetColorHex(Transform t)
    {
        var r = t.GetComponent<Renderer>();
        if (r == null || r.sharedMaterial == null) return "#FFFFFF";

        // 캐시에서 원본 색 추출
        if (TownGenerator.materialColors.TryGetValue(r.sharedMaterial, out Color c))
            return ColorHex.ToHex(c);
        // 백업: 머티리얼의 현재 색
        return ColorHex.ToHex(r.sharedMaterial.color);
    }

    static int ParseFloors(string buildingName)
    {
        // "Building_8F" → 8
        int u = buildingName.LastIndexOf('_');
        int f = buildingName.LastIndexOf('F');
        if (u < 0 || f < 0 || f <= u) return 1;
        if (int.TryParse(buildingName.Substring(u + 1, f - u - 1), out int n))
            return n;
        return 1;
    }

    // 임포트 시에도 머티리얼 캐싱 활용 (같은 색 재사용)
    static System.Collections.Generic.Dictionary<int, Material> importCache
        = new System.Collections.Generic.Dictionary<int, Material>();

    static Material MakeMat(TownGenerator gen, Color color)
    {
        int key = (Mathf.RoundToInt(color.r * 255) << 16)
                | (Mathf.RoundToInt(color.g * 255) << 8)
                |  Mathf.RoundToInt(color.b * 255);
        if (importCache.TryGetValue(key, out var existing) && existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("HDRP/Lit");

        var mat = new Material(shader);
        mat.color = color;
        mat.SetColor("_BaseColor", color);
        importCache[key] = mat;
        TownGenerator.materialColors[mat] = color;
        return mat;
    }
}