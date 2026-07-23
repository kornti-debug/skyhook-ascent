using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class GrappleProjectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;

        private Rigidbody body;
        private Collider projectileCollider;
        private GrappleController owner;
        private float expiresAt;
        private bool launched;
        private bool finished;

        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public bool IsLaunched => launched && !finished;

        private void Awake()
        {
            CacheComponents();
        }

        private void CacheComponents()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (projectileCollider == null)
            {
                projectileCollider = GetComponent<Collider>();
            }

            body.useGravity = true;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private void Update()
        {
            if (IsLaunched && Time.time >= expiresAt)
            {
                Finish(null);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsLaunched)
            {
                return;
            }

            GrappleAnchor hitAnchor =
                collision.collider.GetComponentInParent<GrappleAnchor>();
            Finish(hitAnchor);
        }

        public void Launch(
            Vector3 initialVelocity,
            GrappleController projectileOwner,
            Collider[] ignoredColliders)
        {
            CacheComponents();

            owner = projectileOwner;
            launched = true;
            finished = false;
            expiresAt = Time.time + lifetime;
            body.linearVelocity = initialVelocity;

            if (ignoredColliders == null)
            {
                return;
            }

            for (int i = 0; i < ignoredColliders.Length; i++)
            {
                if (ignoredColliders[i] != null)
                {
                    Physics.IgnoreCollision(
                        projectileCollider,
                        ignoredColliders[i],
                        true);
                }
            }
        }

        public void Cancel()
        {
            Finish(null);
        }

        private void Finish(GrappleAnchor hitAnchor)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            projectileCollider.enabled = false;
            owner?.HandleProjectileFinished(this, hitAnchor);
            Destroy(gameObject);
        }

        private void OnValidate()
        {
            lifetime = Mathf.Max(0.1f, lifetime);
        }
    }
}
