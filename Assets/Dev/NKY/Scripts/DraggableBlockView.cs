using System;
using Dev.NKY.Scripts.Dev.NKY.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Dev.NKY.Scripts
{
    [RequireComponent(typeof(CanvasGroup), typeof(BlockVisualizer))]
    public class DraggableBlockView : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private MachinePresent presentPrefab;
        [SerializeField] private SoundDataSO equipSound;
        [SerializeField] private SoundDataSO unEquipSound;
        [SerializeField] private SoundDataSO trashSound;

        private InventoryGrid grid;
        private InventoryGridView gridView;
        private BlockData data;
        public MachinePartsDataSo MachinePartsData { get; set; }
        private Transform homeParent;

        public event Action<DraggableBlockView> OnPlaced;
        public event Action<DraggableBlockView> OnUnplaced;
        public event Action<DraggableBlockView> OnDiscarded;

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

            // ★ 1. ScriptableObject를 복제하여 이 블록만의 독자적인 스탯 인스턴스를 만듭니다.
            if (statData != null)
            {
                MachinePartsData = Instantiate(statData);
            }

            grid = gridRef;
            gridView = gridViewRef;
            homeParent = home;
            isPlaced = false;

            visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);


            if (MachinePartsData != null && MachinePartsData.statData != null)
            {
                for (int i = 0; i < MachinePartsData.statData.Count; i++)
                {
                    var stat = MachinePartsData.statData[i];
            
                    // Random.Range (float/int 판별하여 무작위 값 할당)
                    if (stat.isRandom) 
                    {
                        stat.value = Random.Range(stat.minValue, stat.maxValue);
                    }
            
                    // struct일 경우 대비하여 원본 리스트에 다시 덮어쓰기
                    MachinePartsData.statData[i] = stat;
                }
            }

            // 툴팁 UI 초기화 (최상단 레이어 생성 방식 적용)
            var present = GetOrCreatePresent();
            if (present != null)
            {
                present.Initialize(MachinePartsData);
                present.Hide();
            }
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
            if (_present != null)
            {
                _present.Hide();
            }
            
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
                var candidate = new BlockInstance(data, MachinePartsData, cell, rotation);

                if (grid.TryPlace(candidate))
                {
                    instance = candidate;
                    isPlaced = true;

                    visualizer.RebuildVisual(rect, data, rotation, CellSize, isPlaced);
                    rect.SetParent(gridView.transform, worldPositionStays: false);

                    Vector2Int topLeftCell = BlockLayoutCalculator.GetTopLeftGridCell(cell, BlockShapeUtility.GetRotatedCells(data.cells, rotation));
                    rect.anchoredPosition = gridView.GridCellToAnchoredPosition(topLeftCell);
                    rect.localScale = Vector3.one;
                    
                    SoundManager.Instance.PlaySFX(equipSound);

                    OnPlaced?.Invoke(this);
                    return;
                }
            }

            // ★ 2. 마우스 아래에 휴지통이 있는지 검사 -> 휴지통으로 버리기
            TrashCanUI trashCan = GetTrashCanUnderPointer(eventData);
            if (trashCan != null)
            {
                SoundManager.Instance.PlaySFX(trashSound);
                trashCan.ResetVisual();
                DiscardBlock();
                return;
            }

            // 3. 실패 시 슬롯으로 복귀
            SoundManager.Instance.PlaySFX(unEquipSound);
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
                var preview = new BlockInstance(data, MachinePartsData, cell, rotation);
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
        private MachinePresent GetOrCreatePresent()
        {
            // 삭제되었거나 아직 없는 경우 최상단 DragLayer에 생성
            if (_present == null && presentPrefab != null)
            {
                Transform targetLayer = GetDragLayer();
                _present = Instantiate(presentPrefab, targetLayer);
                _present.Initialize(MachinePartsData);
                _present.Hide();
            }
            return _present;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // 드래그 중이 아닐 때만 툴팁 표시
            if (isDragging) return;

            var present = GetOrCreatePresent();
            if (present != null)
            {
                present.Show(transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_present != null)
            {
                _present.Hide();
            }
        }
        
        private TrashCanUI GetTrashCanUnderPointer(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                return eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<TrashCanUI>();
            }
            return null;
        }

// ★ 블록 삭제 및 완전히 파괴하는 처리
        public void DiscardBlock()
        {
            // 그리드에 장착되어 있던 블록이면 그리드에서 제거 (스탯 차감도 자동 동작)
            if (instance != null)
            {
                grid.Remove(instance);
                instance = null;
            }

            // 툴팁 UI 정리
            if (_present != null)
            {
                Destroy(_present.gameObject);
            }

            // 슬롯 등 구독자들에게 버려짐 알림
            OnDiscarded?.Invoke(this);

            // 블록 오브젝트 완전 삭제
            Destroy(gameObject);
        }

        // 오브젝트 파괴 시 툴팁도 함께 정리
        private void OnDestroy()
        {
            if (_present != null)
            {
                Destroy(_present.gameObject);
            }
        }
        
        
    }
}