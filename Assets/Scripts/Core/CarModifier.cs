using UnityEngine;

public enum CarModifier
{
    Normal,
    Express,
    Covered,
    Broken
}

public static class CarModifierRules
{
    private const float ExpressSpeedMultiplier = 1.55f;
    private const float BrokenSpeedMultiplier = 0.82f;
    private const int ExpressScoreMultiplier = 2;

    public static float GetSpeedMultiplier(CarModifier modifier)
    {
        return modifier switch
        {
            CarModifier.Express => ExpressSpeedMultiplier,
            CarModifier.Broken => BrokenSpeedMultiplier,
            _ => 1f
        };
    }

    public static int GetScoreMultiplier(CarModifier modifier)
    {
        return modifier == CarModifier.Express ? ExpressScoreMultiplier : 1;
    }

    public static bool RequiresRepair(CarModifier modifier)
    {
        return modifier == CarModifier.Broken;
    }

    public static bool StartsCovered(CarModifier modifier)
    {
        return modifier == CarModifier.Covered;
    }

    public static float ClampChance(float chance)
    {
        return Mathf.Clamp01(chance);
    }
}
