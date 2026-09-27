using NUnit.Framework;
using SkyhookAscent.Gameplay;
using UnityEngine;

namespace SkyhookAscent.Tests
{
    public sealed class MovingAnchorPlacementRulesTests
    {
        [Test]
        public void SweepFitsInsideLandingFootprintWithSafetyMargin()
        {
            bool fits = MovingAnchorPlacementRules.IsSweepInsideFootprint(
                Vector2.zero,
                new Vector2(0.4f, 0f),
                new Vector2(1.5f, 1.4f),
                new Vector2(0.55f, 0.55f));

            Assert.That(fits, Is.True);
        }

        [Test]
        public void SweepIsRejectedWhenItsEdgeLeavesLandingFootprint()
        {
            bool fits = MovingAnchorPlacementRules.IsSweepInsideFootprint(
                new Vector2(0.8f, 0f),
                new Vector2(0.3f, 0f),
                new Vector2(1.5f, 1.4f),
                new Vector2(0.55f, 0.55f));

            Assert.That(fits, Is.False);
        }

        [Test]
        public void SweptAnchorRejectsAnIntersectingTraversalCorridor()
        {
            TraversalClearanceSegment anchorSweep =
                new TraversalClearanceSegment(
                    new Vector3(-1f, 2f, 0f),
                    new Vector3(1f, 2f, 0f),
                    0.3f);
            TraversalClearanceSegment crossingCorridor =
                new TraversalClearanceSegment(
                    new Vector3(0f, 0f, -1f),
                    new Vector3(0f, 3f, 1f),
                    0.3f);

            Assert.That(
                ChunkPlacementRules.ClearanceCapsulesOverlap(
                    anchorSweep,
                    crossingCorridor),
                Is.True);
        }

        [Test]
        public void SweptAnchorAllowsAClearTraversalCorridor()
        {
            TraversalClearanceSegment anchorSweep =
                new TraversalClearanceSegment(
                    new Vector3(-1f, 2f, 0f),
                    new Vector3(1f, 2f, 0f),
                    0.3f);
            TraversalClearanceSegment clearCorridor =
                new TraversalClearanceSegment(
                    new Vector3(-1f, 0f, 3f),
                    new Vector3(1f, 0f, 3f),
                    0.3f);

            Assert.That(
                ChunkPlacementRules.ClearanceCapsulesOverlap(
                    anchorSweep,
                    clearCorridor),
                Is.False);
        }
    }
}
