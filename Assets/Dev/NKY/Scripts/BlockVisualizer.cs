using UnityEngine;

namespace Dev.NKY.Scripts
{
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    public class BlockVisualizer : MonoBehaviour
    {
        [SerializeField] private Image cellImagePrefab;

        public void RebuildVisual(RectTransform rect, BlockData data, int rotation, float cellSize, bool isPlaced)
        {
            if (data == null || data.cells == null || data.cells.Count == 0) return;

            ClearChildren();

            var rotatedCells = BlockShapeUtility.GetRotatedCells(data.cells, rotation);
            var bounds = BlockLayoutCalculator.CalculateBounds(rotatedCells);

            float blockWidth = bounds.WidthCells * cellSize;
            float blockHeight = bounds.HeightCells * cellSize;
            Vector2 blockSize = new Vector2(blockWidth, blockHeight);

            // 피벗 및 앵커 설정
            Vector2 targetPivot = isPlaced ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rect.pivot = targetPivot;
            rect.anchorMin = targetPivot;
            rect.anchorMax = targetPivot;
            rect.sizeDelta = blockSize;

            EnsureLayoutElement(blockWidth, blockHeight);
            LayoutRebuilder.MarkLayoutForRebuild(rect);

            // 자식 셀 생성 및 정렬
            foreach (var c in rotatedCells)
            {
                Image img = CreateCellImage();
                img.raycastTarget = true;

                if (data.icon != null)
                {
                    img.sprite = data.icon;
                    img.color = Color.white;
                }

                var cellRt = img.rectTransform;
                cellRt.sizeDelta = new Vector2(cellSize, cellSize);
                cellRt.localScale = Vector3.one;

                Vector2 pivot = isPlaced ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
                cellRt.anchorMin = cellRt.anchorMax = cellRt.pivot = pivot;
                cellRt.anchoredPosition = BlockLayoutCalculator.GetCellLocalPosition(c, bounds, cellSize, isPlaced, blockSize);
            }
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        private Image CreateCellImage()
        {
            if (cellImagePrefab != null) return Instantiate(cellImagePrefab, transform);

            var cellObj = new GameObject("Cell", typeof(RectTransform), typeof(Image));
            cellObj.transform.SetParent(transform, false);
            var img = cellObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            return img;
        }

        private void EnsureLayoutElement(float width, float height)
        {
            if (!TryGetComponent<LayoutElement>(out var layoutElement))
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
        }
    }
}
}