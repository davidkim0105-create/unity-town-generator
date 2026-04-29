using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class GridWarpTool
{
    public enum Mode { Handle, Brush }

    public static bool IsActive { get; private set; }
    public static Mode CurrentMode = Mode.Handle;
    public static float BrushRadius = 30f;
    public static float BrushFalloff = 1f;  // 1 = 자연스러움, 2 = 가운데 강

    private static TownGenerator activeGen;
    private static int draggingPointIdx = -1;     // 핸들 모드: 드래그 중인 점
    private static Vector3 dragStartWorld;
    private static Vector3 dragStartPointPos;
    private static Dictionary<int, Vector3> brushOriginalPositions; // 브러시 모드: 시작 위치 백업
    private static HashSet<Vector2Int> affectedDuringDrag = new HashSet<Vector2Int>();
    private static double lastRebuildTime;
    private const double REBUILD_DEBOUNCE = 0.2;

    public static void Activate(TownGenerator gen)
    {
        if (IsActive) return;
        if (DensityBrushTool.IsActive) DensityBrushTool.Deactivate();
        if (BlockPickerTool.IsActive) BlockPickerTool.Deactivate();
        activeGen = gen;
        IsActive = true;
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
    }

    public static void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        activeGen = null;
        draggingPointIdx = -1;
        brushOriginalPositions = null;
        SceneView.duringSceneGui -= OnSceneGUI;
        FlushDirty();
        SceneView.RepaintAll();
    }

    static void OnSceneGUI(SceneView sv)
    {
        if (activeGen == null) { Deactivate(); return; }
        var gen = activeGen;
        var pts = gen.GridPoints;
        if (pts == null || pts.Count == 0) return;

        Event e = Event.current;

        // 마우스 → 월드
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        bool hitPlane = ground.Raycast(ray, out float planeDist);
        Vector3 mouseWorld = hitPlane ? ray.GetPoint(planeDist) : Vector3.zero;

        // hot control 가로채기
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        if (CurrentMode == Mode.Handle)
            DrawHandleMode(gen, pts, e, controlId, mouseWorld);
        else
            DrawBrushMode(gen, pts, e, controlId, mouseWorld, hitPlane);

        DrawHUD(gen);

        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        SceneView.RepaintAll();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 핸들 모드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static void DrawHandleMode(TownGenerator gen, List<Vector3> pts, Event e, int controlId, Vector3 mouseWorld)
    {
        int gx = gen.GridX;
        int gz = gen.GridZ;

        // 카메라 거리에 따른 핸들 크기
        Camera cam = SceneView.lastActiveSceneView?.camera;
        float baseSize = 1.5f;

        // 활성 영역 가까운 점만 표시 (성능)
        int nearestIdx = FindNearestPoint(pts, mouseWorld, out float nearestDist);

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 p = pts[i] + Vector3.up * 0.3f;

            // 활성 블록 인접한 점만 (외곽 노이즈 점 제외)
            int x = i % gx;
            int z = i / gx;
            if (!IsPointActive(gen, x, z)) continue;

            float size = HandleUtility.GetHandleSize(p) * 0.08f;
            bool isHover = (i == nearestIdx && nearestDist < size * 8f);
            bool isDragging = (i == draggingPointIdx);

            Color c = isDragging ? Color.yellow
                    : isHover ? new Color(1f, 0.6f, 0.2f)
                    : new Color(1f, 0.3f, 0.3f, 0.85f);

            Handles.color = c;
            Handles.SphereHandleCap(0, p, Quaternion.identity, size, EventType.Repaint);
        }

        // 마우스 다운 → 가장 가까운 점 잡기
        if (e.type == EventType.MouseDown && e.button == 0 && nearestIdx >= 0)
        {
            Vector3 np = pts[nearestIdx];
            float screenDist = Vector2.Distance(
                HandleUtility.WorldToGUIPoint(np),
                e.mousePosition);
            if (screenDist < 20f)
            {
                draggingPointIdx = nearestIdx;
                dragStartWorld = mouseWorld;
                dragStartPointPos = np;
                affectedDuringDrag.Clear();
                int x = nearestIdx % gen.GridX;
                int z = nearestIdx / gen.GridX;
                affectedDuringDrag.Add(new Vector2Int(x, z));
                GUIUtility.hotControl = controlId;
                e.Use();
            }
        }

        // 드래그
        if (e.type == EventType.MouseDrag && draggingPointIdx >= 0)
        {
            Vector3 delta = mouseWorld - dragStartWorld;
            Vector3 newPos = dragStartPointPos + new Vector3(delta.x, 0, delta.z);
            int x = draggingPointIdx % gen.GridX;
            int z = draggingPointIdx / gen.GridX;

            Undo.RecordObject(gen, "Move Grid Point");
            gen.SetGridPoint(x, z, newPos);
            EditorUtility.SetDirty(gen);

            // 디바운스 재빌드
            if (EditorApplication.timeSinceStartup - lastRebuildTime > REBUILD_DEBOUNCE)
                FlushDirty();
            e.Use();
        }

        if (e.type == EventType.MouseUp && draggingPointIdx >= 0)
        {
            FlushDirty();
            draggingPointIdx = -1;
            GUIUtility.hotControl = 0;
            e.Use();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 브러시 모드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static void DrawBrushMode(TownGenerator gen, List<Vector3> pts, Event e, int controlId, Vector3 mouseWorld, bool hitPlane)
    {
        if (!hitPlane) return;

        // 브러시 원
        Handles.color = new Color(0.4f, 1f, 0.5f, 0.15f);
        Handles.DrawSolidDisc(mouseWorld, Vector3.up, BrushRadius);
        Handles.color = new Color(0.4f, 1f, 0.5f, 0.9f);
        Handles.DrawWireDisc(mouseWorld, Vector3.up, BrushRadius, 3f);

        // 영향받는 점 미리보기
        float r2 = BrushRadius * BrushRadius;
        int gx = gen.GridX;
        for (int i = 0; i < pts.Count; i++)
        {
            int px = i % gx;
            int pz = i / gx;
            if (!IsPointActive(gen, px, pz)) continue;

            Vector3 p = pts[i];
            float dist2 = (new Vector2(p.x - mouseWorld.x, p.z - mouseWorld.z)).sqrMagnitude;
            if (dist2 > r2) continue;

            float falloff = Mathf.Pow(1f - Mathf.Sqrt(dist2) / BrushRadius, BrushFalloff);
            Handles.color = new Color(1f, 1f, 0.3f, falloff * 0.9f);
            Handles.SphereHandleCap(0, p + Vector3.up * 0.3f,
                Quaternion.identity,
                HandleUtility.GetHandleSize(p) * 0.06f * (0.5f + falloff),
                EventType.Repaint);
        }

        // Ctrl+휠 = 반경
        if (e.type == EventType.ScrollWheel && e.control)
        {
            BrushRadius = Mathf.Clamp(BrushRadius - e.delta.y * 2f, 5f, 200f);
            e.Use();
        }

        // 드래그 시작
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            brushOriginalPositions = new Dictionary<int, Vector3>();
            affectedDuringDrag.Clear();

            for (int i = 0; i < pts.Count; i++)
            {
                int px = i % gx;
                int pz = i / gx;
                if (!IsPointActive(gen, px, pz)) continue;

                Vector3 p = pts[i];
                float dist2 = (new Vector2(p.x - mouseWorld.x, p.z - mouseWorld.z)).sqrMagnitude;
                if (dist2 > r2) continue;

                brushOriginalPositions[i] = p;
                affectedDuringDrag.Add(new Vector2Int(px, pz));
            }
            dragStartWorld = mouseWorld;

            if (brushOriginalPositions.Count > 0)
            {
                Undo.RecordObject(gen, "Warp Grid Brush");
                GUIUtility.hotControl = controlId;
                e.Use();
            }
        }

        // 드래그 중
        if (e.type == EventType.MouseDrag && brushOriginalPositions != null)
        {
            Vector3 delta = mouseWorld - dragStartWorld;

            foreach (var kv in brushOriginalPositions)
            {
                int idx = kv.Key;
                Vector3 origin = kv.Value;

                float dist = Vector2.Distance(
                    new Vector2(origin.x, origin.z),
                    new Vector2(dragStartWorld.x, dragStartWorld.z));

                float falloff = Mathf.Pow(
                    Mathf.Clamp01(1f - dist / BrushRadius),
                    BrushFalloff);

                Vector3 newPos = origin + new Vector3(delta.x, 0, delta.z) * falloff;
                int px = idx % gx;
                int pz = idx / gx;
                gen.SetGridPoint(px, pz, newPos);
            }
            EditorUtility.SetDirty(gen);

            if (EditorApplication.timeSinceStartup - lastRebuildTime > REBUILD_DEBOUNCE)
                FlushDirty();
            e.Use();
        }

        // 드래그 끝
        if (e.type == EventType.MouseUp && brushOriginalPositions != null)
        {
            FlushDirty();
            brushOriginalPositions = null;
            GUIUtility.hotControl = 0;
            e.Use();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static bool IsPointActive(TownGenerator gen, int x, int z)
    {
        // 그리드 포인트가 활성 블록과 인접해 있는지
        var active = gen.ActiveBlocks;
        if (active == null) return false;
        int gxLim = gen.GridX - 1;
        int gzLim = gen.GridZ - 1;

        for (int dx = -1; dx <= 0; dx++)
        for (int dz = -1; dz <= 0; dz++)
        {
            int bx = x + dx, bz = z + dz;
            if (bx < 0 || bx >= gxLim) continue;
            if (bz < 0 || bz >= gzLim) continue;
            if (active[bx, bz]) return true;
        }
        return false;
    }

    static int FindNearestPoint(List<Vector3> pts, Vector3 worldPos, out float dist)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < pts.Count; i++)
        {
            float d = Vector3.Distance(pts[i], worldPos);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }
        dist = bestDist;
        return best;
    }

    static void FlushDirty()
    {
        if (activeGen == null) return;
        if (affectedDuringDrag.Count == 0) return;

        activeGen.RebuildAffectedBlocks(new HashSet<Vector2Int>(affectedDuringDrag));
        lastRebuildTime = EditorApplication.timeSinceStartup;
        // 누적된 영향 점은 그대로 유지 (계속 드래그 시 추가됨)
    }

    static void DrawHUD(TownGenerator gen)
    {
        Handles.BeginGUI();
        var bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = MakeBg(new Color(0, 0, 0, 0.7f));
        bgStyle.normal.textColor = Color.white;
        bgStyle.padding = new RectOffset(10, 10, 6, 6);
        bgStyle.fontSize = 11;

        string mode = CurrentMode == Mode.Handle ? "🤚 핸들 모드" : "🖌️ 브러시 모드";
        string text = $"🌀 그리드 워프 - {mode}\n";

        if (CurrentMode == Mode.Handle)
            text += "빨간 점 클릭+드래그 = 그 점만 이동";
        else
            text += $"드래그 = 반경 내 점들 이동\n반경: {BrushRadius:F0}m   (Ctrl+휠로 조절)";

        var rect = new Rect(10, 10, 320, CurrentMode == Mode.Brush ? 70 : 50);
        GUI.Box(rect, text, bgStyle);
        Handles.EndGUI();
    }

    static Texture2D _bg;
    static Color _bgC;
    static Texture2D MakeBg(Color c)
    {
        if (_bg != null && _bgC == c) return _bg;
        _bg = new Texture2D(1, 1);
        _bg.SetPixel(0, 0, c);
        _bg.Apply();
        _bg.hideFlags = HideFlags.HideAndDontSave;
        _bgC = c;
        return _bg;
    }
}