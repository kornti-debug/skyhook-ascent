using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class GrappleProjectile : MonoBehaviour
    {
        private enum ProjectileState
        {
            Idle,
            Outbound,
            Returning,
            Attached,
            Finished
        }

        [SerializeField, Min(0.1f)] private float safetyLifetime = 8f;
        [SerializeField, Min(0.1f)] private float maximumReturnDuration = 1.25f;

        private static readonly Color OutboundColor =
            new Color(1f, 0.28f, 0.04f, 1f);
        private static readonly Color OutboundEmission =
            new Color(5f, 0.7f, 0.05f, 1f);
        private static readonly Color ReturningColor =
            new Color(1f, 0.06f, 0.28f, 1f);
        private static readonly Color ReturningEmission =
            new Color(5f, 0.08f, 0.7f, 1f);
        private static readonly Color AttachedColor =
            new Color(0.28f, 0.95f, 1f, 1f);
        private static readonly Color AttachedEmission =
            new Color(0.5f, 4.5f, 6f, 1f);

        private Rigidbody body;
        private Collider projectileCollider;
        private Renderer projectileRenderer;
        private TrailRenderer trailRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseScale;
        private GrappleController owner;
        private float expiresAt;
        private float maximumRange;
        private float distanceTravelled;
        private float returnSpeed;
        private float returnCatchDistance;
        private float returnStartedAt;
        private float returnDuration;
        private Vector3 previousPosition;
        private Vector3 returnStartPosition;
        private GrappleAnchor attachedAnchor;
        private ProjectileState state;

        public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;
        public bool IsLaunched => state == ProjectileState.Outbound;
        public bool IsReturning => state == ProjectileState.Returning;
        public bool IsAttached => state == ProjectileState.Attached;

        private void Awake()
        {
            CacheComponents();
            projectileRenderer = GetComponent<Renderer>();
            trailRenderer = GetComponent<TrailRenderer>();
            propertyBlock = new MaterialPropertyBlock();
            baseScale = transform.localScale;
            if (projectileRenderer != null)
            {
                projectileRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                projectileRenderer.receiveShadows = false;
            }

            if (trailRenderer != null)
            {
                trailRenderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                trailRenderer.receiveShadows = false;
            }

            ApplyVisual(OutboundColor, OutboundEmission, 1f);
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

        private void FixedUpdate()
        {
            if (state == ProjectileState.Outbound)
            {
                distanceTravelled += Vector3.Distance(body.position, previousPosition);
                previousPosition = body.position;

                if (distanceTravelled >= maximumRange || Time.time >= expiresAt)
                {
                    BeginReturn();
                }
            }
        }

        private void Update()
        {
            if (state == ProjectileState.Returning)
            {
                UpdateReturn();
                if (state == ProjectileState.Returning)
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 13f) * 0.16f;
                    ApplyVisual(ReturningColor, ReturningEmission, pulse);
                }
            }
            else if (state == ProjectileState.Attached)
            {
                float pulse = 1.12f + Mathf.Sin(Time.time * 10f) * 0.08f;
                ApplyVisual(AttachedColor, AttachedEmission, pulse);
            }
        }

        private void LateUpdate()
        {
            if (state == ProjectileState.Attached && attachedAnchor != null)
            {
                transform.position = attachedAnchor.AttachmentPosition;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (state != ProjectileState.Outbound)
            {
                return;
            }

            GrappleAnchor hitAnchor =
                collision.collider.GetComponentInParent<GrappleAnchor>();
            if (hitAnchor != null)
            {
                Attach(hitAnchor);
            }
            else
            {
                BeginReturn();
            }
        }

        public void Launch(
            Vector3 initialVelocity,
            GrappleController projectileOwner,
            Collider[] ignoredColliders,
            float allowedRange,
            float retrievalSpeed,
            float catchDistance)
        {
            CacheComponents();

            owner = projectileOwner;
            state = ProjectileState.Outbound;
            expiresAt = Time.time + safetyLifetime;
            maximumRange = Mathf.Max(1f, allowedRange);
            returnSpeed = Mathf.Max(0.1f, retrievalSpeed);
            returnCatchDistance = Mathf.Max(0.05f, catchDistance);
            distanceTravelled = 0f;
            previousPosition = body.position;
            body.linearVelocity = initialVelocity;
            if (trailRenderer != null)
            {
                trailRenderer.emitting = true;
                trailRenderer.time = 0.35f;
            }
            ApplyVisual(OutboundColor, OutboundEmission, 1f);

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
            Finish();
        }

        public void CompleteAttachment()
        {
            if (state == ProjectileState.Attached)
            {
                Finish();
            }
        }

        private void Attach(GrappleAnchor hitAnchor)
        {
            state = ProjectileState.Attached;
            attachedAnchor = hitAnchor;
            body.linearVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
            projectileCollider.enabled = false;
            transform.position = hitAnchor.AttachmentPosition;
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }
            ApplyVisual(AttachedColor, AttachedEmission, 1.2f);
            hitAnchor.PlayHitFeedback();
            owner?.HandleProjectileAttached(this, hitAnchor);
        }

        private void BeginReturn()
        {
            if (state != ProjectileState.Outbound)
            {
                return;
            }

            state = ProjectileState.Returning;
            body.linearVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
            projectileCollider.enabled = false;

            if (trailRenderer != null)
            {
                trailRenderer.emitting = true;
                trailRenderer.time = 0.2f;
            }
            ApplyVisual(ReturningColor, ReturningEmission, 1f);

            returnStartPosition = transform.position;
            returnStartedAt = Time.time;
            float initialReturnDistance = owner != null
                ? Vector3.Distance(returnStartPosition, owner.HookOriginPosition)
                : 0f;
            returnDuration = Mathf.Clamp(
                initialReturnDistance / returnSpeed,
                Mathf.Max(0.01f, Time.deltaTime),
                maximumReturnDuration);
        }

        private void UpdateReturn()
        {
            if (owner == null)
            {
                Finish();
                return;
            }

            Vector3 target = owner.HookOriginPosition;
            float returnProgress = Mathf.Clamp01(
                (Time.time - returnStartedAt) / returnDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, returnProgress);
            Vector3 previousReturnPosition = transform.position;
            transform.position = Vector3.Lerp(
                returnStartPosition,
                target,
                easedProgress);

            Vector3 returnDirection = transform.position - previousReturnPosition;
            if (returnDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    returnDirection.normalized);
            }

            if (returnProgress >= 1f ||
                Vector3.Distance(transform.position, target) <= returnCatchDistance)
            {
                transform.position = target;
                Finish();
            }
        }

        private void Finish()
        {
            if (state == ProjectileState.Finished)
            {
                return;
            }

            state = ProjectileState.Finished;
            projectileCollider.enabled = false;
            owner?.HandleProjectileRecovered(this);
            Destroy(gameObject);
        }

        private void ApplyVisual(
            Color baseColor,
            Color emissionColor,
            float scaleMultiplier)
        {
            transform.localScale = baseScale * scaleMultiplier;
            ApplyColors(projectileRenderer, baseColor, emissionColor);
            ApplyColors(trailRenderer, baseColor, emissionColor);
        }

        private void ApplyColors(
            Renderer targetRenderer,
            Color baseColor,
            Color emissionColor)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", baseColor);
            propertyBlock.SetColor("_Color", baseColor);
            propertyBlock.SetColor("_EmissionColor", emissionColor);
            targetRenderer.SetPropertyBlock(propertyBlock);
            propertyBlock.Clear();
        }

        private void OnValidate()
        {
            safetyLifetime = Mathf.Max(0.1f, safetyLifetime);
            maximumReturnDuration = Mathf.Max(0.1f, maximumReturnDuration);
        }
    }
}
