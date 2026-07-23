namespace Dev.NKY.Scripts
{
    using System.Collections.Generic;
    using UnityEngine;
 
    public class BlockInstance
    {
        public readonly BlockData data;
        public readonly StatModifierDataSo statData;
        public Vector2Int origin;
        public int rotation;
 
        public BlockInstance(BlockData data, StatModifierDataSo stat, Vector2Int origin, int rotation = 0)
        {
            this.data = data;
            this.origin = origin;
            this.rotation = rotation;
            this.statData = stat;
        }
 
        public List<Vector2Int> GetOccupiedCells()
        {
            var rotated = BlockShapeUtility.GetRotatedCells(data.cells, rotation);
            var occupied = new List<Vector2Int>(rotated.Count);
            foreach (var c in rotated)
                occupied.Add(origin + c);
            return occupied;
        }
    }
}