using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Dev.NKY.Scripts
{
    [RequireComponent(typeof(RectTransform))]
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private DraggableBlockView blockPrefab;
        [SerializeField] private InventoryTray tray;
        [SerializeField] private UnityEngine.UI.Image background;

        [SerializeField] private InventoryGrid grid;
        [SerializeField] private InventoryGridView gridView;

        private readonly List<DraggableBlockView> blockList = new List<DraggableBlockView>();

        public void SetGridReferences(InventoryGrid gridRef, InventoryGridView gridViewRef)
        {
            grid = gridRef;
            gridView = gridViewRef;
        }

        private void Start()
        {
            SpawnNewBlock();
        }

        public void SpawnNewBlock()
        {
            if (tray == null) return;
            if (!tray.TryTakeRandom(out var blockData, out var statData)) return;

            var newBlock = Instantiate(blockPrefab, transform);
            newBlock.Initialize(blockData, statData, grid, gridView, transform);

            RegisterBlock(newBlock);
        }

        private void RegisterBlock(DraggableBlockView block)
        {
            if (!blockList.Contains(block))
            {
                blockList.Add(block);
            }

            block.OnPlaced -= HandleBlockPlaced;
            block.OnPlaced += HandleBlockPlaced;
            block.OnUnplaced -= HandleBlockUnplaced;
            block.OnUnplaced += HandleBlockUnplaced;

            UpdateSlotDisplay();
        }

        // 그리드 배치 성공 -> 슬롯 목록에서만 제거
        private void HandleBlockPlaced(DraggableBlockView view)
        {
            view.OnPlaced -= HandleBlockPlaced; 
            // ★ 중요: OnUnplaced 해제 구문을 제거했습니다!
            // 나중에 그리드에서 슬롯으로 다시 되돌아올 때 OnUnplaced 이벤트를 받아야 하기 때문입니다.

            blockList.Remove(view);

            if (blockList.Count == 0)
            {
                SpawnNewBlock();
            }
            else
            {
                UpdateSlotDisplay();
            }
        }

        // 배치 실패 또는 그리드에서 슬롯으로 복귀했을 때 실행
        private void HandleBlockUnplaced(DraggableBlockView view)
        {
            if (!blockList.Contains(view))
            {
                blockList.Add(view);
            }

            // 슬롯으로 되돌아왔으므로 다시 그리드에 장착할 수 있도록 OnPlaced 재연결
            view.OnPlaced -= HandleBlockPlaced;
            view.OnPlaced += HandleBlockPlaced;

            UpdateSlotDisplay();
        }

        private void UpdateSlotDisplay()
        {
            for (int i = 0; i < blockList.Count; i++)
            {
                if (blockList[i] == null) continue;

                // 맨 마지막(리스트 상단) 블록만 켜고, 밑에 깔린 기존 블록들은 SetActive(false)로 가립니다.
                bool isTop = (i == blockList.Count - 1);
                blockList[i].gameObject.SetActive(isTop);

                if (isTop)
                {
                    var rt = blockList[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                }
            }
        }
        
        public void OnClickNextBlock()
        {
            // 보관된 블록이 2개 이상일 때만 순환
            if (blockList.Count <= 1) return;

            // 맨 위에 있는 블록(마지막 인덱스)을 맨 아래(0번)로 이동
            var topBlock = blockList[blockList.Count - 1];
            blockList.RemoveAt(blockList.Count - 1);
            blockList.Insert(0, topBlock);

            // 화면 갱신
            UpdateSlotDisplay();
        }
        
        public void OnClickPreviousBlock()
        {
            if (blockList.Count <= 1) return;

            // 맨 아래(0번) 블록을 꺼내서 맨 위(마지막 인덱스)로 이동
            var bottomBlock = blockList[0];
            blockList.RemoveAt(0);
            blockList.Add(bottomBlock); // Add는 리스트 맨 끝(상단)으로 추가됨

            UpdateSlotDisplay();
        }
    }
}