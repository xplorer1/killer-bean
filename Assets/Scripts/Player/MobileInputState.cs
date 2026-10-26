using System;
using UnityEngine;

namespace BeanBlaster.Player
{
    /// <summary>Stores touch-control state for consumption by the platform-neutral player input reader.</summary>
    public sealed class MobileInputState : MonoBehaviour
    {
        private Vector2 move;
        private Vector2 aimDirection = Vector2.right;
        private bool hasAimDirection;
        private bool aimEngaged;
        private bool jumpHeld;
        private bool jumpPressPending;
        private bool fireHeld;

        public event Action NextWeaponPressed;
        public event Action PrevWeaponPressed;
        public event Action PausePressed;

        public Vector2 Move => move;
        public Vector2 AimDirection => aimDirection;
        public bool HasAimDirection => hasAimDirection;
        public bool AimEngaged => aimEngaged;
        public bool JumpHeld => jumpHeld;
        public bool FireHeld => fireHeld;

        /// <summary>Sets normalized movement from a virtual stick.</summary>
        public void SetMove(Vector2 value)
        {
            move = Vector2.ClampMagnitude(value, 1f);
        }

        /// <summary>Sets aim from the aim/fire stick and records whether that stick is held.</summary>
        public void SetAim(Vector2 value, bool engaged)
        {
            aimEngaged = engaged;
            if (value.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            aimDirection = value.normalized;
            hasAimDirection = true;
        }

        /// <summary>Sets the jump button state and latches each new press until gameplay consumes it.</summary>
        public void SetJumpHeld(bool held)
        {
            if (held && !jumpHeld)
            {
                jumpPressPending = true;
            }

            jumpHeld = held;
        }

        /// <summary>Returns and clears the pending jump-press edge.</summary>
        public bool ConsumeJumpPress()
        {
            bool wasPending = jumpPressPending;
            jumpPressPending = false;
            return wasPending;
        }

        /// <summary>Sets whether the touch fire control is held.</summary>
        public void SetFireHeld(bool held)
        {
            fireHeld = held;
        }

        /// <summary>Signals a touch request to select the next owned weapon.</summary>
        public void PressNextWeapon()
        {
            NextWeaponPressed?.Invoke();
        }

        /// <summary>Signals a touch request to select the previous owned weapon.</summary>
        public void PressPreviousWeapon()
        {
            PrevWeaponPressed?.Invoke();
        }

        /// <summary>Signals a touch request to pause or resume gameplay.</summary>
        public void PressPause()
        {
            PausePressed?.Invoke();
        }

        /// <summary>Releases held controls when the HUD or application is disabled.</summary>
        public void ReleaseAll()
        {
            move = Vector2.zero;
            aimEngaged = false;
            jumpHeld = false;
            jumpPressPending = false;
            fireHeld = false;
        }

        private void OnDisable()
        {
            ReleaseAll();
        }
    }
}
