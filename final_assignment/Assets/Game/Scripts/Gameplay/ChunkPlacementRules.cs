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

        public static bool ClearanceCapsulesOverlap(
            TraversalClearanceSegment first,
            TraversalClearanceSegment second)
        {
            float combinedRadius = first.Radius + second.Radius;
            return SegmentSegmentSqrDistance(
                first.Start,
                first.End,
                second.Start,
                second.End) <= combinedRadius * combinedRadius;
        }

        private static float SegmentSegmentSqrDistance(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd)
        {
            const float epsilon = 0.000001f;
            Vector3 firstDirection = firstEnd - firstStart;
            Vector3 secondDirection = secondEnd - secondStart;
            Vector3 betweenStarts = firstStart - secondStart;
            float firstLengthSquared = Vector3.Dot(
                firstDirection,
                firstDirection);
            float secondLengthSquared = Vector3.Dot(
                secondDirection,
                secondDirection);
            float secondProjection = Vector3.Dot(
                secondDirection,
                betweenStarts);
            float firstProgress;
            float secondProgress;

            if (firstLengthSquared <= epsilon &&
                secondLengthSquared <= epsilon)
            {
                return betweenStarts.sqrMagnitude;
            }

            if (firstLengthSquared <= epsilon)
            {
                firstProgress = 0f;
                secondProgress = Mathf.Clamp01(
                    secondProjection / secondLengthSquared);
            }
            else
            {
                float firstProjection = Vector3.Dot(
                    firstDirection,
                    betweenStarts);
                if (secondLengthSquared <= epsilon)
                {
                    secondProgress = 0f;
                    firstProgress = Mathf.Clamp01(
                        -firstProjection / firstLengthSquared);
                }
                else
                {
                    float directionDot = Vector3.Dot(
                        firstDirection,
                        secondDirection);
                    float denominator = firstLengthSquared * secondLengthSquared -
                        directionDot * directionDot;
                    firstProgress = denominator > epsilon
                        ? Mathf.Clamp01(
                            (directionDot * secondProjection -
                                firstProjection * secondLengthSquared) /
                            denominator)
                        : 0f;
                    secondProgress =
                        (directionDot * firstProgress + secondProjection) /
                        secondLengthSquared;

                    if (secondProgress < 0f)
                    {
                        secondProgress = 0f;
                        firstProgress = Mathf.Clamp01(
                            -firstProjection / firstLengthSquared);
                    }
                    else if (secondProgress > 1f)
                    {
                        secondProgress = 1f;
                        firstProgress = Mathf.Clamp01(
                            (directionDot - firstProjection) /
                            firstLengthSquared);
                    }
                }
            }

            Vector3 closestFirst = firstStart + firstDirection * firstProgress;
            Vector3 closestSecond = secondStart + secondDirection * secondProgress;
            return (closestFirst - closestSecond).sqrMagnitude;
        }

        private static Vector3 Horizontal(Vector3 direction)
        {
            direction.y = 0f;
            return direction.normalized;
        }
    }
}
