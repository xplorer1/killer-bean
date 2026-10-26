using System.Collections;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Level;
using UnityEngine;

namespace BeanBlaster.Player
{
    /// <summary>Controls velocity-based movement, triple jumping, hover, aiming, damage reactions, and respawn.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Health))]
    [RequireComponent(typeof(PlayerInputReader), typeof(SpriteRenderer))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CapsuleCollider2D bodyCollider;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Health health;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private LayerMask groundLayerMask;

        [Header("Horizontal Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0f)] private float groundAcceleration = 70f;
        [SerializeField, Min(0f)] private float groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 45f;
        [SerializeField, Min(0f)] private float airDeceleration = 30f;

        [Header("Gravity and Hover")]
        [SerializeField, Min(0f)] private float baseGravityScale = 3.5f;
        [SerializeField, Min(0f)] private float hoverGravityScale = 0.15f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 20f;
        [SerializeField, Min(0f)] private float maxHoverFallSpeed = 3f;

        [Header("Jumping")]
        [SerializeField, Min(0f)] private float firstJumpVelocity = 14f;
        [SerializeField, Min(0f)] private float extraJumpVelocity = 12f;
        [SerializeField, Min(1)] private int maxJumps = 3;
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Min(0f)] private float groundCheckRadius = 0.15f;
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.8f);

        [Header("Damage Response")]
        [SerializeField, Min(0f)] private float knockbackHorizontal = 8f;
        [SerializeField, Min(0f)] private float knockbackVertical = 6f;
        [SerializeField, Min(0f)] private float knockbackControlLock = 0.2f;
        [SerializeField, Min(0.01f)] private float invulnerabilityFlashInterval = 0.1f;

        private float moveInput;
        private bool jumpHeld;
        private bool jumpHeldPrevious;
        private bool jumpReleasedSinceFixedUpdate;
        private bool fireHeld;
        private float lastGroundedTime = float.NegativeInfinity;
        private float lastJumpPressedTime = float.NegativeInfinity;
        private float controlLockedUntil;
        private int jumpsUsed;
        private bool grounded;
        private bool wasGrounded;
        private bool jumpCutApplied;
        private bool dead;
        private Coroutine flashRoutine;

        public bool IsHovering { get; private set; }
        public bool IsGrounded => grounded;
        public bool IsAlive => !dead && health != null && health.IsAlive;
        public int JumpsRemaining => Mathf.Max(0, maxJumps - jumpsUsed);
        public Vector2 AimDirection => input == null ? Vector2.right : input.AimDirection;

        private void Awake()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<CapsuleCollider2D>();
            bodyRenderer ??= GetComponent<SpriteRenderer>();
            health ??= GetComponent<Health>();
            input ??= GetComponent<PlayerInputReader>();

            body.gravityScale = baseGravityScale;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (weaponPivot != null)
            {
                input.SetAimOrigin(weaponPivot);
            }
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Update()
        {
            if (dead)
            {
                moveInput = 0f;
                fireHeld = false;
                return;
            }

            if (levelManager != null && transform.position.y < levelManager.KillPlaneY)
            {
                health.TryApplyDamage(new DamageInfo(
                    health.CurrentHealth + 1f,
                    transform.position,
                    Vector2.zero,
                    0f,
                    Team.Neutral,
                    DamageType.KillPlane,
                    true));
                return;
            }

            moveInput = input.MoveX;
            jumpHeldPrevious = jumpHeld;
            jumpHeld = input.JumpHeld;
            jumpReleasedSinceFixedUpdate |= jumpHeldPrevious && !jumpHeld;
            fireHeld = input.FireHeld;
            if (input.JumpPressedThisFrame)
            {
                lastJumpPressedTime = Time.time;
            }

            UpdateAimVisuals();
        }

        private void FixedUpdate()
        {
            if (dead)
            {
                return;
            }

            UpdateGroundedState();
            ProcessJump();
            ProcessJumpCut();
            ProcessHorizontalMovement();
            ProcessHoverAndFallClamp();

            wasGrounded = grounded;
            jumpReleasedSinceFixedUpdate = false;
        }

        /// <summary>Restores the player at a checkpoint with full health and movement state.</summary>
        public void RespawnAt(Vector3 position)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            transform.position = position;
            body.velocity = Vector2.zero;
            body.gravityScale = baseGravityScale;
            jumpsUsed = 0;
            grounded = false;
            wasGrounded = false;
            jumpCutApplied = false;
            jumpReleasedSinceFixedUpdate = false;
            lastGroundedTime = float.NegativeInfinity;
            lastJumpPressedTime = float.NegativeInfinity;
            controlLockedUntil = 0f;
            IsHovering = false;
            dead = false;
            bodyCollider.enabled = true;
            bodyRenderer.enabled = true;
            health.ResetHealth();
            input.enabled = true;
        }

