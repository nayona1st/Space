namespace Dev.NKY.Scripts
{
    using System.Collections.Generic;
    using UnityEngine;
 
    public static class BlockShapeUtility
    {
        // rotation: 0~3 (90도 단위)
        public static List<Vector2Int> GetRotatedCells(List<Vector2Int> baseCells, int rotation)
        {
            rotation = ((rotation % 4) + 4) % 4;
            var result = new List<Vector2Int>(baseCells.Count);
 
            foreach (var c in baseCells)
            {
                Vector2Int rotated = c;
                for (int i = 0; i < rotation; i++)
                    rotated = new Vector2Int(-rotated.y, rotated.x);
                result.Add(rotated);
            }
            return result;
        }
    }
}