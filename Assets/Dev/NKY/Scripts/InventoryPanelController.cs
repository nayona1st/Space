using UnityEngine;
using DG.Tweening;

namespace Dev.NKY.Scripts
{
    public class InventoryPanelController : MonoBehaviour
    {
        [Header("Target UI")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private Ease openEase = Ease.OutCubic;
        [SerializeField] private Ease closeEase = Ease.InCubic;

        [Header("Initial State")]
        [SerializeField] private bool startOpen = true;

        private Tween activeTween;
        private bool isOpen;
        private float originalWidth; // 인스펙터에 설정된 원래 너비 저장용

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (panelRect != null)
            {
                // 시작할 때 원래 패널의 너비(Width)를 기록해 둡니다.
                originalWidth = panelRect.sizeDelta.x;
            }
        }

        private void Start()
        {
            InitState(startOpen);
        }

        private void OnDisable()
        {
            activeTween?.Kill();
            activeTween = null;
        }

        private void InitState(bool show)
        {
            isOpen = show;

            if (panelRect != null)
            {
                // 너비를 원래 값 또는 0으로 초기화
                panelRect.sizeDelta = new Vector2(show ? originalWidth : 0f, panelRect.sizeDelta.y);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.interactable = show;
                canvasGroup.blocksRaycasts = show;
            }
        }

        public void ToggleUI()
        {
            if (isOpen) HideUI();
            else ShowUI();
        }

        public void ShowUI()
        {
            if (isOpen && activeTween != null && activeTween.IsActive()) return;

            isOpen = true;
            activeTween?.Kill();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // 너비(Width)를 0 -> 원래 너비로 늘려서 커튼 열리듯 가림 해제
            activeTween = panelRect.DOSizeDelta(new Vector2(originalWidth, panelRect.sizeDelta.y), duration)
                .SetEase(openEase)
                .SetUpdate(true);
        }

        public void HideUI()
        {
            if (!isOpen && activeTween != null && activeTween.IsActive()) return;

            isOpen = false;
            activeTween?.Kill();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // 너비(Width)를 원래 너비 -> 0으로 줄여서 커튼 닫히듯 가리기
            activeTween = panelRect.DOSizeDelta(new Vector2(0f, panelRect.sizeDelta.y), duration)
                .SetEase(closeEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (canvasGroup != null) canvasGroup.alpha = 0f;
                });
        }
    }
}
