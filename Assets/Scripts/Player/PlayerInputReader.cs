using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BeanBlaster.Player
{
    /// <summary>Owns code-defined player Input Actions and exposes frame-stable gameplay input.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private MobileInputState mobileInput;
        [SerializeField, Range(0f, 1f)] private float gamepadAimDeadzone = 0.3f;

        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction fireAction;
        private InputAction aimPointerAction;
        private InputAction aimStickAction;
        private InputAction nextWeaponAction;
        private InputAction previousWeaponAction;
        private InputAction weaponScrollAction;
        private InputAction pauseAction;
        private InputAction[] selectWeaponActions;
        private Vector2 aimDirection = Vector2.right;
        private bool usingGamepadAim;
        private bool actionsCreated;

        public event Action NextWeaponPressed;
        public event Action PrevWeaponPressed;
        public event Action PausePressed;
        public event Action<int> SelectWeaponPressed;

        public float MoveX
        {
            get
            {
                float deviceMove = moveAction?.ReadValue<float>() ?? 0f;
                float touchMove = mobileInput == null ? 0f : mobileInput.Move.x;
                return Mathf.Abs(touchMove) > Mathf.Abs(deviceMove) ? touchMove : deviceMove;
            }
        }

        public bool JumpPressedThisFrame
        {
            get
            {
                bool touchPressed = mobileInput != null && mobileInput.ConsumeJumpPress();
                return touchPressed || (jumpAction?.WasPressedThisFrame() ?? false);
            }
        }

        public bool JumpHeld => (jumpAction?.IsPressed() ?? false) || (mobileInput != null && mobileInput.JumpHeld);
        public bool FireHeld => (fireAction?.IsPressed() ?? false) ||
                                (mobileInput != null && (mobileInput.FireHeld || mobileInput.AimEngaged));
        public Vector2 AimDirection => aimDirection;
        public bool UsingGamepadAim => usingGamepadAim;

        private void Awake()
        {
            CreateActions();
        }

        private void OnEnable()
        {
            CreateActions();
            SetActionsEnabled(true);
            SubscribeToMobileInput();
        }

        private void OnDisable()
        {
            if (actionsCreated)
            {
                SetActionsEnabled(false);
            }

            UnsubscribeFromMobileInput();
        }

        private void OnDestroy()
        {
            if (!actionsCreated)
            {
                return;
            }

            moveAction.Dispose();
            jumpAction.Dispose();
            fireAction.Dispose();
            aimPointerAction.Dispose();
            aimStickAction.Dispose();
            nextWeaponAction.Dispose();
            previousWeaponAction.Dispose();
            weaponScrollAction.Dispose();
            pauseAction.Dispose();
            foreach (InputAction action in selectWeaponActions)
            {
                action.Dispose();
            }

            actionsCreated = false;
        }

        private void Update()
        {
            UpdateAim();

            float scroll = weaponScrollAction.ReadValue<float>();
            if (scroll > 0f)
            {
                NextWeaponPressed?.Invoke();
            }
            else if (scroll < 0f)
            {
                PrevWeaponPressed?.Invoke();
            }
        }

        /// <summary>Sets the world-space origin used to convert pointer position into an aim direction.</summary>
        public void SetAimOrigin(Transform origin)
        {
            aimOrigin = origin;
        }

        /// <summary>Connects the touch HUD input state used by mobile builds.</summary>
        public void SetMobileInput(MobileInputState value)
        {
            if (mobileInput == value)
            {
                return;
            }

            UnsubscribeFromMobileInput();
            mobileInput = value;
            if (isActiveAndEnabled)
            {
                SubscribeToMobileInput();
            }
        }

        private void CreateActions()
        {
            if (actionsCreated)
            {
                return;
            }

            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
            moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick/x");
            moveAction.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/dpad/left").With("Positive", "<Gamepad>/dpad/right");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            fireAction = new InputAction("Fire", InputActionType.Button);
            fireAction.AddBinding("<Mouse>/leftButton");
            fireAction.AddBinding("<Gamepad>/rightTrigger");

            aimPointerAction = new InputAction("AimPointer", InputActionType.PassThrough, "<Pointer>/position", expectedControlType: "Vector2");
            aimStickAction = new InputAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");

            nextWeaponAction = new InputAction("NextWeapon", InputActionType.Button);
            nextWeaponAction.AddBinding("<Keyboard>/e");
            nextWeaponAction.AddBinding("<Gamepad>/rightShoulder");
            nextWeaponAction.performed += OnNextWeapon;

            previousWeaponAction = new InputAction("PrevWeapon", InputActionType.Button);
            previousWeaponAction.AddBinding("<Keyboard>/q");
            previousWeaponAction.AddBinding("<Gamepad>/leftShoulder");
            previousWeaponAction.performed += OnPreviousWeapon;

            weaponScrollAction = new InputAction("WeaponScroll", InputActionType.PassThrough, "<Mouse>/scroll/y", expectedControlType: "Axis");

            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Keyboard>/p");
            pauseAction.AddBinding("<Gamepad>/start");
            pauseAction.performed += OnPause;

            selectWeaponActions = new InputAction[5];
            for (int index = 0; index < selectWeaponActions.Length; index++)
            {
                int capturedIndex = index;
                InputAction action = new InputAction($"SelectWeapon{index + 1}", InputActionType.Button);
                action.AddBinding($"<Keyboard>/digit{index + 1}");
                action.performed += _ => SelectWeaponPressed?.Invoke(capturedIndex);
                selectWeaponActions[index] = action;
            }

            actionsCreated = true;
        }

        private void SetActionsEnabled(bool enabled)
        {
            SetEnabled(moveAction, enabled);
            SetEnabled(jumpAction, enabled);
            SetEnabled(fireAction, enabled);
            SetEnabled(aimPointerAction, enabled);
            SetEnabled(aimStickAction, enabled);
            SetEnabled(nextWeaponAction, enabled);
            SetEnabled(previousWeaponAction, enabled);
            SetEnabled(weaponScrollAction, enabled);
            SetEnabled(pauseAction, enabled);
            foreach (InputAction action in selectWeaponActions)
            {
                SetEnabled(action, enabled);
            }
        }

        private void UpdateAim()
        {
            if (mobileInput != null && mobileInput.HasAimDirection)
            {
                aimDirection = mobileInput.AimDirection;
                usingGamepadAim = false;
                if (mobileInput.AimEngaged || Application.isMobilePlatform)
                {
                    return;
                }
            }

            Vector2 stick = aimStickAction.ReadValue<Vector2>();
            if (stick.magnitude > gamepadAimDeadzone)
            {
                aimDirection = stick.normalized;
                usingGamepadAim = true;
                return;
            }

            bool pointerMoved = Pointer.current != null && Pointer.current.delta.ReadValue().sqrMagnitude > 0f;
            if (!pointerMoved && usingGamepadAim)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null || aimOrigin == null)
            {
                return;
            }

            Vector2 screenPosition = aimPointerAction.ReadValue<Vector2>();
            float distanceFromCamera = Mathf.Abs(aimOrigin.position.z - mainCamera.transform.position.z);
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
            Vector2 direction = worldPosition - aimOrigin.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            aimDirection = direction.normalized;
            usingGamepadAim = false;
        }

        private void OnNextWeapon(InputAction.CallbackContext context)
        {
            NextWeaponPressed?.Invoke();
        }

        private void OnPreviousWeapon(InputAction.CallbackContext context)
        {
            PrevWeaponPressed?.Invoke();
        }

        private void OnPause(InputAction.CallbackContext context)
        {
            PausePressed?.Invoke();
        }

        private void SubscribeToMobileInput()
        {
            if (mobileInput == null)
            {
                return;
            }

            mobileInput.NextWeaponPressed -= OnMobileNextWeapon;
            mobileInput.PrevWeaponPressed -= OnMobilePreviousWeapon;
            mobileInput.PausePressed -= OnMobilePause;
            mobileInput.NextWeaponPressed += OnMobileNextWeapon;
            mobileInput.PrevWeaponPressed += OnMobilePreviousWeapon;
            mobileInput.PausePressed += OnMobilePause;
        }

        private void UnsubscribeFromMobileInput()
        {
            if (mobileInput == null)
            {
                return;
            }

            mobileInput.NextWeaponPressed -= OnMobileNextWeapon;
            mobileInput.PrevWeaponPressed -= OnMobilePreviousWeapon;
            mobileInput.PausePressed -= OnMobilePause;
        }

        private void OnMobileNextWeapon()
        {
            NextWeaponPressed?.Invoke();
        }

        private void OnMobilePreviousWeapon()
        {
            PrevWeaponPressed?.Invoke();
        }

        private void OnMobilePause()
        {
            PausePressed?.Invoke();
        }

        private static void SetEnabled(InputAction action, bool enabled)
        {
            if (enabled)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }
    }
}
