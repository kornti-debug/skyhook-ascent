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
        [SerializeField] private float verticalSpawnOffset = 1.25f;
        [SerializeField, Min(0f)] private float forwardSpawnOffset = 0.8f;

        [Header("Aim")]
        [SerializeField, Min(1f)] private float zeroingDistance = 25f;

        private InputAction grappleAction;
        private Collider[] ownerColliders;
        private GrappleProjectile activeProjectile;

        public GrappleProjectile ActiveProjectile => activeProjectile;
        public bool HasActiveProjectile => activeProjectile != null;
        public GrappleAnchor LastHitAnchor { get; private set; }

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
                ownerColliders);
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
            zeroingDistance = Mathf.Max(1f, zeroingDistance);
        }
    }
}
