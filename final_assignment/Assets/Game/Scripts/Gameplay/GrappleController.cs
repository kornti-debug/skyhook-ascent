using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
    public sealed class GrappleController : MonoBehaviour
    {
        private const string GameplayMapName = "Gameplay";
        private const string GrappleActionName = "Grapple";

        [Header("References")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private GrappleProjectile projectilePrefab;

        [Header("Launch")]
        [SerializeField, Min(0.1f)] private float launchSpeed = 18f;
        [SerializeField] private float horizontalSpawnOffset;
        [SerializeField] private float verticalSpawnOffset = 1.25f;
        [SerializeField, Min(0f)] private float forwardSpawnOffset = 0.8f;

        [Header("Aim")]
        [SerializeField, Min(1f)] private float zeroingDistance = 25f;

        [Header("Hook Cycle")]
        [SerializeField, Min(1f)] private float maximumRange = 22f;
        [SerializeField, Min(0.1f)] private float returnSpeed = 28f;
        [SerializeField, Min(0.05f)] private float returnCatchDistance = 0.45f;

        [Header("Zip Pull")]
        [SerializeField, Min(0.1f)] private float pullSpeed = 16f;
        [SerializeField, Min(0.1f)] private float pullAcceleration = 70f;
        [SerializeField, Min(0.1f)] private float arrivalDistance = 1.35f;
        [SerializeField, Min(0.1f)] private float maximumPullDuration = 2.5f;

        [Header("Rope")]
        [SerializeField, Min(0.005f)] private float ropeWidth = 0.035f;
        [SerializeField] private Color ropeColor = new Color(0.15f, 0.9f, 1f, 1f);

        private static readonly Color OutboundRopeColor =
            new Color(1f, 0.48f, 0.08f, 1f);
        private static readonly Color ReturningRopeColor =
            new Color(1f, 0.08f, 0.3f, 1f);

        private InputAction grappleAction;
        private Collider[] ownerColliders;
        private GrappleProjectile activeProjectile;
        private GrappleAnchor activeAnchor;
        private Rigidbody playerBody;
        private PlayerController playerController;
        private LineRenderer ropeRenderer;
        private Material runtimeRopeMaterial;
        private float pullExpiresAt;
        private bool grappleEnabled = true;

        public GrappleProjectile ActiveProjectile => activeProjectile;
        public bool HasActiveProjectile => activeProjectile != null;
        public bool IsPulling => activeAnchor != null;
        public bool CanFire => grappleEnabled && activeProjectile == null && !IsPulling;
        public float MaximumRange => maximumRange;
        public Vector3 HookOriginPosition =>
            transform.position + Vector3.up * verticalSpawnOffset;
        public GrappleAnchor LastHitAnchor { get; private set; }

        private void Awake()
        {
            ownerColliders = GetComponentsInChildren<Collider>(true);
            playerBody = GetComponent<Rigidbody>();
            playerController = GetComponent<PlayerController>();
            ConfigureRopeRenderer();

            if (aimCamera == null)
            {
                aimCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                return;
            }

            InputActionMap gameplayMap = inputActions.FindActionMap(GameplayMapName, true);
            grappleAction = gameplayMap.FindAction(GrappleActionName, true);
            grappleAction.Enable();
        }

        private void OnDisable()
        {
            grappleAction?.Disable();
            ResetGrapple(true);
        }

        private void Update()
        {
            if (grappleEnabled && grappleAction != null && grappleAction.WasPressedThisFrame())
            {
                FireProjectile();
            }
        }

        private void FixedUpdate()
        {
            if (!IsPulling || playerBody == null)
            {
                return;
            }

            Vector3 toAnchor = activeAnchor.AttachmentPosition - playerBody.position;
            if (toAnchor.magnitude <= arrivalDistance)
            {
                CompleteZip(true);
                return;
            }

            if (Time.time >= pullExpiresAt)
            {
                CompleteZip(false);
                return;
            }

            Vector3 desiredVelocity = toAnchor.normalized * pullSpeed;
            playerBody.linearVelocity = Vector3.MoveTowards(
                playerBody.linearVelocity,
                desiredVelocity,
                pullAcceleration * Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            if (ropeRenderer == null)
            {
                return;
            }

            bool showRope = activeProjectile != null;
            ropeRenderer.enabled = showRope;
            if (!showRope)
            {
                return;
            }

            ropeRenderer.SetPosition(0, HookOriginPosition);
            ropeRenderer.SetPosition(1, activeProjectile.transform.position);
            UpdateRopeAppearance();
        }

        public void FireProjectile()
        {
            if (!grappleEnabled || !CanFire || aimCamera == null || projectilePrefab == null)
            {
                return;
            }

            LastHitAnchor = null;

            Transform cameraTransform = aimCamera.transform;
            Ray aimRay = aimCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            Vector3 launchPosition =
                transform.position +
                Vector3.up * verticalSpawnOffset +
                cameraTransform.right * horizontalSpawnOffset +
                aimRay.direction * forwardSpawnOffset;
            Vector3 aimPoint = aimRay.GetPoint(zeroingDistance);
            Vector3 launchDirection =
                (aimPoint - launchPosition).normalized;
            Vector3 launchVelocity = launchDirection * launchSpeed;

            activeProjectile = Instantiate(
                projectilePrefab,
                launchPosition,
                Quaternion.LookRotation(launchVelocity.normalized));
            activeProjectile.Launch(
                launchVelocity,
                this,
                ownerColliders,
                maximumRange,
                returnSpeed,
                returnCatchDistance);
        }

        public void CancelActiveProjectile()
        {
            ResetGrapple(true);
        }

        public void SetGrappleEnabled(bool enabled)
        {
            grappleEnabled = enabled;
            if (!enabled)
            {
                CancelActiveProjectile();
            }
        }

        public void ResetForNewRun()
        {
            grappleEnabled = true;
            LastHitAnchor = null;
            ResetGrapple(true);
        }

        internal void HandleProjectileAttached(
            GrappleProjectile projectile,
            GrappleAnchor hitAnchor)
        {
            if (activeProjectile != projectile || hitAnchor == null)
            {
                return;
            }

            LastHitAnchor = hitAnchor;
            activeAnchor = hitAnchor;
            pullExpiresAt = Time.time + maximumPullDuration;
            playerController.SetZipMovementActive(true);
            playerBody.useGravity = false;
        }

        internal void HandleProjectileRecovered(GrappleProjectile projectile)
        {
            if (activeProjectile != projectile)
            {
                return;
            }

            activeProjectile = null;

            if (activeAnchor != null)
            {
                EndPlayerPull(false);
            }
        }

        private void CompleteZip(bool reachedAnchor)
        {
            GrappleProjectile projectile = activeProjectile;
            GrappleAnchor completedAnchor = activeAnchor;
            activeAnchor = null;
            EndPlayerPull(reachedAnchor);
            completedAnchor?.PlayReleaseFeedback(reachedAnchor);

            if (projectile != null)
            {
                projectile.CompleteAttachment();
            }
        }

        private void EndPlayerPull(bool reachedAnchor)
        {
            playerController.SetZipMovementActive(false);
            playerBody.useGravity = true;

            if (reachedAnchor)
            {
                playerBody.linearVelocity = Vector3.zero;
            }
            else
            {
                playerBody.linearVelocity = Vector3.ClampMagnitude(
                    playerBody.linearVelocity,
                    4f);
            }
        }

        private void ResetGrapple(bool stopPlayer)
        {
            GrappleProjectile projectile = activeProjectile;
            activeProjectile = null;
            activeAnchor = null;

            if (stopPlayer && playerController != null && playerBody != null)
            {
                EndPlayerPull(false);
            }

            if (projectile != null)
            {
                projectile.Cancel();
            }

            if (ropeRenderer != null)
            {
                ropeRenderer.enabled = false;
            }
        }

        private void ConfigureRopeRenderer()
        {
            ropeRenderer = GetComponent<LineRenderer>();
            if (ropeRenderer == null)
            {
                ropeRenderer = gameObject.AddComponent<LineRenderer>();
            }

            ropeRenderer.useWorldSpace = true;
            ropeRenderer.positionCount = 2;
            ropeRenderer.startWidth = ropeWidth;
            ropeRenderer.endWidth = ropeWidth;
            ropeRenderer.numCapVertices = 4;
            ropeRenderer.enabled = false;

            Shader ropeShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (ropeShader == null)
            {
                ropeShader = Shader.Find("Sprites/Default");
            }

            if (ropeShader != null)
            {
                runtimeRopeMaterial = new Material(ropeShader)
                {
                    color = Color.white
                };
                ropeRenderer.sharedMaterial = runtimeRopeMaterial;
            }
        }

        private void UpdateRopeAppearance()
        {
            Color stateColor;
            float stateWidth;
            if (IsPulling)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 12f) * 0.12f;
                stateColor = Color.Lerp(ropeColor, Color.white, 0.28f);
                stateWidth = ropeWidth * 1.65f * pulse;
            }
            else if (activeProjectile != null && activeProjectile.IsReturning)
            {
                stateColor = ReturningRopeColor;
                stateWidth = ropeWidth * 0.72f;
            }
            else
            {
                stateColor = OutboundRopeColor;
                stateWidth = ropeWidth;
            }

            ropeRenderer.startColor = stateColor;
            ropeRenderer.endColor = stateColor;
            ropeRenderer.startWidth = stateWidth;
            ropeRenderer.endWidth = stateWidth;
        }

        private void OnDestroy()
        {
            if (runtimeRopeMaterial != null)
            {
                Destroy(runtimeRopeMaterial);
            }
        }

        private void OnValidate()
        {
            launchSpeed = Mathf.Max(0.1f, launchSpeed);
            forwardSpawnOffset = Mathf.Max(0f, forwardSpawnOffset);
            zeroingDistance = Mathf.Max(1f, zeroingDistance);
            maximumRange = Mathf.Max(1f, maximumRange);
            returnSpeed = Mathf.Max(0.1f, returnSpeed);
            returnCatchDistance = Mathf.Max(0.05f, returnCatchDistance);
            pullSpeed = Mathf.Max(0.1f, pullSpeed);
            pullAcceleration = Mathf.Max(0.1f, pullAcceleration);
            arrivalDistance = Mathf.Max(0.1f, arrivalDistance);
            maximumPullDuration = Mathf.Max(0.1f, maximumPullDuration);
            ropeWidth = Mathf.Max(0.005f, ropeWidth);
        }
    }
}
