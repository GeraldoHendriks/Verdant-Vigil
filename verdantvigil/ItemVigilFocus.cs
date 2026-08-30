using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace VerdantVigil;

public sealed class ItemVigilFocus : Item
{
    public const int MaxCharge = 100;
    private const int LegacyMaxCharge = 5;

    internal const string ChargeKey = "verdantvigil:charge";
    private const string ChargeFormatKey = "verdantvigil:chargeformat";
    private const string ToolModeKey = "verdantvigil:toolmode";

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

    public override SkillItem[] GetToolModes(ItemSlot inSlot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        return
        [
            Mode("flight", "verdantvigil:mode-flight"),
            Mode("dash", "verdantvigil:mode-dash"),
            Mode("step", "verdantvigil:mode-step"),
            Mode("sense", "verdantvigil:mode-sense"),
            Mode("recall", "verdantvigil:mode-recall")
        ];
    }

    public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        return Math.Clamp(slot.Itemstack?.Attributes.GetInt(ToolModeKey, 0) ?? 0, 0, 4);
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
    {
        if (slot.Itemstack == null)
        {
            return;
        }

        slot.Itemstack.Attributes.SetInt(ToolModeKey, Math.Clamp(toolMode, 0, 4));
        slot.MarkDirty();
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (!firstEvent || byEntity.Api.Side != EnumAppSide.Server || byEntity is not EntityPlayer playerEntity ||
            playerEntity.Player is not IServerPlayer player)
        {
            return;
        }

        if (blockSelection != null && byEntity.World.BlockAccessor.GetBlockEntity(blockSelection.Position) is BlockEntityResonanceVessel)
        {
            return;
        }

        byEntity.Api.ModLoader.GetModSystem<VerdantVigilModSystem>().UseSelectedMode(player, GetToolMode(slot, player, blockSelection!));
        handling = EnumHandHandling.PreventDefault;
    }

    private static SkillItem Mode(string code, string langCode)
    {
        return new SkillItem
        {
            Code = new AssetLocation("verdantvigil", code),
            Name = Lang.Get(langCode)
        };
    }
}
