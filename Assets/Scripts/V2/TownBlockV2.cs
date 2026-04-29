using System.Collections.Generic;
using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 폴리곤 기반 도시 블록.
    /// face 하나 = 블록 하나.
    /// </summary>
    [DisallowMultipleComponent]
    public class TownBlockV2 : MonoBehaviour
    {
        [Tooltip("로컬 좌표 폴리곤 (시계방향 권장, XZ 평면)")]
        public List<Vector3> polygon = new List<Vector3>();

        public int faceIndex = -1;
        public float density = 0.5f;
        public float signedArea;

        // (Phase 4b에서 빌딩 채움 데이터 추가 예정)
    }
}