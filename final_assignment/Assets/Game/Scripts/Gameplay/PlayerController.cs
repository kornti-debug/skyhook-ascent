using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyhookAscent.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const string GameplayMapName = "Gameplay";
        private const string MoveActionName = "Move";
        private const string RunActionName = "Run";
        private const string JumpActionName = "Jump";

        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform movementReference;

        [Header("Ground Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 6.5f;
        [SerializeField, Min(0f)] private float runSpeed = 10f;
        [SerializeField, Min(0f)] private float groundAcceleration = 60f;
        [SerializeField, Min(0f)] private float groundDeceleration = 75f;

        [Header("Air Movement")]
        [SerializeField, Min(0f)] private float airAcceleration = 24f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.8f;
        [SerializeField, Min(1f)] private float riseGravityMultiplier = 1.8f;
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 2.6f;
        [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Range(0f, 89f)] private float maximumGroundAngle = 50f;

        private Rigidbody body;
        private CapsuleCollider capsuleCollider;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction runAction;
        private InputAction jumpAction;
        private Vector2 moveInput;
        private bool runHeld;
        private float jumpQueuedUntil = float.NegativeInfinity;
        private float lastGroundedTime = float.NegativeInfinity;
        private float ignoreGroundUntil = float.NegativeInfinity;
        private float minimumGroundNormalY;
        private float baseWalkSpeed;
        private float baseRunSpeed;
        private float baseJumpHeight;
        private MovingPlatform movingSupport;
        private TowerChunk groundedChunk;
        private float lastMovingSupportTime = float.NegativeInfinity;
        private bool zipMovementActive;
        private bool movementEnabled = true;
        private float verticalVelocityBeforePhysics;
        private readonly List<Collider> ignoredOneWayPlatforms = new List<Collider>();

        public bool IsGrounded => Time.time <= lastGroundedTime + coyoteTime;
        public TowerChunk GroundedChunk => IsGrounded ? groundedChunk : null;
        public bool IsZipMovementActive => zipMovementActive;
        public bool MovementEnabled => movementEnabled;
        public float HorizontalSpeed
        {
            get
            {
                Vector3 velocity = body != null ? body.linearVelocity : Vector3.zero;
                return new Vector2(velocity.x, velocity.z).magnitude;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            baseWalkSpeed = walkSpeed;
            baseRunSpeed = runSpeed;
            baseJumpHeight = jumpHeight;
            ConfigureBody();
            minimumGroundNormalY = Mathf.Cos(maximumGroundAngle * Mathf.Deg2Rad);

            if (movementReference == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    movementReference = mainCamera.transform;
                }
            }
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                return;
            }

            gameplayMap = inputActions.FindActionMap(GameplayMapName, true);
            moveAction = gameplayMap.FindAction(MoveActionName, true);
            runAction = gameplayMap.FindAction(RunActionName, true);
            jumpAction = gameplayMap.FindAction(JumpActionName, true);
            gameplayMap.Enable();
        }

        private void OnDisable()
        {
            ClearOneWayPlatformIgnores();
            gameplayMap?.Disable();
        }

        private void Update()
        {
            if (gameplayMap == null || !movementEnabled)
            {
                moveInput = Vector2.zero;
                runHeld = false;
                return;
            }

            moveInput = moveAction.ReadValue<Vector2>();
            runHeld = runAction.IsPressed();

            if (jumpAction.WasPressedThisFrame())
            {
                jumpQueuedUntil = Time.time + jumpBufferTime;
            }
        }

        private void FixedUpdate()
        {
            UpdateOneWayPlatformIgnores();

            if (gameplayMap == null || zipMovementActive || !movementEnabled)
            {
                verticalVelocityBeforePhysics = body != null
                    ? body.linearVelocity.y
                    : 0f;
                return;
            }

            ApplyHorizontalMovement();
            TryJump();
            ApplyVerticalGravity();
            verticalVelocityBeforePhysics = body.linearVelocity.y;
        }

        public void SetZipMovementActive(bool active)
        {
            zipMovementActive = active;

            if (active)
            {
                jumpQueuedUntil = float.NegativeInfinity;
                lastGroundedTime = float.NegativeInfinity;
                ClearMovingSupport();
            }
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (enabled)
            {
                return;
            }

            moveInput = Vector2.zero;
            runHeld = false;
            jumpQueuedUntil = float.NegativeInfinity;
            ClearMovingSupport();

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
            }
        }

        public void LaunchFromTrampoline(float launchSpeed)
        {
            if (body == null || launchSpeed <= 0f)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, launchSpeed);
            body.linearVelocity = velocity;
            verticalVelocityBeforePhysics = velocity.y;
            jumpQueuedUntil = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            groundedChunk = null;
            ignoreGroundUntil = Time.time + 0.1f;
            ClearMovingSupport();
        }

        public void ResetForNewRun()
        {
            ClearOneWayPlatformIgnores();
            ApplyMovementPerkStacks(0, 0);
            movementEnabled = false;
            zipMovementActive = false;
            moveInput = Vector2.zero;
            runHeld = false;
            jumpQueuedUntil = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            groundedChunk = null;
            ignoreGroundUntil = Time.time + 0.1f;
            ClearMovingSupport();
        }

        public void ApplyMovementPerkStacks(
            int climbersPaceStacks,
            int lightFeetStacks)
        {
            walkSpeed = baseWalkSpeed * RunPerkRules.GetMultiplier(
                RunPerk.ClimbersPace,
                climbersPaceStacks);
            runSpeed = baseRunSpeed * RunPerkRules.GetMultiplier(
                RunPerk.ClimbersPace,
                climbersPaceStacks);
            jumpHeight = baseJumpHeight * RunPerkRules.GetMultiplier(
                RunPerk.LightFeet,
                lightFeetStacks);
        }

        private void ApplyHorizontalMovement()
        {
            Vector3 velocity = body.linearVelocity;
            Vector3 supportVelocity = GetMovingSupportVelocity();
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 relativeHorizontalVelocity = horizontalVelocity - supportVelocity;
            Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
            bool grounded = IsGrounded;
            float targetSpeed = grounded
                ? (runHeld ? runSpeed : walkSpeed)
                : (runHeld ? baseRunSpeed : baseWalkSpeed);
            Vector3 targetVelocity = moveDirection * targetSpeed;

            if (grounded)
            {
                float acceleration = moveDirection.sqrMagnitude > 0.001f
                    ? groundAcceleration
                    : groundDeceleration;
                relativeHorizontalVelocity = Vector3.MoveTowards(
                    relativeHorizontalVelocity,
                    targetVelocity,
                    acceleration * Time.fixedDeltaTime);
            }
            else if (moveDirection.sqrMagnitude > 0.001f)
            {
                relativeHorizontalVelocity = Vector3.MoveTowards(
                    relativeHorizontalVelocity,
                    targetVelocity,
                    airAcceleration * Time.fixedDeltaTime);
            }

            horizontalVelocity = relativeHorizontalVelocity + supportVelocity;
            body.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }

        private void TryJump()
        {
            if (Time.time > jumpQueuedUntil || !IsGrounded)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            float jumpSpeed = Mathf.Sqrt(
                Mathf.Max(0f, -2f * Physics.gravity.y * jumpHeight * riseGravityMultiplier));
            body.linearVelocity = new Vector3(velocity.x, jumpSpeed, velocity.z);

            jumpQueuedUntil = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            ignoreGroundUntil = Time.time + 0.1f;
            ClearMovingSupport();
        }

        private void ApplyVerticalGravity()
        {
            Vector3 velocity = body.linearVelocity;
            float gravityMultiplier = 1f;

            if (velocity.y > 0.01f)
            {
                gravityMultiplier = riseGravityMultiplier;
            }
            else if (velocity.y < -0.01f)
            {
                gravityMultiplier = fallGravityMultiplier;
            }

            if (gravityMultiplier <= 1f)
            {
                return;
            }

            velocity += Physics.gravity * ((gravityMultiplier - 1f) * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (input.sqrMagnitude < 0.001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = movementReference != null ? movementReference.forward : Vector3.forward;
            Vector3 right = movementReference != null ? movementReference.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (TryIgnoreOneWayPlatform(collision))
            {
                return;
            }

            UpdateGroundedState(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            if (TryIgnoreOneWayPlatform(collision))
            {
                return;
            }

            UpdateGroundedState(collision);
        }

        private bool TryIgnoreOneWayPlatform(Collision collision)
        {
            Collider platform = collision.collider;
            if (platform == null || platform.isTrigger ||
                platform.GetComponentInParent<TowerChunk>() == null ||
                platform.GetComponentInParent<GrappleAnchor>() != null)
            {
                return false;
            }

            Bounds playerBounds = capsuleCollider.bounds;
            Bounds platformBounds = platform.bounds;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                Vector3 platformToPlayerNormal;
                if (contact.thisCollider == capsuleCollider &&
                    contact.otherCollider == platform)
                {
                    platformToPlayerNormal = contact.normal;
                }
                else if (contact.otherCollider == capsuleCollider &&
                    contact.thisCollider == platform)
                {
                    platformToPlayerNormal = -contact.normal;
                }
                else
                {
                    continue;
                }

                if (!OneWayPlatformRules.ShouldIgnoreUndersideCollision(
                    playerBounds,
                    platformBounds,
                    platformToPlayerNormal))
                {
                    continue;
                }

                Physics.IgnoreCollision(capsuleCollider, platform, true);
                if (verticalVelocityBeforePhysics > 0f &&
                    body.linearVelocity.y < verticalVelocityBeforePhysics)
                {
                    Vector3 velocity = body.linearVelocity;
                    velocity.y = verticalVelocityBeforePhysics;
                    body.linearVelocity = velocity;
                }

                if (!ignoredOneWayPlatforms.Contains(platform))
                {
                    ignoredOneWayPlatforms.Add(platform);
                }

                return true;
            }

            return false;
        }

        private void UpdateOneWayPlatformIgnores()
        {
            if (capsuleCollider == null)
            {
                return;
            }

            Bounds playerBounds = capsuleCollider.bounds;
            for (int i = ignoredOneWayPlatforms.Count - 1; i >= 0; i--)
            {
                Collider platform = ignoredOneWayPlatforms[i];
                if (platform == null || !platform.enabled ||
                    !platform.gameObject.activeInHierarchy)
                {
                    ignoredOneWayPlatforms.RemoveAt(i);
                    continue;
                }

                if (!OneWayPlatformRules.ShouldRestoreCollision(
                    playerBounds,
                    platform.bounds,
                    body.linearVelocity.y))
                {
                    continue;
                }

                Physics.IgnoreCollision(capsuleCollider, platform, false);
                ignoredOneWayPlatforms.RemoveAt(i);
            }
        }

        private void ClearOneWayPlatformIgnores()
        {
            if (capsuleCollider != null && capsuleCollider.enabled &&
                capsuleCollider.gameObject.activeInHierarchy)
            {
                for (int i = 0; i < ignoredOneWayPlatforms.Count; i++)
                {
                    Collider platform = ignoredOneWayPlatforms[i];
                    if (platform != null && platform.enabled &&
                        platform.gameObject.activeInHierarchy)
                    {
                        Physics.IgnoreCollision(capsuleCollider, platform, false);
                    }
                }
            }

            ignoredOneWayPlatforms.Clear();
        }

        private void UpdateGroundedState(Collision collision)
        {
            if (Time.time < ignoreGroundUntil)
            {
                return;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y >= minimumGroundNormalY)
                {
                    lastGroundedTime = Time.time;
                    groundedChunk = collision.collider.GetComponentInParent<TowerChunk>();
                    MovingPlatform platform = collision.rigidbody != null
                        ? collision.rigidbody.GetComponent<MovingPlatform>()
                        : null;
                    if (platform == null)
                    {
                        platform = collision.collider.GetComponentInParent<MovingPlatform>();
                    }

                    if (platform != null)
                    {
                        movingSupport = platform;
                        lastMovingSupportTime = Time.time;
                    }

                    return;
                }
            }
        }

        private Vector3 GetMovingSupportVelocity()
        {
            if (movingSupport == null ||
                Time.time > lastMovingSupportTime + Time.fixedDeltaTime * 2f ||
                !IsGrounded)
            {
                return Vector3.zero;
            }

            Vector3 velocity = movingSupport.GetWorldVelocity();
            velocity.y = 0f;
            return velocity;
        }

        private void ClearMovingSupport()
        {
            movingSupport = null;
            lastMovingSupportTime = float.NegativeInfinity;
        }

        private void ConfigureBody()
        {
            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
        }

        private void OnValidate()
        {
            runSpeed = Mathf.Max(runSpeed, walkSpeed);
            riseGravityMultiplier = Mathf.Max(1f, riseGravityMultiplier);
            fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
            minimumGroundNormalY = Mathf.Cos(maximumGroundAngle * Mathf.Deg2Rad);
        }
    }
}
