using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TownGeneratorWindow : EditorWindow
{
    // 자동 분류 설정
    private int _autoClassifyTiers = 4;
    private int _autoClassifyMinSize = 3;
    private bool _autoClassifyClear = true;
    private bool _autoClassifyProtect = true;
    
    private bool showRegions = true;
    private TownRegion editingRegion;     // 편집 대상 영역
    private bool regionAddMode = false;   // 영역 멤버 추가 모드
    
    private TownGenerator gen;
    private Vector2 scrollPos;
    private Vector2 blockListScrollPos;

    private bool showParameters = true;
    private bool showPreset = true;
    private bool showJson = true;
    private bool showBrush = true;
    private bool showBlocks = true;       // ★ 새 섹션
    private bool showStats = true;

    // 블록 리스트 관련
    private string blockSearch = "";
    private BlockSortMode blockSortMode = BlockSortMode.GridOrder;
    private bool showOnlyOverridden = false;
    private bool autoFocusOnSelect = false;

    private enum BlockSortMode { GridOrder, DensityHigh, DensityLow, OverriddenFirst }

    private Editor cachedEditor;
    private Editor cachedBlockEditor;

    [MenuItem("Tools/Town Generator/Open Window %#t")]
    public static void OpenWindow()
    {
        var window = GetWindow<TownGeneratorWindow>("🏙️ Town Generator");
        window.minSize = new Vector2(360, 600);
        window.Show();
    }

    void OnEnable()
    {
        FindGenerator();
        EditorApplication.hierarchyChanged += FindGenerator;
        Selection.selectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        EditorApplication.hierarchyChanged -= FindGenerator;
        Selection.selectionChanged -= OnSelectionChanged;
    }

    void OnSelectionChanged()
    {
        _multiBatchInited = false;  // 다음 평균값으로 다시 초기화
        Repaint();
    }

    void FindGenerator()
    {
        if (gen == null)
            gen = FindFirstObjectByType<TownGenerator>();

        if (gen != null && (cachedEditor == null || cachedEditor.target != gen))
            cachedEditor = Editor.CreateEditor(gen);

        if (gen == null)
            cachedEditor = null;

        Repaint();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GUI
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void OnGUI()
    {
        DrawHeader();

        if (gen == null)
        {
            DrawNoGeneratorUI();
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 파라미터
        showParameters = EditorGUILayout.BeginFoldoutHeaderGroup(showParameters, "⚙️ 파라미터");
        if (showParameters && cachedEditor != null)
        {
            EditorGUI.indentLevel++;
            cachedEditor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        showPreset = EditorGUILayout.BeginFoldoutHeaderGroup(showPreset, "📦 프리셋");
        if (showPreset) DrawPresetSection();
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        showJson = EditorGUILayout.BeginFoldoutHeaderGroup(showJson, "💾 JSON 익스포트/임포트");
        if (showJson) DrawJsonSection();
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        showBrush = EditorGUILayout.BeginFoldoutHeaderGroup(showBrush, "🖌️ Density Brush");
        if (showBrush) DrawBrushSection();
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(8);

        // 카메라 프리셋
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📷 카메라 뷰", EditorStyles.boldLabel);
        DrawCameraPresets();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // Scene 클릭 모드
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎯 Scene 클릭 선택", EditorStyles.boldLabel);
        DrawBlockPickerSection();
        EditorGUILayout.EndVertical();

        // ★ 그리드 워프 추가
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🌀 그리드 워프 (도로 형태 변형)", EditorStyles.boldLabel);
        DrawGridWarpSection();
        EditorGUILayout.EndVertical(); 

        // ★ 영역 섹션
        showRegions = EditorGUILayout.BeginFoldoutHeaderGroup(showRegions, "🏘️ 영역(Region)");
        if (showRegions) DrawRegionsSection();
        EditorGUILayout.EndFoldoutHeaderGroup();

        // ★ 블록 편집 섹션
        showBlocks = EditorGUILayout.BeginFoldoutHeaderGroup(showBlocks, "🧱 블록 편집");
        if (showBlocks) DrawBlocksSection();
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(15);

        DrawGenerateButtons();

        showStats = EditorGUILayout.BeginFoldoutHeaderGroup(showStats, "📊 통계");
        if (showStats) DrawStats();
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.EndScrollView();
    }

    void DrawHeader()
    {
        var rect = EditorGUILayout.GetControlRect(false, 32);
        EditorGUI.DrawRect(rect, new Color(0.2f, 0.25f, 0.3f));
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 14;
        EditorGUI.LabelField(rect, "🏙️  Town Generator", titleStyle);
    }

    void DrawNoGeneratorUI()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.HelpBox(
            "현재 씬에 TownGenerator가 없습니다.\n아래 버튼으로 생성해주세요.",
            MessageType.Warning);

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("➕ Create Town in Scene", GUILayout.Height(40)))
            CreateTownInScene();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        if (GUILayout.Button("🔍 씬에서 다시 찾기", GUILayout.Height(24)))
            FindGenerator();
    }

    void CreateTownInScene()
    {
        var go = new GameObject("Town");
        gen = go.AddComponent<TownGenerator>();
        Undo.RegisterCreatedObjectUndo(go, "Create Town");
        Selection.activeGameObject = go;
        FindGenerator();
        Debug.Log("✅ Town 생성됨.");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 영역(Region) 섹션
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawRegionsSection()
    {
        if (gen.regions == null) gen.regions = new List<TownRegion>();

        // 표시 토글
        bool newShow = EditorGUILayout.ToggleLeft("Scene에 영역 색상 표시", gen.showRegionGizmos);
        if (newShow != gen.showRegionGizmos)
        {
            Undo.RecordObject(gen, "Toggle Region Gizmos");
            gen.showRegionGizmos = newShow;
            EditorUtility.SetDirty(gen);
        }

        // ★ 라벨 토글 추가
        bool newLabel = EditorGUILayout.ToggleLeft("Scene에 영역 라벨 표시", gen.showRegionLabels);
        if (newLabel != gen.showRegionLabels)
        {
            Undo.RecordObject(gen, "Toggle Region Labels");
            gen.showRegionLabels = newLabel;
            EditorUtility.SetDirty(gen);
        }

        // 선택된 블록들 수집
        List<TownBlock> selBlocks = new List<TownBlock>();
        foreach (var obj in Selection.objects)
        {
            var go = obj as GameObject;
            if (go == null) continue;
            var bb = go.GetComponent<TownBlock>();
            if (bb != null) selBlocks.Add(bb);
        }

        // 새 영역 만들기
        EditorGUILayout.Space(3);
        EditorGUI.BeginDisabledGroup(selBlocks.Count == 0);
        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button($"+ 선택된 {selBlocks.Count}개 블록으로 새 영역 만들기", GUILayout.Height(28)))
        {
            CreateRegionFromSelection(selBlocks);
        }
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);

        // ★ 자동 분류 섹션
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🤖 자동 분류 (밀도 + 클러스터링)", EditorStyles.boldLabel);

        _autoClassifyTiers = EditorGUILayout.IntSlider(
            new GUIContent("분할 단계",
                "밀도를 몇 단계로 나눌지.\n2=도심/외곽, 4=권장, 6=세분화"),
            _autoClassifyTiers, 2, 6);

        _autoClassifyMinSize = EditorGUILayout.IntSlider(
            new GUIContent("최소 영역 크기",
                "이 블록 수 미만의 영역은 무시.\n작을수록 자투리 영역 많아짐"),
            _autoClassifyMinSize, 1, 10);

        _autoClassifyClear = EditorGUILayout.ToggleLeft(
            new GUIContent("기존 영역 삭제",
                "체크 시 기존 영역 모두 삭제 (잠긴 영역은 유지)"),
            _autoClassifyClear);

        _autoClassifyProtect = EditorGUILayout.ToggleLeft(
            new GUIContent("잠긴 영역 블록 보호",
                "체크 시 잠긴 영역의 블록은 분류 대상에서 제외"),
            _autoClassifyProtect);

        GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
        if (GUILayout.Button("🤖 자동 분류 실행", GUILayout.Height(32)))
        {
            var settings = new TownAutoClassify.Settings
            {
                tierCount = _autoClassifyTiers,
                minRegionSize = _autoClassifyMinSize,
                clearExisting = _autoClassifyClear,
                protectLocked = _autoClassifyProtect,
            };
            int count = TownAutoClassify.Classify(gen, settings);
            if (count > 0)
            {
                EditorUtility.DisplayDialog("자동 분류 완료",
                    $"{count}개의 영역이 생성되었습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("자동 분류 실패",
                    "영역이 생성되지 않았습니다.\n블록이 있는지 또는 모든 블록이 잠긴 영역에 속하는지 확인하세요.",
                    "확인");
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();

        // 영역 리스트
        if (gen.regions.Count == 0)
        {
            EditorGUILayout.HelpBox("등록된 영역이 없습니다.\n블록을 다중 선택 후 위 버튼으로 만드세요.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"📋 영역 {gen.regions.Count}개", EditorStyles.miniLabel);

        for (int i = 0; i < gen.regions.Count; i++)
        {
            DrawRegionRow(gen.regions[i], i);
        }

        // 선택된 영역 편집 UI
        if (editingRegion != null && gen.regions.Contains(editingRegion))
        {
            EditorGUILayout.Space(8);
            DrawRegionEditor(editingRegion, selBlocks);
        }
    }

    void CreateRegionFromSelection(List<TownBlock> blocks)
    {
        if (blocks.Count == 0) return;

        Undo.RecordObject(gen, "Create Region");

        var region = new TownRegion();
        region.name = $"Region {gen.regions.Count + 1}";
        // 랜덤 색상 (밝은 채도)
        float h = Random.value;
        Color c = Color.HSVToRGB(h, 0.55f, 1f);
        c.a = 0.35f;
        region.color = c;

        foreach (var b in blocks)
        {
            gen.AddBlockToRegion(region, b.gridX, b.gridZ);
        }
        gen.regions.Add(region);
        editingRegion = region;
        EditorUtility.SetDirty(gen);
        SceneView.RepaintAll();
    }

    void DrawRegionRow(TownRegion region, int index)
    {
        bool isEditing = (region == editingRegion);

        var origBg = GUI.backgroundColor;
        if (isEditing) GUI.backgroundColor = new Color(1f, 0.95f, 0.4f);
        else if (region.isLocked) GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);

        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        // 색상 점
        Rect dotRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
        dotRect.y += 2;
        dotRect.height = 16;
        var solidColor = region.color;
        solidColor.a = 1f;
        EditorGUI.DrawRect(dotRect, solidColor);

        // ★ 잠금 토글 버튼
        string lockIcon = region.isLocked ? "🔒" : "🔓";
        var lockStyle = new GUIStyle(GUI.skin.button);
        lockStyle.fontSize = 11;
        if (GUILayout.Button(lockIcon, lockStyle, GUILayout.Width(28), GUILayout.Height(20)))
        {
            Undo.RecordObject(gen, "Toggle Region Lock");
            region.isLocked = !region.isLocked;
            EditorUtility.SetDirty(gen);
            SceneView.RepaintAll();
        }

        // 이름 + 블록 수
        var lblStyle = new GUIStyle(EditorStyles.label);
        if (isEditing) lblStyle.fontStyle = FontStyle.Bold;
        if (region.isLocked) lblStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);

        string label = $"{region.name}  ({region.blockIds.Count})";
        EditorGUILayout.LabelField(label, lblStyle);

        // 멤버 선택
        if (GUILayout.Button("멤버선택", GUILayout.Width(70), GUILayout.Height(18)))
        {
            var members = gen.GetBlocksInRegion(region);
            Selection.objects = members.Select(b => (Object)b.gameObject).ToArray();
            editingRegion = region;
        }

        // 편집
        if (GUILayout.Button(isEditing ? "닫기" : "편집", GUILayout.Width(50), GUILayout.Height(18)))
        {
            editingRegion = isEditing ? null : region;
            regionAddMode = false;
        }

        // 삭제
        GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
        if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
        {
            if (region.isLocked)
            {
                EditorUtility.DisplayDialog("삭제 불가",
                    $"'{region.name}' 영역은 잠겨 있어 삭제할 수 없습니다.\n먼저 잠금을 해제해주세요.", "확인");
            }
            else if (EditorUtility.DisplayDialog("영역 삭제",
                $"'{region.name}' 영역을 삭제하시겠습니까?", "삭제", "취소"))
            {
                Undo.RecordObject(gen, "Delete Region");
                gen.regions.RemoveAt(index);
                if (editingRegion == region) editingRegion = null;
                EditorUtility.SetDirty(gen);
                SceneView.RepaintAll();
                GUIUtility.ExitGUI();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = origBg;
    }

    void DrawRegionEditor(TownRegion region, List<TownBlock> currentSelection)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"⚙️ '{region.name}' 편집", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.TextField("이름", region.name);
        Color newColor = EditorGUILayout.ColorField("색상", region.color);
        string newNote = EditorGUILayout.TextField("메모", region.note);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(gen, "Edit Region");
            region.name = newName;
            region.color = newColor;
            region.note = newNote;
            EditorUtility.SetDirty(gen);
            SceneView.RepaintAll();
        }

        EditorGUILayout.LabelField($"멤버 블록: {region.blockIds.Count}개");

        EditorGUILayout.Space(5);

        // 멤버 추가/제거
        EditorGUILayout.LabelField("멤버 관리", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginDisabledGroup(currentSelection.Count == 0);
        if (GUILayout.Button($"+ 선택된 {currentSelection.Count}개 추가", GUILayout.Height(22)))
        {
            Undo.RecordObject(gen, "Add Blocks to Region");
            foreach (var b in currentSelection)
                gen.AddBlockToRegion(region, b.gridX, b.gridZ);
            EditorUtility.SetDirty(gen);
            SceneView.RepaintAll();
        }
        if (GUILayout.Button($"− 선택된 {currentSelection.Count}개 제거", GUILayout.Height(22)))
        {
            Undo.RecordObject(gen, "Remove Blocks from Region");
            foreach (var b in currentSelection)
                region.blockIds.RemoveAll(v => v.x == b.gridX && v.y == b.gridZ);
            EditorUtility.SetDirty(gen);
            SceneView.RepaintAll();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 일괄 액션
        EditorGUILayout.LabelField("영역 일괄 액션", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("멤버 모두 선택", GUILayout.Height(22)))
        {
            var members = gen.GetBlocksInRegion(region);
            Selection.objects = members.Select(b => (Object)b.gameObject).ToArray();
        }
        if (GUILayout.Button("🎯 카메라 포커스", GUILayout.Height(22)))
        {
            var members = gen.GetBlocksInRegion(region);
            if (members.Count > 0)
            {
                Selection.objects = members.Select(b => (Object)b.gameObject).ToArray();
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 일괄 밀도
        EditorGUILayout.Space(3);
        _regionBatchDensity = EditorGUILayout.Slider("Set Density", _regionBatchDensity, 0f, 1f);
        GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
        if (GUILayout.Button("✓ 영역 전체에 밀도 적용 + 재생성", GUILayout.Height(26)))
        {
            var members = gen.GetBlocksInRegion(region);
            foreach (var b in members)
            {
                Undo.RecordObject(b, "Region Set Density");
                b.densityOverride = _regionBatchDensity;
                EditorUtility.SetDirty(b);
                Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Region Regen");
                b.Regenerate();
            }
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("↺ 영역 전체 자동 밀도로", GUILayout.Height(22)))
        {
            var members = gen.GetBlocksInRegion(region);
            foreach (var b in members)
            {
                Undo.RecordObject(b, "Region Reset Override");
                b.densityOverride = -1f;
                b.Regenerate();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private float _regionBatchDensity = 0.5f;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ★ 블록 편집 섹션 (신규)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawBlocksSection()
    {
        Transform blocksRoot = gen.transform.Find("Blocks");
        if (blocksRoot == null || blocksRoot.childCount == 0)
        {
            EditorGUILayout.HelpBox("아직 블록이 없습니다. Generate Town을 먼저 실행해주세요.", MessageType.Info);
            return;
        }

        List<TownBlock> allBlocks = new List<TownBlock>();
        foreach (Transform t in blocksRoot)
        {
            var b = t.GetComponent<TownBlock>();
            if (b != null) allBlocks.Add(b);
        }

        // 검색 + 필터 UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
        blockSearch = EditorGUILayout.TextField(blockSearch);
        if (GUILayout.Button("✕", GUILayout.Width(22)))
            blockSearch = "";
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("정렬", GUILayout.Width(35));
        blockSortMode = (BlockSortMode)EditorGUILayout.EnumPopup(blockSortMode);
        showOnlyOverridden = EditorGUILayout.ToggleLeft("오버라이드만", showOnlyOverridden, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        autoFocusOnSelect = EditorGUILayout.ToggleLeft("선택 시 카메라 포커스", autoFocusOnSelect);

        // ★ 현재 다중 선택된 블록들 모두 수집
        HashSet<TownBlock> selectedSet = new HashSet<TownBlock>();
        foreach (var obj in Selection.objects)
        {
            var go = obj as GameObject;
            if (go == null) continue;
            var bb = go.GetComponent<TownBlock>();
            if (bb != null) selectedSet.Add(bb);
        }

        // 필터링
        var filtered = allBlocks.AsEnumerable();
        if (!string.IsNullOrEmpty(blockSearch))
            filtered = filtered.Where(b => $"{b.gridX}_{b.gridZ}".Contains(blockSearch.Trim()));
        if (showOnlyOverridden)
            filtered = filtered.Where(b => b.densityOverride >= 0f);

        // 정렬
        switch (blockSortMode)
        {
            case BlockSortMode.GridOrder:
                filtered = filtered.OrderBy(b => b.gridZ).ThenBy(b => b.gridX); break;
            case BlockSortMode.DensityHigh:
                filtered = filtered.OrderByDescending(b => b.EffectiveDensity); break;
            case BlockSortMode.DensityLow:
                filtered = filtered.OrderBy(b => b.EffectiveDensity); break;
            case BlockSortMode.OverriddenFirst:
                filtered = filtered
                    .OrderByDescending(b => b.densityOverride >= 0f)
                    .ThenBy(b => b.gridZ).ThenBy(b => b.gridX); break;
        }
        var list = filtered.ToList();

        // ★ 선택 개수 표시
        string selInfo = selectedSet.Count > 0 ? $"  |  ✓ {selectedSet.Count}개 선택됨" : "";
        EditorGUILayout.LabelField($"📋 {list.Count} / {allBlocks.Count} 블록{selInfo}", EditorStyles.miniLabel);

        blockListScrollPos = EditorGUILayout.BeginScrollView(
            blockListScrollPos, GUILayout.Height(180));

        // ★ 각 항목이 selectedSet에 있는지로 강조 판단
        foreach (var b in list)
        {
            DrawBlockListItem(b, selectedSet.Contains(b));
        }

        EditorGUILayout.EndScrollView();

        // 빠른 액션
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("모든 오버라이드 초기화", GUILayout.Height(22)))
        {
            if (EditorUtility.DisplayDialog("확인",
                "모든 블록의 밀도 오버라이드를 자동(-1)으로 되돌리시겠습니까?", "확인", "취소"))
            {
                foreach (var b in allBlocks)
                {
                    Undo.RecordObject(b, "Reset Override");
                    b.densityOverride = -1f;
                    b.Regenerate();
                }
            }
        }
        if (GUILayout.Button("선택 해제", GUILayout.Height(22)))
            Selection.activeGameObject = null;
        EditorGUILayout.EndHorizontal();

        // ★ 보너스: 필터 결과 모두 선택
        if (list.Count > 0 && list.Count < allBlocks.Count)
        {
            if (GUILayout.Button($"☑ 보이는 {list.Count}개 모두 선택", GUILayout.Height(22)))
            {
                Selection.objects = list.Select(b => (Object)b.gameObject).ToArray();
            }
        }

        EditorGUILayout.Space(5);

        // 다중 편집 UI
        DrawSelectedBlocksEditor(selectedSet.ToList());
    }

    void DrawBlockListItem(TownBlock b, bool isSelected)
    {
        bool overridden = b.densityOverride >= 0f;

        var origBg = GUI.backgroundColor;
        if (isSelected) GUI.backgroundColor = new Color(1f, 1f, 0.3f);
        else if (overridden) GUI.backgroundColor = new Color(1f, 0.95f, 0.7f);

        Rect rowRect = EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        Rect dotRect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14));
        dotRect.y += 2;
        dotRect.height = 14;
        Color densCol = Color.Lerp(
            new Color(0.4f, 0.7f, 0.4f),
            new Color(0.9f, 0.4f, 0.3f),
            b.EffectiveDensity);
        EditorGUI.DrawRect(dotRect, densCol);

        string label = $"({b.gridX}, {b.gridZ})  {b.EffectiveDensity:F2}";
        if (overridden) label += "  *";

        var lblStyle = new GUIStyle(EditorStyles.label);
        if (isSelected) lblStyle.fontStyle = FontStyle.Bold;
        EditorGUILayout.LabelField(label, lblStyle);

        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = origBg;

        EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

        Event e = Event.current;
        // 호버 강조선 (옅은 노랑)
        if (rowRect.Contains(e.mousePosition))
        {
            EditorGUI.DrawRect(
                new Rect(rowRect.x, rowRect.y, 3, rowRect.height),
                new Color(1f, 0.95f, 0.4f, 0.9f));
        }

        // ★ 선택된 항목엔 좌측에 굵은 노란 막대
        if (isSelected)
        {
            EditorGUI.DrawRect(
                new Rect(rowRect.x, rowRect.y, 5, rowRect.height),
                new Color(1f, 0.7f, 0f, 1f));   // 진한 황색
        }

        if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
        {
            // ★ Ctrl/Cmd: 토글 추가, Shift: 범위 선택, 기본: 단일
            var current = new List<Object>(Selection.objects);

            if (e.control || e.command)
            {
                // 토글 추가/제거
                if (current.Contains(b.gameObject))
                    current.Remove(b.gameObject);
                else
                    current.Add(b.gameObject);
                Selection.objects = current.ToArray();
            }
            else if (e.shift && Selection.activeGameObject != null)
            {
                // 범위 선택 (마지막 활성 ↔ 클릭한 블록)
                var lastBlk = Selection.activeGameObject.GetComponent<TownBlock>();
                if (lastBlk != null)
                {
                    AddRangeToSelection(lastBlk, b);
                }
                else
                {
                    Selection.activeGameObject = b.gameObject;
                }
            }
            else
            {
                // 단일 선택
                Selection.activeGameObject = b.gameObject;
            }

            EditorGUIUtility.PingObject(b.gameObject);
            if (autoFocusOnSelect)
                SceneView.lastActiveSceneView?.FrameSelected();
            e.Use();
            Repaint();
        }
    }

    // ★ 범위 선택 헬퍼
    void AddRangeToSelection(TownBlock from, TownBlock to)
    {
        Transform blocksRoot = gen.transform.Find("Blocks");
        if (blocksRoot == null) return;

        int minX = Mathf.Min(from.gridX, to.gridX);
        int maxX = Mathf.Max(from.gridX, to.gridX);
        int minZ = Mathf.Min(from.gridZ, to.gridZ);
        int maxZ = Mathf.Max(from.gridZ, to.gridZ);

        var newSelection = new List<Object>();
        foreach (Transform t in blocksRoot)
        {
            var bb = t.GetComponent<TownBlock>();
            if (bb == null) continue;
            if (bb.gridX >= minX && bb.gridX <= maxX &&
                bb.gridZ >= minZ && bb.gridZ <= maxZ)
            {
                newSelection.Add(t.gameObject);
            }
        }
        Selection.objects = newSelection.ToArray();
    }
    // ★ 다중 블록 일괄 편집 UI
    void DrawSelectedBlocksEditor(List<TownBlock> blocks)
    {
        EditorGUILayout.LabelField("⚙️ 선택된 블록 편집", EditorStyles.boldLabel);

        if (blocks == null || blocks.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "위 리스트에서 블록을 선택하세요.\n" +
                "• 클릭 = 단일 선택\n" +
                "• Ctrl/Cmd + 클릭 = 토글 추가\n" +
                "• Shift + 클릭 = 범위 선택",
                MessageType.None);
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (blocks.Count == 1)
        {
            DrawSingleBlockUI(blocks[0]);
        }
        else
        {
            DrawMultiBlockUI(blocks);
        }

        EditorGUILayout.EndVertical();
    }

    void DrawSingleBlockUI(TownBlock block)
    {
        EditorGUILayout.LabelField($"📦 Block ({block.gridX}, {block.gridZ})", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("자동 밀도", block.autoDensity.ToString("F2"));
        EditorGUILayout.LabelField("적용 밀도", block.EffectiveDensity.ToString("F2"));

        EditorGUILayout.Space(5);

        float newDensity = EditorGUILayout.Slider(
            new GUIContent("Density Override", "-1 = 자동 사용 / 0~1 = 강제 밀도"),
            block.densityOverride, -1f, 1f);
        if (!Mathf.Approximately(newDensity, block.densityOverride))
        {
            Undo.RecordObject(block, "Change Block Density");
            block.densityOverride = newDensity;
            EditorUtility.SetDirty(block);
        }

        if (block.densityOverride < 0f)
            EditorGUILayout.HelpBox("자동 밀도 사용 중", MessageType.None);
        else
            EditorGUILayout.HelpBox($"수동 밀도: {block.densityOverride:F2}", MessageType.Info);

        int newOffset = EditorGUILayout.IntField("Seed Offset", block.seedOffset);
        if (newOffset != block.seedOffset)
        {
            Undo.RecordObject(block, "Change Block Seed");
            block.seedOffset = newOffset;
            EditorUtility.SetDirty(block);
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button("🔄 재생성", GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(block.gameObject, "Regen Block");
            block.Regenerate();
        }
        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
        if (GUILayout.Button("🎲 랜덤 재생성", GUILayout.Height(28)))
        {
            Undo.RecordObject(block, "Random Block Seed");
            Undo.RegisterFullObjectHierarchyUndo(block.gameObject, "Random Regen Block");
            block.RandomRegenerate();
            EditorUtility.SetDirty(block);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎯 카메라 포커스", GUILayout.Height(22)))
            SceneView.lastActiveSceneView?.FrameSelected();
        if (GUILayout.Button("↺ 자동 밀도로", GUILayout.Height(22)))
        {
            Undo.RecordObject(block, "Reset Override");
            block.densityOverride = -1f;
            block.Regenerate();
        }
        EditorGUILayout.EndHorizontal();
    }

    // ★ 다중 선택 시 UI
    void DrawMultiBlockUI(List<TownBlock> blocks)
    {
        EditorGUILayout.LabelField($"📦 다중 선택: {blocks.Count}개 블록", EditorStyles.boldLabel);

        // 통계
        float avgDensity = 0f;
        int overriddenCount = 0;
        foreach (var b in blocks)
        {
            avgDensity += b.EffectiveDensity;
            if (b.densityOverride >= 0f) overriddenCount++;
        }
        avgDensity /= blocks.Count;

        EditorGUILayout.LabelField($"평균 밀도: {avgDensity:F2}");
        EditorGUILayout.LabelField($"오버라이드된 블록: {overriddenCount} / {blocks.Count}");

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("일괄 변경", EditorStyles.boldLabel);

        // 일괄 밀도 설정
        if (!_multiBatchInited)
        {
            _multiBatchDensity = avgDensity;
            _multiBatchInited = true;
        }
        _multiBatchDensity = EditorGUILayout.Slider(
            new GUIContent("Set Density", "선택된 모든 블록을 이 밀도로 설정"),
            _multiBatchDensity, 0f, 1f);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
        if (GUILayout.Button($"✓ 적용 + 재생성", GUILayout.Height(28)))
        {
            foreach (var b in blocks)
            {
                Undo.RecordObject(b, "Batch Set Density");
                b.densityOverride = _multiBatchDensity;
                EditorUtility.SetDirty(b);
                Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Batch Regen");
                b.Regenerate();
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 일괄 가산 (현재 밀도 ± delta)
        EditorGUILayout.LabelField("상대 변경 (현재값 기준)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("− 0.1", GUILayout.Height(24))) BatchAddDensity(blocks, -0.1f);
        if (GUILayout.Button("− 0.05", GUILayout.Height(24))) BatchAddDensity(blocks, -0.05f);
        if (GUILayout.Button("+ 0.05", GUILayout.Height(24))) BatchAddDensity(blocks, +0.05f);
        if (GUILayout.Button("+ 0.1", GUILayout.Height(24))) BatchAddDensity(blocks, +0.1f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // 일괄 액션
        EditorGUILayout.LabelField("일괄 액션", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button($"🔄 모두 재생성", GUILayout.Height(28)))
        {
            foreach (var b in blocks)
            {
                Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Batch Regen Block");
                b.Regenerate();
            }
        }
        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
        if (GUILayout.Button($"🎲 랜덤 재생성", GUILayout.Height(28)))
        {
            foreach (var b in blocks)
            {
                Undo.RecordObject(b, "Batch Random Seed");
                Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Batch Random Regen");
                b.RandomRegenerate();
                EditorUtility.SetDirty(b);
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↺ 모두 자동 밀도로", GUILayout.Height(24)))
        {
            foreach (var b in blocks)
            {
                Undo.RecordObject(b, "Batch Reset Override");
                b.densityOverride = -1f;
                b.Regenerate();
            }
        }
        if (GUILayout.Button("🎯 카메라 포커스", GUILayout.Height(24)))
            SceneView.lastActiveSceneView?.FrameSelected();
        EditorGUILayout.EndHorizontal();
    }

    // 다중 편집용 임시 상태
    private float _multiBatchDensity = 0.5f;
    private bool _multiBatchInited = false;

    void BatchAddDensity(List<TownBlock> blocks, float delta)
    {
        foreach (var b in blocks)
        {
            Undo.RecordObject(b, "Batch Add Density");
            float current = b.EffectiveDensity;
            b.densityOverride = Mathf.Clamp01(current + delta);
            EditorUtility.SetDirty(b);
            Undo.RegisterFullObjectHierarchyUndo(b.gameObject, "Batch Regen");
            b.Regenerate();
        }
    }
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 프리셋
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawPresetSection()
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
        if (GUILayout.Button("Apply Preset", GUILayout.Height(28)))
        {
            if (gen.preset == null)
                EditorUtility.DisplayDialog("프리셋 없음", "Preset 슬롯에 프리셋을 끌어다 놓으세요.", "확인");
            else
            {
                Undo.RecordObject(gen, "Apply Preset");
                gen.preset.ApplyTo(gen);
                EditorUtility.SetDirty(gen);
            }
        }
        GUI.backgroundColor = new Color(1f, 0.85f, 0.7f);
        if (GUILayout.Button("Apply + Generate", GUILayout.Height(28)))
        {
            if (gen.preset == null)
                EditorUtility.DisplayDialog("프리셋 없음", "Preset 슬롯에 프리셋을 끌어다 놓으세요.", "확인");
            else
            {
                Undo.RecordObject(gen, "Apply Preset");
                gen.preset.ApplyTo(gen);
                EditorUtility.SetDirty(gen);
                gen.Generate();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        GUI.backgroundColor = new Color(0.9f, 0.95f, 0.9f);
        if (GUILayout.Button("💾 Save Current as New Preset", GUILayout.Height(24)))
            SaveAsNewPreset();
        GUI.backgroundColor = Color.white;

        if (gen.preset != null)
            EditorGUILayout.HelpBox($"📋 {gen.preset.name}\n{gen.preset.description}", MessageType.None);
    }

    void SaveAsNewPreset()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Presets"))
            AssetDatabase.CreateFolder("Assets", "Presets");

        string path = EditorUtility.SaveFilePanelInProject(
            "프리셋 저장", "MyTownPreset", "asset", "프리셋 저장 위치", "Assets/Presets");
        if (string.IsNullOrEmpty(path)) return;

        var preset = ScriptableObject.CreateInstance<TownPreset>();
        preset.CopyFrom(gen);
        preset.description = $"{gen.townShape} / 밀도 {gen.globalDensity:F2} / 최대 {gen.maxFloors}층";
        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        gen.preset = preset;
        EditorUtility.SetDirty(gen);
        EditorGUIUtility.PingObject(preset);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // JSON
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawJsonSection()
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
        if (GUILayout.Button("📤 Export JSON", GUILayout.Height(28)))
        {
            if (gen.transform.childCount == 0)
                EditorUtility.DisplayDialog("도시 없음", "먼저 Generate Town을 실행해주세요.", "확인");
            else
            {
                string defaultName = $"Town_{gen.seed}_{System.DateTime.Now:yyyyMMdd_HHmm}.json";
                string path = EditorUtility.SaveFilePanel("JSON으로 익스포트",
                    Application.dataPath, defaultName, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    TownJsonIO.Export(gen, path);
                    EditorUtility.RevealInFinder(path);
                }
            }
        }
        GUI.backgroundColor = new Color(1f, 0.9f, 0.7f);
        if (GUILayout.Button("📥 Import JSON", GUILayout.Height(28)))
        {
            string path = EditorUtility.OpenFilePanel("JSON 임포트", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                Undo.RegisterFullObjectHierarchyUndo(gen.gameObject, "Import JSON");
                TownJsonIO.Import(gen, path);
                EditorUtility.SetDirty(gen);
            }
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 브러시
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawBrushSection()
    {
        if (DensityBrushTool.IsActive)
        {
            EditorGUILayout.HelpBox(
                "🖌️ 브러시 모드 ON\n• 좌클릭 = 칠하기\n• Shift+좌클릭 = 지우기\n• Ctrl+휠 = 크기 조절",
                MessageType.Info);
            DensityBrushTool.BrushRadius = EditorGUILayout.Slider(
                "Brush Radius", DensityBrushTool.BrushRadius, 5f, 200f);
            DensityBrushTool.BrushStrength = EditorGUILayout.Slider(
                "Brush Strength", DensityBrushTool.BrushStrength, 0.01f, 0.3f);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✖ 브러시 모드 끄기", GUILayout.Height(28)))
                DensityBrushTool.Deactivate();
            GUI.backgroundColor = Color.white;
        }
        else
        {
            if (gen.transform.childCount == 0)
                EditorGUILayout.HelpBox("Generate Town을 먼저 실행해주세요.", MessageType.Warning);
            else
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("🖌️ 브러시 모드 켜기", GUILayout.Height(28)))
                    DensityBrushTool.Activate(gen);
                GUI.backgroundColor = Color.white;
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Generate / Clear
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawGenerateButtons()
    {
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("Generate Town", GUILayout.Height(40))) gen.Generate();
        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("Clear", GUILayout.Height(40))) gen.Clear();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
        if (GUILayout.Button("🎲 Random Seed + Generate", GUILayout.Height(30)))
        {
            gen.seed = Random.Range(0, 99999);
            gen.Generate();
            EditorUtility.SetDirty(gen);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.85f, 0.85f, 0.95f);
        if (GUILayout.Button("📍 Select Town", GUILayout.Height(22)))
        {
            Selection.activeGameObject = gen.gameObject;
            EditorGUIUtility.PingObject(gen.gameObject);
        }
        if (GUILayout.Button("🎯 Focus Camera", GUILayout.Height(22)))
        {
            Selection.activeGameObject = gen.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
        EditorGUILayout.EndHorizontal();
        GUI.backgroundColor = Color.white;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 카메라 프리셋
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawCameraPresets()
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = new Color(0.85f, 0.95f, 1f);
        if (GUILayout.Button("🔭 Top", GUILayout.Height(28)))
            TownCameraPresets.Apply(gen, TownCameraPresets.View.Top);
        if (GUILayout.Button("📐 Iso", GUILayout.Height(28)))
            TownCameraPresets.Apply(gen, TownCameraPresets.View.Iso);
        if (GUILayout.Button("🦅 Bird", GUILayout.Height(28)))
            TownCameraPresets.Apply(gen, TownCameraPresets.View.Bird);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌆 Perspective", GUILayout.Height(28)))
            TownCameraPresets.Apply(gen, TownCameraPresets.View.Perspective);
        if (GUILayout.Button("👁️ Street", GUILayout.Height(28)))
            TownCameraPresets.Apply(gen, TownCameraPresets.View.Street);
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎯 Frame Town", GUILayout.Height(22)))
            TownCameraPresets.FrameTown(gen);
        if (GUILayout.Button("🎯 Frame Selected", GUILayout.Height(22)))
            TownCameraPresets.FrameSelected();
        EditorGUILayout.EndHorizontal();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Scene 클릭 모드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawBlockPickerSection()
    {
        if (BlockPickerTool.IsActive)
        {
            EditorGUILayout.HelpBox(
                "🎯 Scene 클릭 모드 ON\n" +
                "• Scene 뷰에서 블록 위에 마우스 → 노란 하이라이트\n" +
                "• 클릭 = 단일 선택\n" +
                "• Shift + 클릭 = 추가 선택\n" +
                "• Ctrl + 클릭 = 토글",
                MessageType.Info);

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✖ 클릭 모드 끄기", GUILayout.Height(28)))
                BlockPickerTool.Deactivate();
            GUI.backgroundColor = Color.white;
        }
        else
        {
            if (gen.transform.childCount == 0)
            {
                EditorGUILayout.HelpBox("Generate Town을 먼저 실행해주세요.", MessageType.Warning);
            }
            else
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("🎯 Scene 클릭 모드 켜기", GUILayout.Height(28)))
                    BlockPickerTool.Activate(gen);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.HelpBox(
                    "Scene 뷰에서 블록을 마우스로 직접 클릭해 선택할 수 있습니다.",
                    MessageType.None);
            }
        }
    }

    void DrawGridWarpSection()
    {
        if (GridWarpTool.IsActive)
        {
            EditorGUILayout.HelpBox(
                "🌀 그리드 워프 모드 ON\n" +
                "그리드 교차점을 이동해 도로/블록 형태를 변형합니다.",
                MessageType.Info);

            var newMode = (GridWarpTool.Mode)EditorGUILayout.EnumPopup(
                new GUIContent("모드",
                    "Handle = 점 하나씩 잡고 이동\nBrush = 반경 내 여러 점 동시 이동"),
                GridWarpTool.CurrentMode);
            if (newMode != GridWarpTool.CurrentMode)
            {
                GridWarpTool.CurrentMode = newMode;
                SceneView.RepaintAll();
            }

            if (GridWarpTool.CurrentMode == GridWarpTool.Mode.Brush)
            {
                GridWarpTool.BrushRadius = EditorGUILayout.Slider(
                    new GUIContent("Brush Radius",
                        "워프 반경 (Ctrl+휠로도 조절)"),
                    GridWarpTool.BrushRadius, 5f, 200f);
                GridWarpTool.BrushFalloff = EditorGUILayout.Slider(
                    new GUIContent("Falloff",
                        "1=자연스러움, 2=중심 강조"),
                    GridWarpTool.BrushFalloff, 0.3f, 3f);
            }

            EditorGUILayout.Space(3);
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✖ 워프 모드 끄기", GUILayout.Height(28)))
                GridWarpTool.Deactivate();
            GUI.backgroundColor = Color.white;
        }
        else
        {
            if (gen.transform.childCount == 0)
            {
                EditorGUILayout.HelpBox("Generate Town을 먼저 실행해주세요.", MessageType.Warning);
            }
            else
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                if (GUILayout.Button("🌀 워프 모드 켜기", GUILayout.Height(28)))
                    GridWarpTool.Activate(gen);
                GUI.backgroundColor = Color.white;

                EditorGUILayout.HelpBox(
                    "도로 교차점을 잡고 움직여 도시 형태를 자유롭게 변형합니다.\n" +
                    "• Handle 모드: 한 점씩 정밀 이동\n" +
                    "• Brush 모드: 반경 내 점들 동시 이동 (watabou 스타일)",
                    MessageType.None);
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 통계
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    void DrawStats()
    {
        if (gen.transform.childCount == 0)
        {
            EditorGUILayout.HelpBox("아직 도시를 생성하지 않았습니다.", MessageType.None);
            return;
        }
        int blocks = 0, buildings = 0, trees = 0, lamps = 0, roads = 0, overrides = 0;
        foreach (Transform child in gen.transform)
        {
            switch (child.name)
            {
                case "Blocks":
                    foreach (Transform block in child)
                    {
                        var bb = block.GetComponent<TownBlock>();
                        if (bb != null)
                        {
                            blocks++;
                            if (bb.densityOverride >= 0f) overrides++;
                        }
                        foreach (Transform sub in block)
                            if (sub.name.StartsWith("Building")) buildings++;
                    }
                    break;
                case "Roads": roads = child.childCount; break;
                case "Props":
                    var t = child.Find("Trees"); if (t != null) trees = t.childCount;
                    var l = child.Find("Lamps"); if (l != null) lamps = l.childCount;
                    break;
            }
        }
        EditorGUILayout.HelpBox(
            $"📦 Blocks: {blocks} (오버라이드 {overrides})\n" +
            $"🏢 Buildings: {buildings}\n" +
            $"🛣 Roads: {roads}\n" +
            $"🌳 Trees: {trees}\n" +
            $"💡 Lamps: {lamps}",
            MessageType.None);
    }

    void OnInspectorUpdate() => Repaint();
}