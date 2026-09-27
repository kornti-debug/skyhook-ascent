using System.Collections.Generic;
using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(GrappleAnchor))]
    public sealed class MovingGrappleAnchor : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private Vector3 localTravelAxis = Vector3.right;
        [SerializeField, Min(0f)] private float travelDistance = 0.4f;
        [SerializeField, Min(0.5f)] private float cycleDuration = 3.4f;

        [Header("Placement Validation")]
        [SerializeField] private BoxCollider landingPlatform;
        [SerializeField, Min(0.05f)] private float sweptRadius = 0.55f;

        private Rigidbody body;
        private GrappleAnchor grappleAnchor;
        private Vector3 restLocalPosition;
        private float motionStartedAt;
        private bool initialized;

        public float TravelDistance => travelDistance;
        public float SweptRadius => sweptRadius;

        private void Awake()
        {
            Initialize();
        }

        private void FixedUpdate()
        {
            Initialize();

            float elapsed = Time.fixedTime - motionStartedAt;
            float phase = elapsed * (2f * Mathf.PI / cycleDuration);
            float offset = Mathf.Sin(phase) * travelDistance;
            Vector3 targetLocalPosition =
                restLocalPosition + NormalizedTravelAxis() * offset;
            Vector3 targetWorldPosition = transform.parent != null
                ? transform.parent.TransformPoint(targetLocalPosition)
                : targetLocalPosition;

            body.MovePosition(targetWorldPosition);
        }

        public bool IsSweepWithinLandingFootprint()
        {
            Initialize();
            if (landingPlatform == null)
            {
                return false;
            }

            Vector3 centerWorld = AttachmentPosition;
            Vector3 worldDisplacement = GetWorldTravelDisplacement();
            Transform platformTransform = landingPlatform.transform;
            Vector3 localCenter =
                platformTransform.InverseTransformPoint(centerWorld) -
                landingPlatform.center;
            Vector3 localEnd =
                platformTransform.InverseTransformPoint(
                    centerWorld + worldDisplacement) -
                landingPlatform.center;
            Vector3 localDisplacement = localEnd - localCenter;
            Vector3 platformScale = platformTransform.lossyScale;
            Vector2 localMargin = new Vector2(
                sweptRadius / Mathf.Max(0.0001f, Mathf.Abs(platformScale.x)),
                sweptRadius / Mathf.Max(0.0001f, Mathf.Abs(platformScale.z)));

            return MovingAnchorPlacementRules.IsSweepInsideFootprint(
                new Vector2(localCenter.x, localCenter.z),
                new Vector2(localDisplacement.x, localDisplacement.z),
                new Vector2(landingPlatform.size.x * 0.5f,
                    landingPlatform.size.z * 0.5f),
                localMargin);
        }

        public void AppendTargetSweepSamples(List<Vector3> output, int sampleCount = 5)
        {
            if (output == null)
            {
                return;
            }

            int samples = Mathf.Max(2, sampleCount);
            Vector3 center = AttachmentPosition;
            Vector3 displacement = GetWorldTravelDisplacement();
            for (int i = 0; i < samples; i++)
            {
                float progress = i / (float)(samples - 1);
                float sweepOffset = Mathf.Lerp(-1f, 1f, progress);
                output.Add(center + displacement * sweepOffset);
            }
        }

        public TraversalClearanceSegment GetWorldSweepClearance()
        {
            Vector3 center = AttachmentPosition;
            Vector3 displacement = GetWorldTravelDisplacement();
            return new TraversalClearanceSegment(
                center - displacement,
                center + displacement,
                sweptRadius);
        }

        public Bounds GetWorldSweptBounds()
        {
            TraversalClearanceSegment sweep = GetWorldSweepClearance();
            Vector3 radius = Vector3.one * sweptRadius;
            Bounds bounds = new Bounds(sweep.Start, Vector3.zero);
            bounds.Encapsulate(sweep.End);
            bounds.Encapsulate(sweep.Start - radius);
            bounds.Encapsulate(sweep.Start + radius);
            bounds.Encapsulate(sweep.End - radius);
            bounds.Encapsulate(sweep.End + radius);
            return bounds;
        }

        private Vector3 AttachmentPosition => grappleAnchor != null
            ? grappleAnchor.AttachmentPosition
            : transform.position;

        private Vector3 GetWorldTravelDisplacement()
        {
            Vector3 axis = NormalizedTravelAxis() * travelDistance;
            return transform.parent != null
                ? transform.parent.TransformVector(axis)
                : transform.TransformVector(axis);
        }

        private Vector3 NormalizedTravelAxis()
        {
            return localTravelAxis.sqrMagnitude > 0.0001f
                ? localTravelAxis.normalized
                : Vector3.right;
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            body = GetComponent<Rigidbody>();
            grappleAnchor = GetComponent<GrappleAnchor>();
            restLocalPosition = transform.localPosition;
            motionStartedAt = Time.fixedTime;
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            initialized = true;
        }

        private void OnValidate()
        {
            if (localTravelAxis.sqrMagnitude < 0.0001f)
            {
                localTravelAxis = Vector3.right;
            }
            else
            {
                localTravelAxis.Normalize();
            }

            travelDistance = Mathf.Max(0f, travelDistance);
            cycleDuration = Mathf.Max(0.5f, cycleDuration);
            sweptRadius = Mathf.Max(0.05f, sweptRadius);
        }
    }

    public static class MovingAnchorPlacementRules
    {
        public static bool IsSweepInsideFootprint(
            Vector2 center,
            Vector2 maximumDisplacement,
            Vector2 halfSize,
            Vector2 margin)
        {
            Vector2 availableHalfSize = halfSize - margin;
            return availableHalfSize.x >= 0f &&
                availableHalfSize.y >= 0f &&
                Mathf.Abs(center.x) + Mathf.Abs(maximumDisplacement.x) <=
                    availableHalfSize.x + 0.0001f &&
                Mathf.Abs(center.y) + Mathf.Abs(maximumDisplacement.y) <=
                    availableHalfSize.y + 0.0001f;
        }
    }
}
