using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace VerdantVigil;

public sealed class BlockEntityResonantBarrier : BlockEntity
{
    private const int LifetimeMilliseconds = 15_000;
    private const string ExpiresAtKey = "expiresAtUtcMs";

    private long expiresAtUtcMs;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.Side != EnumAppSide.Server)
        {
            return;
        }

        if (expiresAtUtcMs <= 0)
        {
            SetExpiration();
        }

        RegisterGameTickListener(OnServerTick, 500);
    }

    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);

        if (Api?.Side == EnumAppSide.Server)
        {
            SetExpiration();
            MarkDirty();
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetLong(ExpiresAtKey, expiresAtUtcMs);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        base.FromTreeAttributes(tree, worldAccessForResolve);
        expiresAtUtcMs = tree.GetLong(ExpiresAtKey);
    }

    private void OnServerTick(float deltaTime)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < expiresAtUtcMs)
        {
            return;
        }

        BlockPos position = Pos.Copy();
        Api.World.BlockAccessor.SetBlock(0, position);
        Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(position);
    }

    private void SetExpiration()
    {
        expiresAtUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + LifetimeMilliseconds;
    }
}
