using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VerdantVigil;

public sealed class BlockEntityResonanceVessel : BlockEntity
{
    public bool OnInteract(IPlayer byPlayer)
    {
        ItemSlot? slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (slot?.Itemstack?.Collectible is not ItemVigilFocus focus)
        {
            return false;
        }

        if (Api.Side != EnumAppSide.Server || byPlayer is not IServerPlayer player)
        {
            return true;
        }

        ItemStack stack = slot.Itemstack;
        int charge = ItemVigilFocus.GetCharge(stack);
        if (charge == ItemVigilFocus.MaxCharge)
        {
            Notify(player, "vessel-already-charged");
            return true;
        }

        ItemVigilFocus.SetCharge(stack, ItemVigilFocus.MaxCharge);
        slot.MarkDirty();
        Notify(player, "vessel-recharged", ItemVigilFocus.MaxCharge);
        return true;
    }

    private static void Notify(IServerPlayer player, string langCode, params object[] args)
    {
        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            Lang.GetL(player.LanguageCode, $"verdantvigil:{langCode}", args),
            EnumChatType.Notification);
    }
}
