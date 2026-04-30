using UnityEngine;

namespace TownGen.V2
{
    [DisallowMultipleComponent]
    public class RoadMeshAuthoring : MonoBehaviour
    {
        [Header("Materials")]
        [Tooltip("도로 표면 머티리얼 (없으면 회색 기본)")]
        public Material roadMaterial;
        [Tooltip("교차로 머티리얼 (없으면 도로 머티리얼 재사용)")]
        public Material intersectionMaterial;

        [Header("Heights (Z-fighting 방지)")]
        [Range(0f, 0.5f)] public float roadYOffset = 0.01f;
        [Range(0f, 0.5f)] public float intersectionYOffset = 0.02f;

        [Header("Intersection")]
        [Tooltip("교차로 원반의 분할 수")]
        [Range(4, 32)] public int intersectionSegments = 12;

        [Header("Trim")]
        [Tooltip("교차로 영역만큼 도로 끝을 잘라내어 깔끔하게 마감")]
        public bool trimRoadEnds = true;
        [Tooltip("트림 반경 배수 (1.0 = 가장 굵은 연결 도로의 halfWidth와 동일)")]
        [Range(0.5f, 2f)] public float trimPadding = 1.0f;
        [Tooltip("교차로 원반이 도로 경계와 겹치는 정도 (빈틈 방지)")]
        [Range(1f, 1.5f)] public float intersectionCoverage = 1.15f;

        const string ROAD_CHILD = "_RoadMesh";
        const string INTERSECTION_CHILD = "_IntersectionMesh";

        public void Build()
        {
            var auth = GetComponent<RoadGraphAuthoring>();
            if (auth == null)
            {
                Debug.LogWarning("[RoadMeshAuthoring] 같은 GameObject에 RoadGraphAuthoring 필요.");
                return;
            }
            var graph = auth.graph;
            if (graph == null || graph.nodes == null || graph.nodes.Count == 0)
            { Clear(); return; }

            var roadMesh = RoadMeshBuilder.Build(graph, roadYOffset, trimRoadEnds, trimPadding);
            ApplyMesh(ROAD_CHILD, roadMesh, roadMaterial);

            var intMesh = IntersectionMeshBuilder.Build(graph, intersectionYOffset,
                intersectionSegments, trimPadding, intersectionCoverage);
            ApplyMesh(INTERSECTION_CHILD, intMesh,
                intersectionMaterial != null ? intersectionMaterial : roadMaterial);
        }

        public void Clear()
        {
            DestroyChild(ROAD_CHILD);
            DestroyChild(INTERSECTION_CHILD);
        }

        /// <summary>
        /// 그래프에서 가장 굵은 도로의 halfWidth를 계산해 반환.
        /// 빌딩 인셋(roadInset)을 이 값에 맞추면 빌딩이 도로 위로 침범하지 않음.
        /// </summary>
        public float ComputeRecommendedBlockInset(float extraMargin = 0.2f)
        {
            var auth = GetComponent<RoadGraphAuthoring>();
            if (auth == null || auth.graph == null) return 0f;
            float maxHalf = 0f;
            foreach (var e in auth.graph.edges)
                maxHalf = Mathf.Max(maxHalf, e.width * 0.5f);
            return maxHalf + extraMargin;
        }

        void DestroyChild(string name)
        {
            var t = transform.Find(name);
            if (t == null) return;
            if (Application.isPlaying) Destroy(t.gameObject);
            else DestroyImmediate(t.gameObject);
        }

        void ApplyMesh(string childName, Mesh mesh, Material mat)
        {
            var t = transform.Find(childName);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
            }
            else go = t.gameObject;

            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                mat != null ? mat : GetDefaultMaterial();
        }

        static Material _defaultMat;
        static Material GetDefaultMaterial()
        {
            if (_defaultMat != null) return _defaultMat;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            _defaultMat = new Material(sh) { name = "DefaultRoadMat" };
            _defaultMat.color = new Color(0.28f, 0.28f, 0.30f);
            return _defaultMat;
        }
    }
}