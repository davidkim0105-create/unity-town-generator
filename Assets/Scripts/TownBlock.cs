using UnityEngine;

public class TownBlock : MonoBehaviour
{
    // ─── 자동 채워지는 정보 ───
    [HideInInspector] public int gridX;
    [HideInInspector] public int gridZ;
    [HideInInspector] public Rect outerRect;
    [HideInInspector] public Rect innerRect;
    [HideInInspector] public float autoDensity;
    [HideInInspector] public TownGenerator generator;

    [Header("밀도 오버라이드")]
    [Tooltip("음수(-1)로 두면 자동 밀도 사용.\n0~1 사이 값으로 두면 그 밀도로 강제.\n↑ 키우면: 이 블록만 더 밀집/고층\n↓ 줄이면: 이 블록만 더 한산")]
    [Range(-1f, 1f)] public float densityOverride = -1f;

    [Header("블록 시드")]
    [Tooltip("블록 시드 오프셋. 0=기본 패턴, 다른 값=다른 배치.\n같은 밀도라도 시드가 다르면 건물 배치가 달라집니다.")]
    public int seedOffset = 0;

    public float EffectiveDensity
    {
        get { return densityOverride >= 0f ? densityOverride : autoDensity; }
    }

    public void Regenerate()
    {
        // ★ 잠긴 영역이면 거부
        if (generator != null && generator.IsBlockLocked(this))
        {
            Debug.LogWarning($"Block ({gridX},{gridZ})는 잠긴 영역에 속해 있어 재생성되지 않습니다.");
            return;
        }

        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
        if (generator != null)
            generator.RegenerateBlock(this);
    }

    public void RandomRegenerate()
    {
        if (generator != null && generator.IsBlockLocked(this))
        {
            Debug.LogWarning($"Block ({gridX},{gridZ})는 잠긴 영역에 속해 있어 재생성되지 않습니다.");
            return;
        }

        seedOffset = Random.Range(1, 99999);
        Regenerate();
    }

    // 선택 시 청록색 하이라이트
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
        Vector3 center = transform.TransformPoint(new Vector3(0, 1.5f, 0));
        Vector3 size = new Vector3(outerRect.width, 3f, outerRect.height);

        // 회전 적용
        Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * 1.5f,
                                       transform.rotation,
                                       Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}