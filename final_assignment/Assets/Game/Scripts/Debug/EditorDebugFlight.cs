using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(
        typeof(Rigidbody),
        typeof(CapsuleCollider),
        typeof(PlayerController))]
    public sealed class EditorDebugFlight : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(0.1f)] private float flightSpeed = 18f;
        [SerializeField, Min(0.1f)] private float boostSpeed = 40f;

        private Rigidbody body;
        private CapsuleCollider playerCollider;
        private PlayerController playerController;
        private GrappleController grappleController;
        private Vector3 requestedVelocity;
        private bool flying;
        private bool colliderWasEnabled;
        private bool gravityWasEnabled;

        public bool IsFlying => flying;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            playerCollider = GetComponent<CapsuleCollider>();
            playerController = GetComponent<PlayerController>();
            grappleController = GetComponent<GrappleController>();

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                SetFlying(!flying);
            }

            if (!flying)
            {
                return;
            }

            Vector3 forward = viewCamera != null
                ? viewCamera.transform.forward
                : transform.forward;
            Vector3 right = viewCamera != null
                ? viewCamera.transform.right
                : transform.right;

            float forwardInput =
                ReadAxis(keyboard.wKey.isPressed, keyboard.sKey.isPressed);
            float rightInput =
                ReadAxis(keyboard.dKey.isPressed, keyboard.aKey.isPressed);
            float verticalInput = ReadAxis(
                keyboard.spaceKey.isPressed || keyboard.eKey.isPressed,
                keyboard.leftCtrlKey.isPressed || keyboard.qKey.isPressed);

            Vector3 direction =
                forward * forwardInput +
                right * rightInput +
                Vector3.up * verticalInput;
            float speed = keyboard.leftShiftKey.isPressed
                ? boostSpeed
                : flightSpeed;
            requestedVelocity = Vector3.ClampMagnitude(direction, 1f) * speed;
        }

        private void FixedUpdate()
        {
            if (flying)
            {
                body.linearVelocity = requestedVelocity;
            }
        }

        private void OnGUI()
        {
            const int width = 430;
            string message = flying
                ? "DEBUG FLIGHT ON - F3 exit | WASD move | Space/Ctrl height | Shift boost"
                : "F3: Debug flight";
            GUI.Box(new Rect(12f, Screen.height - 42f, width, 28f), message);
        }
#endif

        private void OnDisable()
        {
            if (flying)
            {
                SetFlying(false);
            }
        }

        private void SetFlying(bool enabled)
        {
            flying = enabled;
            requestedVelocity = Vector3.zero;

            if (enabled)
            {
                colliderWasEnabled = playerCollider.enabled;
                gravityWasEnabled = body.useGravity;
                playerController.SetMovementEnabled(false);
                grappleController?.SetGrappleEnabled(false);
                playerCollider.enabled = false;
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                return;
            }

            playerCollider.enabled = colliderWasEnabled;
            body.useGravity = gravityWasEnabled;
            body.linearVelocity = Vector3.zero;
            grappleController?.SetGrappleEnabled(true);
            playerController.SetMovementEnabled(true);
        }

        private static float ReadAxis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }

        private void OnValidate()
        {
            flightSpeed = Mathf.Max(0.1f, flightSpeed);
            boostSpeed = Mathf.Max(flightSpeed, boostSpeed);
        }
    }
}
