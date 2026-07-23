using UnityEngine;

namespace Dev.NKY.Scripts
{
    // 대기 영역의 슬롯 하나. "어떤 블록/스탯을 줄지" 결정하고,
    // 배치되면 새 블록으로 다시 채우는 역할만 함.
    // DraggableBlockView가 어떻게 그려지고 드래그되는지는 전혀 모름 (SRP)
    [RequireComponent(typeof(RectTransform))]
    public class InventorySlot : MonoBehaviour
    {
        [SerializeField] private DraggableBlockView blockPrefab;
        [SerializeField] private BlockDataList blockDataList;
        [SerializeField] private StatDataListSo statDataList;
        [SerializeField] private UnityEngine.UI.Image background; // 슬롯 테두리/배경 (선택, 없어도 동작함)

        // 씬 오브젝트라 여기엔 정상적으로 연결 가능. 블록 생성 시 이걸 넘겨줌.
        [SerializeField] private InventoryGrid grid;
        [SerializeField] private InventoryGridView gridView;

        private DraggableBlockView current;

        // 동적으로 생성될 때(스포너가 Instantiate) 씬 참조를 나중에 채워 넣기 위한 용도.
        // 씬에 직접 배치해서 Inspector로 연결한 경우엔 안 써도 됨.
        public void SetGridReferences(InventoryGrid gridRef, InventoryGridView gridViewRef)
        {
            grid = gridRef;
            gridView = gridViewRef;
        }

        private void Start()
        {
            SpawnNewBlock();
        }

        private void SpawnNewBlock()
        {
            if (blockDataList == null || blockDataList.blocks.Count == 0) return;
            if (statDataList == null || statDataList.statDataList.Count == 0) return;

            var blockData = blockDataList.blocks[Random.Range(0, blockDataList.blocks.Count)];
            var statData = statDataList.statDataList[Random.Range(0, statDataList.statDataList.Count)];

            current = Instantiate(blockPrefab, transform);
            current.Initialize(blockData, statData, grid, gridView);
            current.OnPlaced += HandleBlockPlaced;
        }

        private void HandleBlockPlaced(DraggableBlockView view)
        {
            view.OnPlaced -= HandleBlockPlaced;
            current = null;
            SpawnNewBlock();
        }
    }
}