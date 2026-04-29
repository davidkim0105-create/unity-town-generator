using UnityEditor;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    [InitializeOnLoad]
    public static class RegionGizmos
    {
        static RegionGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sv)
        {
            var auths = Object.FindObjectsByType<RegionAuthoring>(FindObjectsSortMode.None);
            foreach (var a in auths) Draw(a);
        }

        static void Draw(RegionAuthoring a)
        {
            if (a == null || !a.showPolygons || a.regions == null) return;
            var prev = Handles.matrix;
            Handles.matrix = a.transform.localToWorldMatrix;

            foreach (var r in a.regions)
            {
                if (r.polygon == null || r.polygon.Count < 2) continue;

                Color c = r.color;
                c.a = 0.95f;
                Handles.color = c;
                int n = r.polygon.Count;
                for (int i = 0; i < n; i++)
                {
                    Vector3 p1 = r.polygon[i] + Vector3.up * a.lift;
                    Vector3 p2 = r.polygon[(i + 1) % n] + Vector3.up * a.lift;
                    Handles.DrawAAPolyLine(3f, p1, p2);
                }

                if (r.polygon.Count >= 3)
                {
                    Color fill = r.color;
                    fill.a = 0.12f;
                    Handles.color = fill;
                    var verts = new Vector3[r.polygon.Count];
                    for (int i = 0; i < verts.Length; i++)
                        verts[i] = r.polygon[i] + Vector3.up * a.lift;
                    Handles.DrawAAConvexPolygon(verts);
                }

                Handles.color = c;
                foreach (var p in r.polygon)
                    Handles.SphereHandleCap(0, p + Vector3.up * a.lift,
                        Quaternion.identity, 0.4f, EventType.Repaint);

                if (r.polygon.Count > 0)
                {
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.normal.textColor = Color.white;
                    Handles.Label(r.polygon[0] + Vector3.up * (a.lift + 1f), r.name, style);
                }
            }

            Handles.matrix = prev;
        }
    }
}