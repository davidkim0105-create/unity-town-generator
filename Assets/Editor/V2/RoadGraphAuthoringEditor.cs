using UnityEditor;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [CustomEditor(typeof(RoadGraphAuthoring))]
    public class RoadGraphAuthoringEditor : Editor
    {
        // ─────────── GUIContent 헬퍼 ───────────
        static GUIContent GC(string label, string tooltip) => new GUIContent(label, tooltip);

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var a = (RoadGraphAuthoring)target;

            if (a.buildSettings.lotSize <= 0f || a.buildSettings.lotDepth <= 0f)
            {
                a.buildSettings = BuildingFiller.DefaultSettings;
                EditorUtility.SetDirty(a);
            }
            if (a.roadInset <= 0f) a.roadInset = 1.5f;

            var regions = a.GetComponentInChildren<RegionAuthoring>();
            if (regions == null) regions = Object.FindAnyObjectByType<RegionAuthoring>();

            // ─────────── Graph Info ───────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Graph Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Nodes: {a.graph.nodes.Count}");
            EditorGUILayout.LabelField($"Edges: {a.graph.edges.Count}");

            var faces = a.GetFaces();
            int inner = 0, outer = 0;
            foreach (var f in faces) { if (f.isOuter) outer++; else inner++; }
            EditorGUILayout.LabelField($"Faces: inner={inner}, outer={outer}");
            EditorGUILayout.LabelField($"Regions: {(regions != null ? regions.regions.Count.ToString() : "(none)")}");

            // ─────────── Test Graphs ───────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Graphs", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("3x3 Grid", "3×3 격자 도로 생성 (face 4개). 빠른 테스트용"))) BuildGrid(a, 3);
                if (GUILayout.Button(GC("5x5 Grid", "5×5 격자 도로 생성 (face 16개). 표준 테스트"))) BuildGrid(a, 5);
                if (GUILayout.Button(GC("Square+Diag", "사각형 + 대각선 (face 2개). 인셋/오목 테스트"))) BuildSquareDiag(a);
                if (GUILayout.Button(GC("T-Shape", "T자 막다른 도로 (face 0). 가지치기 테스트"))) BuildT(a);
            }

            // ─────────── Clear ───────────
            EditorGUILayout.LabelField("Clear", EditorStyles.miniBoldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Graph", "도로 그래프(노드+엣지)만 삭제. 블록/빌딩은 남음")))
                {
                    Undo.RegisterCompleteObjectUndo(a, "Clear Graph");
                    a.graph.Clear(); a.InvalidateFaceCache(); MarkDirty(a);
                }
                if (GUILayout.Button(GC("Buildings", "빌딩(박스)만 삭제. 블록 메쉬와 그래프는 남음")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(a.gameObject, "Clear Buildings");
                    BlockBuilder.ClearAllBuildings(a);
                    EditorUtility.SetDirty(a);
                }
                if (GUILayout.Button(GC("Blocks", "블록 메쉬 + 그 안의 빌딩 삭제. 그래프는 남음")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(a.gameObject, "Clear Blocks");
                    BlockBuilder.ClearAllBlocks(a);
                    EditorUtility.SetDirty(a);
                }
                if (GUILayout.Button(GC("ALL", "그래프+블록+빌딩+Region 폴리곤 모두 (Region 정의는 보존)")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(a.gameObject, "Clear All");
                    BlockBuilder.ClearAllBuildings(a);
                    BlockBuilder.ClearAllBlocks(a);
                    a.graph.Clear();
                    a.InvalidateFaceCache();

                    if (regions != null)
                    {
                        Undo.RegisterCompleteObjectUndo(regions, "Clear All");
                        foreach (var r in regions.regions) r.polygon.Clear();
                        EditorUtility.SetDirty(regions);
                    }

                    MarkDirty(a);
                }
            }

            // ─────────── Build Settings ───────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
            a.roadInset = EditorGUILayout.FloatField(
                GC("Road Inset", "face → 블록 줄이는 거리 (m, 도로 폭의 절반)\n• 0: 도로 한가운데까지 블록\n• 1.5 (기본): 일반 도로\n• 3+: 대로\n• 너무 크면 작은 face 블록 사라짐"),
                a.roadInset);
            a.blockThickness = EditorGUILayout.Slider(
                GC("Block Thickness", "블록 메쉬 두께 (m)\n• 0: 평면\n• 0.1: 보도블록 살짝 솟음\n• 1+: 단차 큰 도시 베이스"),
                a.blockThickness, 0f, 5f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Buildings (fallback for blocks outside any Region)", EditorStyles.miniBoldLabel);

            a.buildSettings.mode = (BuildingFiller.FillMode)EditorGUILayout.EnumPopup(
                GC("Fill Mode", "빌딩 배치 방식\n• GridOBB: 블록 전체 격자 채움\n• RoadFacing: 도로변 정렬 (도시형)"),
                a.buildSettings.mode);

            a.buildSettings.lotSize = EditorGUILayout.FloatField(
                GC("Lot Size", "빌딩 한 칸 폭 (m, 도로변 따라)\n• 작게(2~3): 빌딩 많음\n• 크게(8~12): 빌딩 적고 큼"),
                a.buildSettings.lotSize);

            if (a.buildSettings.mode == BuildingFiller.FillMode.RoadFacing)
            {
                a.buildSettings.lotDepth = EditorGUILayout.FloatField(
                    GC("Lot Depth", "빌딩 깊이 (도로→안쪽 m)\n• 작게(2~4): 얇은 빌딩\n• 크게(6~10): 두꺼운 빌딩"),
                    a.buildSettings.lotDepth);
                a.buildSettings.fillInteriorRows = EditorGUILayout.Toggle(
                    GC("Fill Interior Rows", "도로변 1줄만(off) vs 안쪽 3줄까지(on)\n• OFF: 가운데 안마당 빔 (자연스러움)\n• ON: 빽빽한 밀집 도시"),
                    a.buildSettings.fillInteriorRows);
            }

            a.buildSettings.lotMargin = EditorGUILayout.Slider(
                GC("Lot Margin", "칸 안에서 빌딩 차지 비율 (0~0.6)\n• 0: 다닥다닥 붙음\n• 0.15: 살짝 간격\n• 0.5: 빌딩 사이 간격 큼"),
                a.buildSettings.lotMargin, 0f, 0.6f);

            a.buildSettings.minHeight = EditorGUILayout.FloatField(
                GC("Min Height", "빌딩 최소 높이 (m). 1층 ≈ 3m"),
                a.buildSettings.minHeight);
            a.buildSettings.maxHeight = EditorGUILayout.FloatField(
                GC("Max Height", "빌딩 최대 높이 (m). 빌딩 키는 min~max 사이 랜덤"),
                a.buildSettings.maxHeight);
            a.buildSettings.density = EditorGUILayout.Slider(
                GC("Density", "각 칸에 빌딩이 생길 확률 (0~1)\n• 1: 모두 채움\n• 0.5: 듬성듬성"),
                a.buildSettings.density, 0f, 1f);
            a.buildSettings.seed = EditorGUILayout.IntField(
                GC("Seed", "랜덤 시드. 같은 값 = 같은 결과 (재현 가능)"),
                a.buildSettings.seed);

            // ─────────── Build ───────────
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Build Blocks", "그래프 face → 블록 메쉬 생성. 기존 블록은 모두 삭제 후 재생성")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(a.gameObject, "Build Blocks");
                    BlockBuilder.RebuildAllBlocks(a, a.roadInset, regions: regions, blockThickness: a.blockThickness);
                    EditorUtility.SetDirty(a);
                }
                if (GUILayout.Button(GC("Fill Buildings", "기존 블록들에 빌딩 박스 채움. 블록은 먼저 만들어져 있어야 함")))
                {
                    Undo.RegisterFullObjectHierarchyUndo(a.gameObject, "Fill Buildings");
                    BlockBuilder.FillAllBlocksWithBuildings(a, a.buildSettings, regions);
                    EditorUtility.SetDirty(a);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(
                GC("⚡ Build Blocks + Fill Buildings", "한 번에 블록 + 빌딩 모두 생성. 가장 자주 쓰는 버튼"),
                GUILayout.Height(30)))
            {
                Undo.RegisterFullObjectHierarchyUndo(a.gameObject, "Build All");
                BlockBuilder.RebuildAllBlocks(a, a.roadInset, regions: regions, blockThickness: a.blockThickness);
                BlockBuilder.FillAllBlocksWithBuildings(a, a.buildSettings, regions);
                EditorUtility.SetDirty(a);
            }

            // ─────────── Document I/O ───────────
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Document (Save = Graph + Regions + Settings, Load = 자동 Build)", EditorStyles.boldLabel);
            var aRef = a;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("💾 Save...", "JSON 파일로 저장. 다이얼로그에서 위치 선택")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = aRef.gameObject; TownDocMenu.SaveSelected(); };
                if (GUILayout.Button(GC("📂 Load...", "JSON 파일에서 불러오기 + 자동 Build. 다이얼로그에서 파일 선택")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = aRef.gameObject; TownDocMenu.LoadIntoSelected(); };
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(GC("Quick Save", "다이얼로그 없이 Assets/_TownDoc_quick.json에 즉시 저장 (실험용)")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = aRef.gameObject; TownDocMenu.QuickSave(); };
                if (GUILayout.Button(GC("Quick Load", "Assets/_TownDoc_quick.json에서 즉시 불러오기 + 자동 Build")))
                    EditorApplication.delayCall += () => { Selection.activeGameObject = aRef.gameObject; TownDocMenu.QuickLoad(); };
            }
        }

        // ─────────── Test graph builders (생략 — 기존과 동일) ───────────
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
            MarkDirty(a);
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
            MarkDirty(a);
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
            MarkDirty(a);
        }

        static void MarkDirty(RoadGraphAuthoring a)
        {
            EditorUtility.SetDirty(a);
            SceneView.RepaintAll();
        }

        [MenuItem("GameObject/TownGen V2/Road Graph", false, 10)]
        static void CreateRoadGraphObject(MenuCommand mc)
        {
            var go = new GameObject("RoadGraph");
            go.AddComponent<RoadGraphAuthoring>();
            GameObjectUtility.SetParentAndAlign(go, mc.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Road Graph");
            Selection.activeObject = go;
        }
    }
}