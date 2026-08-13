using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    public enum ChunkTraversalCategory
    {
        Warmup,
        Jumps,
        Grapple,
        Mixed,
        Recovery
    }

    [DisallowMultipleComponent]
    public sealed class TowerChunk : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private Transform entry;
        [SerializeField] private Transform exit;

        [Header("Traversal Validation")]
        [SerializeField] private Vector3 localEntryDirection = Vector3.forward;
        [SerializeField] private Vector3 localExitDirection = Vector3.forward;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 1.1f;
        [SerializeField, Range(0.4f, 0.9f)] private float clearanceEndFraction = 0.78f;

        [Header("Selection")]
        [SerializeField] private string chunkId = "chunk";
        [SerializeField, Min(1)] private int difficulty = 1;
        [SerializeField, Min(1)] private int selectionWeight = 1;
        [SerializeField] private ChunkTraversalCategory traversalCategory;

        [Header("Placement Bounds")]
        [SerializeField] private Vector3 localBoundsCenter;
        [SerializeField] private Vector3 localBoundsSize = Vector3.one;

        public Transform Entry => entry;
        public Transform Exit => exit;
        public string ChunkId => chunkId;
        public int Difficulty => difficulty;
        public int SelectionWeight => selectionWeight;
        public ChunkTraversalCategory TraversalCategory => traversalCategory;
        public Vector3 WorldEntryDirection => GetHorizontalWorldDirection(
            localEntryDirection);
        public Vector3 WorldExitDirection => GetHorizontalWorldDirection(
            localExitDirection);

        public ChunkCandidate ToCandidate()
        {
            return new ChunkCandidate(
                chunkId,
                difficulty,
                selectionWeight,
                traversalCategory);
        }

        public Bounds GetWorldBounds()
        {
            Vector3 localExtents = localBoundsSize * 0.5f;
            Vector3 worldCenter = transform.TransformPoint(localBoundsCenter);
            Vector3 axisX = transform.TransformVector(Vector3.right * localExtents.x);
            Vector3 axisY = transform.TransformVector(Vector3.up * localExtents.y);
            Vector3 axisZ = transform.TransformVector(Vector3.forward * localExtents.z);
            Vector3 worldExtents = Abs(axisX) + Abs(axisY) + Abs(axisZ);
            return new Bounds(worldCenter, worldExtents * 2f);
        }

        public void Configure(
            string id,
            Transform entryMarker,
            Transform exitMarker,
            int chunkDifficulty,
            int weight,
            ChunkTraversalCategory category)
        {
            chunkId = string.IsNullOrWhiteSpace(id) ? name : id;
            entry = entryMarker;
            exit = exitMarker;
            difficulty = Mathf.Max(1, chunkDifficulty);
            selectionWeight = Mathf.Max(1, weight);
            traversalCategory = category;
        }

        public void CaptureBoundsFromRenderers(float padding = 0.25f)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                localBoundsCenter = Vector3.zero;
                localBoundsSize = Vector3.one;
                return;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 localCorner = transform.InverseTransformPoint(corner);
                        minimum = Vector3.Min(minimum, localCorner);
                        maximum = Vector3.Max(maximum, localCorner);
                    }
                }
            }

            localBoundsCenter = (minimum + maximum) * 0.5f;
            localBoundsSize = maximum - minimum + Vector3.one * Mathf.Max(0f, padding);
        }

        public void CaptureTraversalMetadata()
        {
            if (entry == null || exit == null)
            {
                return;
            }

            Vector3 localEntry = transform.InverseTransformPoint(entry.position);
            Vector3 localExit = transform.InverseTransformPoint(exit.position);
            localExitDirection = HorizontalOrFallback(
                localExit - localEntry,
                Vector3.forward);

            GrappleAnchor[] anchors = GetComponentsInChildren<GrappleAnchor>(true);
            if (anchors.Length > 0)
            {
                GrappleAnchor nearestAnchor = anchors[0];
                float nearestDistance = Vector3.SqrMagnitude(
                    transform.InverseTransformPoint(nearestAnchor.AttachmentPosition) -
                    localEntry);
                for (int i = 1; i < anchors.Length; i++)
                {
                    float distance = Vector3.SqrMagnitude(
                        transform.InverseTransformPoint(anchors[i].AttachmentPosition) -
                        localEntry);
                    if (distance < nearestDistance)
                    {
                        nearestAnchor = anchors[i];
                        nearestDistance = distance;
                    }
                }

                Vector3 localTarget = transform.InverseTransformPoint(
                    nearestAnchor.AttachmentPosition);
                localEntryDirection = HorizontalOrFallback(
                    localTarget - localEntry,
                    localExitDirection);
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Vector3 nearestRendererDirection = localExitDirection;
            float nearestRendererDistance = float.PositiveInfinity;
            for (int i = 0; i < renderers.Length; i++)
            {
                Vector3 localCenter = transform.InverseTransformPoint(
                    renderers[i].bounds.center);
                Vector3 direction = localCenter - localEntry;
                direction.y = 0f;
                float distance = direction.sqrMagnitude;
                if (distance > 0.01f && distance < nearestRendererDistance)
                {
                    nearestRendererDirection = direction.normalized;
                    nearestRendererDistance = distance;
                }
            }

            localEntryDirection = HorizontalOrFallback(
                nearestRendererDirection,
                localExitDirection);
        }

        public void AppendWorldClearanceSegments(
            List<TraversalClearanceSegment> output)
        {
            if (output == null || entry == null || exit == null)
            {
                return;
            }

            output.Add(GetWorldExitHeadroomClearance());

            Vector3 source = entry.position + Vector3.up * 1.25f;
            GrappleAnchor[] anchors = GetComponentsInChildren<GrappleAnchor>(true);
            if (anchors.Length > 0)
            {
                for (int i = 0; i < anchors.Length; i++)
                {
                    AppendClearanceSegment(
                        output,
                        source,
                        anchors[i].AttachmentPosition);
                }
                return;
            }

            AppendClearanceSegment(
                output,
                source,
                exit.position + Vector3.up);
        }

        public bool BlocksAnyClearance(
            IReadOnlyList<TraversalClearanceSegment> clearances)
        {
            if (clearances == null || clearances.Count == 0)
            {
                return false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider candidate = colliders[colliderIndex];
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                {
                    continue;
                }

                for (int clearanceIndex = 0;
                    clearanceIndex < clearances.Count;
                    clearanceIndex++)
                {
                    if (ChunkPlacementRules.BoundsBlockSegment(
                        candidate.bounds,
                        clearances[clearanceIndex]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public TraversalClearanceSegment GetWorldExitApproachClearance()
        {
            Vector3 direction = GetWorldExitApproachDirection();
            Vector3 standingOffset = Vector3.up * 1.1f;
            Vector3 start = exit.position - direction * 5f + standingOffset;
            Vector3 end = exit.position - direction * 0.75f + standingOffset;
            return new TraversalClearanceSegment(start, end, 0.6f);
        }

        public TraversalClearanceSegment GetWorldExitHeadroomClearance()
        {
            Vector3 standingCenter = exit.position + Vector3.up;
            Vector3 jumpApexCenter = exit.position + Vector3.up * 2.9f;
            return new TraversalClearanceSegment(
                standingCenter,
                jumpApexCenter,
                0.6f);
        }

        public bool BlocksClearance(TraversalClearanceSegment clearance)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                {
                    continue;
                }

                if (ChunkPlacementRules.BoundsBlockSegment(
                    candidate.bounds,
                    clearance))
                {
                    return true;
                }
            }

            return false;
        }

        private void AppendClearanceSegment(
            List<TraversalClearanceSegment> output,
            Vector3 source,
            Vector3 target)
        {
            Vector3 protectedEnd = Vector3.Lerp(
                source,
                target,
                clearanceEndFraction);
            output.Add(new TraversalClearanceSegment(
                source,
                protectedEnd,
                clearanceRadius));
        }

        private Vector3 GetHorizontalWorldDirection(Vector3 localDirection)
        {
            return HorizontalOrFallback(
                transform.TransformDirection(localDirection),
                transform.forward);
        }

        private static Vector3 HorizontalOrFallback(
            Vector3 direction,
            Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude >= 0.0001f)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude >= 0.0001f
                ? fallback.normalized
                : Vector3.forward;
        }

        private Vector3 GetWorldExitApproachDirection()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            Collider finalSurface = null;
            float finalDistance = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (!IsTraversalSurface(candidate))
                {
                    continue;
                }

                float distance = candidate.bounds.SqrDistance(exit.position);
                if (distance < finalDistance)
                {
                    finalSurface = candidate;
                    finalDistance = distance;
                }
            }

            if (finalSurface == null)
            {
                return WorldExitDirection;
            }

            Vector3 finalCenter = SurfaceCenter(finalSurface.bounds);
            Collider previousSurface = null;
            float previousDistance = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == finalSurface || !IsTraversalSurface(candidate))
                {
                    continue;
                }

                Vector3 candidateCenter = SurfaceCenter(candidate.bounds);
                float distance = Vector3.SqrMagnitude(finalCenter - candidateCenter);
                if (distance < previousDistance)
                {
                    previousSurface = candidate;
                    previousDistance = distance;
                }
            }

            if (previousSurface == null)
            {
                return WorldExitDirection;
            }

            Vector3 approach = finalCenter - SurfaceCenter(previousSurface.bounds);
            approach.y = 0f;
            return approach.sqrMagnitude >= 0.01f
                ? approach.normalized
                : WorldExitDirection;
        }

        private static bool IsTraversalSurface(Collider candidate)
        {
            return candidate != null &&
                candidate.enabled &&
                !candidate.isTrigger &&
                candidate.GetComponentInParent<GrappleAnchor>() == null;
        }

        private static Vector3 SurfaceCenter(Bounds bounds)
        {
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(
                Mathf.Abs(value.x),
                Mathf.Abs(value.y),
                Mathf.Abs(value.z));
        }

        private void OnValidate()
        {
            difficulty = Mathf.Max(1, difficulty);
            selectionWeight = Mathf.Max(1, selectionWeight);
            localEntryDirection = HorizontalOrFallback(
                localEntryDirection,
                Vector3.forward);
            localExitDirection = HorizontalOrFallback(
                localExitDirection,
                localEntryDirection);
            clearanceRadius = Mathf.Max(0.1f, clearanceRadius);
            clearanceEndFraction = Mathf.Clamp(
                clearanceEndFraction,
                0.4f,
                0.9f);
            localBoundsSize = new Vector3(
                Mathf.Max(0.1f, localBoundsSize.x),
                Mathf.Max(0.1f, localBoundsSize.y),
                Mathf.Max(0.1f, localBoundsSize.z));
        }
    }
}
