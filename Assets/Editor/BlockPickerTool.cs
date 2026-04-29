using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class BlockPickerTool
{
    public static bool IsActive { get; private set; }
    private static TownGenerator activeGenerator;

    public static void Activate(TownGenerator gen)
    {
        if (IsActive) return;
        if (DensityBrushTool.IsActive) DensityBrushTool.Deactivate();
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
        SceneView.RepaintAll();
    }

    static void OnSceneGUI(SceneView sv)
    {
        if (activeGenerator == null) { Deactivate(); return; }

        Event e = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (!ground.Raycast(ray, out float dist)) return;
        Vector3 worldPos = ray.GetPoint(dist);

        TownBlock hovered = FindBlockAt(worldPos);

        if (hovered != null)
        {
            DrawBlockHighlight(hovered, new Color(1f, 0.9f, 0.3f, 0.4f));
        }

        DrawHUD(hovered);

        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.GetTypeForControl(controlId))
        {
            case EventType.Layout:
                HandleUtility.AddDefaultControl(controlId);
                break;

            case EventType.MouseDown:
                if (e.button == 0 && hovered != null)
                {
                    HandleClick(hovered, e);
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0 && GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
        }

        SceneView.RepaintAll();
    }

    static void HandleClick(TownBlock block, Event e)
    {
        var current = new List<Object>(Selection.objects);

        if (e.control || e.command)
        {
            if (current.Contains(block.gameObject))
                current.Remove(block.gameObject);
            else
                current.Add(block.gameObject);
            Selection.objects = current.ToArray();
        }
        else if (e.shift && Selection.activeGameObject != null)
        {
            if (!current.Contains(block.gameObject))
                current.Add(block.gameObject);
            Selection.objects = current.ToArray();
        }
        else
        {
            Selection.activeGameObject = block.gameObject;
        }
        EditorGUIUtility.PingObject(block.gameObject);
    }

    static TownBlock FindBlockAt(Vector3 worldPos)
    {
        if (activeGenerator == null) return null;
        Transform root = activeGenerator.transform.Find("Blocks");
        if (root == null) return null;

        TownBlock best = null;
        float bestDist = float.MaxValue;

        foreach (Transform t in root)
        {
            var blk = t.GetComponent<TownBlock>();
            if (blk == null) continue;

            Vector3 local = t.InverseTransformPoint(worldPos);
            float halfW = blk.outerRect.width * 0.5f;
            float halfD = blk.outerRect.height * 0.5f;

            if (Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.z) <= halfD)
            {
                float d = Vector3.Distance(t.position, worldPos);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = blk;
                }
            }
        }
        return best;
    }

    static void DrawBlockHighlight(TownBlock blk, Color color)
    {
        Transform t = blk.transform;
        float halfW = blk.outerRect.width * 0.5f;
        float halfD = blk.outerRect.height * 0.5f;

        Vector3 c1 = t.TransformPoint(new Vector3(-halfW, 0.7f, -halfD));
        Vector3 c2 = t.TransformPoint(new Vector3( halfW, 0.7f, -halfD));
        Vector3 c3 = t.TransformPoint(new Vector3( halfW, 0.7f,  halfD));
        Vector3 c4 = t.TransformPoint(new Vector3(-halfW, 0.7f,  halfD));

        Handles.color = color;
        Handles.DrawAAConvexPolygon(c1, c2, c3, c4);

        Color line = color; line.a = 1f;
        Handles.color = line;
        Handles.DrawAAPolyLine(4f, c1, c2, c3, c4, c1);
    }

    static void DrawHUD(TownBlock hovered)
    {
        Handles.BeginGUI();

        var bgStyle = new GUIStyle(GUI.skin.box);
        bgStyle.normal.background = MakeBgTex(new Color(0, 0, 0, 0.7f));
        bgStyle.normal.textColor = Color.white;
        bgStyle.padding = new RectOffset(10, 10, 6, 6);
        bgStyle.fontSize = 11;

        string text = "블록 클릭 모드 ON\n클릭=선택  /  Ctrl+클릭=토글  /  Shift+클릭=추가";
        if (hovered != null)
        {
            text += "\n\nHover: (" + hovered.gridX + ", " + hovered.gridZ + ")  밀도 " + hovered.EffectiveDensity.ToString("F2");
        }

        var rect = new Rect(10, 10, 320, hovered != null ? 70 : 50);
        GUI.Box(rect, text, bgStyle);

        Handles.EndGUI();
    }

    static Texture2D _bgTex;
    static Color _bgTexColor;
    static Texture2D MakeBgTex(Color c)
    {
        if (_bgTex != null && _bgTexColor == c) return _bgTex;
        _bgTex = new Texture2D(1, 1);
        _bgTex.SetPixel(0, 0, c);
        _bgTex.Apply();
        _bgTex.hideFlags = HideFlags.HideAndDontSave;
        _bgTexColor = c;
        return _bgTex;
    }
}