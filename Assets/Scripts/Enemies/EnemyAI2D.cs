using System;
using System.Collections;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Level;
using UnityEngine;

namespace BeanBlaster.Enemies
{
    /// <summary>Runs the shared patrol, perception, combat, stun, and death state machine.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public sealed class EnemyAI2D : MonoBehaviour
    {
        [SerializeField] private EnemyStats stats;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform player;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private Transform fireSocket;
        [SerializeField] private EnemyProjectilePool projectilePool;
        [SerializeField] private LayerMask groundLayerMask = 1 << 6;
        [SerializeField] private LayerMask playerLayerMask = 1 << 7;
        [SerializeField] private float leftBound;
        [SerializeField] private float rightBound;

        private const float PerceptionInterval = 0.1f;
        private const float TurnCooldown = 0.2f;
        private const float BehindDetectionDistance = 2f;

        private float perceptionAccumulator;
        private float stateTimer;
        private float attackCooldownRemaining;
        private float lostSightTimer;
        private float turnCooldownRemaining;
        private int facing = 1;
        private bool playerDetected;
        private bool deathHandled;
        private Color baseColor = Color.white;

        public EnemyState CurrentState { get; private set; } = EnemyState.Patrol;
        public float StunRemaining => CurrentState == EnemyState.Stunned ? stateTimer : 0f;
        public bool PlayerDetected => playerDetected;

        public event Action<EnemyState, EnemyState> StateChanged;
        public event Action AttackCommitted;
        public event Action<EnemyAI2D> DeathResolved;

        private void Awake()
        {
            body ??= GetComponent<Rigidbody2D>();
            bodyCollider ??= GetComponent<Collider2D>();
            health ??= GetComponent<Health>();
            bodyRenderer ??= GetComponent<SpriteRenderer>();
            if (bodyRenderer != null)
            {
                baseColor = bodyRenderer.color;
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                player = playerObject == null ? null : playerObject.transform;
            }

            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void FixedUpdate()
        {
            AdvanceState(Time.fixedDeltaTime);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            ProcessContact(collision.collider);
        }

        /// <summary>Applies enemy contact damage only while alive and not stunned.</summary>
        public void ProcessContact(Collider2D other)
        {
            if (stats == null || other == null || CurrentState == EnemyState.Stunned || CurrentState == EnemyState.Dead)
            {
                return;
            }

            Health target = other.GetComponentInParent<Health>();
            if (target == null || target.Team != Team.Player)
            {
                return;
            }

            Vector2 direction = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            target.TryApplyDamage(new DamageInfo(
                stats.ContactDamage,
                transform.position,
                direction,
                stats.ContactDamage * 0.5f,
                Team.Enemy,
                DamageType.Contact));
        }

        /// <summary>Assigns runtime dependencies for generated enemies and deterministic tests.</summary>
        public void Configure(EnemyStats enemyStats, Transform playerTarget, LevelManager manager = null)
        {
            stats = enemyStats;
            player = playerTarget;
            levelManager = manager;
            health ??= GetComponent<Health>();
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        /// <summary>Advances timers and the current state by a deterministic interval.</summary>
        public void AdvanceState(float deltaTime)
        {
            if (stats == null || CurrentState == EnemyState.Dead || deltaTime <= 0f)
            {
                return;
            }

            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - deltaTime);
            turnCooldownRemaining = Mathf.Max(0f, turnCooldownRemaining - deltaTime);
            perceptionAccumulator += deltaTime;
            while (perceptionAccumulator >= PerceptionInterval)
            {
                perceptionAccumulator -= PerceptionInterval;
                SamplePerception();
            }

            switch (CurrentState)
            {
                case EnemyState.Patrol:
                    UpdatePatrol();
                    if (playerDetected)
                    {
                        TransitionTo(EnemyState.Chase);
                    }
                    break;
                case EnemyState.Chase:
                    UpdateChase(deltaTime);
                    break;
                case EnemyState.Attack:
                    UpdateAttack(deltaTime);
                    break;
                case EnemyState.Stunned:
                    stateTimer -= deltaTime;
                    if (stateTimer <= 0f)
                    {
                        TransitionTo(playerDetected ? EnemyState.Chase : EnemyState.Patrol);
                    }
                    break;
            }
        }

        /// <summary>Samples range, facing, and Ground-obstructed line of sight.</summary>
        public void SamplePerception()
        {
            if (stats == null || player == null)
            {
                playerDetected = false;
                return;
            }

            Vector2 offset = player.position - transform.position;
            bool inRange = offset.magnitude <= stats.DetectionRange;
            bool inFront = Mathf.Sign(offset.x) == facing || Mathf.Abs(offset.x) <= Mathf.Epsilon;
            bool closeBehind = Mathf.Abs(offset.x) <= BehindDetectionDistance;
            Vector2 eye = (Vector2)transform.position + new Vector2(0f, 0.4f);
            bool clearLine = Physics2D.Linecast(eye, player.position, groundLayerMask).collider == null;
            playerDetected = inRange && (inFront || closeBehind) && clearLine;
        }

        private void UpdatePatrol()
        {
            if (IsBlocked(facing))
            {
                TryTurn();
                StopHorizontal();
                return;
            }

            SetHorizontalVelocity(facing * stats.PatrolSpeed);
        }

        private void UpdateChase(float deltaTime)
        {
            if (!playerDetected)
            {
                lostSightTimer += deltaTime;
                if (lostSightTimer >= stats.LoseSightTime)
                {
                    TransitionTo(EnemyState.Patrol);
                }
                return;
            }

            lostSightTimer = 0f;
            float horizontalDistance = player.position.x - transform.position.x;
            facing = horizontalDistance < 0f ? -1 : 1;
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= stats.AttackRange && attackCooldownRemaining <= 0f)
            {
                TransitionTo(EnemyState.Attack);
                return;
            }

            int movementDirection = facing;
            if (stats.UsesRangedAttack && distance < 3.5f)
            {
                movementDirection = -facing;
            }
            else if (stats.UsesRangedAttack && distance <= stats.PreferredDistance)
            {
                StopHorizontal();
                return;
            }

            if (IsBlocked(movementDirection))
            {
                StopHorizontal();
                return;
            }

            SetHorizontalVelocity(movementDirection * stats.ChaseSpeed);
        }

        private void UpdateAttack(float deltaTime)
        {
            StopHorizontal();
            stateTimer -= deltaTime;
            if (stateTimer > 0f)
            {
                return;
            }

            CommitAttack();
            attackCooldownRemaining = stats.AttackCooldown;
            TransitionTo(EnemyState.Chase);
        }

        private void CommitAttack()
        {
            if (stats.UsesRangedAttack)
            {
                EnemyProjectile projectile = projectilePool == null ? null : projectilePool.Get();
                if (projectile != null && player != null)
                {
                    Vector2 origin = fireSocket == null ? transform.position : fireSocket.position;
                    projectile.transform.position = origin;
                    projectile.Launch((Vector2)player.position - origin, stats.ProjectileSpeed, stats.AttackDamage, bodyCollider);
                }
            }
            else
            {
                Vector2 centre = (Vector2)transform.position + new Vector2(facing * stats.AttackRange * 0.5f, 0f);
                Vector2 size = new Vector2(stats.AttackRange, stats.Size.y);
                Collider2D hit = Physics2D.OverlapBox(centre, size, 0f, playerLayerMask);
                Health target = hit == null ? null : hit.GetComponentInParent<Health>();
                target?.TryApplyDamage(new DamageInfo(
                    stats.AttackDamage,
                    transform.position,
                    new Vector2(facing, 0.25f).normalized,
                    stats.AttackDamage * 0.5f,
                    Team.Enemy,
                    DamageType.Melee));
            }

            AttackCommitted?.Invoke();
        }

        private bool IsBlocked(int direction)
        {
            if (bodyCollider == null)
            {
                return false;
            }

            Bounds bounds = bodyCollider.bounds;
            Vector2 foot = new Vector2(bounds.center.x + direction * bounds.extents.x, bounds.min.y + 0.05f);
            bool hasGround = Physics2D.Raycast(foot, Vector2.down, 1f, groundLayerMask).collider != null;
            Vector2 wallOrigin = new Vector2(bounds.center.x, bounds.center.y);
            bool hasWall = Physics2D.Raycast(wallOrigin, Vector2.right * direction, bounds.extents.x + 0.5f, groundLayerMask).collider != null;
            bool outsideBounds = leftBound != rightBound &&
                                 ((direction < 0 && bounds.min.x <= leftBound) || (direction > 0 && bounds.max.x >= rightBound));
            return !hasGround || hasWall || outsideBounds;
        }

        private void TryTurn()
        {
            if (turnCooldownRemaining > 0f)
            {
                return;
            }

            facing *= -1;
            turnCooldownRemaining = TurnCooldown;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (CurrentState == EnemyState.Dead || stats == null)
            {
                return;
            }

            bool stuns = info.type == DamageType.Explosion || info.knockbackForce >= stats.StunKnockbackThreshold;
            float appliedForce = stuns ? info.knockbackForce : info.knockbackForce * 0.3f;
            if (body != null)
            {
                body.velocity = info.knockbackDirection.normalized * appliedForce;
            }

            if (stuns)
            {
                TransitionTo(EnemyState.Stunned, true);
            }
        }

        private void OnDied(DamageInfo info)
        {
            if (deathHandled)
            {
                return;
            }

            deathHandled = true;
            TransitionTo(EnemyState.Dead);
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.simulated = false;
            }

            levelManager?.OnEnemyKilled(this);
            DeathResolved?.Invoke(this);
            if (Application.isPlaying)
            {
                StartCoroutine(DeathFade());
            }
        }

