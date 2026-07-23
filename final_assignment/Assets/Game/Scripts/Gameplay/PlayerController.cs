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
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 2.6f;
        [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.12f;
        [SerializeField, Range(0f, 0.3f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Range(0f, 89f)] private float maximumGroundAngle = 50f;

        private Rigidbody body;
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

        public bool IsGrounded => Time.time <= lastGroundedTime + coyoteTime;
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
            gameplayMap?.Disable();
        }

        private void Update()
        {
            if (gameplayMap == null)
            {
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
            if (gameplayMap == null)
            {
                return;
            }

            ApplyHorizontalMovement();
            TryJump();
            ApplyFallGravity();
        }

        private void ApplyHorizontalMovement()
        {
            Vector3 velocity = body.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 moveDirection = GetCameraRelativeDirection(moveInput);
            float targetSpeed = runHeld ? runSpeed : walkSpeed;
            Vector3 targetVelocity = moveDirection * targetSpeed;

            if (IsGrounded)
            {
                float acceleration = moveDirection.sqrMagnitude > 0.001f
                    ? groundAcceleration
                    : groundDeceleration;
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    acceleration * Time.fixedDeltaTime);
            }
            else if (moveDirection.sqrMagnitude > 0.001f)
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetVelocity,
                    airAcceleration * Time.fixedDeltaTime);
            }

            body.linearVelocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }

        private void TryJump()
        {
            if (Time.time > jumpQueuedUntil || !IsGrounded)
            {
                return;
            }

            Vector3 velocity = body.linearVelocity;
            float jumpSpeed = Mathf.Sqrt(Mathf.Max(0f, -2f * Physics.gravity.y * jumpHeight));
            body.linearVelocity = new Vector3(velocity.x, jumpSpeed, velocity.z);

            jumpQueuedUntil = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
            ignoreGroundUntil = Time.time + 0.1f;
        }

        private void ApplyFallGravity()
        {
            Vector3 velocity = body.linearVelocity;
            if (velocity.y >= -0.01f || fallGravityMultiplier <= 1f)
            {
                return;
            }

            velocity += Physics.gravity * ((fallGravityMultiplier - 1f) * Time.fixedDeltaTime);
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
            UpdateGroundedState(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            UpdateGroundedState(collision);
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
                    return;
                }
            }
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
            fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
            minimumGroundNormalY = Mathf.Cos(maximumGroundAngle * Mathf.Deg2Rad);
        }
    }
}
