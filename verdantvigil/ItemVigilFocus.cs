using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VerdantVigil;

public sealed class ItemVigilFocus : Item
{
    public const int MaxCharge = BracerRules.MaxCharge;

    internal const string ChargeKey = "verdantvigil:charge";
    private const string ChargeFormatKey = "verdantvigil:chargeformat";

    public override void GetHeldItemInfo(
        ItemSlot inSlot,
        StringBuilder dsc,
        IWorldAccessor world,
        bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        if (inSlot.Itemstack is ItemStack stack)
        {
            dsc.AppendLine(Lang.Get("verdantvigil:focus-charge", GetCharge(stack)));
        }
    }

    internal static int GetCharge(ItemStack stack)
    {
        int charge = stack.Attributes.GetInt(ChargeKey, BracerRules.LegacyMaxCharge);
        return BracerRules.NormalizeCharge(charge, stack.Attributes.GetInt(ChargeFormatKey, 0) == 1);
    }

    internal static void SetCharge(ItemStack stack, int charge)
    {
        stack.Attributes.SetInt(ChargeKey, Math.Clamp(charge, 0, MaxCharge));
        stack.Attributes.SetInt(ChargeFormatKey, 1);
    }
}
