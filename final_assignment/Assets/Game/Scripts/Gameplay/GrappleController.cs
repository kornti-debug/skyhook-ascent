using UnityEngine;
using UnityEngine.InputSystem;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
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
        [SerializeField] private float verticalSpawnOffset = 0.65f;
        [SerializeField, Min(0f)] private float forwardSpawnOffset = 0.8f;

        [Header("Aim")]
        [SerializeField, Min(1f)] private float aimDistance = 50f;
        [SerializeField, Min(0.05f)] private float minimumFlightTime = 0.2f;
        [SerializeField, Min(0.05f)] private float maximumFlightTime = 1.5f;

        private InputAction grappleAction;
        private Collider[] ownerColliders;
        private GrappleProjectile activeProjectile;

        public GrappleProjectile ActiveProjectile => activeProjectile;
        public bool HasActiveProjectile => activeProjectile != null;
        public GrappleAnchor LastHitAnchor { get; private set; }
        public Vector3 LastAimPoint { get; private set; }
        public bool LastShotHadAimTarget { get; private set; }

        private void Awake()
        {
            ownerColliders = GetComponentsInChildren<Collider>(true);

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
        }

        private void Update()
        {
            if (grappleAction != null && grappleAction.WasPressedThisFrame())
            {
                FireProjectile();
            }
        }

        public void FireProjectile()
        {
            if (aimCamera == null || projectilePrefab == null)
            {
                return;
            }

            CancelActiveProjectile();
            LastHitAnchor = null;

            Transform cameraTransform = aimCamera.transform;
            Ray aimRay = aimCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f));
            Vector3 launchPosition =
                transform.position +
                Vector3.up * verticalSpawnOffset +
                cameraTransform.right * horizontalSpawnOffset +
                aimRay.direction * forwardSpawnOffset;
            Vector3 launchVelocity = ResolveLaunchVelocity(
                aimRay,
                launchPosition);

            activeProjectile = Instantiate(
                projectilePrefab,
                launchPosition,
                Quaternion.LookRotation(launchVelocity.normalized));
            activeProjectile.Launch(
                launchVelocity,
                this,
                ownerColliders);
        }

        private Vector3 ResolveLaunchVelocity(Ray aimRay, Vector3 launchPosition)
        {
            if (Physics.Raycast(
                aimRay,
                out RaycastHit hit,
                aimDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                GrappleAnchor aimedAnchor =
                    hit.collider.GetComponentInParent<GrappleAnchor>();
                LastAimPoint = aimedAnchor != null
                    ? aimedAnchor.AttachmentPosition
                    : hit.point;
                LastShotHadAimTarget = true;

                float desiredSpeed = Mathf.Max(0.01f, launchSpeed);
                float distanceToTarget = Vector3.Distance(
                    launchPosition,
                    LastAimPoint);
                float flightTime = Mathf.Clamp(
                    distanceToTarget / desiredSpeed,
                    minimumFlightTime,
                    maximumFlightTime);

                return
                    (LastAimPoint - launchPosition) / flightTime -
                    0.5f * Physics.gravity * flightTime;
            }

            LastAimPoint = aimRay.GetPoint(aimDistance);
            LastShotHadAimTarget = false;
            return aimRay.direction.normalized * launchSpeed;
        }

        public void CancelActiveProjectile()
        {
            if (activeProjectile != null)
            {
                activeProjectile.Cancel();
            }
        }

        internal void HandleProjectileFinished(
            GrappleProjectile projectile,
            GrappleAnchor hitAnchor)
        {
            if (activeProjectile != projectile)
            {
                return;
            }

            LastHitAnchor = hitAnchor;
            activeProjectile = null;
        }

        private void OnValidate()
        {
            launchSpeed = Mathf.Max(0.1f, launchSpeed);
            forwardSpawnOffset = Mathf.Max(0f, forwardSpawnOffset);
            aimDistance = Mathf.Max(1f, aimDistance);
            minimumFlightTime = Mathf.Max(0.05f, minimumFlightTime);
            maximumFlightTime = Mathf.Max(
                minimumFlightTime,
                maximumFlightTime);
        }
    }
}
