using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Block Inspector", typeof(RoadGraphAuthoring))]
    public class BlockInspectorTool : EditorTool
    {
        Vector3 currentMouse;
        TownBlockV2 hoveredBlock;

        public override GUIContent toolbarIcon =>
            new GUIContent("🔍", "Block Inspector — 블록 위에 마우스로 정보 표시");

        public override void OnToolGUI(EditorWindow window)
        {
            var auth = target as RoadGraphAuthoring;
            if (auth == null) return;
            var sv = window as SceneView;
            if (sv == null) return;

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            if (!RaycastGroundPlane(e.mousePosition, out currentMouse))
                return;

            hoveredBlock = FindBlockAt(auth, currentMouse);

            // 클릭하면 그 블록 GameObject 선택
            if (e.GetTypeForControl(controlId) == EventType.MouseDown && e.button == 0 && hoveredBlock != null)
            {
                Selection.activeGameObject = hoveredBlock.gameObject;
                EditorGUIUtility.PingObject(hoveredBlock.gameObject);
                e.Use();
            }

            DrawHandles();
            DrawOverlay(auth);
            sv.Repaint();
        }

        TownBlockV2 FindBlockAt(RoadGraphAuthoring auth, Vector3 p)
        {
            var blocksRoot = auth.transform.Find("_Blocks");
            if (blocksRoot == null) return null;

            TownBlockV2 best = null;
            float bestArea = float.MaxValue;
            foreach (Transform child in blocksRoot)
            {
                var b = child.GetComponent<TownBlockV2>();
                if (b == null || b.polygon == null || b.polygon.Count < 3) continue;
                if (PointInPolygon2D(p, b.polygon))
                {
                    float a = Mathf.Abs(SignedArea2D(b.polygon));
                    if (a < bestArea) { bestArea = a; best = b; }
                }
            }
            return best;
        }

        void DrawHandles()
        {
            if (hoveredBlock == null || hoveredBlock.polygon == null) return;
            Handles.color = new Color(0.3f, 0.8f, 1f, 0.4f);
            Handles.DrawAAConvexPolygon(hoveredBlock.polygon.ToArray());
            Handles.color = new Color(0.3f, 0.8f, 1f, 1f);
            for (int i = 0; i < hoveredBlock.polygon.Count; i++)
            {
                Vector3 a = hoveredBlock.polygon[i];
                Vector3 b = hoveredBlock.polygon[(i + 1) % hoveredBlock.polygon.Count];
                Handles.DrawAAPolyLine(3f, a, b);
            }
        }

        void DrawOverlay(RoadGraphAuthoring auth)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 300, 130), GUI.skin.box);
            GUILayout.Label("🔍 Block Inspector", EditorStyles.boldLabel);

            if (hoveredBlock != null)
            {
                float area = Mathf.Abs(SignedArea2D(hoveredBlock.polygon));
                int verts = hoveredBlock.polygon.Count;
                int buildings = hoveredBlock.transform.childCount;

                GUILayout.Label($"Name: {hoveredBlock.gameObject.name}");
                GUILayout.Label($"Vertices: {verts}   Area: {area:F1} m²");
                GUILayout.Label($"Buildings: {buildings}");

                // Region 매칭 (있으면)
                var regionAuth = auth.GetComponentInChildren<RegionAuthoring>()
                                 ?? Object.FindAnyObjectByType<RegionAuthoring>();
                if (regionAuth != null)
                {
                    var center = ComputeCentroid(hoveredBlock.polygon);
                    var r = regionAuth.ResolveRegionAt(center);
                    string regionName = (r == null || r == regionAuth.defaultRegion)
                        ? "(default)"
                        : r.name;
                    GUILayout.Label($"Region: {regionName}");
                }

                GUILayout.Label("Click to select this block GameObject", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("Hover over a block to see info", EditorStyles.miniLabel);
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // ─── helpers ───
        static bool PointInPolygon2D(Vector3 p, List<Vector3> poly)
        {
            int n = poly.Count;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = poly[i].x, zi = poly[i].z;
                float xj = poly[j].x, zj = poly[j].z;
                if (((zi > p.z) != (zj > p.z)) &&
                    (p.x < (xj - xi) * (p.z - zi) / (zj - zi + 1e-9f) + xi))
                    inside = !inside;
            }
            return inside;
        }

        static float SignedArea2D(List<Vector3> poly)
        {
            float s = 0; int n = poly.Count;
            for (int i = 0; i < n; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % n];
                s += a.x * b.z - b.x * a.z;
            }
            return s * 0.5f;
        }

        static Vector3 ComputeCentroid(List<Vector3> poly)
        {
            Vector3 sum = Vector3.zero;
            foreach (var p in poly) sum += p;
            return sum / poly.Count;
        }

        static bool RaycastGroundPlane(Vector2 mp, out Vector3 wp)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mp);
            Plane g = new Plane(Vector3.up, Vector3.zero);
            if (g.Raycast(ray, out float t)) { wp = ray.GetPoint(t); return true; }
            wp = default; return false;
        }
    }
}