using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TownGen.V2
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class RoadGraphAuthoring : MonoBehaviour
    {
        [Tooltip("도로 그래프 데이터 (노드 + 엣지). 직접 수정하지 말고 Scene 도구로 편집하세요.")]
        [SerializeField] public RoadGraph graph = new RoadGraph();

        [Header("Visualization")]
        [Tooltip("노드(점)를 Scene에 표시\n• ON: 청록 구체로 표시 (편집용)\n• OFF: 노드 안 보임 (최종 결과 미리보기 용)")]
        public bool showNodes = true;

        [Tooltip("도로 엣지(선)를 Scene에 표시\n• ON: 흰 선으로 표시\n• OFF: 도로선 안 보임")]
        public bool showEdges = true;

        [Tooltip("face(블록 영역) 폴리곤을 Scene에 색칠 표시\n• ON: 내부 face는 연두, 외곽은 빨강 점선\n• OFF: face 안 보임 (메쉬가 생성된 상태에서 보고 싶을 때 끔)")]
        public bool showFaces = true;

        [Tooltip("노드/엣지에 ID 라벨 표시\n• ON: 디버깅용 (N0, E3 등)\n• OFF: 깔끔한 화면")]
        public bool showLabels = false;

        [Tooltip("face 추출 시 막다른 가지 자동 제거\n• ON: 도시의 막다른 골목은 face 만들 때 무시 (안정적)\n• OFF: 막다른 가지 포함 (실험용, 주의)")]
        public bool pruneDeadEndsForFaces = true;

        [Header("Style")]
        [Tooltip("노드 구의 표시 크기 (m)\n• 키우면: 작은 화면에서도 클릭 쉬움\n• 줄이면: 큰 도시에서 노드가 덜 거슬림\n• 예: 0.4 = 작은 격자, 1.0 = 큰 도시")]
        public float nodeRadius = 0.4f;

        public Color nodeColor = new Color(0.2f, 0.7f, 1f);
        public Color edgeColor = new Color(1f, 1f, 1f, 0.9f);
        public Color innerFaceColor = new Color(0.3f, 1f, 0.4f, 0.18f);
        public Color outerFaceColor = new Color(1f, 0.3f, 0.3f, 0.05f);

        [Tooltip("엣지/face를 지면에서 살짝 띄우는 높이 (z-fighting 방지)\n• 0: 지면과 똑같은 높이 (지글거릴 수 있음)\n• 0.02 (기본): 거의 안 보이지만 깜빡임 방지\n• 0.5+: 명확히 떠 있는 디버그 뷰")]
        public float edgeLift = 0.02f;

        [Header("Build Settings")]
        [Tooltip("face → 블록 변환 시 도로 폭만큼 안쪽으로 줄이는 거리 (m)\n• 0: 도로 한가운데까지 블록 차지 (도로 안 보임)\n• 1.5 (기본): 일반 도로 폭 ~3m\n• 3.0+: 넓은 대로\n• 너무 크면: 작은 face는 블록 사라짐")]
        public float roadInset = 1.5f;

        [Tooltip("블록 메쉬 두께 (m). 보도블록 솟아오름\n• 0: 평면 (도로와 같은 높이)\n• 0.1 (기본): 살짝 솟아 도시 베이스 강조\n• 1.0+: 큰 단차 (요새/단지 느낌)")]
        public float blockThickness = 0.1f;

        [Tooltip("Region 영역 밖의 블록(=Default)에 사용할 빌딩 세팅")]
        public BuildingFiller.Settings buildSettings = BuildingFiller.DefaultSettings;

        [System.NonSerialized] private System.Collections.Generic.List<FaceResult> cachedFaces;
        [System.NonSerialized] private int cachedHash;

        public System.Collections.Generic.List<FaceResult> GetFaces()
        {
            int h = ComputeGraphHash();
            if (cachedFaces == null || h != cachedHash)
            {
                cachedFaces = FaceExtractor.Extract(graph, pruneDeadEndsForFaces);
                cachedHash = h;
            }
            return cachedFaces;
        }

        public void InvalidateFaceCache() { cachedFaces = null; }

        int ComputeGraphHash()
        {
            int h = 17;
            h = h * 31 + graph.nodes.Count;
            h = h * 31 + graph.edges.Count;
            foreach (var n in graph.nodes) { h = h * 31 + n.id; h = h * 31 + n.position.GetHashCode(); }
            foreach (var e in graph.edges) { h = h * 31 + e.id; h = h * 31 + e.nodeAId; h = h * 31 + e.nodeBId; }
            return h;
        }

#if UNITY_EDITOR
        void OnEnable() { Undo.undoRedoPerformed += OnUndoRedo; }
        void OnDisable() { Undo.undoRedoPerformed -= OnUndoRedo; }
        void OnUndoRedo()
        {
            graph?.InvalidateCache();
            InvalidateFaceCache();
            SceneView.RepaintAll();
        }
#endif
    }
}