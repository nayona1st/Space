using UnityEngine;
using UnityEngine.InputSystem;

namespace Dev.CSU._02_Scripts.SpaceShip
{
    [DisallowMultipleComponent]
    public sealed class RocketTurnInput : MonoBehaviour
    {
        public float TurnInput { get; private set; }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
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
            TurnInput = 0f;
        }
    }
}
