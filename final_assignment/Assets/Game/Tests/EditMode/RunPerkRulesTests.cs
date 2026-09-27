using NUnit.Framework;
using SkyhookAscent.Gameplay;

namespace SkyhookAscent.Tests
{
    public sealed class RunPerkRulesTests
    {
        [TestCase(RunPerk.QuickRecall)]
        [TestCase(RunPerk.ClimbersPace)]
        [TestCase(RunPerk.LightFeet)]
        public void UnownedPerkDoesNotChangeBaseStats(RunPerk perk)
        {
            Assert.That(RunPerkRules.GetMultiplier(perk, 0), Is.EqualTo(1f));
        }

        [Test]
        public void QuickRecallAddsThirtyFivePercentPerPick()
        {
            Assert.That(
                RunPerkRules.GetMultiplier(RunPerk.QuickRecall, 1),
                Is.EqualTo(1.35f).Within(0.001f));
            Assert.That(
                RunPerkRules.GetMultiplier(RunPerk.QuickRecall, 2),
                Is.EqualTo(1.70f).Within(0.001f));
        }

        [Test]
        public void MovementAndJumpPerksStackByTenPercentPerPick()
        {
            Assert.That(
                RunPerkRules.GetMultiplier(RunPerk.ClimbersPace, 3),
                Is.EqualTo(1.30f).Within(0.001f));
            Assert.That(
                RunPerkRules.GetMultiplier(RunPerk.LightFeet, 2),
                Is.EqualTo(1.20f).Within(0.001f));
        }

        [Test]
        public void NegativeStackCountBehavesLikeNoPerk()
        {
            Assert.That(
                RunPerkRules.GetMultiplier(RunPerk.LightFeet, -1),
                Is.EqualTo(1f));
        }
    }
}
