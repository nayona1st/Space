using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    // 스크롤뷰 대신 좌/우 화살표 버튼으로 블록을 하나씩 순환하며 보여주는 트레이
    public class InventoryTray : MonoBehaviour
    {
        [Header("Prefabs & Data")]
        [SerializeField] private DraggableBlockView blockPrefab;
        [SerializeField] private Transform traySlotParent; // 블록이 위치할 UI 슬롯 (Viewport/Content 대신 단일 Transform)
        [SerializeField] private BlockDataList blockDataList;
        [SerializeField] private StatDataListSo statDataList;
        [SerializeField] private InventoryGrid grid;
        [SerializeField] private InventoryGridView gridView;

        [Header("Buttons & Settings")]
        [SerializeField] private Button prevButton; // 이전 블록 화살표 버튼
        [SerializeField] private Button nextButton; // 다음 블록 화살표 버튼
        [SerializeField] private int initialBlockCount = 10;
        [SerializeField] private bool loopNavigation = true; // 마지막 블록에서 다음 누르면 첫 블록으로 돌아갈지 여부

        private readonly List<DraggableBlockView> spawnedBlocks = new List<DraggableBlockView>();
        private int currentIndex = 0;

        private void Awake()
        {
            // 화살표 버튼 클릭 이벤트 연결
            if (prevButton != null) prevButton.onClick.AddListener(OnClickPrev);
            if (nextButton != null) nextButton.onClick.AddListener(OnClickNext);
        }

        private void Start()
        {
            for (int i = 0; i < initialBlockCount; i++)
            {
                SpawnBlock();
            }

            // 시작 시 첫 번째 블록만 표시
            UpdateTrayDisplay();
        }

        private void OnDestroy()
        {
            if (prevButton != null) prevButton.onClick.RemoveListener(OnClickPrev);
            if (nextButton != null) nextButton.onClick.RemoveListener(OnClickNext);
        }

        private void SpawnBlock()
        {
            if (blockDataList == null || blockDataList.blocks.Count == 0) return;
            if (statDataList == null || statDataList.statDataList.Count == 0) return;

            var blockData = blockDataList.blocks[Random.Range(0, blockDataList.blocks.Count)];
            var statData = statDataList.statDataList[Random.Range(0, statDataList.statDataList.Count)];

            var block = Instantiate(blockPrefab, traySlotParent);
            block.Initialize(blockData, statData, grid, gridView);
            block.OnPlaced += HandleBlockPlaced;

            spawnedBlocks.Add(block);
        }

        private void HandleBlockPlaced(DraggableBlockView view)
        {
            view.OnPlaced -= HandleBlockPlaced;

            // 그리드에 설치되어 트레이를 이탈한 블록을 리스트에서 제거
            spawnedBlocks.Remove(view);

            // 빈자리를 채우기 위해 새 블록 생성 (리스트 끝에 추가됨)
            SpawnBlock();

            // 인덱스 범위 초과 방지 보정
            if (currentIndex >= spawnedBlocks.Count)
            {
                currentIndex = Mathf.Max(0, spawnedBlocks.Count - 1);
            }

            // 트레이 화면 갱신
            UpdateTrayDisplay();
        }

        /// <summary>
        /// 이전 화살표 버튼 클릭 시
        /// </summary>
        public void OnClickPrev()
        {
            if (spawnedBlocks.Count == 0) return;

            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = loopNavigation ? spawnedBlocks.Count - 1 : 0;
            }

            UpdateTrayDisplay();
        }

        /// <summary>
        /// 다음 화살표 버튼 클릭 시
        /// </summary>
        public void OnClickNext()
        {
            if (spawnedBlocks.Count == 0) return;

            currentIndex++;
            if (currentIndex >= spawnedBlocks.Count)
            {
                currentIndex = loopNavigation ? 0 : spawnedBlocks.Count - 1;
            }

            UpdateTrayDisplay();
        }

        /// <summary>
        /// 현재 currentIndex에 해당하는 블록만 보여주고 나머지는 숨깁니다.
        /// </summary>
        private void UpdateTrayDisplay()
        {
            for (int i = 0; i < spawnedBlocks.Count; i++)
            {
                bool isActive = (i == currentIndex);
                spawnedBlocks[i].gameObject.SetActive(isActive);

                // 활성화된 블록은 슬롯 정중앙 위치로 초기화
                if (isActive)
                {
                    var rt = spawnedBlocks[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = Vector2.zero;
                }
            }

            // 순환 모드가 아닌 경우 버튼 활성/비활성화 처리
            if (!loopNavigation)
            {
                if (prevButton != null) prevButton.interactable = (currentIndex > 0);
                if (nextButton != null) nextButton.interactable = (currentIndex < spawnedBlocks.Count - 1);
            }
        }
    }
}