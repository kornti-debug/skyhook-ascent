using System;
using System.Collections.Generic;
using NUnit.Framework;
using SkyhookAscent.Gameplay;

namespace SkyhookAscent.Tests
{
    public sealed class ChunkSelectionRulesTests
    {
        private static readonly ChunkCandidate[] Candidates =
        {
            new ChunkCandidate("jumps-a", 1, 2, ChunkTraversalCategory.Jumps),
            new ChunkCandidate("jumps-b", 1, 1, ChunkTraversalCategory.Jumps),
            new ChunkCandidate("zip-a", 1, 2, ChunkTraversalCategory.Grapple),
            new ChunkCandidate("zip-b", 2, 1, ChunkTraversalCategory.Grapple),
            new ChunkCandidate("mixed", 2, 1, ChunkTraversalCategory.Mixed)
        };

        [Test]
        public void SameSeedProducesSameSequence()
        {
            CollectionAssert.AreEqual(BuildSequence(104729), BuildSequence(104729));
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            CollectionAssert.AreNotEqual(BuildSequence(104729), BuildSequence(104759));
        }

        [Test]
        public void SelectionAllowsSecondCategoryInStreak()
        {
            Random random = new Random(7);
            int index = ChunkSelectionRules.ChooseIndex(
                random,
                Candidates,
                2,
                new[] { "zip-a", "zip-b", "mixed" },
                ChunkTraversalCategory.Jumps,
                1);

            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(Candidates[index].Category, Is.EqualTo(ChunkTraversalCategory.Jumps));
        }

        [Test]
        public void SelectionAvoidsThirdCategoryWhenAlternativeExists()
        {
            Random random = new Random(7);
            int index = ChunkSelectionRules.ChooseIndex(
                random,
                Candidates,
                2,
                Array.Empty<string>(),
                ChunkTraversalCategory.Jumps,
                2);

            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(Candidates[index].Category, Is.Not.EqualTo(ChunkTraversalCategory.Jumps));
        }

        [Test]
        public void SelectionNeverExceedsDifficultyBand()
        {
            Random random = new Random(11);
            for (int i = 0; i < 20; i++)
            {
                int index = ChunkSelectionRules.ChooseIndex(
                    random,
                    Candidates,
                    1,
                    Array.Empty<string>(),
                    null,
                    0);
                Assert.That(index, Is.GreaterThanOrEqualTo(0));
                Assert.That(Candidates[index].Difficulty, Is.LessThanOrEqualTo(1));
            }
        }

        [Test]
        public void TurnRuleAllowsForwardAndQuarterTurnButRejectsReverse()
        {
            Assert.That(ChunkPlacementRules.IsTurnAllowed(
                UnityEngine.Vector3.forward,
                UnityEngine.Vector3.right,
                100f), Is.True);
            Assert.That(ChunkPlacementRules.IsTurnAllowed(
                UnityEngine.Vector3.forward,
                UnityEngine.Vector3.back,
                100f), Is.False);
        }

        [Test]
        public void ClearanceRuleDetectsObstacleOnProtectedSegment()
        {
            TraversalClearanceSegment segment = new TraversalClearanceSegment(
                UnityEngine.Vector3.zero,
                UnityEngine.Vector3.forward * 8f,
                1f);
            UnityEngine.Bounds blocking = new UnityEngine.Bounds(
                UnityEngine.Vector3.forward * 4f,
                UnityEngine.Vector3.one);
            UnityEngine.Bounds clear = new UnityEngine.Bounds(
                UnityEngine.Vector3.right * 5f + UnityEngine.Vector3.forward * 4f,
                UnityEngine.Vector3.one);

            Assert.That(ChunkPlacementRules.BoundsBlockSegment(
                blocking,
                segment), Is.True);
            Assert.That(ChunkPlacementRules.BoundsBlockSegment(
                clear,
                segment), Is.False);
        }

        [Test]
        public void StreamingAppendsWhenPlayerEntersAheadWindow()
        {
            Assert.That(TowerStreamingRules.ShouldAppend(100f, 64f, 35f), Is.False);
            Assert.That(TowerStreamingRules.ShouldAppend(100f, 65f, 35f), Is.True);
        }

        [Test]
        public void StreamingRecyclesOnlyAfterStageIsBelowWaterMargin()
        {
            Assert.That(TowerStreamingRules.CanRecycle(98f, 100f, 2f), Is.False);
            Assert.That(TowerStreamingRules.CanRecycle(97.9f, 100f, 2f), Is.True);
        }

        [Test]
        public void StageSeedIsDeterministicAndVariesByStage()
        {
            int first = TowerStreamingRules.DeriveStageSeed(104729, 1, 0);
            int repeated = TowerStreamingRules.DeriveStageSeed(104729, 1, 0);
            int nextStage = TowerStreamingRules.DeriveStageSeed(104729, 2, 0);

            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(nextStage, Is.Not.EqualTo(first));
            Assert.That(first, Is.GreaterThan(0));
        }

        private static string[] BuildSequence(int seed)
        {
            Random random = new Random(seed);
            Queue<string> recent = new Queue<string>();
            List<string> sequence = new List<string>();
            ChunkTraversalCategory? previous = null;
            int streak = 0;

            for (int i = 0; i < 10; i++)
            {
                int index = ChunkSelectionRules.ChooseIndex(
                    random,
                    Candidates,
                    2,
                    recent,
                    previous,
                    streak);
                ChunkCandidate selected = Candidates[index];
                sequence.Add(selected.Id);
                streak = previous.HasValue && previous.Value == selected.Category
                    ? streak + 1
                    : 1;
                previous = selected.Category;
                recent.Enqueue(selected.Id);
                while (recent.Count > 2)
                {
                    recent.Dequeue();
                }
            }

            return sequence.ToArray();
        }
    }
}
