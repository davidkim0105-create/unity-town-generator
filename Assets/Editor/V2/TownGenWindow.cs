using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;
using TownGen.V2.Presets;

namespace TownGen.V2.EditorTools
{
    public class TownGenWindow : EditorWindow
    {
        // ─── 윈도우 메뉴 ───
        [MenuItem("TownGen V2/Open Window &#t")]   // Alt+Shift+T
        public static void Open()
        {
            var w = GetWindow<TownGenWindow>("Town V2");
            w.minSize = new Vector2(320, 600);
        }

        // ─── 활성 컨텍스트 ───
        RoadGraphAuthoring activeAuth;
        RegionAuthoring activeRegion;
        RoadMeshAuthoring activeRoadMesh;
        bool autoFollowSelection = true;

        Vector2 scroll;

        // ─── Preset 슬롯 ───
        TownPresetV2 townPresetSlot;
        BuildingPresetV2 buildingPresetSlot;
        LSystemPresetV2 lsystemPresetSlot;

        // ─── OSM Import 옵션 ───
        TownGen.V2.OSM.OSMImporter.ImportOptions osmOptions = new TownGen.V2.OSM.OSMImporter.ImportOptions();

        // ─── Foldout 상태 (EditorPrefs로 영속) ───
        bool foldHeader        => GetFold("header", true);
        bool foldTools         => GetFold("tools", true);
        bool foldGraphInfo     => GetFold("graphInfo", false);
        bool foldTestGraphs    => GetFold("testGraphs", false);
        bool foldClear         => GetFold("clear", false);
        bool foldBuildSet      => GetFold("buildSet", true);
        bool foldRoadMesh      => GetFold("roadMesh", false);
        bool foldBuildBtns     => GetFold("buildBtns", true);
        bool foldDoc           => GetFold("doc", false);

        const string FOLD_KEY = "TownGenV2.Fold.";
        bool GetFold(string k, bool def) => EditorPrefs.GetBool(FOLD_KEY + k, def);
        void SetFold(string k, bool v) => EditorPrefs.SetBool(FOLD_KEY + k, v);

        bool DrawSectionFoldout(string key, string label, bool defaultOpen)
        {
            // 색은 OnGUI에서 카테고리별로 지정. 여기는 폴백 회색
            return UIStyles.ColoredFoldout(key, label, UIStyles.SectionDebugColor, defaultOpen);
        }

        // 카테고리별 wrapper
        bool DrawEditFoldout(string key, string label, bool defaultOpen) =>
            UIStyles.ColoredFoldout(key, label, UIStyles.SectionEditColor, defaultOpen);

        bool DrawBuildFoldout(string key, string label, bool defaultOpen) =>
            UIStyles.ColoredFoldout(key, label, UIStyles.SectionBuildColor, defaultOpen);

        bool DrawDataFoldout(string key, string label, bool defaultOpen) =>
            UIStyles.ColoredFoldout(key, label, UIStyles.SectionDataColor, defaultOpen);

        bool DrawDebugFoldout(string key, string label, bool defaultOpen) =>
            UIStyles.ColoredFoldout(key, label, UIStyles.SectionDebugColor, defaultOpen);

        // ─── GUIContent helper ───
        static GUIContent GC(string label, string tooltip) => new GUIContent(label, tooltip);

        void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            UnityEditor.EditorTools.ToolManager.activeToolChanged += OnActiveToolChanged;
            RefreshActive();
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            UnityEditor.EditorTools.ToolManager.activeToolChanged -= OnActiveToolChanged;
        }

        void OnActiveToolChanged() { Repaint(); }

        void OnSelectionChanged()
        {
            if (autoFollowSelection) RefreshFromSelection();
            Repaint();
        }

        void OnHierarchyChanged()
        {
            if (activeAuth == null) RefreshActive();
            Repaint();
        }

        void RefreshFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var auth = go.GetComponent<RoadGraphAuthoring>()
                     ?? go.GetComponentInParent<RoadGraphAuthoring>()
                     ?? go.GetComponentInChildren<RoadGraphAuthoring>();
            if (auth != null) { activeAuth = auth; activeRoadMesh = null; }

            var reg = go.GetComponent<RegionAuthoring>()
                    ?? go.GetComponentInParent<RegionAuthoring>()
                    ?? go.GetComponentInChildren<RegionAuthoring>();
            if (reg != null) activeRegion = reg;

            if (activeRegion == null && activeAuth != null)
                activeRegion = FindRegionFor(activeAuth);

