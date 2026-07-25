using UnityEngine;
using UnityEngine.InputSystem;

namespace Dev.CSU._02_Scripts.SpaceShip
{
    [DisallowMultipleComponent]
    public sealed class RocketTurnInput : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;

        public float TurnInput { get; private set; }

        private bool enabledMoveActionLocally;

        private void OnEnable()
        {
            InputAction action = moveAction?.action;
            if (action != null && !action.enabled)
            {
                action.Enable();
                enabledMoveActionLocally = true;
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            InputAction action = moveAction?.action;
            if (action != null)
            {
                TurnInput = Mathf.Clamp(
                    action.ReadValue<Vector2>().x,
                    -1f,
                    1f);
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                TurnInput = 0f;
                return;
            }

            bool leftHeld = keyboard.aKey.isPressed;
            bool rightHeld = keyboard.dKey.isPressed;
#else
            bool leftHeld = Input.GetKey(KeyCode.A);
            bool rightHeld = Input.GetKey(KeyCode.D);
#endif

            if (leftHeld == rightHeld)
            {
                TurnInput = 0f;
                return;
            }

            TurnInput = leftHeld ? -1f : 1f;
        }

        private void OnDisable()
        {
            InputAction action = moveAction?.action;
            if (enabledMoveActionLocally &&
                action != null &&
                action.enabled)
            {
                action.Disable();
            }

            enabledMoveActionLocally = false;
            TurnInput = 0f;
        }
    }
}
