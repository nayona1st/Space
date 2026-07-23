using UnityEngine;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    public class InventoryGridView : MonoBehaviour
    {
        [SerializeField] private InventoryGrid grid;
        [SerializeField] private float cellSize = 64f;
        [SerializeField] private Image cellPrefab; // 1칸짜리 배경 Image 프리팹
        [SerializeField] private Color emptyColor = Color.white;
        [SerializeField] private Color validPreviewColor = new Color(0.4f, 1f, 0.4f);
        [SerializeField] private Color invalidPreviewColor = new Color(1f, 0.4f, 0.4f);
 
        private RectTransform rect;
        private Image[,] cellImages;
        private Canvas parentCanvas;
 
        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            BuildGrid();
        }
 
        private void BuildGrid()
        {
            cellImages = new Image[grid.Width, grid.Height];
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var img = Instantiate(cellPrefab, rect);
                    var rt = img.rectTransform;
                    rt.sizeDelta = new Vector2(cellSize, cellSize);
                    rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                    img.color = emptyColor;
                    cellImages[x, y] = img;
                }
            }
        }
 
        // 스크린 좌표를 그리드 셀 좌표로 변환 (블록의 origin 후보)
        public bool ScreenToGridCell(Vector2 screenPos, Camera cam, out Vector2Int cell)
        {
            // Canvas가 Overlay 모드일 때는 eventCamera 대신 null을 전달해야 좌표가 정확함
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cam = null;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, cam, out var local))
            {
                // rect의 Pivot 설정(기본 0.5, 0.5 등)에 상관없이 
                // 항상 그리드 패널의 '좌측 상단'을 (0,0) 기준으로 보정하는 공식
                float originX = local.x + (rect.rect.width * rect.pivot.x);
                float originY = (rect.rect.height * (1f - rect.pivot.y)) - local.y;

                int x = Mathf.FloorToInt(originX / cellSize);
                int y = Mathf.FloorToInt(originY / cellSize);

                cell = new Vector2Int(x, y);
                return grid.IsInside(cell);
            }

            cell = Vector2Int.zero;
            return false;
        }
 
        // 셀 좌표를 anchoredPosition으로 변환 (블록 스냅용)
        public Vector2 GridCellToAnchoredPosition(Vector2Int cell)
            => new Vector2(cell.x * cellSize, -cell.y * cellSize);
 
        public float CellSize => cellSize;
 
        public void ShowPreview(BlockInstance instance, bool valid)
        {
            ClearPreview();
            var color = valid ? validPreviewColor : invalidPreviewColor;
            foreach (var c in instance.GetOccupiedCells())
            {
                if (grid.IsInside(c))
                    cellImages[c.x, c.y].color = color;
            }
        }
 
        public void ClearPreview()
        {
            for (int y = 0; y < grid.Height; y++)
                for (int x = 0; x < grid.Width; x++)
                    cellImages[x, y].color = emptyColor;
        }
    }
}