using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyhookAscent.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        private const string GameplayMapName = "Gameplay";
        private const string LookActionName = "Look";

        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform target;

        [Header("Orbit")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField, Min(0f)] private float distance = 4.5f;
        [SerializeField, Range(-89f, 89f)] private float initialPitch = 15f;
        [SerializeField] private float initialYaw;
        [SerializeField, Range(-89f, 89f)] private float minimumPitch = -25f;
        [SerializeField, Range(-89f, 89f)] private float maximumPitch = 70f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float gamepadLookSpeed = 120f;

        [Header("Follow")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.04f;

        [Header("Wall Avoidance")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0f)] private float collisionRadius = 0.2f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.1f;
        [SerializeField, Min(0f)] private float minimumDistance = 0.75f;

        [Header("Lens")]
        [SerializeField, Range(40f, 100f)] private float fieldOfView = 72f;

        private Camera controlledCamera;
        private InputAction lookAction;
        private readonly RaycastHit[] collisionHits = new RaycastHit[16];
        private Vector3 smoothedPivot;
        private Vector3 followVelocity;
        private float yaw;
        private float pitch;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public float CurrentDistance { get; private set; }

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            yaw = initialYaw;
            pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);

            if (target == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            controlledCamera.fieldOfView = fieldOfView;
            SnapToTarget();
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                InputActionMap gameplayMap = inputActions.FindActionMap(GameplayMapName, true);
                lookAction = gameplayMap.FindAction(LookActionName, true);
                lookAction.Enable();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            lookAction?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (lookAction == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            bool isMouseInput = lookAction.activeControl?.device is Mouse;
            float sensitivity = isMouseInput
                ? mouseSensitivity
                : gamepadLookSpeed * Time.unscaledDeltaTime;

            yaw += lookInput.x * sensitivity;
            pitch = Mathf.Clamp(
                pitch - lookInput.y * sensitivity,
                minimumPitch,
                maximumPitch);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPivot = target.position + targetOffset;
            smoothedPivot = followSmoothTime <= 0f
                ? desiredPivot
                : Vector3.SmoothDamp(
                    smoothedPivot,
                    desiredPivot,
                    ref followVelocity,
                    followSmoothTime);

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 cameraDirection = orbitRotation * Vector3.back;
            CurrentDistance = ResolveCameraDistance(smoothedPivot, cameraDirection);

            transform.position = smoothedPivot + cameraDirection * CurrentDistance;
            transform.rotation = Quaternion.LookRotation(
                smoothedPivot - transform.position,
                Vector3.up);
        }

        private float ResolveCameraDistance(Vector3 pivot, Vector3 direction)
        {
            float resolvedDistance = distance;
            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                collisionRadius,
                direction,
                collisionHits,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Transform hitTransform = collisionHits[i].collider.transform;
                if (hitTransform == target || hitTransform.IsChildOf(target))
                {
                    continue;
                }

                resolvedDistance = Mathf.Min(
                    resolvedDistance,
                    collisionHits[i].distance - collisionPadding);
            }

            return Mathf.Clamp(resolvedDistance, minimumDistance, distance);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            smoothedPivot = target.position + targetOffset;
            followVelocity = Vector3.zero;

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 cameraDirection = orbitRotation * Vector3.back;
            CurrentDistance = ResolveCameraDistance(smoothedPivot, cameraDirection);
            transform.position = smoothedPivot + cameraDirection * CurrentDistance;
            transform.rotation = Quaternion.LookRotation(
                smoothedPivot - transform.position,
                Vector3.up);
        }

        private void OnValidate()
        {
            maximumPitch = Mathf.Max(minimumPitch, maximumPitch);
            distance = Mathf.Max(minimumDistance, distance);
            collisionRadius = Mathf.Max(0f, collisionRadius);
            collisionPadding = Mathf.Max(0f, collisionPadding);

            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView = fieldOfView;
            }
        }
    }
}
