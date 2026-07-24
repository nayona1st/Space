namespace Dev.NKY.Scripts
{
using System.Collections.Generic;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    public static class BlockLayoutCalculator
    {
        public struct Bounds
        {
            public int minX, maxX, minY, maxY;
            public int WidthCells => maxX - minX + 1;
            public int HeightCells => maxY - minY + 1;
        }

        public static Bounds CalculateBounds(IReadOnlyList<Vector2Int> rotatedCells)
        {
            var bounds = new Bounds
            {
                minX = int.MaxValue, maxX = int.MinValue,
                minY = int.MaxValue, maxY = int.MinValue
            };

            foreach (var c in rotatedCells)
            {
                if (c.x < bounds.minX) bounds.minX = c.x;
                if (c.x > bounds.maxX) bounds.maxX = c.x;
                if (c.y < bounds.minY) bounds.minY = c.y;
                if (c.y > bounds.maxY) bounds.maxY = c.y;
            }

            return bounds;
        }

        public static Vector2 GetCellLocalPosition(Vector2Int cell, Bounds bounds, float cellSize, bool isPlaced, Vector2 blockSize)
        {
            if (isPlaced)
            {
                // 좌상단(0,1) 피벗 기준
                return new Vector2((cell.x - bounds.minX) * cellSize, -(cell.y - bounds.minY) * cellSize);
            }

            // 중앙(0.5, 0.5) 피벗 기준
            Vector2 topLeftLocal = new Vector2(-blockSize.x * 0.5f, blockSize.y * 0.5f);
            return new Vector2(
                topLeftLocal.x + (cell.x - bounds.minX + 0.5f) * cellSize,
                topLeftLocal.y - (cell.y - bounds.minY + 0.5f) * cellSize
            );
        }

        public static Vector2 GetOriginCellScreenPos(RectTransform rect, IReadOnlyList<Vector2Int> rotatedCells, float cellSize, bool isPlaced, Camera cam)
        {
            var bounds = CalculateBounds(rotatedCells);
            Vector2 originCellCenterLocal;

            if (isPlaced)
            {
                originCellCenterLocal = new Vector2(
                    (0 - bounds.minX + 0.5f) * cellSize,
                    -(0 - bounds.minY + 0.5f) * cellSize
                );
            }
            else
            {
                Vector2 topLeftLocal = new Vector2(-rect.sizeDelta.x * 0.5f, rect.sizeDelta.y * 0.5f);
                originCellCenterLocal = new Vector2(
                    topLeftLocal.x + (0 - bounds.minX + 0.5f) * cellSize,
                    topLeftLocal.y - (0 - bounds.minY + 0.5f) * cellSize
                );
            }

            Vector3 originWorldPos = rect.TransformPoint(originCellCenterLocal);
            return RectTransformUtility.WorldToScreenPoint(cam, originWorldPos);
        }

        public static Vector2Int GetTopLeftGridCell(Vector2Int cell, IReadOnlyList<Vector2Int> rotatedCells)
        {
            var bounds = CalculateBounds(rotatedCells);
            return new Vector2Int(cell.x + bounds.minX, cell.y + bounds.minY);
        }
    }
}
}