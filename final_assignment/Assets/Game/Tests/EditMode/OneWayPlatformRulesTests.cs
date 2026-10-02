using NUnit.Framework;
using SkyhookAscent.Gameplay;
using UnityEngine;

namespace SkyhookAscent.Tests
{
    public sealed class OneWayPlatformRulesTests
    {
        [Test]
        public void PlayerCanPassThroughUndersideWhileBelowTop()
        {
            Bounds player = new Bounds(Vector3.zero, Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldIgnoreUndersideCollision(
                    player,
                    platform,
                    Vector3.down),
                Is.True);
        }

        [TestCaseSource(nameof(NonUndersideNormals))]
        public void LandingAndSideContactsRemainSolid(Vector3 normal)
        {
            Bounds player = new Bounds(
                new Vector3(0f, 2.5f, 0f),
                Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldIgnoreUndersideCollision(
                    player,
                    platform,
                    normal),
                Is.False);
        }

        [Test]
        public void UnderPlatformContactFromItsSideRemainsSolid()
        {
            Bounds player = new Bounds(Vector3.zero, Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldIgnoreUndersideCollision(
                    player,
                    platform,
                    Vector3.right),
                Is.False);
        }

        [Test]
        public void CollisionIsRestoredAfterPlayerClearsTop()
        {
            Bounds player = new Bounds(
                new Vector3(0f, 2.6f, 0f),
                Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldRestoreCollision(player, platform),
                Is.True);
        }

        [Test]
        public void CollisionIsRestoredAfterPlayerLeavesPlatformFootprint()
        {
            Bounds player = new Bounds(
                new Vector3(3f, 0f, 0f),
                Vector3.one);
            Bounds platform = new Bounds(
                Vector3.zero,
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldRestoreCollision(player, platform),
                Is.True);
        }

        [Test]
        public void CollisionStaysIgnoredWhilePlayerIsBelowAndOverPlatform()
        {
            Bounds player = new Bounds(Vector3.zero, Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldRestoreCollision(player, platform),
                Is.False);
        }

        [Test]
        public void CollisionIsRestoredIfPlayerFallsBackBelowWithoutClearingTop()
        {
            Bounds player = new Bounds(
                new Vector3(0f, 0f, 0f),
                Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldRestoreCollision(
                    player,
                    platform,
                    -1f),
                Is.True);
        }

        [Test]
        public void CollisionDoesNotRestoreBeforeFallingClearOfUnderside()
        {
            Bounds player = new Bounds(
                new Vector3(0f, 0.75f, 0f),
                Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldRestoreCollision(
                    player,
                    platform,
                    -1f),
                Is.False);
        }

        [Test]
        public void CollisionDoesNotRestoreBelowPlatformWhileStillAscending()
        {
            Bounds player = new Bounds(
                new Vector3(0f, 0f, 0f),
                Vector3.one);
            Bounds platform = new Bounds(
                new Vector3(0f, 1.5f, 0f),
                new Vector3(4f, 1f, 4f));

            Assert.That(
                OneWayPlatformRules.ShouldRestoreCollision(
                    player,
                    platform,
                    1f),
                Is.False);
        }

        private static object[] NonUndersideNormals => new object[]
        {
            Vector3.up,
            Vector3.right,
            Vector3.left
        };
    }
}