        private IEnumerator DeathFade()
        {
            const float duration = 0.4f;
            float elapsed = 0f;
            Vector3 initialScale = transform.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
                if (bodyRenderer != null)
                {
                    Color color = bodyRenderer.color;
                    color.a = 1f - progress;
                    bodyRenderer.color = color;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private void TransitionTo(EnemyState next, bool refresh = false)
        {
            if (CurrentState == next && !refresh)
            {
                return;
            }

            EnemyState previous = CurrentState;
            CurrentState = next;
            switch (next)
            {
                case EnemyState.Attack:
                    stateTimer = stats.AttackWindup;
                    if (bodyRenderer != null) bodyRenderer.color = Color.white;
                    break;
                case EnemyState.Stunned:
                    stateTimer = stats.StunDuration;
                    if (bodyRenderer != null) bodyRenderer.color = new Color(0.5f, 0.85f, 1f);
                    break;
                default:
                    if (bodyRenderer != null) bodyRenderer.color = baseColor;
                    break;
            }

            StateChanged?.Invoke(previous, next);
        }

        private void SetHorizontalVelocity(float velocity)
        {
            if (body != null)
            {
                body.velocity = new Vector2(velocity, body.velocity.y);
            }
        }

        private void StopHorizontal()
        {
            SetHorizontalVelocity(0f);
        }
    }
}
