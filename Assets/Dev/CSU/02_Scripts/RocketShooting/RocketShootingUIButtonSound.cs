using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dev.CSU._02_Scripts.RocketShooting
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class RocketShootingUIButtonSound :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerClickHandler,
        ISubmitHandler
    {
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlay())
            {
                return;
            }

            RocketShootingSoundPlayer.Play(
                RocketShootingSoundCue.UIHover);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanPlay())
            {
                return;
            }

            RocketShootingSoundPlayer.Play(
                RocketShootingSoundCue.UIClick);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!CanPlay())
            {
                return;
            }

            RocketShootingSoundPlayer.Play(
                RocketShootingSoundCue.UIClick);
        }

        private bool CanPlay()
        {
            return isActiveAndEnabled
                && _button != null
                && _button.IsInteractable();
        }
    }
}
