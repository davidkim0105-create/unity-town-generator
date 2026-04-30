using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    public static class ToolShortcuts
    {
        const ShortcutModifiers MOD = ShortcutModifiers.Shift;

        // ── 편집 도구 (글자 키) ──
        [Shortcut("TownGen V2/Tool: Move",        KeyCode.Q, MOD)]
        static void ActivateMove()       => SafeToggle<NodeMoveTool>();

        [Shortcut("TownGen V2/Tool: Draw",        KeyCode.W, MOD)]
        static void ActivateDraw()       => SafeToggle<RoadDrawTool>();

        [Shortcut("TownGen V2/Tool: Cut",         KeyCode.E, MOD)]
        static void ActivateCut()        => SafeToggle<RoadCutTool>();

        [Shortcut("TownGen V2/Tool: Delete",      KeyCode.R, MOD)]
        static void ActivateDelete()     => SafeToggle<RoadDeleteTool>();

        [Shortcut("TownGen V2/Tool: Connect",     KeyCode.T, MOD)]
        static void ActivateConnect()    => SafeToggle<NodeConnectTool>();

        // ── 생성/편집 도구 (Z X C V) ──
        [Shortcut("TownGen V2/Tool: Brush",       KeyCode.Z, MOD)]
        static void ActivateBrush()      => SafeToggle<RoadBrushTool>();

        [Shortcut("TownGen V2/Tool: Subdivide",   KeyCode.X, MOD)]
        static void ActivateSubdivide()  => SafeToggle<SubdivideTool>();

        [Shortcut("TownGen V2/Tool: L-System",    KeyCode.C, MOD)]
        static void ActivateLSystem()    => SafeToggle<LSystemGrowTool>();

        [Shortcut("TownGen V2/Tool: Width Brush", KeyCode.V, MOD)]
        static void ActivateWidthBrush() => SafeToggle<WidthBrushTool>();

        // ── 전역 해제 ──
        [Shortcut("TownGen V2/Tool: Deactivate",  KeyCode.Escape, MOD)]
        static void DeactivateAll()
        {
            EditorApplication.delayCall += () => Tools.current = Tool.View;
        }

        /// <summary>
        /// 같은 도구가 이미 활성이면 해제(View로), 아니면 활성화.
        /// Selection이 도구 활성 시점에 깨져도 자동 복구.
        /// </summary>
        static void SafeToggle<T>() where T : EditorTool
        {
            if (ToolManager.activeToolType == typeof(T))
            {
                EditorApplication.delayCall += () => Tools.current = Tool.View;
                return;
            }

            var auth = ResolveAuth();
            if (auth == null)
            {
                Debug.LogWarning("[TownGen V2] No RoadGraph in scene; cannot activate tool.");
                return;
            }

            Selection.activeGameObject = auth.gameObject;

            EditorApplication.delayCall += () =>
            {
                var auth2 = ResolveAuth();
                if (auth2 == null) return;
                if (Selection.activeGameObject != auth2.gameObject)
                    Selection.activeGameObject = auth2.gameObject;

                try
                {
                    ToolManager.SetActiveTool<T>();
                }
                catch (System.InvalidOperationException ex)
                {
                    Debug.LogWarning($"[TownGen V2] Tool activation failed: {ex.Message}");
                }
            };
        }

        static RoadGraphAuthoring ResolveAuth()
        {
            var go = Selection.activeGameObject;
            var auth = go != null ? go.GetComponentInParent<RoadGraphAuthoring>() : null;
            if (auth == null) auth = Object.FindAnyObjectByType<RoadGraphAuthoring>();
            return auth;
        }
    }
}