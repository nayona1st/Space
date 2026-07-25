using UnityEngine;
using UnityEngine.EventSystems;

namespace Dev.NKY.Scripts
{
    public class UISoundHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("UI Sound Data Settings")]
        [Tooltip("마우스 커서를 UI 위에 올렸을 때 재생할 사운드")]
        [SerializeField] private SoundDataSO pointerEnterSound;

        [Tooltip("마우스 커서가 UI 밖으로 나갔을 때 재생할 사운드")]
        [SerializeField] private SoundDataSO pointerExitSound;

        [Tooltip("UI를 클릭했을 때 재생할 사운드")]
        [SerializeField] private SoundDataSO clickSound;

        /// <summary>
        /// 마우스 커서가 UI 영역 내부로 진입했을 때 호출
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pointerEnterSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayUI(pointerEnterSound);
            }
        }

        /// <summary>
        /// 마우스 커서가 UI 영역 밖으로 벗어났을 때 호출
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (pointerExitSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayUI(pointerExitSound);
            }
        }

        /// <summary>
        /// UI를 클릭했을 때 호출
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayUI(clickSound);
            }
        }
    }
}
