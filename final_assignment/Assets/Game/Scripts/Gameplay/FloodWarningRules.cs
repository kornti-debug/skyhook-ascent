using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    public static class FloodWarningRules
    {
        public static float CalculateStrength(
            float clearance,
            float warningDistance)
        {
            if (warningDistance <= 0f)
            {
                return clearance <= 0f ? 1f : 0f;
            }

            return Mathf.Clamp01(1f - clearance / warningDistance);
        }

        public static float CalculateCriticalStrength(
            float clearance,
            float criticalDistance)
        {
            return CalculateStrength(clearance, criticalDistance);
        }
    }
}
