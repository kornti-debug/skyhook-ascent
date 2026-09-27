using UnityEngine;

namespace SkyhookAscent.Gameplay
{
    public enum RunPerk
    {
        QuickRecall,
        ClimbersPace,
        LightFeet
    }

    public static class RunPerkRules
    {
        public const float QuickRecallBonusPerStack = 0.35f;
        public const float ClimbersPaceBonusPerStack = 0.10f;
        public const float LightFeetBonusPerStack = 0.10f;

        public static float GetMultiplier(RunPerk perk, int stacks)
        {
            float bonusPerStack = perk switch
            {
                RunPerk.QuickRecall => QuickRecallBonusPerStack,
                RunPerk.ClimbersPace => ClimbersPaceBonusPerStack,
                RunPerk.LightFeet => LightFeetBonusPerStack,
                _ => 0f
            };

            return 1f + bonusPerStack * Mathf.Max(0, stacks);
        }
    }
}
