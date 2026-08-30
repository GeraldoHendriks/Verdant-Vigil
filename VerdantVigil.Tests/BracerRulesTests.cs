using Xunit;

namespace VerdantVigil.Tests;

public sealed class BracerRulesTests
{
    [Theory]
    [InlineData(-1, false, 0)]
    [InlineData(0, false, 0)]
    [InlineData(1, false, 20)]
    [InlineData(5, false, 100)]
    [InlineData(6, false, 100)]
    [InlineData(-1, true, 0)]
    [InlineData(42, true, 42)]
    [InlineData(101, true, 100)]
    public void NormalizeCharge_ClampsLegacyAndPercentFormats(int storedCharge, bool isPercentFormat, int expected)
    {
        Assert.Equal(expected, BracerRules.NormalizeCharge(storedCharge, isPercentFormat));
    }

    [Theory]
    [InlineData(100, 20, true, 80)]
    [InlineData(20, 20, true, 0)]
    [InlineData(19, 20, false, 19)]
    [InlineData(25, -1, false, 25)]
    public void TrySpendCharge_OnlySpendsAffordablePositiveCosts(int charge, int cost, bool expectedSuccess, int expectedRemaining)
    {
        bool success = BracerRules.TrySpendCharge(charge, cost, out int remaining);

        Assert.Equal(expectedSuccess, success);
        Assert.Equal(expectedRemaining, remaining);
    }

    [Theory]
    [InlineData(3_000, 0, 3_000, true)]
    [InlineData(2_999, 0, 3_000, false)]
    [InlineData(1_000, 2_000, 3_000, false)]
    public void IsCooldownReady_RequiresElapsedCooldownWithoutClockRollback(long now, long lastUsed, int cooldown, bool expected)
    {
        Assert.Equal(expected, BracerRules.IsCooldownReady(now, lastUsed, cooldown));
    }

    [Theory]
    [InlineData(0, 20, 20)]
    [InlineData(80, 20, 20)]
    [InlineData(90, 20, 10)]
    [InlineData(100, 20, 0)]
    public void GetRecallGain_ReportsOnlyAppliedResonance(int charge, int recallAmount, int expected)
    {
        Assert.Equal(expected, BracerRules.GetRecallGain(charge, recallAmount));
    }

    [Theory]
    [InlineData(true, true, true, 1, true)]
    [InlineData(false, true, true, 100, false)]
    [InlineData(true, false, true, 100, false)]
    [InlineData(true, true, false, 100, false)]
    [InlineData(true, true, true, 0, false)]
    public void CanActivateFlight_RequiresSurvivalPlayerActiveFocusAndCharge(bool alive, bool survival, bool hasFocus, int charge, bool expected)
    {
        Assert.Equal(expected, BracerRules.CanActivateFlight(alive, survival, hasFocus, charge));
    }

    [Theory]
    [InlineData(60_000, 0, 60_000, true)]
    [InlineData(59_999, 0, 60_000, false)]
    [InlineData(1_000, 2_000, 60_000, false)]
    public void ShouldDrainFlight_RequiresTheConfiguredIntervalWithoutClockRollback(long now, long lastDrain, int interval, bool expected)
    {
        Assert.Equal(expected, BracerRules.ShouldDrainFlight(now, lastDrain, interval));
    }
}
