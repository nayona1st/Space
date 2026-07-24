using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dev.NKY.Scripts
{
    [RequireComponent(typeof(Image))]
    public class TrashCanUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.red; // 올려놓았을 때 바뀔 색상
        [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

        private Image image;
        private Vector3 originalScale;

        private void Awake()
        {
            image = GetComponent<Image>();
            originalScale = transform.localScale;
        }

        // 블록을 끌고 휴지통 영역 안으로 들어왔을 때
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null && eventData.pointerDrag.GetComponent<DraggableBlockView>() != null)
            {
                image.color = highlightColor;
                transform.localScale = Vector3.Scale(originalScale, hoverScale);
            }
        }

        // 휴지통 영역을 벗어났을 때
        public void OnPointerExit(PointerEventData eventData)
        {
            ResetVisual();
        }

        public void ResetVisual()
        {
            image.color = normalColor;
            transform.localScale = originalScale;
        }
    }
}