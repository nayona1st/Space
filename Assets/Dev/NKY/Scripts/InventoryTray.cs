using UnityEngine;

namespace Dev.NKY.Scripts
{
    // 화면에 보이지 않는 순수 데이터 보관함.
    // "블록 하나 랜덤으로 꺼내줘"라는 요청만 처리함. 스폰/UI/드래그는 전혀 모름 (SRP)
    public class InventoryTray : MonoBehaviour
    {
        [SerializeField] private BlockDataList blockDataList;
        [SerializeField] private MachinePartsListSo statDataList;

        // 보관함에서 하나 꺼내줌. 데이터가 없으면 false.
        public bool TryTakeRandom(out BlockData block, out MachinePartsDataSo stat)
        {
            if (blockDataList == null || blockDataList.blocks.Count == 0 ||
                statDataList == null || statDataList.machineParts.Count == 0)
            {
                block = null;
                stat = null;
                return false;
            }

            block = blockDataList.blocks[Random.Range(0, blockDataList.blocks.Count)];
            stat = statDataList.machineParts[Random.Range(0, statDataList.machineParts.Count)];
            return true;
        }
    }
}