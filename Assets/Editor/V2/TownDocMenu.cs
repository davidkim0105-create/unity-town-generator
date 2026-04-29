using UnityEditor;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    public static class TownDocMenu
    {
        const string QUICK_PATH = "Assets/_TownDoc_quick.json";

        // ─────────── Save ───────────

        [MenuItem("TownGen V2/Document/Save Selected RoadGraph...")]
        public static void SaveSelected()
        {
            var auth = GetSelectedAuthoring();
            if (auth == null)
            {
                EditorUtility.DisplayDialog("TownDoc", "Select a RoadGraph object first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Save Town Document", Application.dataPath, "town_v2.json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var doc = BuildDocFromScene(auth);
            TownDocumentIO.Save(doc, path);
            AssetDatabase.Refresh();
        }

        [MenuItem("TownGen V2/Document/Quick Save (project)")]
        public static void QuickSave()
        {
            var auth = GetSelectedAuthoring();
            if (auth == null) return;
            var doc = BuildDocFromScene(auth);
            TownDocumentIO.Save(doc, QUICK_PATH);
            AssetDatabase.Refresh();
        }

        // ─────────── Load (자동 Build 포함) ───────────

        [MenuItem("TownGen V2/Document/Load Into Selected RoadGraph...")]
        public static void LoadIntoSelected()
        {
            var auth = GetSelectedAuthoring();
            if (auth == null)
            {
                EditorUtility.DisplayDialog("TownDoc", "Select a RoadGraph object first.", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel(
                "Load Town Document", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return;

            LoadAndApply(auth, path);
        }

        [MenuItem("TownGen V2/Document/Quick Load (project)")]
        public static void QuickLoad()
        {
            var auth = GetSelectedAuthoring();
            if (auth == null) return;
            LoadAndApply(auth, QUICK_PATH);
        }

        static void LoadAndApply(RoadGraphAuthoring auth, string path)
        {
            var doc = TownDocumentIO.Load(path);
            if (doc == null) return;

            Undo.RegisterFullObjectHierarchyUndo(auth.gameObject, "Load Town Document");
            ApplyDocToScene(auth, doc);

            // 자동 Build
            var regions = FindRegionFor(auth);
            BlockBuilder.RebuildAllBlocks(auth, auth.roadInset, regions: regions, blockThickness: auth.blockThickness);
            BlockBuilder.FillAllBlocksWithBuildings(auth, auth.buildSettings, regions);

            EditorUtility.SetDirty(auth);
            SceneView.RepaintAll();
        }

        // ─────────── Doc <-> Scene ───────────

        public static TownDocumentV2 BuildDocFromScene(RoadGraphAuthoring auth)
        {
            var doc = new TownDocumentV2();
            doc.graph = CloneGraph(auth.graph);

            doc.buildSettings = auth.buildSettings;
            doc.blockThickness = auth.blockThickness;
            doc.roadInset = auth.roadInset;

            var ra = FindRegionFor(auth);
            if (ra != null)
            {
                foreach (var r in ra.regions) doc.regions.Add(CloneRegion(r));
                doc.defaultRegion = CloneRegion(ra.defaultRegion);
            }
            return doc;
        }

        public static void ApplyDocToScene(RoadGraphAuthoring auth, TownDocumentV2 doc)
        {
            auth.graph = CloneGraph(doc.graph);
            auth.graph.InvalidateCache();
            auth.InvalidateFaceCache();

            auth.buildSettings = doc.buildSettings;
            auth.blockThickness = doc.blockThickness;
            auth.roadInset = doc.roadInset;

            // sanity (옛 파일 호환)
            if (auth.buildSettings.lotSize <= 0f) auth.buildSettings = BuildingFiller.DefaultSettings;
            if (auth.roadInset <= 0f) auth.roadInset = 1.5f;

            var ra = FindRegionFor(auth);
            if (ra != null)
            {
                ra.regions.Clear();
                foreach (var r in doc.regions) ra.regions.Add(CloneRegion(r));
                if (doc.defaultRegion != null) ra.defaultRegion = CloneRegion(doc.defaultRegion);
                EditorUtility.SetDirty(ra);
            }
        }

        // ─────────── helpers ───────────

        static RoadGraphAuthoring GetSelectedAuthoring()
        {
            var go = Selection.activeGameObject;
            if (go == null) return null;
            return go.GetComponent<RoadGraphAuthoring>()
                   ?? go.GetComponentInParent<RoadGraphAuthoring>()
                   ?? go.GetComponentInChildren<RoadGraphAuthoring>();
        }

        public static RegionAuthoring FindRegionFor(RoadGraphAuthoring auth)
        {
            var r = auth.GetComponentInChildren<RegionAuthoring>();
            if (r == null) r = Object.FindAnyObjectByType<RegionAuthoring>();
            return r;
        }

        // ─────────── 깊은 복사 ───────────

        static RoadGraph CloneGraph(RoadGraph src)
        {
            var dst = new RoadGraph();
            if (src == null) return dst;
            foreach (var n in src.nodes)
            {
                var nn = new RoadNode(n.id, n.position) { isFixed = n.isFixed };
                nn.edgeIds = new System.Collections.Generic.List<int>(n.edgeIds);
                dst.nodes.Add(nn);
            }
            foreach (var e in src.edges)
            {
                var ne = new RoadEdge(e.id, e.nodeAId, e.nodeBId) { width = e.width, type = e.type };
                dst.edges.Add(ne);
            }
            dst.nextNodeId = src.nextNodeId;
            dst.nextEdgeId = src.nextEdgeId;
            return dst;
        }

        static TownRegionV2 CloneRegion(TownRegionV2 src)
        {
            if (src == null) return new TownRegionV2();
            return new TownRegionV2
            {
                name = src.name,
                color = src.color,
                polygon = new System.Collections.Generic.List<Vector3>(src.polygon),
                lotSize = src.lotSize,
                lotMargin = src.lotMargin,
                minHeight = src.minHeight,
                maxHeight = src.maxHeight,
                density = src.density,
            };
        }
    }
}