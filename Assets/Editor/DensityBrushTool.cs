using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class DensityBrushTool
{
    // ─── 상태 ───
    public static bool IsActive { get; private set; }
    public static float BrushRadius = 30f;
    public static float BrushStrength = 0.05f;

    private static TownGenerator activeGenerator;
    private static HashSet<TownBlock> dirtyBlocks = new HashSet<TownBlock>();
    private static bool isPainting = false;
    private static double lastRegenTime;
    private const double REGEN_DEBOUNCE = 0.15; // 초

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 활성화 / 비활성화
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public static void Activate(TownGenerator gen)
    {
        if (IsActive) return;
        if (BlockPickerTool.IsActive) BlockPickerTool.Deactivate();  
        if (GridWarpTool.IsActive) GridWarpTool.Deactivate();
        activeGenerator = gen;
        IsActive = true;
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
    }

    public static void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        activeGenerator = null;
        SceneView.duringSceneGui -= OnSceneGUI;
        FlushDirtyBlocks();
        SceneView.RepaintAll();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 메인: Scene 뷰 GUI 콜백
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static void OnSceneGUI(SceneView sv)
    {
        if (activeGenerator == null)
        {
            Deactivate();
            return;
        }

        Event e = Event.current;

        // 마우스 위치 → 바닥 평면(y=0)에서의 월드 좌표
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float dist)) return;
        Vector3 worldPos = ray.GetPoint(dist);

        // 브러시 시각화
        DrawBrushHandle(worldPos, e.shift);

        // 키 입력으로 브러시 크기 조절
        if (e.type == EventType.ScrollWheel && e.control)
        {
            BrushRadius = Mathf.Clamp(BrushRadius - e.delta.y * 2f, 5f, 200f);
            e.Use();
            SceneView.RepaintAll();
        }

        // Hot control 가로채서 기본 동작 차단
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    isPainting = true;
                    PaintAt(worldPos, e.shift);
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0 && isPainting)
                {
                    PaintAt(worldPos, e.shift);
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 && isPainting)
                {
                    isPainting = false;
                    GUIUtility.hotControl = 0;
                    FlushDirtyBlocks();
                    e.Use();
                }
                break;

            case EventType.Layout:
                // Scene 뷰에서 다른 오브젝트 클릭 가로채기
                HandleUtility.AddDefaultControl(controlId);
                break;
        }

        // 디바운스 재생성 (드래그 중에도 일정 시간마다)
        if (isPainting && EditorApplication.timeSinceStartup - lastRegenTime > REGEN_DEBOUNCE)
        {
            FlushDirtyBlocks();
        }

        SceneView.RepaintAll();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 브러시 시각화
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static void DrawBrushHandle(Vector3 center, bool isErase)
    {
        Color outerColor = isErase ? new Color(0.4f, 0.6f, 1f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.9f);
        Color fillColor  = isErase ? new Color(0.4f, 0.6f, 1f, 0.1f) : new Color(1f, 0.4f, 0.4f, 0.1f);

        Handles.color = fillColor;
        Handles.DrawSolidDisc(center, Vector3.up, BrushRadius);

        Handles.color = outerColor;
        Handles.DrawWireDisc(center, Vector3.up, BrushRadius, 3f);

        // 중심 십자
        Handles.DrawLine(center + Vector3.left * 2, center + Vector3.right * 2);
        Handles.DrawLine(center + Vector3.forward * 2, center + Vector3.back * 2);

        // 텍스트 라벨
        Handles.BeginGUI();
        Vector2 screen = HandleUtility.WorldToGUIPoint(center);
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        GUI.Box(new Rect(screen.x + 20, screen.y - 30, 180, 50),
            $"R: {BrushRadius:F0}m  |  {(isErase ? "🔵 지우기" : "🔴 칠하기")}\nCtrl+휠: 크기 조절", style);
        Handles.EndGUI();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 페인팅 (밀도 변경)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static void PaintAt(Vector3 worldPos, bool isErase)
    {
        if (activeGenerator == null) return;

        // 모든 블록 순회 (브러시 영역 안에 들어오는 것만)
        Transform blocks = activeGenerator.transform.Find("Blocks");
        if (blocks == null) return;

        float r2 = BrushRadius * BrushRadius;
        float delta = isErase ? -BrushStrength : BrushStrength;

        foreach (Transform blockTr in blocks)
        {
            Vector3 d = blockTr.position - worldPos;
            d.y = 0;
            float dist2 = d.sqrMagnitude;
            if (dist2 > r2) continue;

            var blk = blockTr.GetComponent<TownBlock>();
            if (blk == null) continue;

            // ★ 잠긴 영역은 건너뛰기
            if (activeGenerator.IsBlockLocked(blk)) continue;

            float falloff = 1f - Mathf.Sqrt(dist2) / BrushRadius;
            float effectiveDelta = delta * falloff;

            float current = blk.densityOverride >= 0f ? blk.densityOverride : blk.autoDensity;
            float next = Mathf.Clamp01(current + effectiveDelta);

            Undo.RecordObject(blk, "Density Brush");
            blk.densityOverride = next;
            EditorUtility.SetDirty(blk);

            dirtyBlocks.Add(blk);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 변경된 블록 일괄 재생성
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static void FlushDirtyBlocks()
    {
        if (dirtyBlocks.Count == 0) return;
        foreach (var blk in dirtyBlocks)
        {
            if (blk != null) blk.Regenerate();
        }
        dirtyBlocks.Clear();
        lastRegenTime = EditorApplication.timeSinceStartup;
    }
}