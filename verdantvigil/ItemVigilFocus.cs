using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace VerdantVigil;

public sealed class ItemVigilFocus : Item
{
    public const int MaxCharge = 100;
    private const int LegacyMaxCharge = 5;

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
        int charge = stack.Attributes.GetInt(ChargeKey, LegacyMaxCharge);
        return stack.Attributes.GetInt(ChargeFormatKey, 0) == 1
            ? Math.Clamp(charge, 0, MaxCharge)
            : Math.Clamp(charge, 0, LegacyMaxCharge) * 20;
    }

    internal static void SetCharge(ItemStack stack, int charge)
    {
        stack.Attributes.SetInt(ChargeKey, Math.Clamp(charge, 0, MaxCharge));
        stack.Attributes.SetInt(ChargeFormatKey, 1);
    }
}
