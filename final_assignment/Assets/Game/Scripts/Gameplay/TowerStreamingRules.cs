using System;

namespace SkyhookAscent.Gameplay
{
    public static class TowerStreamingRules
    {
        public static bool ShouldAppend(
            float highestGeneratedHeight,
            float playerHeight,
            float generationAheadDistance)
        {
            return highestGeneratedHeight - playerHeight <=
                Math.Max(0f, generationAheadDistance);
        }

        public static bool CanRecycle(
            float stageHighestPoint,
            float waterSurfaceHeight,
            float cleanupMargin)
        {
            return stageHighestPoint <
                waterSurfaceHeight - Math.Max(0f, cleanupMargin);
        }

        public static int DeriveStageSeed(
            int runSeed,
            int stageIndex,
            int attempt)
        {
            int derived = unchecked(
                runSeed * 73856093 ^
                (stageIndex + 1) * 19349663 ^
                (attempt + 1) * 83492791) & int.MaxValue;
            return derived == 0 ? 1 : derived;
        }
    }
}
