using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace VerdantVigil;

public sealed class BracerWardBehavior : EntityBehavior
{
    public BracerWardBehavior(Entity entity) : base(entity)
    {
    }

    public override string PropertyName() => "verdantvigilbracerward";

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
        if (entity.Api.Side != EnumAppSide.Server || entity is not EntityPlayer playerEntity ||
            playerEntity.Player is not IServerPlayer player || damage <= 0)
        {
            return;
        }

        VerdantVigilModSystem.TryAbsorbSevereDamage(player, ref damage);
    }
}
