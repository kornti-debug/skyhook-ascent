using System.Collections.Generic;
using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class MovingPlatform : MonoBehaviour
    {
        [Header("Motion")]
        [SerializeField] private Vector3 localTravelAxis = Vector3.right;
        [SerializeField, Min(0f)] private float travelDistance = 0.55f;
        [SerializeField, Min(2f)] private float cycleDuration = 4.2f;

        [Header("Placement Validation")]
        [SerializeField, Min(0f)] private float clearanceMargin = 0.12f;

        private Rigidbody body;
        private BoxCollider platformCollider;
        private Vector3 restLocalPosition;
        private float motionStartedAt;
        private bool initialized;

        public BoxCollider PlatformCollider
        {
            get
            {
                Initialize();
                return platformCollider;
            }
        }

        public float ClearanceMargin => clearanceMargin;

        public Vector3 GetWorldVelocity()
        {
            Initialize();

            float angularFrequency = 2f * Mathf.PI / cycleDuration;
            float phase = (Time.fixedTime - motionStartedAt) * angularFrequency;
            return GetWorldTravelAxis() *
                (Mathf.Cos(phase) * travelDistance * angularFrequency);
        }

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

        public Bounds GetWorldSweptBounds()
        {
            Initialize();

            Bounds restBounds = platformCollider.bounds;
            Vector3 restWorldPosition = transform.parent != null
                ? transform.parent.TransformPoint(restLocalPosition)
                : restLocalPosition;
            restBounds.center += restWorldPosition - transform.position;

            return MovingPlatformPlacementRules.GetSweepBounds(
                restBounds,
                GetWorldTravelDisplacement());
        }

        private Vector3 GetWorldTravelDisplacement()
        {
            return GetWorldTravelAxis() * travelDistance;
        }

        private Vector3 GetWorldTravelAxis()
        {
            Vector3 localAxis = NormalizedTravelAxis();
            return transform.parent != null
                ? transform.parent.TransformVector(localAxis)
                : transform.rotation * localAxis;
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
            platformCollider = GetComponent<BoxCollider>();
            restLocalPosition = transform.localPosition;
            motionStartedAt = Time.fixedTime;
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
            cycleDuration = Mathf.Max(2f, cycleDuration);
            clearanceMargin = Mathf.Max(0f, clearanceMargin);
        }
    }

    public static class MovingPlatformPlacementRules
    {
        public static Bounds GetSweepBounds(
            Bounds restBounds,
            Vector3 maximumDisplacement)
        {
            Bounds sweepBounds = restBounds;
            sweepBounds.Encapsulate(Translate(restBounds, maximumDisplacement));
            sweepBounds.Encapsulate(Translate(restBounds, -maximumDisplacement));
            return sweepBounds;
        }

        public static bool IsSweepClear(
            Bounds sweepBounds,
            IReadOnlyList<Bounds> obstacleBounds)
        {
            if (obstacleBounds == null)
            {
                return true;
            }

            for (int i = 0; i < obstacleBounds.Count; i++)
            {
                if (sweepBounds.Intersects(obstacleBounds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static Bounds Translate(Bounds bounds, Vector3 offset)
        {
            bounds.center += offset;
            return bounds;
        }
    }
}
