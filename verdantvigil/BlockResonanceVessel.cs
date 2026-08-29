using Vintagestory.API.Common;

namespace VerdantVigil;

public sealed class BlockResonanceVessel : Block
{
    public override bool OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        BlockEntityResonanceVessel? vessel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityResonanceVessel;
        return vessel?.OnInteract(byPlayer) == true || base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}
