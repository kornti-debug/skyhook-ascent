using System;
using System.Collections.Generic;

namespace SkyhookAscent.Gameplay
{
    public readonly struct ChunkCandidate
    {
        public ChunkCandidate(
            string id,
            int difficulty,
            int weight,
            ChunkTraversalCategory category)
        {
            Id = id;
            Difficulty = difficulty;
            Weight = weight;
            Category = category;
        }

        public string Id { get; }
        public int Difficulty { get; }
        public int Weight { get; }
        public ChunkTraversalCategory Category { get; }
    }

    public static class ChunkSelectionRules
    {
        public static int ChooseIndex(
            Random random,
            IReadOnlyList<ChunkCandidate> candidates,
            int maximumDifficulty,
            IReadOnlyCollection<string> recentChunkIds,
            ChunkTraversalCategory? previousCategory,
            int previousCategoryStreak)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (candidates == null || candidates.Count == 0)
            {
                return -1;
            }

            List<int> eligible = CollectEligible(
                candidates,
                maximumDifficulty,
                recentChunkIds,
                previousCategory,
                avoidPreviousCategory: previousCategoryStreak >= 2,
                avoidRecentIds: true);

            if (eligible.Count == 0)
            {
                eligible = CollectEligible(
                    candidates,
                    maximumDifficulty,
                    recentChunkIds,
                    previousCategory,
                    avoidPreviousCategory: false,
                    avoidRecentIds: true);
            }

            if (eligible.Count == 0)
            {
                eligible = CollectEligible(
                    candidates,
                    maximumDifficulty,
                    recentChunkIds,
                    previousCategory,
                    avoidPreviousCategory: false,
                    avoidRecentIds: false);
            }

            int totalWeight = 0;
            for (int i = 0; i < eligible.Count; i++)
            {
                totalWeight += Math.Max(1, candidates[eligible[i]].Weight);
            }

            if (totalWeight == 0)
            {
                return -1;
            }

            int roll = random.Next(totalWeight);
            for (int i = 0; i < eligible.Count; i++)
            {
                int index = eligible[i];
                roll -= Math.Max(1, candidates[index].Weight);
                if (roll < 0)
                {
                    return index;
                }
            }

            return eligible[eligible.Count - 1];
        }

        private static List<int> CollectEligible(
            IReadOnlyList<ChunkCandidate> candidates,
            int maximumDifficulty,
            IReadOnlyCollection<string> recentChunkIds,
            ChunkTraversalCategory? previousCategory,
            bool avoidPreviousCategory,
            bool avoidRecentIds)
        {
            List<int> eligible = new List<int>();
            for (int i = 0; i < candidates.Count; i++)
            {
                ChunkCandidate candidate = candidates[i];
                if (candidate.Difficulty > maximumDifficulty)
                {
                    continue;
                }

                if (avoidPreviousCategory && previousCategory.HasValue &&
                    candidate.Category == previousCategory.Value)
                {
                    continue;
                }

                if (avoidRecentIds && ContainsId(recentChunkIds, candidate.Id))
                {
                    continue;
                }

                eligible.Add(i);
            }

            return eligible;
        }

        private static bool ContainsId(
            IReadOnlyCollection<string> recentChunkIds,
            string candidateId)
        {
            if (recentChunkIds == null)
            {
                return false;
            }

            foreach (string recentId in recentChunkIds)
            {
                if (string.Equals(recentId, candidateId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
