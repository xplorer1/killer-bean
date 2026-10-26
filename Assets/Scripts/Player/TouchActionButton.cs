using UnityEngine;
using UnityEngine.EventSystems;

namespace BeanBlaster.Player
{
    public enum TouchActionButtonRole
    {
        Jump,
        NextWeapon,
        PreviousWeapon,
        Pause
    }

    /// <summary>Converts one owned touch pointer into a held or momentary mobile action.</summary>
    public sealed class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private MobileInputState input;
        [SerializeField] private TouchActionButtonRole role;

        private const int NoPointer = int.MinValue;
        private int activePointerId = NoPointer;

        /// <summary>Assigns dependencies when the touch HUD is generated.</summary>
        public void Configure(MobileInputState inputState, TouchActionButtonRole buttonRole)
        {
            input = inputState;
            role = buttonRole;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer || input == null)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            switch (role)
            {
                case TouchActionButtonRole.Jump:
                    input.SetJumpHeld(true);
                    break;
                case TouchActionButtonRole.NextWeapon:
                    input.PressNextWeapon();
                    break;
                case TouchActionButtonRole.PreviousWeapon:
                    input.PressPreviousWeapon();
                    break;
                case TouchActionButtonRole.Pause:
                    input.PressPause();
                    break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            ReleasePointer();
        }

        private void ReleasePointer()
        {
            activePointerId = NoPointer;
            if (input != null && role == TouchActionButtonRole.Jump)
            {
                input.SetJumpHeld(false);
            }
        }

        private void OnDisable()
        {
            ReleasePointer();
        }
    }
}
