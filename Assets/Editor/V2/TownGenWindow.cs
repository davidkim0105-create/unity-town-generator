using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

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
        RoadMeshAuthoring activeRoadMesh;          // ★ 추가
        bool autoFollowSelection = true;

        Vector2 scroll;

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
            DrawHeader();
            EditorGUILayout.Space();

            if (activeAuth == null)
            {
                EditorGUILayout.HelpBox("No RoadGraph in scene. Click 'Create RoadGraph' below.", MessageType.Info);
                if (GUILayout.Button("Create RoadGraph")) CreateRoadGraph();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawTools();
            EditorGUILayout.Space();
            DrawGraphInfo();
            DrawSelectionInfo();
            EditorGUILayout.Space();
            DrawTestGraphs();
            EditorGUILayout.Space();
            DrawClearSection();
            EditorGUILayout.Space();
            DrawBuildSettings();
            EditorGUILayout.Space();
            DrawRoadMeshSection();         // ★ 추가
            EditorGUILayout.Space();
            DrawBuildButtons();
            EditorGUILayout.Space();
            DrawDocSection();
            EditorGUILayout.EndScrollView();
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
            EditorGUILayout.LabelField("Tools (shortcuts: Shift+Q/W/E/R/T)", EditorStyles.boldLabel);

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
        }

        // 도구 활성화 버튼
        void ToolButton<T>(Object autoSelect, string label, string tooltip) where T : EditorTool
        {
            bool active = UnityEditor.EditorTools.ToolManager.activeToolType == typeof(T);
            var bg = GUI.backgroundColor;
            if (active) GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button(GC(label, tooltip), GUILayout.Height(24)))
            {
                if (autoSelect is Component c)
                    Selection.activeGameObject = c.gameObject;
                else if (autoSelect is GameObject g)
                    Selection.activeGameObject = g;

                EditorApplication.delayCall += () => UnityEditor.EditorTools.ToolManager.SetActiveTool<T>();
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
        }

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

        bool buildSettingsFoldout = true;
        void DrawBuildSettings()
        {
            buildSettingsFoldout = EditorGUILayout.Foldout(buildSettingsFoldout, "Build Settings", true);
            if (!buildSettingsFoldout) return;
            var a = activeAuth;

            // 도로 폭 기준 권장 인셋
            float maxHalfW = ComputeMaxRoadHalfWidth(a);
            float recommended = maxHalfW + 0.2f;

            using (new EditorGUILayout.HorizontalScope())
            {
                a.roadInset = EditorGUILayout.FloatField(GC("Road Inset",
                    "face → 블록 줄이는 거리(m). 도로 폭의 절반 이상을 권장 (빌딩이 도로 위로 안 올라옴)"),
                    a.roadInset);

                if (recommended > 0f && a.roadInset < recommended * 0.95f)
                {
                    if (GUILayout.Button(GC($"⤴ {recommended:F1}",
                        $"권장값({recommended:F2}m)으로 자동 설정. 도로 폭 {maxHalfW * 2f:F1}m 기준."),
                        GUILayout.Width(60)))
                    {
                        Undo.RecordObject(a, "Set Recommended Inset");
                        a.roadInset = recommended;
                        EditorUtility.SetDirty(a);
                    }
                }
            }
            if (recommended > 0f && a.roadInset < recommended * 0.95f)
            {
                EditorGUILayout.HelpBox(
                    $"Road Inset({a.roadInset:F2}m)이 가장 굵은 도로 폭의 절반({maxHalfW:F2}m)보다 작습니다.\n" +
                    $"빌딩이 도로 위로 침범할 수 있습니다. 권장: {recommended:F2}m",
                    MessageType.Warning);
            }

            a.blockThickness = EditorGUILayout.Slider(GC("Block Thickness",
                "블록 메쉬 두께(m). 0=평면, 0.1=보도블록, 1+=단차"), a.blockThickness, 0f, 5f);

            EditorGUILayout.LabelField("Buildings (fallback)", EditorStyles.miniBoldLabel);
            a.buildSettings.mode = (BuildingFiller.FillMode)EditorGUILayout.EnumPopup(
                GC("Fill Mode", "GridOBB=격자 채움, RoadFacing=도로변 정렬"), a.buildSettings.mode);
            a.buildSettings.lotSize = EditorGUILayout.FloatField(
                GC("Lot Size", "빌딩 한 칸 폭(m)"), a.buildSettings.lotSize);
            if (a.buildSettings.mode == BuildingFiller.FillMode.RoadFacing)
            {
                a.buildSettings.lotDepth = EditorGUILayout.FloatField(
                    GC("Lot Depth", "빌딩 깊이(m)"), a.buildSettings.lotDepth);
                a.buildSettings.fillInteriorRows = EditorGUILayout.Toggle(
                    GC("Fill Interior Rows", "도로변 1줄 vs 안쪽 3줄"), a.buildSettings.fillInteriorRows);
            }
            a.buildSettings.lotMargin = EditorGUILayout.Slider(
                GC("Lot Margin", "칸 안 빌딩 차지 비율"), a.buildSettings.lotMargin, 0f, 0.6f);
            a.buildSettings.minHeight = EditorGUILayout.FloatField(GC("Min Height", "최소 높이(m)"), a.buildSettings.minHeight);
            a.buildSettings.maxHeight = EditorGUILayout.FloatField(GC("Max Height", "최대 높이(m)"), a.buildSettings.maxHeight);
            a.buildSettings.density = EditorGUILayout.Slider(GC("Density", "생성 확률"), a.buildSettings.density, 0f, 1f);
            a.buildSettings.seed = EditorGUILayout.IntField(GC("Seed", "랜덤 시드"), a.buildSettings.seed);
        }

        // ─── Road Mesh 섹션 ───
        bool roadMeshFoldout = true;
        void DrawRoadMeshSection()
        {
            roadMeshFoldout = EditorGUILayout.Foldout(roadMeshFoldout, "Road Mesh", true);
            if (!roadMeshFoldout) return;

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

            rm.roadMaterial = (Material)EditorGUILayout.ObjectField(
                GC("Road Material", "도로 머티리얼 (없으면 회색 기본)"),
                rm.roadMaterial, typeof(Material), false);
            rm.intersectionMaterial = (Material)EditorGUILayout.ObjectField(
                GC("Intersection Mat", "교차로 머티리얼 (없으면 도로 머티리얼 재사용)"),
                rm.intersectionMaterial, typeof(Material), false);

            rm.roadYOffset = EditorGUILayout.Slider(
                GC("Road Y Offset", "도로 메쉬 높이 (z-fighting 방지)"),
                rm.roadYOffset, 0f, 0.5f);
            rm.intersectionYOffset = EditorGUILayout.Slider(
                GC("Intersection Y Offset", "교차로는 도로보다 위에 있어야 잘 덮임"),
                rm.intersectionYOffset, 0f, 0.5f);

            rm.trimRoadEnds = EditorGUILayout.Toggle(
                GC("Trim Road Ends", "교차로 영역만큼 도로 끝을 잘라 깔끔하게 마감"),
                rm.trimRoadEnds);
            using (new EditorGUI.DisabledScope(!rm.trimRoadEnds))
            {
                rm.trimPadding = EditorGUILayout.Slider(
                    GC("Trim Padding", "트림 반경 배수"),
                    rm.trimPadding, 0.5f, 2f);
                rm.intersectionCoverage = EditorGUILayout.Slider(
                    GC("Intersection Coverage", "교차로가 도로 경계와 겹치는 정도 (빈틈 방지)"),
                    rm.intersectionCoverage, 1f, 1.5f);
            }
            rm.intersectionSegments = EditorGUILayout.IntSlider(
                GC("Intersection Segments", "교차로 원반 분할 수"),
                rm.intersectionSegments, 4, 32);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(rm);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Build Road Mesh", "도로/교차로 메쉬 생성"), GUILayout.Height(24)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(activeAuth.gameObject, "Build Road Mesh");
                    rm.Build(); MarkDirty();
                }
                if (GUILayout.Button(GC("Clear", "도로/교차로 메쉬 삭제"), GUILayout.Width(60), GUILayout.Height(24)))
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
                        regions: activeRegion, blockThickness: activeAuth.blockThickness);
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
                    regions: activeRegion, blockThickness: activeAuth.blockThickness);
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

        static float ComputeMaxRoadHalfWidth(RoadGraphAuthoring a)
        {
            if (a == null || a.graph == null) return 0f;
            float maxHalf = 0f;
            foreach (var e in a.graph.edges)
                maxHalf = Mathf.Max(maxHalf, e.width * 0.5f);
            return maxHalf;
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