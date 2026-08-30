namespace VerdantVigil;

public static class BracerRules
{
    public const int MaxCharge = 100;
    public const int LegacyMaxCharge = 5;

    public static int NormalizeCharge(int storedCharge, bool isPercentFormat)
    {
        return isPercentFormat
            ? Math.Clamp(storedCharge, 0, MaxCharge)
            : Math.Clamp(storedCharge, 0, LegacyMaxCharge) * 20;
    }

    public static bool TrySpendCharge(int currentCharge, int cost, out int remainingCharge)
    {
        remainingCharge = Math.Clamp(currentCharge, 0, MaxCharge);
        if (cost < 0 || remainingCharge < cost)
        {
            return false;
        }

        remainingCharge -= cost;
        return true;
    }

    public static bool IsCooldownReady(long nowMs, long lastUsedMs, int cooldownMs)
    {
        return nowMs >= lastUsedMs && nowMs - lastUsedMs >= cooldownMs;
    }

    public static int GetRecallGain(int currentCharge, int recallAmount)
    {
        return Math.Clamp(currentCharge + recallAmount, 0, MaxCharge) - Math.Clamp(currentCharge, 0, MaxCharge);
    }

    public static bool CanActivateFlight(bool isAlive, bool isSurvival, bool hasActiveFocus, int charge)
    {
        return isAlive && isSurvival && hasActiveFocus && charge > 0;
    }

    public static bool ShouldDrainFlight(long nowMs, long lastDrainMs, int intervalMs)
    {
        return nowMs >= lastDrainMs && nowMs - lastDrainMs >= intervalMs;
    }
}