        private void UpdateGroundedState()
        {
            Vector2 checkPosition = transform.TransformPoint(groundCheckOffset);
            grounded = body.velocity.y <= 0.01f &&
                       Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayerMask) != null;
            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (!wasGrounded)
                {
                    jumpsUsed = 0;
                }
            }
            else if (wasGrounded && jumpsUsed == 0)
            {
                lastGroundedTime = Time.time;
            }

            if (!grounded && jumpsUsed == 0 && Time.time > lastGroundedTime + coyoteTime)
            {
                jumpsUsed = 1;
            }
        }

        private void ProcessJump()
        {
            if (Time.time > lastJumpPressedTime + jumpBufferTime)
            {
                return;
            }

            bool canGroundJump = grounded || Time.time <= lastGroundedTime + coyoteTime;
            if (canGroundJump)
            {
                SetVerticalVelocity(firstJumpVelocity);
                jumpsUsed = 1;
            }
            else if (jumpsUsed < maxJumps)
            {
                SetVerticalVelocity(extraJumpVelocity);
                jumpsUsed++;
            }
            else
            {
                return;
            }

            lastJumpPressedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            grounded = false;
            jumpCutApplied = false;
        }

        private void ProcessJumpCut()
        {
            if (!jumpCutApplied && jumpReleasedSinceFixedUpdate && body.velocity.y > 0f)
            {
                SetVerticalVelocity(body.velocity.y * jumpCutMultiplier);
                jumpCutApplied = true;
            }
        }

        private void ProcessHorizontalMovement()
        {
            if (Time.time < controlLockedUntil)
            {
                return;
            }

            float targetVelocity = moveInput * moveSpeed;
            bool accelerating = Mathf.Abs(moveInput) > Mathf.Epsilon &&
                                (Mathf.Abs(body.velocity.x) <= Mathf.Epsilon || Mathf.Sign(moveInput) == Mathf.Sign(body.velocity.x));
            float acceleration = grounded
                ? (accelerating ? groundAcceleration : groundDeceleration)
                : (accelerating ? airAcceleration : airDeceleration);
            Vector2 velocity = body.velocity;
            velocity.x = Mathf.MoveTowards(velocity.x, targetVelocity, acceleration * Time.fixedDeltaTime);
            body.velocity = velocity;
        }

        private void ProcessHoverAndFallClamp()
        {
            IsHovering = !grounded && fireHeld && Time.time >= controlLockedUntil;
            body.gravityScale = IsHovering ? hoverGravityScale : baseGravityScale;
            float minimumVelocity = IsHovering ? -maxHoverFallSpeed : -maxFallSpeed;
            if (body.velocity.y < minimumVelocity)
            {
                SetVerticalVelocity(minimumVelocity);
            }
        }

        private void UpdateAimVisuals()
        {
            if (weaponPivot == null)
            {
                return;
            }

            Vector2 aim = input.AimDirection;
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            bool facingRight = angle >= -90f && angle <= 90f;
            bodyRenderer.flipX = !facingRight;
            weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            weaponPivot.localScale = facingRight ? Vector3.one : new Vector3(1f, -1f, 1f);
        }

        private void OnDamaged(DamageInfo info)
        {
            float horizontalDirection = Mathf.Sign(transform.position.x - info.sourcePosition.x);
            if (Mathf.Approximately(horizontalDirection, 0f))
            {
                horizontalDirection = 1f;
            }

            body.velocity = new Vector2(horizontalDirection * knockbackHorizontal, knockbackVertical);
            controlLockedUntil = Time.time + knockbackControlLock;
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashWhileInvulnerable());
        }

        private void OnDied(DamageInfo info)
        {
            if (dead)
            {
                return;
            }

            dead = true;
            IsHovering = false;
            input.enabled = false;
            bodyCollider.enabled = false;
            body.velocity = Vector2.zero;
            body.gravityScale = baseGravityScale;
            levelManager?.OnPlayerDied(this);
        }

        private IEnumerator FlashWhileInvulnerable()
        {
            while (!dead && health.IsInvulnerable)
            {
                bodyRenderer.enabled = !bodyRenderer.enabled;
                yield return new WaitForSeconds(invulnerabilityFlashInterval);
            }

            bodyRenderer.enabled = true;
            flashRoutine = null;
        }

        private void SetVerticalVelocity(float velocityY)
        {
            Vector2 velocity = body.velocity;
            velocity.y = velocityY;
            body.velocity = velocity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            groundAcceleration = Mathf.Max(0f, groundAcceleration);
            groundDeceleration = Mathf.Max(0f, groundDeceleration);
            airAcceleration = Mathf.Max(0f, airAcceleration);
            airDeceleration = Mathf.Max(0f, airDeceleration);
            baseGravityScale = Mathf.Max(0f, baseGravityScale);
            hoverGravityScale = Mathf.Max(0f, hoverGravityScale);
            maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
            maxHoverFallSpeed = Mathf.Max(0f, maxHoverFallSpeed);
            firstJumpVelocity = Mathf.Max(0f, firstJumpVelocity);
            extraJumpVelocity = Mathf.Max(0f, extraJumpVelocity);
            maxJumps = Mathf.Max(1, maxJumps);
            jumpCutMultiplier = Mathf.Clamp01(jumpCutMultiplier);
            coyoteTime = Mathf.Max(0f, coyoteTime);
            jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
            groundCheckRadius = Mathf.Max(0f, groundCheckRadius);
            knockbackHorizontal = Mathf.Max(0f, knockbackHorizontal);
            knockbackVertical = Mathf.Max(0f, knockbackVertical);
            knockbackControlLock = Mathf.Max(0f, knockbackControlLock);
            invulnerabilityFlashInterval = Mathf.Max(0.01f, invulnerabilityFlashInterval);
        }
    }
}
