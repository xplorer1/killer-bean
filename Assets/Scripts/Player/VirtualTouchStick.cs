using UnityEngine;
using UnityEngine.EventSystems;

namespace BeanBlaster.Player
{
    public enum VirtualTouchStickRole
    {
        Move,
        AimAndFire
    }

    /// <summary>Converts one owned touch pointer into normalized movement or aim input.</summary>
    public sealed class VirtualTouchStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private MobileInputState input;
        [SerializeField] private RectTransform interactionArea;
        [SerializeField] private RectTransform handle;
        [SerializeField] private VirtualTouchStickRole role;
        [SerializeField, Range(0f, 0.95f)] private float deadZone = 0.1f;

        private const int NoPointer = int.MinValue;
        private int activePointerId = NoPointer;

        /// <summary>Assigns dependencies when the touch HUD is generated.</summary>
        public void Configure(
            MobileInputState inputState,
            RectTransform area,
            RectTransform visualHandle,
            VirtualTouchStickRole stickRole)
        {
            input = inputState;
            interactionArea = area;
            handle = visualHandle;
            role = stickRole;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                UpdateValue(eventData);
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

        private void UpdateValue(PointerEventData eventData)
        {
            if (input == null || interactionArea == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    interactionArea,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            Rect rect = interactionArea.rect;
            float radius = Mathf.Max(1f, Mathf.Min(rect.width, rect.height) * 0.5f);
            Vector2 value = Vector2.ClampMagnitude(localPoint / radius, 1f);
            if (value.magnitude < deadZone)
            {
                value = Vector2.zero;
            }

            if (handle != null)
            {
                handle.anchoredPosition = value * radius;
            }

            if (role == VirtualTouchStickRole.Move)
            {
                input.SetMove(value);
            }
            else
            {
                input.SetAim(value, true);
            }
        }

        private void ReleasePointer()
        {
            activePointerId = NoPointer;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (input == null)
            {
                return;
            }

            if (role == VirtualTouchStickRole.Move)
            {
                input.SetMove(Vector2.zero);
            }
            else
            {
                input.SetAim(Vector2.zero, false);
            }
        }

        private void OnDisable()
        {
            ReleasePointer();
        }

        private void OnValidate()
        {
            deadZone = Mathf.Clamp(deadZone, 0f, 0.95f);
        }
    }
}
