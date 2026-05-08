using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2.Presets;

namespace TownGen.V2.EditorTools
{
    public static class QuickActions
    {
        // 매번 다른 결과를 위해 누적 카운터
        static int s_QuickRunCount = 0;

        // ON: 매번 그래프 새로 생성 / OFF: 그래프 있으면 빌드만 다시
        public static bool alwaysRegenerate = true;

        // ─────────────────────────────────────────
        // 1. 빠른 마을 (L-System, Experimental)
        // ─────────────────────────────────────────
        public static void QuickVillage(RoadGraphAuthoring auth, RegionAuthoring region)
        {
            if (auth == null) { Debug.LogWarning("[QuickActions] No RoadGraph"); return; }

            Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Quick Village");

            // L-System (도로 얇게, 간격 넓게)
            var lsPreset = LoadResource<LSystemPresetV2>("Presets/LSystem/LS_SmallVillage");
            if (lsPreset != null)
            {
                var s = lsPreset.GetSettings();
                s.seed = (int)System.DateTime.Now.Ticks + s_QuickRunCount;
                s.segmentLength = 12f;
                s.roadWidth = 1.5f;
                s.branchProbability = 0.25f;
                LSystemGrowTool.settings = s;
            }

            // Bldg
            var bldgPreset = LoadResource<BuildingPresetV2>("Presets/Building/Bldg_LowResi");
            if (bldgPreset != null)
            {
                auth.buildSettings = bldgPreset.GetSettings();
                auth.buildSettings.seed = s_QuickRunCount * 7919;
                auth.buildSettings.lotSize = 2.5f;
                auth.buildSettings.lotMargin = 0.1f;
            }

            // Town
            auth.useEdgeWidthForInset = true;
            auth.insetExtraMargin = 0.3f;
            auth.blockThickness = 0.1f;

            // 그래프
            if (alwaysRegenerate || auth.graph == null || auth.graph.nodes.Count == 0)
            {
                auth.graph.Clear();
                GrowFromMultipleSeeds(auth, seedCount: 4, spread: 35f);
                auth.InvalidateFaceCache();
            }

            s_QuickRunCount++;

            BuildAll(auth, region);
            ReportStats(auth, "Quick Village");
            EditorUtility.SetDirty(auth);
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────
        // 2. 거대 도시 + Voronoi 영역 (L-System, Experimental)
        // ─────────────────────────────────────────
        public static void QuickBigCityWithRegions(RoadGraphAuthoring auth, ref RegionAuthoring region)
        {
            if (auth == null) { Debug.LogWarning("[QuickActions] No RoadGraph"); return; }

            Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Quick Big City");

            // L-System
            var lsPreset = LoadResource<LSystemPresetV2>("Presets/LSystem/LS_BigCity");
            if (lsPreset != null)
            {
                var s = lsPreset.GetSettings();
                s.seed = (int)System.DateTime.Now.Ticks + s_QuickRunCount;
                s.segmentLength = 18f;
                s.roadWidth = 2f;
                s.branchProbability = 0.30f;
                LSystemGrowTool.settings = s;
            }

            // Bldg
            var bldgPreset = LoadResource<BuildingPresetV2>("Presets/Building/Bldg_HighCommercial");
            if (bldgPreset != null)
            {
                auth.buildSettings = bldgPreset.GetSettings();
                auth.buildSettings.seed = s_QuickRunCount * 7919;
                auth.buildSettings.lotSize = 4f;
                auth.buildSettings.lotMargin = 0.15f;
            }

            // Town
            auth.useEdgeWidthForInset = true;
            auth.insetExtraMargin = 0.5f;
            auth.blockThickness = 0.1f;

            // 그래프
            if (alwaysRegenerate || auth.graph == null || auth.graph.nodes.Count == 0)
            {
                auth.graph.Clear();
                GrowFromMultipleSeeds(auth, seedCount: 6, spread: 70f);
                auth.InvalidateFaceCache();
            }

            // Region 자동 생성
            if (region == null)
            {
                var regGo = new GameObject("Regions");
                Undo.RegisterCreatedObjectUndo(regGo, "Quick Region");
                region = regGo.AddComponent<RegionAuthoring>();
            }

            VoronoiTool.RandomPlaceSeeds(auth, 5);
            VoronoiTool.mode = VoronoiTool.CellTypeMode.OnePerSeed;
            VoronoiTool.GenerateRegions(auth, region);

            s_QuickRunCount++;

            BuildAll(auth, region);
            ReportStats(auth, "Quick Big City");
            EditorUtility.SetDirty(auth);
            EditorUtility.SetDirty(region);
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────
        // 3. OSM 정리 + 빌드 (이미 임포트된 후)
        // ─────────────────────────────────────────
        public static void QuickCleanupOSM(RoadGraphAuthoring auth, RegionAuthoring region)
        {
            if (auth == null || auth.graph == null || auth.graph.nodes.Count == 0)
            {
                Debug.LogWarning("[QuickActions] OSM 임포트 먼저 하세요.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "OSM Cleanup");

            // 외톨이 노드 제거
            int removed = 0;
            for (int i = auth.graph.nodes.Count - 1; i >= 0; i--)
            {
                var n = auth.graph.nodes[i];
                if (n.edgeIds == null || n.edgeIds.Count == 0)
                {
                    auth.graph.nodes.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0) auth.graph.InvalidateCache();

            // 도로 폭 축소
            foreach (var e in auth.graph.edges)
                e.width = Mathf.Max(0.5f, e.width * 0.49f);

            // Build Settings
            auth.useEdgeWidthForInset = false;
            auth.roadInset = 1.5f;
            auth.patternSettings.pattern = BlockPatternFiller.BlockPattern.SingleTower;

            // Bldg
            var bldgPreset = LoadResource<BuildingPresetV2>("Presets/Building/Bldg_HighCommercial");
            if (bldgPreset != null)
            {
                auth.buildSettings = bldgPreset.GetSettings();
                auth.buildSettings.seed = s_QuickRunCount * 7919;
            }
            s_QuickRunCount++;

            auth.InvalidateFaceCache();

            BuildAll(auth, region);
            ReportStats(auth, "OSM Cleanup");
            EditorUtility.SetDirty(auth);
            SceneView.RepaintAll();
            Debug.Log($"[QuickActions] OSM Cleanup: removed {removed} orphans, scaled widths × 0.49.");
        }

        // ─────────────────────────────────────────
        // 4. 격자 도시 (테스트용)
        // ─────────────────────────────────────────
        public static void QuickGridCity(RoadGraphAuthoring auth, RegionAuthoring region)
        {
            if (auth == null) return;

            Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Quick Grid City");

            auth.graph.Clear();
            float step = 12f;
            int n = 5;
            var nodes = new RoadNode[n, n];
            for (int x = 0; x < n; x++)
                for (int z = 0; z < n; z++)
                    nodes[x, z] = auth.graph.AddNode(new Vector3(x * step, 0, z * step));
            for (int x = 0; x < n; x++)
                for (int z = 0; z < n; z++)
                {
                    if (x < n - 1) auth.graph.AddEdge(nodes[x, z].id, nodes[x + 1, z].id);
                    if (z < n - 1) auth.graph.AddEdge(nodes[x, z].id, nodes[x, z + 1].id);
                }
            auth.InvalidateFaceCache();

            var townPreset = LoadResource<TownPresetV2>("Presets/Town/Town_Default");
            if (townPreset != null) townPreset.ApplyTo(auth);
            var bldgPreset = LoadResource<BuildingPresetV2>("Presets/Building/Bldg_MidResi");
            if (bldgPreset != null)
            {
                auth.buildSettings = bldgPreset.GetSettings();
                auth.buildSettings.seed = s_QuickRunCount * 7919;
            }
            s_QuickRunCount++;

            BuildAll(auth, region);
            ReportStats(auth, "Quick Grid City");
            EditorUtility.SetDirty(auth);
            SceneView.RepaintAll();
        }

        // ─────────────────────────────────────────
        // 5. ★ Quick OSM City — 한 클릭 (메인)
        // ─────────────────────────────────────────
        public static void QuickOSMCity(RoadGraphAuthoring auth, ref RegionAuthoring region)
        {
            if (auth == null) { Debug.LogWarning("[QuickActions] No RoadGraph"); return; }

            // 1) 파일 선택
            string path = EditorUtility.OpenFilePanel(
                "Select .osm file", Application.dataPath, "osm");
            if (string.IsNullOrEmpty(path)) return;

            Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Quick OSM City");

            // 2) Clear + Import
            auth.graph.Clear();

            var opts = new TownGen.V2.OSM.OSMImporter.ImportOptions
            {
                clearGraphFirst = false,
                useOSMRoadWidths = true,
                defaultWidth = 3f,
                scaleFactor = 1f,
                excludeFootways = false   // 골목 포함 → face 더 많이
            };
            var importResult = TownGen.V2.OSM.OSMImporter.Import(path, auth, opts);
            Debug.Log("[QuickOSMCity] " + importResult.summary);

            if (importResult.edgesAdded == 0)
            {
                Debug.LogWarning("[QuickOSMCity] OSM 데이터에 도로가 없습니다.");
                return;
            }

            // 3) 외톨이 노드 제거
            int orphans = 0;
            for (int i = auth.graph.nodes.Count - 1; i >= 0; i--)
            {
                var n = auth.graph.nodes[i];
                if (n.edgeIds == null || n.edgeIds.Count == 0)
                {
                    auth.graph.nodes.RemoveAt(i);
                    orphans++;
                }
            }
            if (orphans > 0) auth.graph.InvalidateCache();

            // 4) 도로 폭 축소 (motorway 10m → 5m)
            foreach (var e in auth.graph.edges)
                e.width = Mathf.Max(0.8f, e.width * 0.5f);

            // 5) Build Settings 자동 조정
            auth.useEdgeWidthForInset = true;
            auth.insetExtraMargin = 0.5f;
            auth.roadInset = 1.5f;
            auth.blockThickness = 0.1f;
            auth.patternSettings.pattern = BlockPatternFiller.BlockPattern.Solid;

            // 6) Bldg Preset (안쪽까지 채움)
            var bldgPreset = LoadResource<BuildingPresetV2>("Presets/Building/Bldg_MidResi");
            if (bldgPreset != null)
            {
                auth.buildSettings = bldgPreset.GetSettings();
                auth.buildSettings.seed = s_QuickRunCount * 7919;
                auth.buildSettings.mode = BuildingFiller.FillMode.GridOBB;
                auth.buildSettings.lotSize = 4f;
                auth.buildSettings.lotMargin = 0.15f;
                auth.buildSettings.density = 0.9f;
                auth.buildSettings.fillInteriorRows = true;
            }
            s_QuickRunCount++;

            // 7) Region 자동 (region 컨테이너 있으면)
            if (region != null)
            {
                VoronoiTool.RandomPlaceSeeds(auth, 4);
                VoronoiTool.mode = VoronoiTool.CellTypeMode.OnePerSeed;
                VoronoiTool.GenerateRegions(auth, region);
            }

            auth.InvalidateFaceCache();

            // 8) 1차 Build
            BuildAll(auth, region);

            // 9) 큰 빈 블록 자동 분할
            int subdivided = SubdivideLargeBlocks(auth, region, minArea: 600f, maxIterations: 4);
            if (subdivided > 0)
            {
                auth.InvalidateFaceCache();
                BuildAll(auth, region);
                Debug.Log($"[QuickOSMCity] Subdivided {subdivided} large blocks.");
            }

            ReportStats(auth, "Quick OSM City");
            EditorUtility.SetDirty(auth);
            if (region != null) EditorUtility.SetDirty(region);
            SceneView.RepaintAll();

            Debug.Log($"[QuickOSMCity] Cleanup: removed {orphans} orphans, scaled widths × 0.5.");
        }

        // ─────────────────────────────────────────
        // 6. Night / Day Mode
        // ─────────────────────────────────────────
        public static void QuickNightCity(RoadGraphAuthoring auth, RegionAuthoring region)
        {
            if (auth == null) return;

            BuildAll(auth, region);
            NightPreset.Apply(NightPreset.Mode.Night);
            NightPreset.ApplyBuildingEmission(auth, true,
                new Color(1f, 0.85f, 0.5f));
            SceneView.RepaintAll();
            Debug.Log("[QuickActions] Night mode applied.");
        }

        public static void QuickDayMode(RoadGraphAuthoring auth)
        {
            NightPreset.Apply(NightPreset.Mode.Day);
            if (auth != null)
                NightPreset.ApplyBuildingEmission(auth, false, Color.black);
            SceneView.RepaintAll();
            Debug.Log("[QuickActions] Day mode applied.");
        }

        // ═════════════════════════════════════════
        // 헬퍼들
        // ═════════════════════════════════════════

        // ─── 공통: Build All ───
        static void BuildAll(RoadGraphAuthoring auth, RegionAuthoring region)
        {
            BlockBuilder.RebuildAllBlocks(auth, auth.roadInset,
                regions: region, blockThickness: auth.blockThickness,
                useEdgeWidthForInset: auth.useEdgeWidthForInset,
                insetExtraMargin: auth.insetExtraMargin);
            BlockBuilder.FillAllBlocksWithBuildings(auth, auth.buildSettings, region);
            var rm = auth.GetComponent<RoadMeshAuthoring>();
            if (rm != null) rm.Build();
        }

        // ─── 여러 시드 위치에서 L-System 성장 ───
        static void GrowFromMultipleSeeds(RoadGraphAuthoring auth, int seedCount, float spread)
        {
            var rng = new System.Random(LSystemGrowTool.settings.seed);
            var origins = new System.Collections.Generic.List<Vector3>();

            origins.Add(Vector3.zero);
            for (int i = 1; i < seedCount; i++)
            {
                float ang = (i / (float)seedCount) * Mathf.PI * 2f
                            + (float)(rng.NextDouble() - 0.5) * 0.5f;
                float dist = spread * (0.7f + (float)rng.NextDouble() * 0.5f);
                origins.Add(new Vector3(
                    Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist));
            }

            var settings = LSystemGrowTool.settings;
            int origSeed = settings.seed;
            foreach (var origin in origins)
            {
                LSystemGenerator.Grow(auth.graph, origin, settings);
                settings.seed++;
            }
            settings.seed = origSeed + seedCount;
            LSystemGrowTool.settings = settings;
        }

        // ─── 큰 블록 자동 분할 ───
        static int SubdivideLargeBlocks(RoadGraphAuthoring auth, RegionAuthoring region,
            float minArea, int maxIterations)
        {
            int totalSubdivided = 0;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                var faces = auth.GetFaces();
                var largeFaces = new System.Collections.Generic.List<FaceResult>();

                foreach (var f in faces)
                {
                    if (f.isOuter) continue;
                    if (f.polygon == null || f.polygon.Count < 4) continue;
                    float area = Mathf.Abs(f.signedArea);
                    if (area >= minArea) largeFaces.Add(f);
                }

                Debug.Log($"[Subdivide] Iter {iter + 1}: {largeFaces.Count} large faces (>= {minArea}㎡)");

                if (largeFaces.Count == 0) break;

                // 큰 블록부터 처리
                largeFaces.Sort((a, b) =>
                    Mathf.Abs(b.signedArea).CompareTo(Mathf.Abs(a.signedArea)));

                int subdividedThisIter = 0;
                foreach (var face in largeFaces)
                {
                    if (TrySubdivideFace(auth, face))
                    {
                        subdividedThisIter++;
                        totalSubdivided++;
                    }
                }

                if (subdividedThisIter == 0) break;
                auth.InvalidateFaceCache();
            }

            return totalSubdivided;
        }

        // ─── face 하나 분할 ───
        static bool TrySubdivideFace(RoadGraphAuthoring auth, FaceResult face)
        {
            int n = face.nodeIds.Count;
            if (n < 4) return false;

            // 좁고 긴 face는 분할해도 더 가늘어지기만 함 → 스킵
            if (IsTooNarrow(face.polygon, ratioLimit: 4f)) return false;

            // 가장 긴 변과 마주보는 변 찾기
            float[] lens = new float[n];
            for (int i = 0; i < n; i++)
                lens[i] = Vector3.Distance(face.polygon[i], face.polygon[(i + 1) % n]);

            int idxA = 0; float maxLen = 0;
            for (int i = 0; i < n; i++)
                if (lens[i] > maxLen) { maxLen = lens[i]; idxA = i; }

            if (lens[idxA] < 4f) return false;

            // 마주보는 변 찾기
            Vector3 dirA = (face.polygon[(idxA + 1) % n] - face.polygon[idxA]).normalized;
            int idxB = -1; float bestScore = float.MinValue;
            for (int j = 0; j < n; j++)
            {
                if (j == idxA) continue;
                if (lens[j] < 3f) continue;
                Vector3 dirB = (face.polygon[(j + 1) % n] - face.polygon[j]).normalized;
                float parallel = Mathf.Abs(Vector3.Dot(dirA, dirB));
                int dist = Mathf.Min(Mathf.Abs(j - idxA), n - Mathf.Abs(j - idxA));
                float score = parallel * 2f + dist * 0.5f;
                if (score > bestScore) { bestScore = score; idxB = j; }
            }
            if (idxB < 0) return false;

            // 두 변의 중점에 노드 삽입 + 도로 추가
            int nA1 = face.nodeIds[idxA], nA2 = face.nodeIds[(idxA + 1) % n];
            int nB1 = face.nodeIds[idxB], nB2 = face.nodeIds[(idxB + 1) % n];

            int edgeA = FindEdgeBetween(auth.graph, nA1, nA2);
            int edgeB = FindEdgeBetween(auth.graph, nB1, nB2);
            if (edgeA < 0 || edgeB < 0) return false;

            Vector3 midA = (face.polygon[idxA] + face.polygon[(idxA + 1) % n]) * 0.5f;
            Vector3 midB = (face.polygon[idxB] + face.polygon[(idxB + 1) % n]) * 0.5f;

            var newA = auth.graph.SplitEdgeAt(edgeA, midA);
            var newB = auth.graph.SplitEdgeAt(edgeB, midB);
            if (newA == null || newB == null) return false;

            var newEdge = auth.graph.AddEdge(newA.id, newB.id);
            if (newEdge != null) newEdge.width = 2.5f;

            return true;
        }

        // ─── OBB 가로세로 비율로 좁고 긴 face 판정 ───
        static bool IsTooNarrow(System.Collections.Generic.List<Vector3> poly, float ratioLimit)
        {
            if (poly == null || poly.Count < 3) return true;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in poly)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z;
                if (p.z > maxZ) maxZ = p.z;
            }

            float w = maxX - minX;
            float h = maxZ - minZ;
            if (w < 0.01f || h < 0.01f) return true;

            float ratio = Mathf.Max(w, h) / Mathf.Min(w, h);
            return ratio > ratioLimit;
        }

        static int FindEdgeBetween(RoadGraph g, int aId, int bId)
        {
            foreach (var e in g.edges)
                if ((e.nodeAId == aId && e.nodeBId == bId) ||
                    (e.nodeAId == bId && e.nodeBId == aId)) return e.id;
            return -1;
        }

        // ─── 빌드 후 통계 ───
        static void ReportStats(RoadGraphAuthoring auth, string action)
        {
            int nodes = auth.graph.nodes.Count;
            int edges = auth.graph.edges.Count;
            var faces = auth.GetFaces();
            int inner = 0, outer = 0;
            foreach (var f in faces) { if (f.isOuter) outer++; else inner++; }

            int blockCount = 0, buildingCount = 0;
            var blocksRoot = auth.transform.Find("_Blocks");
            if (blocksRoot != null)
            {
                foreach (Transform b in blocksRoot)
                {
                    blockCount++;
                    buildingCount += b.childCount;
                }
            }

            string msg = $"[QuickActions] {action}: " +
                         $"{nodes} nodes, {edges} edges, " +
                         $"{inner} inner faces → {blockCount} blocks, {buildingCount} buildings.";

            if (inner == 0)
                msg += "\n⚠️ No closed face — try clicking [Quick Big City] for cycles, or use Brush/Draw to close loops.";
            else if (buildingCount == 0)
                msg += "\n⚠️ No buildings placed — check Lot Size or Density.";

            Debug.Log(msg);
        }

        // ─── Resources 로드 ───
        static T LoadResource<T>(string path) where T : ScriptableObject
        {
            var asset = Resources.Load<T>(path);
            if (asset == null)
                Debug.LogWarning($"[QuickActions] Preset not found: Resources/{path}.\n" +
                                 "Run [TownGen V2 → Create Built-in Presets] first.");
            return asset;
        }
    }
}