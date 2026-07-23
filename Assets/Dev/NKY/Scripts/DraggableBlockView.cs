using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Dev.NKY.Scripts
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DraggableBlockView : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        private InventoryGrid grid;
        private InventoryGridView gridView;
        [SerializeField] private Image cellImagePrefab;
        [SerializeField] private RectTransform dragLayer;

        private BlockData data;
        private StatModifierDataSo assignedStat;

        public event Action<DraggableBlockView> OnPlaced;

        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private BlockInstance instance;
        private Vector2 dragStartAnchoredPos;
        private Transform originalParent;
        private Vector2 dragOffset;
        private int rotation;
        private bool isDragging;
        private PointerEventData currentEventData;
        private RectTransform dragParentRect;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            // 루트 객체의 Image 컴포넌트는 비활성화
            if (TryGetComponent<Image>(out var rootImg))
            {
                rootImg.enabled = false;
            }

            // 중앙 피벗(0.5, 0.5) 고정
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public void Initialize(BlockData blockData, StatModifierDataSo statData, InventoryGrid gridRef, InventoryGridView gridViewRef)
        {
            data = blockData;
            assignedStat = statData;
            grid = gridRef;
            gridView = gridViewRef;
            RebuildBlockVisual();
        }

        private void Update()
        {
            if (isDragging && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                rotation = (rotation + 1) % 4;
                RebuildBlockVisual();

                if (currentEventData != null)
                {
                    UpdatePreview(currentEventData);
                }
            }
        }

        public void RebuildBlockVisual()
        {
            if (data == null || data.cells == null || data.cells.Count == 0) return;

            // 1. 기존 자식 UI 즉시 분리 후 제거
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            float cellSize = gridView != null ? gridView.CellSize : 64f;
            var rotatedCells = BlockShapeUtility.GetRotatedCells(data.cells, rotation);

            // 2. 바운딩 박스 계산
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var c in rotatedCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }

            int widthCells = maxX - minX + 1;
            int heightCells = maxY - minY + 1;
            float blockWidth = widthCells * cellSize;
            float blockHeight = heightCells * cellSize;

            // 3. 중앙 피벗 및 RectTransform 크기 지정
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(blockWidth, blockHeight);

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = blockWidth;
            layoutElement.preferredHeight = blockHeight;

            LayoutRebuilder.MarkLayoutForRebuild(rect);

            Vector2 topLeftLocal = new Vector2(-blockWidth * 0.5f, blockHeight * 0.5f);

            // 4. 자식 셀 생성 및 배치
            foreach (var c in rotatedCells)
            {
                Image img;
                if (cellImagePrefab != null)
                {
                    img = Instantiate(cellImagePrefab, transform);
                }
                else
                {
                    var cellObj = new GameObject($"Cell_({c.x},{c.y})", typeof(RectTransform), typeof(Image));
                    cellObj.transform.SetParent(transform, false);
                    img = cellObj.GetComponent<Image>();
                    img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                }

                // ★ [핵심 수정]: 마우스 클릭/드래그 이벤트를 받을 수 있도록 raycastTarget을 true로 변경
                img.raycastTarget = true;

                if (data.icon != null)
                {
                    img.sprite = data.icon;
                    img.color = Color.white;
                }

                var cellRt = img.rectTransform;
                cellRt.anchorMin = cellRt.anchorMax = cellRt.pivot = new Vector2(0.5f, 0.5f);
                cellRt.sizeDelta = new Vector2(cellSize, cellSize);
                cellRt.localScale = Vector3.one;

                float cellLocalX = topLeftLocal.x + (c.x - minX + 0.5f) * cellSize;
                float cellLocalY = topLeftLocal.y - (c.y - minY + 0.5f) * cellSize;
                cellRt.anchoredPosition = new Vector2(cellLocalX, cellLocalY);
            }
        }

        private Vector2 GetOriginCellScreenPos(Camera cam)
        {
            float cellSize = gridView != null ? gridView.CellSize : 64f;
            var rotatedCells = BlockShapeUtility.GetRotatedCells(data.cells, rotation);

            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in rotatedCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
            }

            Vector2 topLeftLocal = new Vector2(-rect.sizeDelta.x * 0.5f, rect.sizeDelta.y * 0.5f);
            Vector2 originCellCenterLocal = new Vector2(
                topLeftLocal.x + (0 - minX + 0.5f) * cellSize,
                topLeftLocal.y - (0 - minY + 0.5f) * cellSize
            );

            Vector3 originWorldPos = rect.TransformPoint(originCellCenterLocal);
            return RectTransformUtility.WorldToScreenPoint(cam, originWorldPos);
        }

        private Vector2 GetAnchoredPositionForGridCell(Vector2Int cell)
        {
            float cellSize = gridView.CellSize;
            var rotatedCells = BlockShapeUtility.GetRotatedCells(data.cells, rotation);

            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in rotatedCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
            }

            Vector2 originTopLeftGridPos = gridView.GridCellToAnchoredPosition(cell);

            Vector2 topLeftLocal = new Vector2(-rect.sizeDelta.x * 0.5f, rect.sizeDelta.y * 0.5f);
            Vector2 originCellTopLeftLocal = new Vector2(
                topLeftLocal.x + (-minX * cellSize),
                topLeftLocal.y - (minY * cellSize)
            );

            return originTopLeftGridPos - originCellTopLeftLocal;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            currentEventData = eventData;
            dragStartAnchoredPos = rect.anchoredPosition;
            originalParent = rect.parent;

            canvasGroup.blocksRaycasts = false;

            var layer = GetDragLayer();
            rect.SetParent(layer, worldPositionStays: true);
            rect.SetAsLastSibling();

            dragParentRect = layer;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragParentRect, eventData.position, eventData.pressEventCamera, out var localMousePos))
            {
                dragOffset = rect.anchoredPosition - localMousePos;
            }

            if (instance != null)
            {
                grid.Remove(instance);
                instance = null;
            }
        }

        private RectTransform GetDragLayer()
        {
            if (dragLayer != null) return dragLayer;
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : originalParent as RectTransform;
        }

        public void OnDrag(PointerEventData eventData)
        {
            currentEventData = eventData;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragParentRect, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                rect.anchoredPosition = localPoint + dragOffset;
            }

            UpdatePreview(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            currentEventData = null;
            canvasGroup.blocksRaycasts = true;
            gridView.ClearPreview();

            Vector2 originScreenPos = GetOriginCellScreenPos(eventData.pressEventCamera);

            if (gridView.ScreenToGridCell(originScreenPos, eventData.pressEventCamera, out var cell))
            {
                var candidate = new BlockInstance(data, assignedStat, cell, rotation);
                if (grid.TryPlace(candidate))
                {
                    instance = candidate;
                    rect.SetParent(gridView.transform, worldPositionStays: false);
                    rect.anchoredPosition = GetAnchoredPositionForGridCell(cell);
                    OnPlaced?.Invoke(this);
                    return;
                }
            }

            rect.SetParent(originalParent, worldPositionStays: false);
            rect.anchoredPosition = dragStartAnchoredPos;
        }

        private void UpdatePreview(PointerEventData eventData)
        {
            Vector2 originScreenPos = GetOriginCellScreenPos(eventData.pressEventCamera);

            if (gridView.ScreenToGridCell(originScreenPos, eventData.pressEventCamera, out var cell))
            {
                var preview = new BlockInstance(data, assignedStat, cell, rotation);
                gridView.ShowPreview(preview, grid.CanPlace(preview));
            }
            else
            {
                gridView.ClearPreview();
            }
        }
    }
}