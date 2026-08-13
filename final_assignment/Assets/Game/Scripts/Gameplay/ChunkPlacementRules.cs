using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    public readonly struct TraversalClearanceSegment
    {
        public TraversalClearanceSegment(
            Vector3 start,
            Vector3 end,
            float radius)
        {
            Start = start;
            End = end;
            Radius = Mathf.Max(0.05f, radius);
        }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public float Radius { get; }
    }

    public static class ChunkPlacementRules
    {
        public static bool IsTurnAllowed(
            Vector3 previousExitDirection,
            Vector3 candidateEntryDirection,
            float maximumTurnAngle)
        {
            Vector3 previous = Horizontal(previousExitDirection);
            Vector3 candidate = Horizontal(candidateEntryDirection);
            if (previous.sqrMagnitude < 0.0001f ||
                candidate.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            return Vector3.Angle(previous, candidate) <=
                Mathf.Clamp(maximumTurnAngle, 0f, 180f);
        }

        public static bool BoundsBlockSegment(
            Bounds obstacle,
            TraversalClearanceSegment segment,
            int sampleCount = 10)
        {
            int samples = Mathf.Max(2, sampleCount);
            float squaredRadius = segment.Radius * segment.Radius;
            for (int i = 0; i <= samples; i++)
            {
                float progress = i / (float)samples;
                Vector3 point = Vector3.Lerp(
                    segment.Start,
                    segment.End,
                    progress);
                if (obstacle.SqrDistance(point) <= squaredRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3 Horizontal(Vector3 direction)
        {
            direction.y = 0f;
            return direction.normalized;
        }
    }
}
