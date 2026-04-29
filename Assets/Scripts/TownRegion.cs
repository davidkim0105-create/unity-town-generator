using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TownRegion
{
    public string name = "New Region";
    public Color color = new Color(1f, 0.5f, 0.5f, 0.35f);
    public List<Vector2Int> blockIds = new List<Vector2Int>();

    [TextArea(2, 4)]
    public string note = "";

    [Tooltip("잠긴 영역은 Generate/Brush로 변경되지 않음.")]
    public bool isLocked = false;

    [Tooltip("Scene 뷰에 영역 이름 라벨 표시.")]
    public bool showLabel = true;

    public bool ContainsBlock(int gx, int gz)
    {
        for (int i = 0; i < blockIds.Count; i++)
            if (blockIds[i].x == gx && blockIds[i].y == gz)
                return true;
        return false;
    }

    public bool ContainsBlock(TownBlock b) => ContainsBlock(b.gridX, b.gridZ);
}