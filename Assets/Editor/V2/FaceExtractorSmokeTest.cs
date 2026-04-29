using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TownGen.V2;

namespace TownGen.V2.EditorTools
{
    public static class FaceExtractorSmokeTest
    {
        [MenuItem("TownGen V2/Tests/Face Extractor — Triangle")]
        public static void TestTriangle()
        {
            var g = new RoadGraph();
            var n1 = g.AddNode(new Vector3(0, 0, 1));
            var n2 = g.AddNode(new Vector3(0, 0, 0));
            var n3 = g.AddNode(new Vector3(1, 0, 0));
            g.AddEdge(n1.id, n2.id);
            g.AddEdge(n2.id, n3.id);
            g.AddEdge(n3.id, n1.id);

            var faces = FaceExtractor.Extract(g);
            Report("Triangle", faces, expectedInner: 1);
        }

        [MenuItem("TownGen V2/Tests/Face Extractor — Square+Diagonal")]
        public static void TestSquareDiagonal()
        {
            var g = new RoadGraph();
            var n1 = g.AddNode(new Vector3(0, 0, 1));
            var n2 = g.AddNode(new Vector3(1, 0, 1));
            var n3 = g.AddNode(new Vector3(1, 0, 0));
            var n4 = g.AddNode(new Vector3(0, 0, 0));
            g.AddEdge(n1.id, n2.id);
            g.AddEdge(n2.id, n3.id);
            g.AddEdge(n3.id, n4.id);
            g.AddEdge(n4.id, n1.id);
            g.AddEdge(n1.id, n3.id);    // 대각선

            var faces = FaceExtractor.Extract(g);
            Report("Square+Diagonal", faces, expectedInner: 2);
        }

        [MenuItem("TownGen V2/Tests/Face Extractor — T-Shape (Dead End)")]
        public static void TestTShape()
        {
            var g = new RoadGraph();
            var n1 = g.AddNode(new Vector3(0, 0, 0));
            var n2 = g.AddNode(new Vector3(1, 0, 0));
            var n3 = g.AddNode(new Vector3(2, 0, 0));
            var n4 = g.AddNode(new Vector3(1, 0, -1));
            g.AddEdge(n1.id, n2.id);
            g.AddEdge(n2.id, n3.id);
            g.AddEdge(n2.id, n4.id);    // 막다른 가지

            var faces = FaceExtractor.Extract(g);
            Report("T-Shape", faces, expectedInner: 0);
        }

        [MenuItem("TownGen V2/Tests/Face Extractor — 2x2 Grid")]
        public static void Test2x2Grid()
        {
            var g = new RoadGraph();
            // 3x3 노드 격자 → 4개의 내부 face
            var ns = new RoadNode[3, 3];
            for (int x = 0; x < 3; x++)
                for (int z = 0; z < 3; z++)
                    ns[x, z] = g.AddNode(new Vector3(x, 0, z));

            for (int x = 0; x < 3; x++)
                for (int z = 0; z < 3; z++)
                {
                    if (x < 2) g.AddEdge(ns[x, z].id, ns[x + 1, z].id);
                    if (z < 2) g.AddEdge(ns[x, z].id, ns[x, z + 1].id);
                }

            var faces = FaceExtractor.Extract(g);
            Report("2x2 Grid", faces, expectedInner: 4);
        }

        // ─────────── 출력 ───────────
        static void Report(string name, List<FaceResult> faces, int expectedInner)
        {
            int inner = 0, outer = 0;
            foreach (var f in faces) { if (f.isOuter) outer++; else inner++; }
            string status = (inner == expectedInner) ? "✅ PASS" : "❌ FAIL";
            Debug.Log($"[FaceTest] {name}: inner={inner}, outer={outer} (expected inner={expectedInner}) {status}");
            for (int i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                string nodes = string.Join(",", f.nodeIds);
                Debug.Log($"  face[{i}] {(f.isOuter ? "OUTER" : "inner")} area={f.signedArea:F3} nodes=[{nodes}]");
            }
        }
    }
}