            EnsureRoadMesh();
        }

        void RefreshActive()
        {
            if (activeAuth == null)
                activeAuth = Object.FindAnyObjectByType<RoadGraphAuthoring>();
            if (activeRegion == null && activeAuth != null)
                activeRegion = FindRegionFor(activeAuth);
            EnsureRoadMesh();
        }

        /// <summary>
        /// activeAuth와 같은 GameObject에서 RoadMeshAuthoring을 찾고, 없으면 자동 추가.
        /// </summary>
        void EnsureRoadMesh()
        {
            if (activeAuth == null) { activeRoadMesh = null; return; }
            activeRoadMesh = activeAuth.GetComponent<RoadMeshAuthoring>();
            // 자동 생성 (사용자가 따로 추가 안 해도 됨)
            if (activeRoadMesh == null)
            {
                activeRoadMesh = Undo.AddComponent<RoadMeshAuthoring>(activeAuth.gameObject);
            }
        }

        static RegionAuthoring FindRegionFor(RoadGraphAuthoring auth)
        {
            var r = auth.GetComponentInChildren<RegionAuthoring>();
            if (r == null) r = Object.FindAnyObjectByType<RegionAuthoring>();
            return r;
        }

        // ─────────── GUI ───────────
        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            // 헤더 (항상 보임)
            DrawHeader();
            EditorGUILayout.Space();

            if (activeAuth == null)
            {
                EditorGUILayout.HelpBox("No RoadGraph in scene. Click 'Create RoadGraph' below.", MessageType.Info);
                if (GUILayout.Button("Create RoadGraph")) CreateRoadGraph();
                EditorGUILayout.EndScrollView();
                return;
            }

            // 펼치기/접기 일괄 버튼
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Expand All", GUILayout.Width(90)))
                {
                    SetFold("tools", true); SetFold("graphInfo", true); SetFold("testGraphs", true);
                    SetFold("clear", true); SetFold("buildSet", true); SetFold("roadMesh", true);
                    SetFold("buildBtns", true); SetFold("doc", true);
                    SetFold("osm", true); SetFold("light", true);
                }
                if (GUILayout.Button("Collapse All", GUILayout.Width(90)))
                {
                    SetFold("tools", false); SetFold("graphInfo", false); SetFold("testGraphs", false);
                    SetFold("clear", false); SetFold("buildSet", false); SetFold("roadMesh", false);
                    SetFold("buildBtns", false); SetFold("doc", false);
                    SetFold("osm", false); SetFold("light", false);
                }
                if (GUILayout.Button("Default", GUILayout.Width(70)))
                {
                    // EDIT
                    SetFold("tools", true);
                    // BUILD
                    SetFold("buildSet", true); SetFold("buildBtns", true);
                    SetFold("roadMesh", false); SetFold("light", false);
                    // DATA
                    SetFold("doc", false); SetFold("osm", false); SetFold("testGraphs", false);
                    // DEBUG
                    SetFold("graphInfo", false); SetFold("clear", false);
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.Space(2);

            // ═══════════════════════════════════
            // 🔵 EDIT — 그래프 편집 도구
            // ═══════════════════════════════════
            DrawCategoryHeader("EDIT", UIStyles.SectionEditColor);

            if (DrawEditFoldout("tools", "🛠 Tools", true))
            {
                DrawTools();
            }
            EditorGUILayout.Space(2);

            // ═══════════════════════════════════
            // 🟢 BUILD — 도시 생성 + 시각화
            // ═══════════════════════════════════
            DrawCategoryHeader("BUILD", UIStyles.SectionBuildColor);

            if (DrawBuildFoldout("quick", "⚡ Quick Actions", true))
            {
                DrawQuickActionsSection();
            }
            EditorGUILayout.Space(2);

            if (DrawBuildFoldout("buildSet", "⚙ Build Settings", false))
            {
                DrawBuildSettings();
            }
            EditorGUILayout.Space(2);

            if (DrawBuildFoldout("buildBtns", "▶ Build", true))
            {
                DrawBuildButtons();
            }
            EditorGUILayout.Space(2);

            if (DrawBuildFoldout("roadMesh", "🛣 Road Mesh", false))
            {
                DrawRoadMeshSection();
            }
            EditorGUILayout.Space(2);

            if (DrawBuildFoldout("light", "🌙 Lighting Preset", false))
            {
                DrawLightingSection();
            }
            EditorGUILayout.Space(2);

            // ═══════════════════════════════════
            // 🟠 DATA — 데이터 입출력
            // ═══════════════════════════════════
            DrawCategoryHeader("DATA", UIStyles.SectionDataColor);

            if (DrawDataFoldout("doc", "💾 Save / Load / Capture", false))
            {
                DrawDocSection();
            }
            EditorGUILayout.Space(2);

            if (DrawDataFoldout("osm", "🌐 OSM Import", false))
            {
                DrawOSMSection();
            }
            EditorGUILayout.Space(2);

            if (DrawDataFoldout("testGraphs", "🧪 Test Graphs / Auto Generators", false))
            {
                DrawTestGraphs();
            }
            EditorGUILayout.Space(2);

            // ═══════════════════════════════════
            // ⚪ DEBUG — 정보 + 정리
            // ═══════════════════════════════════
            DrawCategoryHeader("DEBUG", UIStyles.SectionDebugColor);

            if (DrawDebugFoldout("graphInfo", "ℹ Graph Info / Stats", false))
            {
                DrawGraphInfo();
                DrawSelectionInfo();
            }
            EditorGUILayout.Space(2);

            if (DrawDebugFoldout("clear", "🗑 Clear", false))
            {
                DrawClearSection();
            }

            EditorGUILayout.EndScrollView();
        }

        // 카테고리 헤더 (큰 색 띠, 윈도우 전체 폭)
        void DrawCategoryHeader(string label, Color color)
        {
            EditorGUILayout.Space(6);
            var rect = EditorGUILayout.GetControlRect(false, 18);

            // 배경 (전체 폭)
            Color bg = color; bg.a = 0.5f;
            var bgRect = new Rect(0, rect.y, EditorGUIUtility.currentViewWidth, rect.height);
            EditorGUI.DrawRect(bgRect, bg);

            // 라벨
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.fontSize = 10;
            style.alignment = TextAnchor.MiddleLeft;
            style.padding = new RectOffset(8, 0, 0, 0);
            style.normal.textColor = new Color(0.95f, 0.95f, 0.95f);
            EditorGUI.LabelField(rect, label, style);

            EditorGUILayout.Space(2);
        }

        // ─────────── 섹션들 ───────────
        void DrawHeader()
        {
            EditorGUILayout.LabelField("Active Context", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("RoadGraph", GUILayout.Width(80));
                var newAuth = (RoadGraphAuthoring)EditorGUILayout.ObjectField(
                    activeAuth, typeof(RoadGraphAuthoring), true);
                if (newAuth != activeAuth) { activeAuth = newAuth; activeRegion = null; activeRoadMesh = null; RefreshActive(); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Regions", GUILayout.Width(80));
                activeRegion = (RegionAuthoring)EditorGUILayout.ObjectField(
                    activeRegion, typeof(RegionAuthoring), true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                autoFollowSelection = EditorGUILayout.ToggleLeft(
                    GC("Follow Selection", "Hierarchy 선택 시 자동으로 활성 컨텍스트 변경"),
                    autoFollowSelection);
                if (GUILayout.Button(GC("Pick from Selection", "Hierarchy에서 선택한 오브젝트로부터 RoadGraph/Region 자동 탐색")))
                {
                    RefreshFromSelection();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Create RoadGraph", "씬에 RoadGraph 오브젝트 생성")))
                    CreateRoadGraph();
                if (GUILayout.Button(GC("Create Regions", "씬에 Regions 컨테이너 생성")))
                    CreateRegions();
                if (GUILayout.Button(GC("Select", "활성 RoadGraph를 Hierarchy에서 선택")))
                {
                    if (activeAuth != null)
                    {
                        Selection.activeGameObject = activeAuth.gameObject;
                        EditorGUIUtility.PingObject(activeAuth.gameObject);
                    }
                }
            }
        }

        void DrawTools()
        {
            EditorGUILayout.LabelField("Tools (Shift+Q/W/E/R/T = edit · Shift+Z/X/C/V = build)", EditorStyles.boldLabel);

            GUILayout.Label("Road Graph", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                ToolButton<NodeMoveTool>(activeAuth, "Move (⇧Q)", "노드 이동 (드래그/박스 선택/Delete). 단축키: Shift+Q");
                ToolButton<RoadDrawTool>(activeAuth, "Draw (⇧W)", "도로 그리기. 단축키: Shift+W\n그리는 중 Shift=각도 스냅 45°, Ctrl=그리드 스냅 1m");
                ToolButton<RoadCutTool>(activeAuth, "Cut (⇧E)", "엣지 위 클릭 → 노드 삽입+분할. 단축키: Shift+E");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                ToolButton<RoadDeleteTool>(activeAuth, "Delete (⇧R)", "엣지/노드 삭제. 단축키: Shift+R");
                ToolButton<NodeConnectTool>(activeAuth, "Connect (⇧T)", "두 노드 → 합치기. 단축키: Shift+T");
                ToolButton<RoadBrushTool>(activeAuth, "Brush (⇧Z)",
                    "도로 브러시: 드래그로 일정 간격마다 노드+엣지 자동 생성. 단축키: Shift+Z");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                ToolButton<SubdivideTool>(activeAuth, "Subdivide (⇧X)",
                    "블록 분할: 클릭한 블록을 가장 긴 변 기준으로 두 개로 나눔. 단축키: Shift+X");
                ToolButton<LSystemGrowTool>(activeAuth, "L-Grow (⇧C)",
                    "L-System: 클릭한 위치에서 도시 도로망 자동 성장. 단축키: Shift+C");
                ToolButton<WidthBrushTool>(activeAuth, "Width (⇧V)",
                    "도로 폭 브러시: 엣지 클릭/드래그로 폭 변경. Alt+클릭=스포이드. 단축키: Shift+V");
            }
using (new EditorGUILayout.HorizontalScope())
            {
                ToolButton<VoronoiTool>(activeAuth, "Voronoi (⇧F)",
                    "시드 점 클릭 → Voronoi 자동 영역 분할. 단축키: Shift+F");
                ToolButton<BlockInspectorTool>(activeAuth, "Inspect (⇧A)",
                    "블록 호버로 면적/빌딩 수/Region 정보 표시. 클릭하면 그 블록 선택. 단축키: Shift+A");
            }

            if (activeRegion != null)
            {
                GUILayout.Space(4);
                GUILayout.Label("Regions", EditorStyles.miniBoldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    ToolButton<RegionPaintTool>(activeRegion, "Paint Region",
                        "영역 폴리곤 그리기 (좌클=추가, 우클=마지막 점 삭제, Tab=다음 영역)");
                }
            }

           GUILayout.Space(4);
            EditorGUILayout.HelpBox(
                UnityEditor.EditorTools.ToolManager.activeToolType != null
                    ? $"Active tool: {UnityEditor.EditorTools.ToolManager.activeToolType.Name}"
                    : "No active tool",
                MessageType.None);

            // ─── Road Brush 옵션 (활성일 때만) ───
            if (UnityEditor.EditorTools.ToolManager.activeToolType == typeof(RoadBrushTool))
            {
                EditorGUILayout.Space(4);
                GUILayout.Label("Brush Options", EditorStyles.miniBoldLabel);
                RoadBrushTool.spacing = EditorGUILayout.Slider(
                    GC("Spacing", "노드 간격(m). 작게=촘촘, 크게=드물게"),
                    RoadBrushTool.spacing, 1f, 30f);
                RoadBrushTool.roadWidth = EditorGUILayout.Slider(
                    GC("Road Width", "그릴 도로의 폭(m)"),
                    RoadBrushTool.roadWidth, 1f, 20f);
                RoadBrushTool.snapToExisting = EditorGUILayout.Toggle(
                    GC("Snap to Existing", "근처 노드/엣지에 자동 합치기"),
                    RoadBrushTool.snapToExisting);
                using (new EditorGUI.DisabledScope(!RoadBrushTool.snapToExisting))
                {
                    RoadBrushTool.snapDistance = EditorGUILayout.Slider(
                        GC("Snap Distance", "이 거리 안에 기존 노드/엣지가 있으면 합침"),
                        RoadBrushTool.snapDistance, 0.5f, 10f);
                }
                if (RoadBrushTool.snapToExisting && RoadBrushTool.spacing < RoadBrushTool.snapDistance * 1.2f)
                {
                    EditorGUILayout.HelpBox(
                        $"Spacing({RoadBrushTool.spacing:F1})이 Snap Distance({RoadBrushTool.snapDistance:F1})보다 작거나 비슷합니다.\n" +
                        $"실제로는 {RoadBrushTool.snapDistance * 1.2f:F1}m 간격으로 그려집니다.",
                        MessageType.Info);
                }
            }

            // ─── Subdivide 옵션 ───
            if (UnityEditor.EditorTools.ToolManager.activeToolType == typeof(SubdivideTool))
            {
                EditorGUILayout.Space(4);
                UIStyles.SubHeader("✂ Subdivide");

                SubdivideTool.newRoadWidth = EditorGUILayout.Slider(
                    GC("New Road Width", "분할 시 추가되는 도로 폭(m)"),
                    SubdivideTool.newRoadWidth, 1f, 10f);

                if (activeAuth != null && SubdivideTool.newRoadWidth < activeAuth.roadInset * 2f * 0.95f)
                {
                    EditorGUILayout.HelpBox(
                        $"권장: {activeAuth.roadInset * 2f + 0.5f:F1}m 이상 (Road Inset × 2)",
                        MessageType.Warning);
                }

                if (UIStyles.AdvancedFoldout("subdivide"))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        SubdivideTool.mode = (SubdivideTool.SplitMode)EditorGUILayout.EnumPopup(
                            GC("Mode", "LongestOpposite=마주보는 변, TwoLongest=두 긴 변"),
                            SubdivideTool.mode);
                        SubdivideTool.minEdgeLength = EditorGUILayout.Slider(
                            GC("Min Edge", "이 길이 미만 변은 분할 안 함"),
                            SubdivideTool.minEdgeLength, 1f, 30f);
                        SubdivideTool.autoRebuild = EditorGUILayout.Toggle(
                            GC("Auto Rebuild", "분할 후 즉시 빌드"),
                            SubdivideTool.autoRebuild);
                    }
                }
            }

            // ─── L-System 옵션 ───
            if (UnityEditor.EditorTools.ToolManager.activeToolType == typeof(LSystemGrowTool))
            {
                EditorGUILayout.Space(4);
                UIStyles.SubHeader("🌱 L-System Options");

                // 🅰 Preset (가장 강조)
                lsystemPresetSlot = PresetGUIHelper.DrawPresetRow<LSystemPresetV2>(
                    "LS Preset", lsystemPresetSlot,
                    preset => { LSystemGrowTool.settings = preset.GetSettings(); },
                    () => {
                        var p = ScriptableObject.CreateInstance<LSystemPresetV2>();
                        p.CopyFrom(LSystemGrowTool.settings);
                        return p;
                    }
                );

                ref var ls = ref LSystemGrowTool.settings;

                // 🅱 핵심 4개만 노출
                EditorGUILayout.Space(2);
                ls.iterations = EditorGUILayout.IntSlider(
                    GC("Size", "성장 단계 수. 1=작음, 12=거대"),
                    ls.iterations, 1, 12);
                ls.branchProbability = EditorGUILayout.Slider(
                    GC("Branching", "분기 확률. 0.15~0.25 권장"),
                    ls.branchProbability, 0f, 1f);
                ls.angleJitter = EditorGUILayout.Slider(
                    GC("Curviness", "방향 무작위. 0=격자, 30°=자연스러움"),
                    ls.angleJitter, 0f, 60f);
                ls.maxRadius = EditorGUILayout.Slider(
                    GC("Max Radius", "성장 반경(m)"),
                    ls.maxRadius, 20f, 300f);

                // ▶ Advanced
                if (UIStyles.AdvancedFoldout("lsystem"))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        ls.segmentLength = EditorGUILayout.Slider(
                            GC("Segment Length", "한 단계 도로 길이(m)"),
                            ls.segmentLength, 3f, 30f);
                        ls.segmentLengthJitter = EditorGUILayout.Slider(
                            GC("Length Jitter", "길이 무작위 ±%"),
                            ls.segmentLengthJitter, 0f, 0.8f);
                        ls.initialBranches = EditorGUILayout.IntSlider(
                            GC("Initial Branches", "시작 방향 수 (4=십자, 6=별)"),
                            ls.initialBranches, 1, 8);
                        ls.roadWidth = EditorGUILayout.Slider(
                            GC("Road Width", "생성 도로 폭(m)"),
                            ls.roadWidth, 1f, 10f);
                        ls.snapDistance = EditorGUILayout.Slider(
                            GC("Snap Distance", "기존 노드/엣지 흡수 거리(m)"),
                            ls.snapDistance, 0.5f, 10f);
                        ls.seed = EditorGUILayout.IntField(
                            GC("Seed", "랜덤 시드 (매 클릭마다 자동 +1)"),
                            ls.seed);
                        LSystemGrowTool.autoRebuild = EditorGUILayout.Toggle(
                            GC("Auto Rebuild", "성장 후 즉시 빌드"),
                            LSystemGrowTool.autoRebuild);
                        LSystemGenerator.absorbExistingEdges = EditorGUILayout.Toggle(
                            GC("Absorb Existing", "ON: 기존 도로 분할 흡수, OFF: 폭 보존(권장)"),
                            LSystemGenerator.absorbExistingEdges);
                    }
                }
            }

            // ─── Width Brush 옵션 ───
            if (UnityEditor.EditorTools.ToolManager.activeToolType == typeof(WidthBrushTool))
            {
                EditorGUILayout.Space(4);
                GUILayout.Label("Width Brush Options", EditorStyles.miniBoldLabel);
                WidthBrushTool.targetWidth = EditorGUILayout.Slider(
                    GC("Target Width",
                       "적용할 도로 폭(m). Alt+클릭으로 스포이드(기존 엣지 폭 복사) 가능."),
                    WidthBrushTool.targetWidth, 0.5f, 15f);
                WidthBrushTool.mode = (WidthBrushTool.BrushMode)EditorGUILayout.EnumPopup(
                    GC("Mode",
                       "Click=한 번에 한 엣지\nPaint=드래그로 반경 안 모든 엣지에 적용"),
                    WidthBrushTool.mode);
                using (new EditorGUI.DisabledScope(WidthBrushTool.mode != WidthBrushTool.BrushMode.Paint))
                {
                    WidthBrushTool.brushRadius = EditorGUILayout.Slider(
                        GC("Brush Radius",
                           "Paint 모드: 마우스 주변 이 거리(m) 안의 엣지에 적용"),
                        WidthBrushTool.brushRadius, 1f, 30f);
                }
                WidthBrushTool.autoRebuild = EditorGUILayout.Toggle(
                    GC("Auto Rebuild",
                       "변경 후 즉시 블록/빌딩/도로메쉬 재생성 (드래그 페인트 한 스트로크 끝날 때)"),
                    WidthBrushTool.autoRebuild);
            }

            // ─── Voronoi Options ───
            if (UnityEditor.EditorTools.ToolManager.activeToolType == typeof(VoronoiTool))
            {
                EditorGUILayout.Space(4);
                UIStyles.SubHeader($"◇ Voronoi (Seeds: {VoronoiTool.seeds.Count})");

                // 🅰 Mode
                VoronoiTool.mode = (VoronoiTool.CellTypeMode)EditorGUILayout.EnumPopup(
                    GC("Mode",
                       "OnePerSeed=시드마다 새 영역 자동 생성 (추천)\n" +
                       "CycleRegions=기존 영역에 순환 매핑\n" +
                       "RandomRegions=무작위 매핑\n" +
                       "AllSame=모두 첫 영역"),
                    VoronoiTool.mode);

                // 🅱 Random Place + Clear
                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    VoronoiTool.randomSeedCount = EditorGUILayout.IntSlider(
                        GC("Count", "[Random Place] 시드 수"),
                        VoronoiTool.randomSeedCount, 2, 30);
                    if (GUILayout.Button("🎲 Random", GUILayout.Width(75)))
                        VoronoiTool.RandomPlaceSeeds(activeAuth, VoronoiTool.randomSeedCount);
                    if (GUILayout.Button("Clear", GUILayout.Width(50)))
                    {
                        VoronoiTool.ClearSeeds();
                        SceneView.RepaintAll();
                    }
                }

                // 🅲 Generate (큰 버튼)
                EditorGUILayout.Space(4);
                using (new EditorGUI.DisabledScope(VoronoiTool.seeds.Count == 0))
                {
                    if (GUILayout.Button(GC("✨ Generate Regions",
                        "시드 → Voronoi 영역 생성"), UIStyles.BigButton))
                    {
                        VoronoiTool.GenerateRegions(activeAuth, activeRegion);
                        MarkDirty();
                    }
                }

                // 경고 (필요시)
                if (VoronoiTool.mode != VoronoiTool.CellTypeMode.OnePerSeed)
                {
                    if (activeRegion == null)
                        EditorGUILayout.HelpBox("Regions 필요. Header [Create Regions].",
                            MessageType.Warning);
                    else if (activeRegion.regions == null || activeRegion.regions.Count == 0)
                        EditorGUILayout.HelpBox("Region 정의 필요 (또는 Mode=OnePerSeed).",
                            MessageType.Warning);
                }

                // ▶ Advanced (품질 옵션)
                if (UIStyles.AdvancedFoldout("voronoi"))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        VoronoiTool.gridResolution = EditorGUILayout.Slider(
                            GC("Grid Resolution", "해상도(m). 작을수록 정확/느림"),
                            VoronoiTool.gridResolution, 0.3f, 5f);
                        VoronoiTool.smoothingPasses = EditorGUILayout.IntSlider(
                            GC("Smoothing", "경계 평활화. 0=거침"),
                            VoronoiTool.smoothingPasses, 0, 5);
                        VoronoiTool.simplifyTolerance = EditorGUILayout.Slider(
                            GC("Simplify", "Douglas-Peucker 단순화(m)"),
                            VoronoiTool.simplifyTolerance, 0f, 3f);
                        VoronoiTool.boundsPadding = EditorGUILayout.Slider(
                            GC("Padding", "외곽 여유(m)"),
                            VoronoiTool.boundsPadding, 0f, 30f);
                    }
                }
            }
        }

        // 도구 활성화 버튼 (다시 누르면 해제)
        void ToolButton<T>(Object autoSelect, string label, string tooltip) where T : EditorTool
        {
            bool active = UnityEditor.EditorTools.ToolManager.activeToolType == typeof(T);
            var bg = GUI.backgroundColor;
            if (active) GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);

            // 활성 상태면 라벨에 ✓ 표시 + 툴팁에 해제 안내
            string finalLabel = active ? $"✓ {label}" : label;
            string finalTooltip = active
                ? tooltip + "\n(다시 클릭하면 해제)"
                : tooltip;

            if (GUILayout.Button(GC(finalLabel, finalTooltip), GUILayout.Height(24)))
            {
                if (active)
                {
                    // 활성 상태에서 다시 누름 → 해제 (View 도구로 복귀)
                    EditorApplication.delayCall += () => UnityEditor.Tools.current = Tool.View;
                }
                else
                {
                    if (autoSelect is Component c)
                        Selection.activeGameObject = c.gameObject;
                    else if (autoSelect is GameObject g)
                        Selection.activeGameObject = g;

                    EditorApplication.delayCall += () => UnityEditor.EditorTools.ToolManager.SetActiveTool<T>();
                }
            }
            GUI.backgroundColor = bg;
        }

        void DrawGraphInfo()
        {
            EditorGUILayout.LabelField("Graph Info", EditorStyles.boldLabel);
            var g = activeAuth.graph;
            EditorGUILayout.LabelField($"Nodes: {g.nodes.Count}    Edges: {g.edges.Count}");
            var faces = activeAuth.GetFaces();
            int inner = 0, outer = 0;
            foreach (var f in faces) { if (f.isOuter) outer++; else inner++; }
            EditorGUILayout.LabelField($"Faces: inner={inner}, outer={outer}");
            EditorGUILayout.LabelField($"Regions: {(activeRegion != null ? activeRegion.regions.Count.ToString() : "(none)")}");

            if (GUILayout.Button(GC("Validate Graph", "그래프 무결성 검사. 결과는 Console에")))
            {
                var issues = GraphValidator.Validate(g);
                Debug.Log(GraphValidator.FormatReport(issues));
            }

            // 외톨이 노드(degree=0) 개수 표시 + 제거 버튼
            int orphanCount = CountOrphanNodes(g);
            using (new EditorGUI.DisabledScope(orphanCount == 0))
            {
                if (GUILayout.Button(GC(
                    orphanCount > 0 ? $"🗑 Remove Orphan Nodes ({orphanCount})" : "🗑 Remove Orphan Nodes (none)",
                    "어떤 엣지에도 연결되지 않은 외톨이 노드를 모두 삭제")))
                {
                    Undo.RegisterCompleteObjectUndo(activeAuth, "Remove Orphan Nodes");
                    int removed = RemoveOrphanNodes(g);
                    activeAuth.InvalidateFaceCache();
                    MarkDirty();
                    Debug.Log($"[TownGen V2] Removed {removed} orphan node(s).");
                }
            }

            // ─── 도로 폭 통계 + 일괄 변경 ───
            if (g.edges.Count > 0)
            {
                EditorGUILayout.Space(2);
                float minW = float.MaxValue, maxW = 0f, sumW = 0f;
                foreach (var ed in g.edges)
                {
                    minW = Mathf.Min(minW, ed.width);
                    maxW = Mathf.Max(maxW, ed.width);
                    sumW += ed.width;
                }
                float avgW = sumW / g.edges.Count;
                EditorGUILayout.LabelField(
                    $"Road Width — min: {minW:F1}  avg: {avgW:F1}  max: {maxW:F1}",
                    EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GC("Set All Widths:",
                        "모든 도로의 폭을 한 번에 변경. 변경 후 Build All 다시 누르세요."),
                        GUILayout.Width(110));
                    if (GUILayout.Button("1.5m")) SetAllRoadWidths(g, 1.5f);
                    if (GUILayout.Button("2m"))   SetAllRoadWidths(g, 2f);
                    if (GUILayout.Button("3m"))   SetAllRoadWidths(g, 3f);
                    if (GUILayout.Button("4m"))   SetAllRoadWidths(g, 4f);
                    if (GUILayout.Button("6m"))   SetAllRoadWidths(g, 6f);
                }

                // 자유 입력 + Scale 버튼
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GC("Custom:",
                        "직접 입력해서 일괄 적용"), GUILayout.Width(60));
                    customWidth = EditorGUILayout.FloatField(customWidth, GUILayout.Width(60));
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                        SetAllRoadWidths(g, Mathf.Max(0.1f, customWidth));
                    if (GUILayout.Button(GC("× 0.7", "현재 폭의 70%로 (전체적으로 얇게)"), GUILayout.Width(50)))
                        ScaleAllRoadWidths(g, 0.7f);
                    if (GUILayout.Button(GC("× 1.4", "현재 폭의 140%로 (전체적으로 굵게)"), GUILayout.Width(50)))
                        ScaleAllRoadWidths(g, 1.4f);
                }
            }

            // ─── 블록 통계 ───
            DrawBlockStats();
        }

        void DrawBlockStats()
        {
            var blocksRoot = activeAuth.transform.Find("_Blocks");
            if (blocksRoot == null || blocksRoot.childCount == 0) return;

            EditorGUILayout.Space(2);
            int blockCount = 0, buildingCount = 0;
            float totalArea = 0f;
            foreach (Transform child in blocksRoot)
            {
                var block = child.GetComponent<TownBlockV2>();
                if (block == null) continue;
                blockCount++;
                buildingCount += child.childCount;
                if (block.polygon != null && block.polygon.Count >= 3)
                    totalArea += Mathf.Abs(SignedArea2D(block.polygon));
            }

            EditorGUILayout.LabelField(
                $"Blocks: {blockCount}   Buildings: {buildingCount}   Total Area: {totalArea:F0} m²",
                EditorStyles.miniLabel);

            // Region별 분포
            if (activeRegion != null && activeRegion.regions != null && activeRegion.regions.Count > 0)
            {
                var regionCounts = new System.Collections.Generic.Dictionary<string, int>();
                int defaultCount = 0;
                foreach (Transform child in blocksRoot)
                {
                    var block = child.GetComponent<TownBlockV2>();
                    if (block == null || block.polygon == null) continue;
                    var center = ComputeCentroidLocal(block.polygon);
                    var r = activeRegion.ResolveRegionAt(center);
                    if (r == null || r == activeRegion.defaultRegion) defaultCount++;
                    else
                    {
                        if (!regionCounts.ContainsKey(r.name)) regionCounts[r.name] = 0;
                        regionCounts[r.name]++;
                    }
                }
                var sb = new System.Text.StringBuilder("By Region: ");
                foreach (var kv in regionCounts)
                    sb.Append($"{kv.Key}={kv.Value}  ");
                if (defaultCount > 0) sb.Append($"(default)={defaultCount}");
                EditorGUILayout.LabelField(sb.ToString(), EditorStyles.miniLabel);
            }
        }

        static float SignedArea2D(System.Collections.Generic.List<Vector3> poly)
        {
            float s = 0; int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % n];
                s += a.x * b.z - b.x * a.z;
            }
            return s * 0.5f;
        }

        static Vector3 ComputeCentroidLocal(System.Collections.Generic.List<Vector3> poly)
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in poly) sum += p;
            return sum / poly.Count;
        }

        float customWidth = 3f;

        void DrawSelectionInfo()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            var auth = go.GetComponentInParent<RoadGraphAuthoring>();
            if (auth == null || auth != activeAuth) return;
            EditorGUILayout.LabelField("Selection / Hover", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("(Use Scene view; details shown there)", EditorStyles.miniLabel);
        }

        void DrawTestGraphs()
        {
            EditorGUILayout.LabelField("Test Graphs", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("3x3", "3×3 격자 (face 4)"))) BuildGrid(activeAuth, 3);
                if (GUILayout.Button(GC("5x5", "5×5 격자 (face 16)"))) BuildGrid(activeAuth, 5);
                if (GUILayout.Button(GC("Square+Diag", "사각형+대각선 (face 2)"))) BuildSquareDiag(activeAuth);
                if (GUILayout.Button(GC("T-Shape", "T자 (face 0)"))) BuildT(activeAuth);
            }

            GUILayout.Label("Auto Generators", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Radial", "방사형 도로 (중심 + 환상)"))) {
                    Undo.RegisterCompleteObjectUndo(activeAuth, "Build Radial");
                    AutoGraphPresets.BuildRadial(activeAuth); MarkDirty();
                }
                if (GUILayout.Button(GC("Hex Loop", "정육각형 외곽 도로 (face 1)"))) {
                    Undo.RegisterCompleteObjectUndo(activeAuth, "Build Hex");
                    AutoGraphPresets.BuildPolygonLoop(activeAuth, 6, 25f); MarkDirty();
                }
                if (GUILayout.Button(GC("Octagon", "정팔각형 외곽 (face 1)"))) {
                    Undo.RegisterCompleteObjectUndo(activeAuth, "Build Octagon");
                    AutoGraphPresets.BuildPolygonLoop(activeAuth, 8, 25f); MarkDirty();
                }
                if (GUILayout.Button(GC("Jittered 5x5", "5x5 격자에 랜덤 흔들림"))) {
                    Undo.RegisterCompleteObjectUndo(activeAuth, "Build Jittered");
                    AutoGraphPresets.BuildJitteredGrid(activeAuth, 5, 10f, 2f, Random.Range(0, 99999));
                    MarkDirty();
                }
            }
        }

        void DrawClearSection()
        {
            EditorGUILayout.LabelField("Clear", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Graph", "그래프(노드+엣지)만 삭제")))
                { Undo.RegisterCompleteObjectUndo(activeAuth, "Clear Graph"); activeAuth.graph.Clear(); activeAuth.InvalidateFaceCache(); MarkDirty(); }
                if (GUILayout.Button(GC("Buildings", "빌딩만 삭제")))
                { Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Clear Buildings"); BlockBuilder.ClearAllBuildings(activeAuth); MarkDirty(); }
                if (GUILayout.Button(GC("Blocks", "블록 + 빌딩 삭제")))
                { Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Clear Blocks"); BlockBuilder.ClearAllBlocks(activeAuth); MarkDirty(); }
                if (GUILayout.Button(GC("Road Mesh", "도로/교차로 메쉬만 삭제")))
                {
                    if (activeRoadMesh != null)
                    {
                        Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Clear Road Mesh");
                        activeRoadMesh.Clear(); MarkDirty();
                    }
                }
                if (GUILayout.Button(GC("ALL", "그래프+블록+빌딩+도로메쉬+Region 폴리곤 모두")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Clear All");
                    BlockBuilder.ClearAllBuildings(activeAuth);
                    BlockBuilder.ClearAllBlocks(activeAuth);
                    if (activeRoadMesh != null) activeRoadMesh.Clear();
                    activeAuth.graph.Clear();
                    activeAuth.InvalidateFaceCache();
                    if (activeRegion != null)
                    {
                        Undo.RegisterCompleteObjectUndo(activeRegion, "Clear All");
                        foreach (var r in activeRegion.regions) r.polygon.Clear();
                        EditorUtility.SetDirty(activeRegion);
                    }
                    MarkDirty();
                }
            }
        }

        void DrawBuildSettings()
        {
            var a = activeAuth;

            // ─── 🅰 Town Preset (가장 큰 강조) ───
            UIStyles.SubHeader("🅰 Quick Setup (Preset)");
            townPresetSlot = PresetGUIHelper.DrawPresetRow<TownPresetV2>(
                "Town Preset", townPresetSlot,
                preset => {
                    Undo.RecordObject(a, "Apply Town Preset");
                    preset.ApplyTo(a);
                    EditorUtility.SetDirty(a);
                },
                () => {
                    var p = ScriptableObject.CreateInstance<TownPresetV2>();
                    p.CopyFrom(a);
                    return p;
                }
            );

            // ─── 🅱 Block Pattern (자주 바꿈) ───
            UIStyles.SubHeader("🅱 Block Pattern");
            a.patternSettings.pattern = (BlockPatternFiller.BlockPattern)EditorGUILayout.EnumPopup(
                GC("Pattern",
                   "Solid=칸칸이 박스\nPerimeter=ㅁ자\nUShape=ㄷ자\nLShape=ㄴ자\nCourtyard=두꺼운 띠\nSingleTower=가운데 1개"),
                a.patternSettings.pattern);
            if (a.patternSettings.pattern != BlockPatternFiller.BlockPattern.Solid &&
                a.patternSettings.pattern != BlockPatternFiller.BlockPattern.SingleTower)
            {
                a.patternSettings.perimeterDepth = EditorGUILayout.Slider(
                    GC("Depth", "띠 두께(m). 작게=얇은 띠, 크게=두꺼운 외곽"),
                    a.patternSettings.perimeterDepth, 2f, 20f);
            }

            // ─── 🅲 Buildings (자주 바꿈) ───
            UIStyles.SubHeader("🅲 Buildings");
            buildingPresetSlot = PresetGUIHelper.DrawPresetRow<BuildingPresetV2>(
                "Bldg Preset", buildingPresetSlot,
                preset => {
                    Undo.RecordObject(a, "Apply Building Preset");
                    a.buildSettings = preset.GetSettings();
                    EditorUtility.SetDirty(a);
                },
                () => {
                    var p = ScriptableObject.CreateInstance<BuildingPresetV2>();
                    p.CopyFrom(a.buildSettings);
                    return p;
                }
            );
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Height", GUILayout.Width(60));
                a.buildSettings.minHeight = EditorGUILayout.FloatField(a.buildSettings.minHeight, GUILayout.Width(50));
                GUILayout.Label("~", GUILayout.Width(15));
                a.buildSettings.maxHeight = EditorGUILayout.FloatField(a.buildSettings.maxHeight, GUILayout.Width(50));
                GUILayout.Label("m");
                GUILayout.FlexibleSpace();
            }
            a.buildSettings.density = EditorGUILayout.Slider(
                GC("Density", "빌딩 생성 확률"), a.buildSettings.density, 0f, 1f);

            // ─── ▶ Advanced ───
            EditorGUILayout.Space(2);
            if (UIStyles.AdvancedFoldout("buildSettings"))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    UIStyles.SubHeader("Inset");
                    a.useEdgeWidthForInset = EditorGUILayout.Toggle(
                        GC("Use Edge Width", "ON: 변마다 도로 폭/2 인셋 (자연)\nOFF: 단일 Road Inset"),
                        a.useEdgeWidthForInset);
                    if (a.useEdgeWidthForInset)
                    {
                        a.insetExtraMargin = EditorGUILayout.Slider(
                            GC("Extra Margin", "도로 폭/2에 더하는 여백(=보도 넓이)"),
                            a.insetExtraMargin, 0f, 5f);
                    }
                    else
                    {
                        a.roadInset = EditorGUILayout.FloatField(
                            GC("Road Inset", "단일 인셋 거리(m)"), a.roadInset);
                    }
                    a.blockThickness = EditorGUILayout.Slider(
                        GC("Block Thickness", "블록 메쉬 두께(m). 0=평면"),
                        a.blockThickness, 0f, 5f);

                    UIStyles.SubHeader("Building Layout");
                    a.buildSettings.mode = (BuildingFiller.FillMode)EditorGUILayout.EnumPopup(
                        GC("Fill Mode", "GridOBB=격자, RoadFacing=도로변"),
                        a.buildSettings.mode);
                    a.buildSettings.lotSize = EditorGUILayout.FloatField(
                        GC("Lot Size", "빌딩 한 칸 폭(m)"), a.buildSettings.lotSize);
                    if (a.buildSettings.mode == BuildingFiller.FillMode.RoadFacing)
                    {
                        a.buildSettings.lotDepth = EditorGUILayout.FloatField(
                            GC("Lot Depth", "빌딩 깊이(m)"), a.buildSettings.lotDepth);
                        a.buildSettings.fillInteriorRows = EditorGUILayout.Toggle(
                            GC("Fill Interior", "도로변 1줄 vs 안쪽 3줄"),
                            a.buildSettings.fillInteriorRows);
                    }
                    a.buildSettings.lotMargin = EditorGUILayout.Slider(
                        GC("Lot Margin", "칸 안 여백 비율"), a.buildSettings.lotMargin, 0f, 0.6f);
                    a.buildSettings.seed = EditorGUILayout.IntField(
                        GC("Seed", "랜덤 시드"), a.buildSettings.seed);
                }
            }
        }

        void DrawRoadMeshSection()
        {
            if (activeRoadMesh == null)
            {
                if (GUILayout.Button("Add RoadMeshAuthoring"))
                {
                    activeRoadMesh = Undo.AddComponent<RoadMeshAuthoring>(activeAuth.gameObject);
                }
                return;
            }

            var rm = activeRoadMesh;
            EditorGUI.BeginChangeCheck();

            // ─── 자주 바꿈: 머티리얼 ───
            UIStyles.SubHeader("Materials");
            rm.roadMaterial = (Material)EditorGUILayout.ObjectField(
                GC("Road", "도로 머티리얼 (없으면 회색 기본)"),
                rm.roadMaterial, typeof(Material), false);
            rm.intersectionMaterial = (Material)EditorGUILayout.ObjectField(
                GC("Intersection", "교차로 머티리얼 (없으면 도로 머티리얼 재사용)"),
                rm.intersectionMaterial, typeof(Material), false);

            // ─── ▶ Advanced ───
            if (UIStyles.AdvancedFoldout("roadMesh"))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    UIStyles.SubHeader("Heights (z-fighting)");
                    rm.roadYOffset = EditorGUILayout.Slider(
                        GC("Road Y", "도로 메쉬 높이"),
                        rm.roadYOffset, 0f, 0.5f);
                    rm.intersectionYOffset = EditorGUILayout.Slider(
                        GC("Intersection Y", "교차로는 도로보다 위"),
                        rm.intersectionYOffset, 0f, 0.5f);

                    UIStyles.SubHeader("Trim");
                    rm.trimRoadEnds = EditorGUILayout.Toggle(
                        GC("Trim Road Ends", "도로 끝을 교차로 영역에서 자름"),
                        rm.trimRoadEnds);
                    using (new EditorGUI.DisabledScope(!rm.trimRoadEnds))
                    {
                        rm.trimPadding = EditorGUILayout.Slider(
                            GC("Padding", "트림 반경 배수"),
                            rm.trimPadding, 0.5f, 2f);
                        rm.intersectionCoverage = EditorGUILayout.Slider(
                            GC("Coverage", "교차로 원반 도로 경계 겹침"),
                            rm.intersectionCoverage, 1f, 1.5f);
                    }
                    rm.intersectionSegments = EditorGUILayout.IntSlider(
                        GC("Segments", "교차로 원반 분할 수"),
                        rm.intersectionSegments, 4, 32);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(rm);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Build Road Mesh", "도로/교차로 메쉬 생성"),
                    GUILayout.Height(24)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Build Road Mesh");
                    rm.Build(); MarkDirty();
                }
                if (GUILayout.Button(GC("Clear", "도로/교차로 메쉬 삭제"),
                    GUILayout.Width(60), GUILayout.Height(24)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Clear Road Mesh");
                    rm.Clear(); MarkDirty();
                }
            }
        }

        void DrawBuildButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Build Blocks", "그래프 face → 블록 메쉬")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Build Blocks");
                    BlockBuilder.RebuildAllBlocks(activeAuth, activeAuth.roadInset,
                        regions: activeRegion, blockThickness: activeAuth.blockThickness,
                        useEdgeWidthForInset: activeAuth.useEdgeWidthForInset,
                        insetExtraMargin: activeAuth.insetExtraMargin);
                    MarkDirty();
                }
                if (GUILayout.Button(GC("Fill Buildings", "기존 블록에 빌딩 채움")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Fill Buildings");
                    BlockBuilder.FillAllBlocksWithBuildings(activeAuth, activeAuth.buildSettings, activeRegion);
                    MarkDirty();
                }
            }
            if (GUILayout.Button(GC("⚡ Build All (Blocks + Buildings + Road Mesh)",
                "한 번에 모두 생성: 블록 → 빌딩 → 도로/교차로 메쉬"),
                GUILayout.Height(34)))
            {
                Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Build All");
                BlockBuilder.RebuildAllBlocks(activeAuth, activeAuth.roadInset,
                    regions: activeRegion, blockThickness: activeAuth.blockThickness,
                    useEdgeWidthForInset: activeAuth.useEdgeWidthForInset,
                    insetExtraMargin: activeAuth.insetExtraMargin);
                BlockBuilder.FillAllBlocksWithBuildings(activeAuth, activeAuth.buildSettings, activeRegion);
                if (activeRoadMesh != null) activeRoadMesh.Build();
                MarkDirty();
            }
        }

        void DrawDocSection()
        {
            EditorGUILayout.LabelField("Document", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("💾 Save...", "JSON 저장 (다이얼로그)")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = activeAuth.gameObject; TownDocMenu.SaveSelected(); };
                if (GUILayout.Button(GC("📂 Load...", "JSON 로드 + 자동 Build")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = activeAuth.gameObject; TownDocMenu.LoadIntoSelected(); };
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Quick Save", "다이얼로그 없이 즉시 저장")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = activeAuth.gameObject; TownDocMenu.QuickSave(); };
                if (GUILayout.Button(GC("Quick Load", "즉시 로드 + 자동 Build")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = activeAuth.gameObject; TownDocMenu.QuickLoad(); };
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Top-Down Capture", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(GC("Size",
                    "출력 PNG 한 변의 픽셀 수"), GUILayout.Width(40));
                TopDownCapture.captureSize = EditorGUILayout.IntPopup(
                    TopDownCapture.captureSize,
                    new[] { "1024", "2048", "4096", "8192" },
                    new[] { 1024, 2048, 4096, 8192 },
                    GUILayout.Width(80));
                TopDownCapture.transparentBackground = EditorGUILayout.ToggleLeft(
                    GC("Transparent BG", "배경을 투명 PNG로 (스카이박스 안 찍힘)"),
                    TopDownCapture.transparentBackground, GUILayout.Width(130));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("📷 Capture Top-Down PNG",
                    "도시를 위에서 본 PNG로 저장 (Scene 카메라 영향 없음)"),
                    GUILayout.Height(24)))
                {
                    string path = EditorUtility.SaveFilePanel(
                        "Save Top-Down PNG",
                        Application.dataPath,
                        $"town_{System.DateTime.Now:yyyyMMdd_HHmmss}",
                        "png");
                    if (!string.IsNullOrEmpty(path))
                        TopDownCapture.Capture(activeAuth, path);
                }
            }
        }

        // ─────────── helpers ───────────
        void CreateRoadGraph()
        {
            var go = new GameObject("RoadGraph");
            go.AddComponent<RoadGraphAuthoring>();
            Undo.RegisterCreatedObjectUndo(go, "Create Road Graph");
            activeAuth = go.GetComponent<RoadGraphAuthoring>();
            Selection.activeGameObject = go;
            EnsureRoadMesh();   // ★ 자동으로 RoadMesh도
        }

        void CreateRegions()
        {
            var go = new GameObject("Regions");
            go.AddComponent<RegionAuthoring>();
            Undo.RegisterCreatedObjectUndo(go, "Create Regions");
            activeRegion = go.GetComponent<RegionAuthoring>();
            Selection.activeGameObject = go;
        }

        void MarkDirty()
        {
            if (activeAuth != null) EditorUtility.SetDirty(activeAuth);
            if (activeRegion != null) EditorUtility.SetDirty(activeRegion);
            if (activeRoadMesh != null) EditorUtility.SetDirty(activeRoadMesh);
            SceneView.RepaintAll();
            Repaint();
        }

        void DrawQuickActionsSection()
        {
            EditorGUILayout.HelpBox(
                "한 클릭으로 자주 쓰는 시퀀스 실행.\n" +
                "처음 사용 시 [TownGen V2 → Create Built-in Presets] 메뉴를 먼저 실행하세요.",
                MessageType.Info);

            QuickActions.alwaysRegenerate = EditorGUILayout.Toggle(
                GC("Always Regenerate",
                   "ON(권장): 매번 그래프 새로 생성 (다른 도시)\n" +
                   "OFF: 그래프 있으면 빌드만 다시 (같은 도시)"),
                QuickActions.alwaysRegenerate);

            // ─── ★ OSM-First (메인 흐름) ───
            UIStyles.SubHeader("🌐 OSM City (Main)");
            if (GUILayout.Button(GC("🌐 Quick OSM City",
                "OSM 파일 선택 → 임포트 + 정리 + Voronoi + Build All\n" +
                "(외톨이 제거 + 도로 폭 자동 + 영역 분할 + 빌딩 채우기)"),
                UIStyles.BigButton))
            {
                QuickActions.QuickOSMCity(activeAuth, ref activeRegion);
                MarkDirty();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("📂 Import Only",
                    "OSM 파일만 임포트 (정리/빌드 없음)"),
                    GUILayout.Height(24)))
                {
                    string path = EditorUtility.OpenFilePanel("Select .osm", Application.dataPath, "osm");
                    if (!string.IsNullOrEmpty(path))
                    {
                        Undo.RegisterCompleteObjectUndo(activeAuth, "OSM Import");
                        var result = TownGen.V2.OSM.OSMImporter.Import(path, activeAuth, osmOptions);
                        Debug.Log("[OSM] " + result.summary);
                        MarkDirty();
                    }
                }
                if (GUILayout.Button(GC("🔧 Cleanup + Build",
                    "이미 임포트된 OSM 정리 + 빌드"),
                    GUILayout.Height(24)))
                {
                    QuickActions.QuickCleanupOSM(activeAuth, activeRegion);
                    MarkDirty();
                }
            }

            // ─── 격자 (간단한 테스트) ───
            UIStyles.SubHeader("📐 Grid (Test)");
            if (GUILayout.Button(GC("📐 Quick Grid City",
                "5×5 격자 + 중층 주거 (테스트/데모용)"),
                GUILayout.Height(24)))
            {
                QuickActions.QuickGridCity(activeAuth, activeRegion);
                MarkDirty();
            }

            // ─── L-System (Experimental, 작게) ───
            UIStyles.SubHeader("🧪 L-System (Experimental)");
            EditorGUILayout.HelpBox(
                "L-System은 실험적입니다. 닫힌 face 형성이 어려워 빌딩이 적게 나옵니다.\n" +
                "권장: OSM 임포트 또는 Grid 사용.",
                MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("🏘 Try Village",
                    "L-System으로 작은 마을 시도 (결과 들쭉날쭉)"),
                    GUILayout.Height(20)))
                {
                    QuickActions.QuickVillage(activeAuth, activeRegion);
                    MarkDirty();
                }
                if (GUILayout.Button(GC("🌆 Try Big City",
                    "L-System으로 거대 도시 시도"),
                    GUILayout.Height(20)))
                {
                    QuickActions.QuickBigCityWithRegions(activeAuth, ref activeRegion);
                    MarkDirty();
                }
            }

            // ─── 라이팅 토글 ───
            UIStyles.SubHeader("🌓 Lighting");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("☀ Day", "주간 라이팅 + 빌딩 emission OFF"),
                    GUILayout.Height(24)))
                {
                    QuickActions.QuickDayMode(activeAuth);
                }
                if (GUILayout.Button(GC("🌙 Night",
                    "야경 라이팅 + 빌딩 emission ON (한 번에)"),
                    GUILayout.Height(24)))
                {
                    QuickActions.QuickNightCity(activeAuth, activeRegion);
                }
            }
        }

        Color nightEmissionColor = new Color(1f, 0.85f, 0.5f);

        void DrawOSMSection()
        {
            // 핵심 버튼만 강조
            if (GUILayout.Button(GC("📂 Import .osm File...",
                "OSM 파일 선택 → 그래프 변환"),
                UIStyles.BigButton))
            {
                string path = EditorUtility.OpenFilePanel("Select OSM file", Application.dataPath, "osm");
                if (!string.IsNullOrEmpty(path))
                {
                    Undo.RegisterCompleteObjectUndo(activeAuth, "OSM Import");
                    var result = TownGen.V2.OSM.OSMImporter.Import(path, activeAuth, osmOptions);
                    Debug.Log("[OSM] " + result.summary);
                    EditorUtility.DisplayDialog("OSM Import", result.summary, "OK");
                    MarkDirty();
                }
            }

            EditorGUILayout.HelpBox(
                "1) https://www.openstreetmap.org → 영역 선택 → Export\n" +
                "2) 200~500m 권장 (큰 영역은 매우 느림)\n" +
                "3) Import 후 [Build All] 클릭",
                MessageType.Info);

            // ─── ▶ Advanced ───
            if (UIStyles.AdvancedFoldout("osm", "▶ Import Options"))
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    osmOptions.clearGraphFirst = EditorGUILayout.Toggle(
                        GC("Clear First", "임포트 전 기존 그래프 삭제"),
                        osmOptions.clearGraphFirst);
                    osmOptions.useOSMRoadWidths = EditorGUILayout.Toggle(
                        GC("Use OSM Widths",
                           "ON: highway별 자동 폭\nOFF: 모두 Default"),
                        osmOptions.useOSMRoadWidths);
                    using (new EditorGUI.DisabledScope(osmOptions.useOSMRoadWidths))
                    {
                        osmOptions.defaultWidth = EditorGUILayout.Slider(
                            GC("Default Width", "OFF일 때 폭"),
                            osmOptions.defaultWidth, 1f, 10f);
                    }
                    osmOptions.scaleFactor = EditorGUILayout.Slider(
                        GC("Scale", "1=실제, 0.1=1/10 미니어처"),
                        osmOptions.scaleFactor, 0.05f, 2f);
                    osmOptions.excludeFootways = EditorGUILayout.Toggle(
                        GC("Skip Footways", "footway/path/cycleway 제외"),
                        osmOptions.excludeFootways);
                }
            }
        }

        void DrawLightingSection()
        {
            EditorGUILayout.LabelField("Scene Lighting", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("☀ Day Preset", "주간 라이팅 (밝은 흰빛)"),
                    GUILayout.Height(28)))
                {
                    NightPreset.Apply(NightPreset.Mode.Day);
                    NightPreset.ApplyBuildingEmission(activeAuth, false, Color.black);
                    SceneView.RepaintAll();
                }
                if (GUILayout.Button(GC("🌙 Night Preset",
                    "야경 라이팅 (어두운 파랑) + 빌딩 emission ON"),
                    GUILayout.Height(28)))
                {
                    NightPreset.Apply(NightPreset.Mode.Night);
                    NightPreset.ApplyBuildingEmission(activeAuth, true, nightEmissionColor);
                    SceneView.RepaintAll();
                }
            }
            nightEmissionColor = EditorGUILayout.ColorField(
                GC("Night Emission Color",
                   "야경 시 빌딩 emission 색 (창문 빛). 따뜻한 노랑/주황 추천"),
                nightEmissionColor);
            EditorGUILayout.HelpBox(
                "Day/Night 토글은 Scene의 Directional Light + Ambient를 변경합니다.\n" +
                "Night는 추가로 모든 빌딩 머티리얼에 emission을 켭니다 (창문 효과).",
                MessageType.Info);
        }

        void SetAllRoadWidths(RoadGraph g, float w)
        {
            Undo.RegisterCompleteObjectUndo(activeAuth, "Set All Road Widths");
            foreach (var e in g.edges) e.width = w;
            activeAuth.InvalidateFaceCache();
            MarkDirty();
            Debug.Log($"[TownGen V2] Set all {g.edges.Count} edges to width = {w:F1}m.");
        }

        void ScaleAllRoadWidths(RoadGraph g, float factor)
        {
            Undo.RegisterCompleteObjectUndo(activeAuth, "Scale All Road Widths");
            foreach (var e in g.edges) e.width = Mathf.Max(0.1f, e.width * factor);
            activeAuth.InvalidateFaceCache();
            MarkDirty();
            Debug.Log($"[TownGen V2] Scaled all road widths by {factor:F2}.");
        }

        static float ComputeMaxRoadHalfWidth(RoadGraphAuthoring a)
        {
            if (a == null || a.graph == null) return 0f;
            float maxHalf = 0f;
            foreach (var e in a.graph.edges)
                maxHalf = Mathf.Max(maxHalf, e.width * 0.5f);
            return maxHalf;
        }

        static int CountOrphanNodes(RoadGraph g)
        {
            if (g == null || g.nodes == null) return 0;
            int count = 0;
            foreach (var n in g.nodes)
                if (n.edgeIds == null || n.edgeIds.Count == 0) count++;
            return count;
        }

        static int RemoveOrphanNodes(RoadGraph g)
        {
            if (g == null || g.nodes == null) return 0;
            // 뒤에서부터 제거 (인덱스 안전)
            int removed = 0;
            for (int i = g.nodes.Count - 1; i >= 0; i--)
            {
                var n = g.nodes[i];
                if (n.edgeIds == null || n.edgeIds.Count == 0)
                {
                    g.nodes.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0) g.InvalidateCache();
            return removed;
        }

        // 테스트 그래프
        static void BuildGrid(RoadGraphAuthoring a, int n)
        {
            a.graph.Clear();
            float step = 10f;
            var nodes = new RoadNode[n, n];
            for (int x = 0; x < n; x++)
                for (int z = 0; z < n; z++)
                    nodes[x, z] = a.graph.AddNode(new Vector3(x * step, 0, z * step));
            for (int x = 0; x < n; x++)
                for (int z = 0; z < n; z++)
                {
                    if (x < n - 1) a.graph.AddEdge(nodes[x, z].id, nodes[x + 1, z].id);
                    if (z < n - 1) a.graph.AddEdge(nodes[x, z].id, nodes[x, z + 1].id);
                }
            a.InvalidateFaceCache();
            EditorUtility.SetDirty(a);
            SceneView.RepaintAll();
        }

        static void BuildSquareDiag(RoadGraphAuthoring a)
        {
            a.graph.Clear();
            float s = 10f;
            var n1 = a.graph.AddNode(new Vector3(0, 0, s));
            var n2 = a.graph.AddNode(new Vector3(s, 0, s));
            var n3 = a.graph.AddNode(new Vector3(s, 0, 0));
            var n4 = a.graph.AddNode(new Vector3(0, 0, 0));
            a.graph.AddEdge(n1.id, n2.id);
            a.graph.AddEdge(n2.id, n3.id);
            a.graph.AddEdge(n3.id, n4.id);
            a.graph.AddEdge(n4.id, n1.id);
            a.graph.AddEdge(n1.id, n3.id);
            a.InvalidateFaceCache();
            EditorUtility.SetDirty(a);
            SceneView.RepaintAll();
        }

        static void BuildT(RoadGraphAuthoring a)
        {
            a.graph.Clear();
            float s = 10f;
            var n1 = a.graph.AddNode(new Vector3(0, 0, 0));
            var n2 = a.graph.AddNode(new Vector3(s, 0, 0));
            var n3 = a.graph.AddNode(new Vector3(2 * s, 0, 0));
            var n4 = a.graph.AddNode(new Vector3(s, 0, -s));
            a.graph.AddEdge(n1.id, n2.id);
            a.graph.AddEdge(n2.id, n3.id);
            a.graph.AddEdge(n2.id, n4.id);
            a.InvalidateFaceCache();
            EditorUtility.SetDirty(a);
            SceneView.RepaintAll();
        }
    }
}