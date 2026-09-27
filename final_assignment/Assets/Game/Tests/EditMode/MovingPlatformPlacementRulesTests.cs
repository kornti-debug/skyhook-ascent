using NUnit.Framework;
using SkyhookAscent.Gameplay;
using UnityEngine;

namespace SkyhookAscent.Tests
{
    public sealed class MovingPlatformPlacementRulesTests
    {
        [Test]
        public void SweepBoundsIncludeBothTravelEndpoints()
        {
            Bounds restBounds = new Bounds(
                Vector3.zero,
                new Vector3(3f, 1f, 2f));

            Bounds sweepBounds = MovingPlatformPlacementRules.GetSweepBounds(
                restBounds,
                new Vector3(0f, 0f, 0.55f));

            Assert.That(sweepBounds.center.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(sweepBounds.size.z, Is.EqualTo(3.1f).Within(0.001f));
        }

        [Test]
        public void SweepIsRejectedWhenItTouchesANeighborPlatform()
        {
            Bounds sweepBounds = new Bounds(
                Vector3.zero,
                new Vector3(3f, 1f, 3.1f));
            Bounds neighboringPlatform = new Bounds(
                new Vector3(0f, 0f, 1.6f),
                new Vector3(3f, 1f, 1f));

            Assert.That(
                MovingPlatformPlacementRules.IsSweepClear(
                    sweepBounds,
                    new[] { neighboringPlatform }),
                Is.False);
        }

        [Test]
        public void SweepIsAllowedWhenItStaysClearOfNeighboringPlatforms()
        {
            Bounds sweepBounds = new Bounds(
                Vector3.zero,
                new Vector3(3f, 1f, 3.1f));
            Bounds neighboringPlatform = new Bounds(
                new Vector3(0f, 0f, 2.3f),
                new Vector3(2.8f, 1f, 1.2f));

            Assert.That(
                MovingPlatformPlacementRules.IsSweepClear(
                    sweepBounds,
                    new[] { neighboringPlatform }),
                Is.True);
        }
    }
}