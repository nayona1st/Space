using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    public class BlockVisualizer : MonoBehaviour
    {
        [SerializeField] private Image cellPrefab;        // 1칸짜리 배경 이미지 프리팹
        [SerializeField] private Color borderLineColor = new Color(0.1f, 0.1f, 0.1f, 1f); // 테두리 선 색상
        [SerializeField] private float borderWidth = 3f;   // 테두리 선 두께

        public void RebuildVisual(RectTransform container, BlockData data, int rotation, float cellSize, bool isPlaced)
        {
            if (cellPrefab == null)
            {
                Debug.LogError($"[BlockVisualizer] {gameObject.name}의 cellPrefab이 Inspector에서 지정되지 않았습니다!", this);
                return;
            }

            // 1. 기존 자식 오브젝트 삭제
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            if (data == null || data.cells == null || data.cells.Count == 0) return;

            List<Vector2Int> rotatedCells = BlockShapeUtility.GetRotatedCells(data.cells, rotation);

            // 2. 바운딩 박스 계산
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var cell in rotatedCells)
            {
                if (cell.x < minX) minX = cell.x;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.y > maxY) maxY = cell.y;
            }

            int widthInCells = maxX - minX + 1;
            int heightInCells = maxY - minY + 1;

            // ★ 3. isPlaced 상태에 따른 Pivot / Anchor 분기
            if (isPlaced)
            {
                // 그리드 배치 완료 상태: 좌상단(0, 1) 피벗
                container.anchorMin = new Vector2(0f, 1f);
                container.anchorMax = new Vector2(0f, 1f);
                container.pivot = new Vector2(0f, 1f);
            }
            else
            {
                // 드래그 중 / 보관 트레이 상태: 중앙(0.5, 0.5) 피벗
                container.anchorMin = new Vector2(0.5f, 0.5f);
                container.anchorMax = new Vector2(0.5f, 0.5f);
                container.pivot = new Vector2(0.5f, 0.5f);
            }

            container.sizeDelta = new Vector2(widthInCells * cellSize, heightInCells * cellSize);

            // 4. 각 셀 배치 (자식 셀의 Anchor는 부모 Top-Left 기준 유지)
            foreach (var cellPos in rotatedCells)
            {
                var cellObj = Instantiate(cellPrefab, container);
                var rt = cellObj.rectTransform;

                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);

                int localX = cellPos.x - minX;
                int localY = cellPos.y - minY;

                rt.anchoredPosition = new Vector2(localX * cellSize, -localY * cellSize);

                // 5. 테두리 라인 생성
                bool hasTop = rotatedCells.Contains(new Vector2Int(cellPos.x, cellPos.y - 1));
                bool hasBottom = rotatedCells.Contains(new Vector2Int(cellPos.x, cellPos.y + 1));
                bool hasLeft = rotatedCells.Contains(new Vector2Int(cellPos.x - 1, cellPos.y));
                bool hasRight = rotatedCells.Contains(new Vector2Int(cellPos.x + 1, cellPos.y));

                if (!hasTop) CreateBorderLine(rt, BorderDirection.Top, cellSize);
                if (!hasBottom) CreateBorderLine(rt, BorderDirection.Bottom, cellSize);
                if (!hasLeft) CreateBorderLine(rt, BorderDirection.Left, cellSize);
                if (!hasRight) CreateBorderLine(rt, BorderDirection.Right, cellSize);
            }
        }

        private enum BorderDirection { Top, Bottom, Left, Right }

        private void CreateBorderLine(RectTransform parentCell, BorderDirection dir, float cellSize)
        {
            GameObject lineObj = new GameObject($"Border_{dir}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineObj.transform.SetParent(parentCell, false);

            Image img = lineObj.GetComponent<Image>();
            img.color = borderLineColor;
            img.raycastTarget = false;

            RectTransform lineRT = lineObj.GetComponent<RectTransform>();

            switch (dir)
            {
                case BorderDirection.Top:
                    lineRT.anchorMin = new Vector2(0f, 1f);
                    lineRT.anchorMax = new Vector2(1f, 1f);
                    lineRT.pivot = new Vector2(0.5f, 1f);
                    lineRT.sizeDelta = new Vector2(0f, borderWidth);
                    lineRT.anchoredPosition = Vector2.zero;
                    break;

                case BorderDirection.Bottom:
                    lineRT.anchorMin = new Vector2(0f, 0f);
                    lineRT.anchorMax = new Vector2(1f, 0f);
                    lineRT.pivot = new Vector2(0.5f, 0f);
                    lineRT.sizeDelta = new Vector2(0f, borderWidth);
                    lineRT.anchoredPosition = Vector2.zero;
                    break;

                case BorderDirection.Left:
                    lineRT.anchorMin = new Vector2(0f, 0f);
                    lineRT.anchorMax = new Vector2(0f, 1f);
                    lineRT.pivot = new Vector2(0f, 0.5f);
                    lineRT.sizeDelta = new Vector2(borderWidth, 0f);
                    lineRT.anchoredPosition = Vector2.zero;
                    break;

                case BorderDirection.Right:
                    lineRT.anchorMin = new Vector2(1f, 0f);
                    lineRT.anchorMax = new Vector2(1f, 1f);
                    lineRT.pivot = new Vector2(1f, 0.5f);
                    lineRT.sizeDelta = new Vector2(borderWidth, 0f);
                    lineRT.anchoredPosition = Vector2.zero;
                    break;
            }
        }
    }
}