using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Region Paint", typeof(RegionAuthoring))]
    public class RegionPaintTool : EditorTool
    {
        static GUIContent s_Icon;
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_Icon == null)
                    s_Icon = new GUIContent(
                        EditorGUIUtility.IconContent("d_Grid.PaintTool").image,
                        "Region Paint — L-click: add point, R-click: remove last, N: next region, ESC: exit");
                return s_Icon;
            }
        }

        [SerializeField] int activeRegionIndex = 0;

        public override void OnToolGUI(EditorWindow window)
        {
            var a = target as RegionAuthoring;
            if (a == null) return;

            Event e = Event.current;
            int passiveId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(passiveId);

            if (a.regions.Count == 0)
            {
                Handles.BeginGUI();
                GUI.Label(new Rect(10, 10, 700, 40),
                    "Region Paint: no regions defined. Add one in inspector first.",
                    EditorStyles.boldLabel);
                Handles.EndGUI();
                return;
            }
            activeRegionIndex = Mathf.Clamp(activeRegionIndex, 0, a.regions.Count - 1);

            // 마우스 평면 투영
            var tr = a.transform;
            Plane plane = new Plane(tr.up, tr.position);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hasMouse = plane.Raycast(ray, out float dist);
            Vector3 mouseLocal = Vector3.zero;
            if (hasMouse) mouseLocal = tr.InverseTransformPoint(ray.GetPoint(dist));

            var region = a.regions[activeRegionIndex];

            // ─────────── 좌클릭: 점 추가 ───────────
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && hasMouse)
            {
                Undo.RegisterCompleteObjectUndo(a, "Add Region Point");
                region.polygon.Add(mouseLocal);
                EditorUtility.SetDirty(a);
                GUIUtility.hotControl = passiveId;
                e.Use();
            }

            // ─────────── 우클릭: 마지막 점 제거 ───────────
            if (e.type == EventType.MouseDown && e.button == 1 && !e.alt)
            {
                if (region.polygon.Count > 0)
                {
                    Undo.RegisterCompleteObjectUndo(a, "Remove Region Point");
                    region.polygon.RemoveAt(region.polygon.Count - 1);
                    EditorUtility.SetDirty(a);
                }
                GUIUtility.hotControl = passiveId;
                e.Use();
            }

            // 마우스 업 → 점유 해제
            if (e.type == EventType.MouseUp &&
                (e.button == 0 || e.button == 1) &&
                GUIUtility.hotControl == passiveId)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }

            // ─────────── Tab 또는 N: 다음 Region (순환) ───────────
            bool nextKeyDown =
                (e.type == EventType.KeyDown &&
                 (e.keyCode == KeyCode.Tab || (e.keyCode == KeyCode.N && !e.shift)));
            bool nextKeyUp =
                (e.type == EventType.KeyUp &&
                 (e.keyCode == KeyCode.Tab || e.keyCode == KeyCode.N));

            if (nextKeyDown)
            {
                activeRegionIndex = (activeRegionIndex + 1) % a.regions.Count;
                EditorUtility.SetDirty(a);
                SceneView.RepaintAll();
                e.Use();
            }
            if (nextKeyUp) e.Use();   // KeyUp까지 막아 Tab의 포커스 이동 차단

            // ─────────── Shift+N: 이전 Region ───────────
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.N && e.shift)
            {
                activeRegionIndex = (activeRegionIndex - 1 + a.regions.Count) % a.regions.Count;
                EditorUtility.SetDirty(a);
                SceneView.RepaintAll();
                e.Use();
            }

            // ─────────── ESC: 도구 종료 ───────────
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                ToolManager.RestorePreviousTool();
                e.Use();
            }

            // ─────────── 미리보기 ───────────
            if (hasMouse)
            {
                var prev = Handles.matrix;
                Handles.matrix = tr.localToWorldMatrix;
                Handles.color = region.color;
                Handles.SphereHandleCap(0, mouseLocal, Quaternion.identity, 0.5f, EventType.Repaint);

                if (region.polygon.Count > 0)
                {
                    var last = region.polygon[region.polygon.Count - 1];
                    Handles.DrawDottedLine(last + Vector3.up * 0.05f,
                        mouseLocal + Vector3.up * 0.05f, 4f);
                }
                Handles.matrix = prev;
            }

            // ─────────── 상태 라벨 ───────────
            Handles.BeginGUI();
            GUI.Label(new Rect(10, 10, 800, 20),
                $"Region Paint — active: '{region.name}' ({activeRegionIndex + 1}/{a.regions.Count})",
                EditorStyles.boldLabel);
            GUI.Label(new Rect(10, 30, 800, 20),
                "L-click=add point   R-click=remove last   N=next region   Shift+N=prev   ESC=exit",
                EditorStyles.miniLabel);
            GUI.Label(new Rect(10, 48, 800, 20),
                "(Tip: 키 단축키는 Scene 뷰를 한 번 클릭한 뒤 작동)",
                EditorStyles.miniLabel);
            Handles.EndGUI();

            window.Repaint();
        }
    }
}