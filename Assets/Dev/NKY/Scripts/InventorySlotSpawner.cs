using UnityEngine;

namespace Dev.NKY.Scripts
{
    // ScrollRect의 Content 밑에 InventorySlot을 slotCount개 생성.
    // 슬롯 프리팹엔 씬 참조를 못 넣으므로, 생성 직후 SetGridReferences로 주입.
    public class InventorySlotSpawner : MonoBehaviour
    {
        [SerializeField] private InventorySlot slotPrefab;
        [SerializeField] private Transform content; // ScrollRect > Viewport > Content
        [SerializeField] private int slotCount = 8;

        [SerializeField] private InventoryGrid grid;
        [SerializeField] private InventoryGridView gridView;

        private void Awake()
        {
            for (int i = 0; i < slotCount; i++)
            {
                InventorySlot slot = Instantiate(slotPrefab, content);
                slot.SetGridReferences(grid, gridView);
            }
        }
    }
}