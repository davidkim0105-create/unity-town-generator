using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    public static class ToolShortcuts
    {
        const ShortcutModifiers MOD = ShortcutModifiers.Shift;

        [Shortcut("TownGen V2/Tool: Move",    KeyCode.Q, MOD)]
        static void ActivateMove()    => SafeActivate<NodeMoveTool>();

        [Shortcut("TownGen V2/Tool: Draw",    KeyCode.W, MOD)]
        static void ActivateDraw()    => SafeActivate<RoadDrawTool>();

        [Shortcut("TownGen V2/Tool: Cut",     KeyCode.E, MOD)]
        static void ActivateCut()     => SafeActivate<RoadCutTool>();

        [Shortcut("TownGen V2/Tool: Delete",  KeyCode.R, MOD)]
        static void ActivateDelete()  => SafeActivate<RoadDeleteTool>();

        [Shortcut("TownGen V2/Tool: Connect", KeyCode.T, MOD)]
        static void ActivateConnect() => SafeActivate<NodeConnectTool>();

        static void SafeActivate<T>() where T : EditorTool
        {
            var go = Selection.activeGameObject;
            var auth = go != null ? go.GetComponentInParent<RoadGraphAuthoring>() : null;
            if (auth == null) auth = Object.FindAnyObjectByType<RoadGraphAuthoring>();
            if (auth == null)
            {
                Debug.LogWarning("[TownGen V2] No RoadGraph in scene; cannot activate tool.");
                return;
            }
            Selection.activeGameObject = auth.gameObject;
            EditorApplication.delayCall += () => ToolManager.SetActiveTool<T>();
        }
    }
}