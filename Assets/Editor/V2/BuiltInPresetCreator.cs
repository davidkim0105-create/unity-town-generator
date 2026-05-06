using System.IO;
using UnityEditor;
using UnityEngine;
using TownGen.V2.Presets;

namespace TownGen.V2.EditorTools
{
    public static class BuiltInPresetCreator
    {
        const string ROOT = "Assets/Resources/Presets";

        [MenuItem("TownGen V2/Create Built-in Presets")]
        public static void CreateAll()
        {
            EnsureFolder(ROOT);
            EnsureFolder($"{ROOT}/LSystem");
            EnsureFolder($"{ROOT}/Building");
            EnsureFolder($"{ROOT}/Town");

            CreateLSystemPresets();
            CreateBuildingPresets();
            CreateTownPresets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TownGen V2] Built-in presets created in " + ROOT);
            EditorUtility.RevealInFinder(ROOT + "/LSystem");
        }

        // ─────────── L-System ───────────
        static void CreateLSystemPresets()
        {
            CreateLS("LS_SmallVillage", "작은 마을 (3 iter, 분기 적음)",
                iter: 3, segLen: 8f, branch: 0.15f, angle: 25f, init: 4, width: 2.5f, radius: 35f);

            CreateLS("LS_GridCity", "격자 신도시 (각도 거의 없음)",
                iter: 5, segLen: 12f, branch: 0.30f, angle: 5f, init: 4, width: 3f, radius: 70f);

            CreateLS("LS_Natural", "자연 발생 (구불구불)",
                iter: 5, segLen: 10f, branch: 0.18f, angle: 25f, init: 4, width: 2.5f, radius: 60f);

            CreateLS("LS_Radial", "방사형 (6방향 별)",
                iter: 4, segLen: 12f, branch: 0.20f, angle: 15f, init: 6, width: 3f, radius: 55f);

            CreateLS("LS_BigCity", "거대 도시 (6 iter, 큰 반경)",
                iter: 6, segLen: 11f, branch: 0.25f, angle: 20f, init: 4, width: 3f, radius: 100f);

            CreateLS("LS_LinearTown", "일자 가도 (2방향)",
                iter: 5, segLen: 10f, branch: 0.10f, angle: 5f, init: 2, width: 3f, radius: 80f);
        }

        static void CreateLS(string name, string desc,
            int iter, float segLen, float branch, float angle, int init, float width, float radius)
        {
            var p = ScriptableObject.CreateInstance<LSystemPresetV2>();
            p.description = desc;
            p.settings = new LSystemGenerator.Settings
            {
                iterations = iter,
                segmentLength = segLen,
                segmentLengthJitter = 0.3f,
                angleJitter = angle,
                branchProbability = branch,
                roadWidth = width,
                maxRadius = radius,
                snapDistance = 3f,
                seed = 0,
                initialBranches = init
            };
            SaveAsset(p, $"{ROOT}/LSystem/{name}.asset");
        }

        // ─────────── Building ───────────
        static void CreateBuildingPresets()
        {
            CreateBldg("Bldg_LowResi", "낮은 주거 (1~2층)",
                lot: 4f, depth: 4f, margin: 0.2f, hMin: 3f, hMax: 6f, density: 0.85f, seed: 100);

            CreateBldg("Bldg_MidResi", "중층 주거 (3~5층)",
                lot: 4f, depth: 5f, margin: 0.15f, hMin: 9f, hMax: 15f, density: 0.9f, seed: 200);

            CreateBldg("Bldg_HighCommercial", "고층 상업 (10~25층)",
                lot: 5f, depth: 6f, margin: 0.1f, hMin: 30f, hMax: 75f, density: 0.95f, seed: 300);

            CreateBldg("Bldg_Industrial", "산업단지 (큰 창고)",
                lot: 8f, depth: 10f, margin: 0.1f, hMin: 4f, hMax: 8f, density: 0.6f, seed: 400);

            CreateBldg("Bldg_Sparse", "전원/마을 (듬성)",
                lot: 5f, depth: 5f, margin: 0.4f, hMin: 3f, hMax: 5f, density: 0.5f, seed: 500);

            CreateBldg("Bldg_Megacity", "메가시티 (랜덤 고층)",
                lot: 6f, depth: 7f, margin: 0.15f, hMin: 15f, hMax: 100f, density: 0.95f, seed: 600);
        }

        static void CreateBldg(string name, string desc,
            float lot, float depth, float margin, float hMin, float hMax, float density, int seed)
        {
            var p = ScriptableObject.CreateInstance<BuildingPresetV2>();
            p.description = desc;
            var s = BuildingFiller.DefaultSettings;
            s.lotSize = lot;
            s.lotDepth = depth;
            s.lotMargin = margin;
            s.minHeight = hMin;
            s.maxHeight = hMax;
            s.density = density;
            s.seed = seed;
            p.settings = s;
            SaveAsset(p, $"{ROOT}/Building/{name}.asset");
        }

        // ─────────── Town ───────────
        static void CreateTownPresets()
        {
            CreateTown("Town_Default", "기본 (보도 약간)",
                inset: 1.5f, thick: 0.1f, useEW: true, margin: 0.5f);

            CreateTown("Town_NoSidewalk", "보도 없음 (구도심/상가)",
                inset: 1.5f, thick: 0.1f, useEW: true, margin: 0f);

            CreateTown("Town_WideSidewalk", "넓은 보도 (강남 느낌)",
                inset: 1.5f, thick: 0.15f, useEW: true, margin: 1.5f);

            CreateTown("Town_ParkRoad", "공원 도로 (큰 여백)",
                inset: 1.5f, thick: 0.2f, useEW: true, margin: 3f);

            CreateTown("Town_LegacyV20", "v2.0 호환 (단일 인셋)",
                inset: 2f, thick: 0.1f, useEW: false, margin: 0.5f);
        }

        static void CreateTown(string name, string desc,
            float inset, float thick, bool useEW, float margin)
        {
            var p = ScriptableObject.CreateInstance<TownPresetV2>();
            p.description = desc;
            p.roadInset = inset;
            p.blockThickness = thick;
            p.useEdgeWidthForInset = useEW;
            p.insetExtraMargin = margin;
            SaveAsset(p, $"{ROOT}/Town/{name}.asset");
        }

        // ─────────── helpers ───────────
        static void SaveAsset(Object obj, string path)
        {
            // 이미 있으면 덮어쓰기 (값만 갱신)
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(obj, existing);
                Object.DestroyImmediate(obj);
            }
            else
            {
                AssetDatabase.CreateAsset(obj, path);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}