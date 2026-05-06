using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    [EditorTool("Voronoi Region", typeof(RoadGraphAuthoring))]
    public class VoronoiTool : EditorTool
    {
        public enum CellTypeMode { OnePerSeed, CycleRegions, RandomRegions, AllSame }

        public static List<Vector3> seeds = new List<Vector3>();
        public static CellTypeMode mode = CellTypeMode.CycleRegions;
        public static float gridResolution = 1.5f;
        public static int smoothingPasses = 1;
        public static float simplifyTolerance = 0.5f;
        public static float boundsPadding = 5f;
        public static int seedSeed = 12345;
        public static int randomSeedCount = 5;

        Vector3 currentMouse;

        public override GUIContent toolbarIcon =>
            new GUIContent("◇", "Voronoi Region — 시드 점 클릭 → 자동 영역 분할");

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

            switch (e.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && !e.alt)
                    {
                        seeds.Add(currentMouse);
                        e.Use();
                    }
                    else if (e.button == 1)  // 우클릭: 가장 가까운 시드 삭제
                    {
                        RemoveNearest(currentMouse, 3f);
                        e.Use();
                    }
                    break;

                case EventType.ScrollWheel:
                    if (e.control)
                    {
                        gridResolution = Mathf.Clamp(gridResolution + (-e.delta.y * 0.1f), 0.3f, 5f);
                        e.Use();
                    }
                    break;
            }

            DrawHandles();
            DrawOverlay();
            sv.Repaint();
        }

        void RemoveNearest(Vector3 p, float maxDist)
        {
            int best = -1;
            float bestD = maxDist;
            for (int i = 0; i < seeds.Count; i++)
            {
                float d = Vector3.Distance(seeds[i], p);
                if (d < bestD) { bestD = d; best = i; }
            }
            if (best >= 0) seeds.RemoveAt(best);
        }

        void DrawHandles()
        {
            // 시드 점들
            for (int i = 0; i < seeds.Count; i++)
            {
                Color c = HSV(i / Mathf.Max(1f, seeds.Count));
                Handles.color = c;
                Handles.SphereHandleCap(0, seeds[i], Quaternion.identity, 1.2f, EventType.Repaint);
                Handles.Label(seeds[i] + Vector3.up * 1.5f, $"#{i}");
            }

            // 커서
            Handles.color = new Color(1f, 1f, 0.3f, 0.7f);
            Handles.SphereHandleCap(0, currentMouse, Quaternion.identity, 0.5f, EventType.Repaint);
        }

        void DrawOverlay()
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 320, 130), GUI.skin.box);
            GUILayout.Label("◇ Voronoi Region", EditorStyles.boldLabel);
            GUILayout.Label($"Seeds: {seeds.Count}   Mode: {mode}");
            GUILayout.Label($"Grid: {gridResolution:F2}m   Smooth: {smoothingPasses}");
            GUILayout.Label($"Simplify: {simplifyTolerance:F2}m   Padding: {boundsPadding:F1}m");
            GUILayout.Label("Click=add seed · Right-click=remove · Ctrl+Wheel=grid",
                EditorStyles.miniLabel);
            GUILayout.Label("(Use window button to Generate)", EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        // ─── HSV → Color ───
        static Color HSV(float h) => Color.HSVToRGB(Mathf.Repeat(h, 1f), 0.7f, 1f);

        // ─── helpers ───
        static bool RaycastGroundPlane(Vector2 mp, out Vector3 wp)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mp);
            Plane g = new Plane(Vector3.up, Vector3.zero);
            if (g.Raycast(ray, out float t)) { wp = ray.GetPoint(t); return true; }
            wp = default; return false;
        }

        // ─── 윈도우에서 호출하는 정적 동작들 ───

        public static void RandomPlaceSeeds(RoadGraphAuthoring auth, int count)
        {
            if (auth == null || auth.graph == null || auth.graph.nodes.Count == 0) return;
            ComputeBounds(auth, out var min, out var max, boundsPadding * 0.5f);
            var rng = new System.Random(seedSeed);
            seeds.Clear();
            for (int i = 0; i < count; i++)
            {
                float x = (float)(min.x + rng.NextDouble() * (max.x - min.x));
                float z = (float)(min.z + rng.NextDouble() * (max.z - min.z));
                seeds.Add(new Vector3(x, 0f, z));
            }
            seedSeed++;
        }

        public static void GenerateRegions(RoadGraphAuthoring auth, RegionAuthoring region)
        {
            if (auth == null || region == null) return;
            if (seeds.Count == 0)
            {
                Debug.LogWarning("[Voronoi] 시드가 없습니다. Scene에 클릭하거나 [Random Place]를 누르세요.");
                return;
            }

            ComputeBounds(auth, out var min, out var max, boundsPadding);
            var voronoi = VoronoiRegionGenerator.Generate(
                seeds, min, max, gridResolution, smoothingPasses, simplifyTolerance);

            Undo.RecordObject(region, "Generate Voronoi Regions");

            if (mode == CellTypeMode.OnePerSeed)
            {
                // 기존 region 모두 제거하고 시드마다 새 region 생성
                region.regions.Clear();
                for (int i = 0; i < voronoi.polygons.Count; i++)
                {
                    int seedIdx = voronoi.seedIndices[i];
                    var rd = new TownRegionV2
                    {
                        name = $"Cell_{seedIdx}",
                        color = HSV(seedIdx / Mathf.Max(1f, seeds.Count))
                    };
                    foreach (var v in voronoi.polygons[i]) rd.polygon.Add(v);
                    region.regions.Add(rd);
                }
                EditorUtility.SetDirty(region);
                Debug.Log($"[Voronoi] {voronoi.polygons.Count} cells → {region.regions.Count} new regions created.");
                return;
            }

            // 그 외 모드: 기존 region에 매핑 (단일 폴리곤 한계 있음)
            if (region.regions == null || region.regions.Count == 0)
            {
                Debug.LogWarning("[Voronoi] RegionAuthoring에 region 정의가 없습니다. " +
                    "먼저 'Add Residential' 등으로 region을 추가하거나 Mode를 OnePerSeed로 바꾸세요.");
                return;
            }

            // 기존 region들의 폴리곤 비움 (정의는 보존)
            foreach (var r in region.regions) r.polygon.Clear();

            int matched = 0, overwritten = 0;
            var rngLocal = new System.Random(seedSeed);
            for (int i = 0; i < voronoi.polygons.Count; i++)
            {
                int seedIdx = voronoi.seedIndices[i];
                int regionIdx = ResolveRegionIndex(seedIdx, region.regions.Count, rngLocal);
                var rd = region.regions[regionIdx];
                if (rd.polygon.Count > 0) overwritten++;
                rd.polygon.Clear();
                foreach (var v in voronoi.polygons[i]) rd.polygon.Add(v);
                matched++;
            }

            EditorUtility.SetDirty(region);

            string msg = $"[Voronoi] {matched} cells → {region.regions.Count} regions.";
            if (overwritten > 0)
                msg += $"\n⚠️ {overwritten} cells were overwritten (Region당 폴리곤 1개 한계).\n" +
                       "→ Mode를 'OnePerSeed'로 하거나 Region 수를 시드 수만큼 늘리세요.";
            Debug.Log(msg);
        }

        static int ResolveRegionIndex(int seedIdx, int regionCount, System.Random rng)
        {
            switch (mode)
            {
                case CellTypeMode.CycleRegions:  return seedIdx % regionCount;
                case CellTypeMode.RandomRegions: return rng.Next(regionCount);
                case CellTypeMode.AllSame:       return 0;
                default: return 0;
            }
        }

        static void ComputeBounds(RoadGraphAuthoring auth, out Vector3 min, out Vector3 max, float padding)
        {
            min = auth.graph.nodes[0].position;
            max = min;
            foreach (var n in auth.graph.nodes)
            {
                min = Vector3.Min(min, n.position);
                max = Vector3.Max(max, n.position);
            }
            min -= new Vector3(padding, 0, padding);
            max += new Vector3(padding, 0, padding);
        }

        public static void ClearSeeds()
        {
            seeds.Clear();
        }
    }
}