using System;
using Dev.NKY.Scripts.Dev.NKY.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    [RequireComponent(typeof(CanvasGroup), typeof(BlockVisualizer))]
    public class DraggableBlockView : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private MachinePresent presentPrefab;

        private InventoryGrid grid;
        private InventoryGridView gridView;
        private BlockData data;
        private MachinePartsDataSo machinePartsData;
        private Transform homeParent;

        public event Action<DraggableBlockView> OnPlaced;
        public event Action<DraggableBlockView> OnUnplaced;

        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private BlockVisualizer visualizer;
        private BlockInstance instance;

        private Vector2 dragStartAnchoredPos;
        private int dragStartRotation;
        private bool dragStartIsPlaced;
        private Vector2Int dragStartCell;
        private Transform originalParent;
        private Vector2 dragOffset;
        private int rotation;
        private bool isDragging;
        private bool isPlaced;
        private PointerEventData currentEventData;
        private RectTransform dragParentRect;

        private float CellSize => gridView != null ? gridView.CellSize : 64f;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            visualizer = GetComponent<BlockVisualizer>();

            if (TryGetComponent<Image>(out var rootImg)) rootImg.enabled = false;
        }

        public void Initialize(BlockData blockData, MachinePartsDataSo statData, InventoryGrid gridRef, InventoryGridView gridViewRef, Transform home)
        {
            data = blockData;
            machinePartsData = statData;
            grid = gridRef;
            gridView = gridViewRef;
            homeParent = home;
            isPlaced = false;

            visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);
        }
        
        private void Update()
        {
            if (isDragging && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RotateBlock();
            }
        }

        private void RotateBlock()
        {
            rotation = (rotation + 1) % 4;
            visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);
            Canvas.ForceUpdateCanvases();

            if (currentEventData != null && dragParentRect != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        dragParentRect, currentEventData.position, currentEventData.pressEventCamera, out var localMousePos))
                {
                    dragOffset = rect.anchoredPosition - localMousePos;
                }

                UpdatePreview(currentEventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
            currentEventData = eventData;
            dragStartAnchoredPos = rect.anchoredPosition;
            dragStartRotation = rotation;
            dragStartIsPlaced = isPlaced;
            dragStartCell = instance != null ? instance.origin : Vector2Int.zero;
            originalParent = rect.parent;

            if (instance != null)
            {
                grid.Remove(instance);
                instance = null;
            }

            dragParentRect = GetDragLayer();

            if (isPlaced)
            {
                Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
                isPlaced = false;

                visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);

                rect.SetParent(dragParentRect, worldPositionStays: false);
                rect.position = worldCenter;
            }
            else
            {
                rect.SetParent(dragParentRect, worldPositionStays: true);
            }

            canvasGroup.blocksRaycasts = false;
            rect.SetAsLastSibling();

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragParentRect, eventData.position, eventData.pressEventCamera, out var localMousePos))
            {
                dragOffset = rect.anchoredPosition - localMousePos;
            }
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

            Vector2 originScreenPos = BlockLayoutCalculator.GetOriginCellScreenPos(
                rect, BlockShapeUtility.GetRotatedCells(data.cells, rotation), CellSize, isPlaced, eventData.pressEventCamera);

            // 1. 그리드 칸 위이며 장착 가능한 경우 -> 그리드 장착
            if (gridView.ScreenToGridCell(originScreenPos, eventData.pressEventCamera, out var cell))
            {
                var candidate = new BlockInstance(data, machinePartsData, cell, rotation);

                if (grid.TryPlace(candidate))
                {
                    instance = candidate;
                    isPlaced = true;

                    visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);
                    rect.SetParent(gridView.transform, worldPositionStays: false);

                    Vector2Int topLeftCell = BlockLayoutCalculator.GetTopLeftGridCell(cell, BlockShapeUtility.GetRotatedCells(data.cells, rotation));
                    rect.anchoredPosition = gridView.GridCellToAnchoredPosition(topLeftCell);
                    rect.localScale = Vector3.one;

                    OnPlaced?.Invoke(this);
                    return;
                }
            }

            ReturnToTray();
        }

        public void ReturnToTray()
        {
            if (instance != null)
            {
                grid.Remove(instance);
                instance = null;
            }

            isPlaced = false;
            rotation = dragStartRotation;
            visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);

            rect.SetParent(homeParent, worldPositionStays: false);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            OnUnplaced?.Invoke(this);
        }

        private void UpdatePreview(PointerEventData eventData)
        {
            Vector2 originScreenPos = BlockLayoutCalculator.GetOriginCellScreenPos(rect, BlockShapeUtility.GetRotatedCells(data.cells, rotation), CellSize, isPlaced, eventData.pressEventCamera);

            if (gridView.ScreenToGridCell(originScreenPos, eventData.pressEventCamera, out var cell))
            {
                var preview = new BlockInstance(data, machinePartsData, cell, rotation);
                gridView.ShowPreview(preview, grid.CanPlace(preview));
            }
            else
            {
                gridView.ClearPreview();
            }
        }

        private RectTransform GetDragLayer()
        {
            if (dragLayer != null) return dragLayer;
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : originalParent as RectTransform;
        }

        private MachinePresent _present;
        public void OnPointerEnter(PointerEventData eventData)
        {
            _present = Instantiate(presentPrefab, transform);
            _present.Initialize(machinePartsData);
            _present.Show(transform.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _present.Hide();
        }
    }
}