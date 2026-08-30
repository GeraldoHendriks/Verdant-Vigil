using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

namespace VerdantVigil;

public sealed class VerdantVigilModSystem : ModSystem
{
    private const string FlightChannelName = "verdantvigilflight";
    private const int FlightChargeIntervalMilliseconds = 60_000;
    private const int FlightChargeCost = 20;
    private const int DescentChargeIntervalMilliseconds = 2_000;
    private const int DescentChargeCost = 2;
    private const int DashChargeCost = 15;
    private const int StepChargeCost = 10;
    private const int SenseChargeCost = 5;
    private const int RecallChargeAmount = 20;
    private const int WardChargeCost = 40;
    private const int DashCooldownMilliseconds = 3_000;
    private const int StepCooldownMilliseconds = 3_000;
    private const int SenseCooldownMilliseconds = 10_000;
    private const int RecallCooldownMilliseconds = 30_000;
    private const int WardCooldownMilliseconds = 60_000;
    private const float FlightSprintMultiplier = 3f;
    private const string HoverAnimationCode = "bracerflight-hover";
    private const string CruiseAnimationCode = "bracerflight-cruise";
    private const string SprintAnimationCode = "bracerflight-sprint";

    private ICoreClientAPI? capi;
    private ICoreServerAPI? sapi;
    private IClientNetworkChannel? clientFlightChannel;
    private IServerNetworkChannel? serverFlightChannel;
    private readonly Dictionary<string, FlightState> activeFlights = new();
    private readonly Dictionary<string, AbilityState> abilityStates = new();
    private long clientFlightTickListener;
    private long serverFlightTickListener;
    private bool clientFlightEnabled;
    private bool previousFreeMove;
    private float previousMoveSpeedMultiplier;
    private uint nextFlightRequestSequence;
    private string? clientFlightAnimationCode;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemVigilFocus", typeof(ItemVigilFocus));
        api.RegisterBlockEntityClass("ResonantBarrier", typeof(BlockEntityResonantBarrier));
        api.RegisterBlockClass("BlockResonanceVessel", typeof(BlockResonanceVessel));
        api.RegisterBlockEntityClass("ResonanceVessel", typeof(BlockEntityResonanceVessel));
        api.RegisterEntityBehaviorClass("verdantvigilbracerward", typeof(BracerWardBehavior));

        api.Network.RegisterChannel(FlightChannelName)
            .RegisterMessageType<FlightRequestPacket>()
            .RegisterMessageType<FlightStatePacket>()
            .RegisterMessageType<BracerAbilityPacket>();

        Mod.Logger.Notification("Verdant Vigil 0.3.0 initialized on {0}.", api.Side);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        clientFlightChannel = api.Network.GetChannel(FlightChannelName)
            .SetMessageHandler<FlightStatePacket>(OnFlightStateReceived);

        api.Input.RegisterHotKey(
            "verdantvigil-flight",
            Lang.Get("verdantvigil:hotkey-flight"),
            GlKeys.R,
            HotkeyType.CharacterControls);
        api.Input.SetHotKeyHandler("verdantvigil-flight", ToggleFlight);
        RegisterAbilityHotKey(api, "verdantvigil-dash", "verdantvigil:hotkey-dash", GlKeys.J, BracerAbility.Dash);
        RegisterAbilityHotKey(api, "verdantvigil-step", "verdantvigil:hotkey-step", GlKeys.K, BracerAbility.Step);
        RegisterAbilityHotKey(api, "verdantvigil-sense", "verdantvigil:hotkey-sense", GlKeys.V, BracerAbility.Sense);
        RegisterAbilityHotKey(api, "verdantvigil-recall", "verdantvigil:hotkey-recall", GlKeys.B, BracerAbility.Recall);
        clientFlightTickListener = api.Event.RegisterGameTickListener(_ => UpdateClientFlightSpeed(), 20);

    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        serverFlightChannel = api.Network.GetChannel(FlightChannelName)
            .SetMessageHandler<FlightRequestPacket>(OnFlightRequested);
        serverFlightChannel.SetMessageHandler<BracerAbilityPacket>(OnAbilityRequested);
        serverFlightTickListener = api.Event.RegisterGameTickListener(OnServerFlightTick, 100);
        api.Event.PlayerDeath += (player, damageSource) => RevokeFlight(player, "verdantvigil:flight-revoked");
        api.Event.PlayerDisconnect += player => RevokeFlight(player, null);
        api.Event.PlayerSwitchGameMode += player => RevokeFlight(player, "verdantvigil:flight-revoked");
    }

    public override void Dispose()
    {
        RestoreClientFlight();

        if (clientFlightTickListener != 0 && capi != null)
        {
            capi.Event.UnregisterGameTickListener(clientFlightTickListener);
        }

        if (serverFlightTickListener != 0 && sapi != null)
        {
            sapi.Event.UnregisterGameTickListener(serverFlightTickListener);
        }

        activeFlights.Clear();
        base.Dispose();
    }

    private bool ToggleFlight(KeyCombination keyCombination)
    {
        if (clientFlightChannel?.Connected != true)
        {
            return false;
        }

        clientFlightChannel.SendPacket(new FlightRequestPacket
        {
            Desired = !clientFlightEnabled,
            Sequence = ++nextFlightRequestSequence
        });
        return true;
    }

    private void OnFlightStateReceived(FlightStatePacket packet)
    {
        if ((packet.Sequence != 0 && packet.Sequence < nextFlightRequestSequence) || capi == null)
        {
            return;
        }

        if (packet.Sequence != 0)
        {
            nextFlightRequestSequence = packet.Sequence;
        }
        if (packet.Granted)
        {
            if (!clientFlightEnabled)
            {
                previousFreeMove = capi.World.Player.WorldData.FreeMove;
                previousMoveSpeedMultiplier = capi.World.Player.WorldData.MoveSpeedMultiplier;
                clientFlightEnabled = true;
            }

            capi.World.Player.WorldData.FreeMove = true;
            capi.World.Player.WorldData.NoClip = false;
            capi.World.Player.Entity.Controls.Gliding = false;
            SetClientFlightAnimation(DetermineClientFlightAnimation());
            return;
        }

        RestoreClientFlight();
        if (!string.IsNullOrEmpty(packet.Reason))
        {
            capi.TriggerIngameError(null, "verdantvigil-flight", Lang.Get(packet.Reason));
        }
    }

    private void RestoreClientFlight()
    {
        if (!clientFlightEnabled || capi == null)
        {
            return;
        }

        // Preserve a mode change to creative or spectator while this feature was active.
        if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
        {
            capi.World.Player.WorldData.FreeMove = previousFreeMove;
            capi.World.Player.WorldData.NoClip = false;
        }

        capi.World.Player.WorldData.MoveSpeedMultiplier = previousMoveSpeedMultiplier;
        StopFlightAnimations(capi.World.Player.Entity);
        clientFlightAnimationCode = null;

        clientFlightEnabled = false;
    }

    private void OnFlightRequested(IServerPlayer player, FlightRequestPacket packet)
    {
        if (!packet.Desired)
        {
            RevokeFlight(player, null, packet.Sequence);
            return;
        }

        if (!CanFly(player, out string? reason))
        {
            SendFlightState(player, false, packet.Sequence, reason);
            return;
        }

        FlightState state = new()
        {
            LastChargeDrainMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            AnimationCode = HoverAnimationCode
        };
        activeFlights[player.PlayerUID] = state;
        ApplyFlightControls(player, state);
        SendFlightState(player, true, packet.Sequence, null);
    }

    private void UpdateClientFlightSpeed()
    {
        if (capi == null)
        {
            return;
        }

        var player = capi.World.Player;
        if (!clientFlightEnabled || player.WorldData.CurrentGameMode != EnumGameMode.Survival)
        {
            return;
        }

        bool sprintFlight = player.Entity.Controls.Sprint && player.Entity.Controls.Forward;
        player.WorldData.MoveSpeedMultiplier = previousMoveSpeedMultiplier *
            (sprintFlight ? FlightSprintMultiplier : 1f);
        SetClientFlightAnimation(DetermineClientFlightAnimation());
    }

    private void OnServerFlightTick(float deltaTime)
    {
        if (sapi == null)
        {
            return;
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (string playerUid in activeFlights.Keys.ToArray())
        {
            IServerPlayer? player = sapi.World.PlayerByUid(playerUid) as IServerPlayer;
            string? reason = null;
            if (player == null || !CanFly(player, out reason))
            {
                if (player != null)
                {
                    RevokeFlight(player, reason);
                }
                else
                {
                    activeFlights.Remove(playerUid);
                }
                continue;
            }

            FlightState state = activeFlights[playerUid];
            if (now - state.LastChargeDrainMs >= FlightChargeIntervalMilliseconds)
            {
                ItemSlot slot = GetEquippedBracerSlot(player)!;
                ItemStack stack = slot.Itemstack!;
                int charge = ItemVigilFocus.GetCharge(stack) - FlightChargeCost;
                ItemVigilFocus.SetCharge(stack, charge);
                slot.MarkDirty();
                state.LastChargeDrainMs = now;

                if (charge <= 0)
                {
                    RevokeFlight(player, "verdantvigil:flight-depleted");
                    continue;
                }
            }

            ApplyFlightControls(player, state);
        }

        foreach (IServerPlayer player in sapi.World.AllOnlinePlayers.OfType<IServerPlayer>())
        {
            ApplyPassiveAbilities(player, now);
        }
    }

    private static bool CanFly(IServerPlayer player, out string? reason)
    {
        reason = null;
        if (player.Entity == null || !player.Entity.Alive ||
            player.WorldData.CurrentGameMode != EnumGameMode.Survival)
        {
            reason = "verdantvigil:flight-unavailable";
            return false;
        }

        ItemStack? stack = GetEquippedBracerSlot(player)?.Itemstack;
        if (stack?.Collectible is not ItemVigilFocus)
        {
            reason = "verdantvigil:flight-focus-required";
            return false;
        }

        if (ItemVigilFocus.GetCharge(stack) <= 0)
        {
            reason = "verdantvigil:flight-depleted";
            return false;
        }

        return true;
    }

    private static ItemSlot? GetEquippedBracerSlot(IPlayer player)
    {
        IInventory? characterInventory = player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
        return characterInventory?[(int)EnumCharacterDressType.Arm];
    }

    private static void ApplyFlightControls(IServerPlayer player, FlightState state)
    {
        player.Entity.Controls.IsFlying = true;
        player.Entity.Controls.Gliding = false;
        player.Entity.Controls.NoClip = false;
        player.Entity.PositionBeforeFalling.Set(player.Entity.Pos);
        string desiredAnimation = DetermineServerFlightAnimation(player);
        if (state.AnimationCode != desiredAnimation)
        {
            player.Entity.StopAnimation(state.AnimationCode);
            state.AnimationCode = desiredAnimation;
            player.Entity.StartAnimation(desiredAnimation);
        }
        else if (!player.Entity.AnimManager.IsAnimationActive(desiredAnimation))
        {
            player.Entity.StartAnimation(desiredAnimation);
        }
    }

    private void RevokeFlight(IServerPlayer player, string? reason, uint sequence = 0)
    {
        if (!activeFlights.Remove(player.PlayerUID, out FlightState? state))
        {
            return;
        }

        if (player.Entity != null)
        {
            player.Entity.Controls.IsFlying = false;
            player.Entity.Controls.Gliding = false;
            player.Entity.Controls.NoClip = false;
            player.Entity.PositionBeforeFalling.Set(player.Entity.Pos);
            StopFlightAnimations(player.Entity);
            GetAbilityState(player).LandingCushionUntilMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 10_000;
        }

        if (player.ConnectionState == EnumClientState.Playing)
        {
            SendFlightState(player, false, sequence, reason);
        }
    }

    private void SendFlightState(IServerPlayer player, bool granted, uint sequence, string? reason)
    {
        serverFlightChannel?.SendPacket(new FlightStatePacket
        {
            Granted = granted,
            Sequence = sequence,
            Reason = reason
        }, player);
    }

    private void RegisterAbilityHotKey(ICoreClientAPI api, string code, string langCode, GlKeys key, BracerAbility ability)
    {
        api.Input.RegisterHotKey(code, Lang.Get(langCode), key, HotkeyType.CharacterControls);
        api.Input.SetHotKeyHandler(code, _ =>
        {
            if (clientFlightChannel?.Connected != true)
            {
                return false;
            }

            clientFlightChannel.SendPacket(new BracerAbilityPacket { Ability = (int)ability });
            return true;
        });
    }

    private void OnAbilityRequested(IServerPlayer player, BracerAbilityPacket packet)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        switch ((BracerAbility)packet.Ability)
        {
            case BracerAbility.Dash:
                TryDash(player, now);
                break;
            case BracerAbility.Step:
                TryStep(player, now);
                break;
            case BracerAbility.Sense:
                TrySense(player, now);
                break;
            case BracerAbility.Recall:
                TryRecall(player, now);
                break;
        }
    }

    private void ApplyPassiveAbilities(IServerPlayer player, long now)
    {
        if (player.Entity == null || player.WorldData.CurrentGameMode != EnumGameMode.Survival ||
            GetEquippedBracerSlot(player)?.Itemstack is not ItemStack stack)
        {
            return;
        }

        AbilityState state = GetAbilityState(player);
        if (state.LandingCushionUntilMs > now)
        {
            player.Entity.PositionBeforeFalling.Set(player.Entity.Pos);
        }

        if (!activeFlights.ContainsKey(player.PlayerUID) && !player.Entity.OnGround && player.Entity.Controls.Jump &&
            player.Entity.Pos.Motion.Y < -0.04 && now - state.LastDescentChargeMs >= DescentChargeIntervalMilliseconds &&
            TrySpendCharge(player, DescentChargeCost))
        {
            player.Entity.Pos.Motion.Y = -0.04;
            player.Entity.PositionBeforeFalling.Set(player.Entity.Pos);
            state.LastDescentChargeMs = now;
        }
    }

    private void TryDash(IServerPlayer player, long now)
    {
        AbilityState state = GetAbilityState(player);
        if (!CanUseAbility(player, now, state.LastDashMs, DashCooldownMilliseconds) || !TrySpendCharge(player, DashChargeCost)) return;
        Vec3f direction = player.Entity.Pos.GetViewVector();
        player.Entity.Pos.Motion.X += direction.X * 1.8;
        player.Entity.Pos.Motion.Y += direction.Y * 1.8;
        player.Entity.Pos.Motion.Z += direction.Z * 1.8;
        state.LastDashMs = now;
        Notify(player, "dash-used");
    }

    private void TryStep(IServerPlayer player, long now)
    {
        AbilityState state = GetAbilityState(player);
        if (!player.Entity.OnGround || !CanUseAbility(player, now, state.LastStepMs, StepCooldownMilliseconds) || !TrySpendCharge(player, StepChargeCost)) return;
        Vec3f direction = player.Entity.Pos.GetViewVector();
        player.Entity.Pos.Motion.X += direction.X * 0.8;
        player.Entity.Pos.Motion.Z += direction.Z * 0.8;
        player.Entity.Pos.Motion.Y = 0.52;
        state.LastStepMs = now;
        Notify(player, "step-used");
    }

    private void TrySense(IServerPlayer player, long now)
    {
        AbilityState state = GetAbilityState(player);
        if (!CanUseAbility(player, now, state.LastSenseMs, SenseCooldownMilliseconds) || !TrySpendCharge(player, SenseChargeCost)) return;
        List<BlockPos> vessels = FindNearbyVessels(player, 24);
        if (vessels.Count > 0)
        {
            sapi!.World.HighlightBlocks(player, 913, vessels, Enumerable.Repeat(unchecked((int)0xff76c98d), vessels.Count).ToList(), EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube, 5);
        }
        state.LastSenseMs = now;
        Notify(player, "sense-result", vessels.Count);
    }

    private void TryRecall(IServerPlayer player, long now)
    {
        AbilityState state = GetAbilityState(player);
        if (!CanUseAbility(player, now, state.LastRecallMs, RecallCooldownMilliseconds)) return;
        if (FindNearbyVessels(player, 32).Count == 0)
        {
            Notify(player, "recall-no-vessel");
            return;
        }
        ItemSlot slot = GetEquippedBracerSlot(player)!;
        ItemVigilFocus.SetCharge(slot.Itemstack!, ItemVigilFocus.GetCharge(slot.Itemstack!) + RecallChargeAmount);
        slot.MarkDirty();
        state.LastRecallMs = now;
        Notify(player, "recall-used", RecallChargeAmount);
    }

    private List<BlockPos> FindNearbyVessels(IServerPlayer player, int range)
    {
        BlockPos origin = player.Entity.Pos.AsBlockPos;
        List<BlockPos> results = [];
        for (int x = -range; x <= range; x++) for (int y = -12; y <= 12; y++) for (int z = -range; z <= range; z++)
        {
            BlockPos pos = origin.AddCopy(x, y, z);
            if (sapi!.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityResonanceVessel) results.Add(pos);
        }
        return results;
    }

    private static bool CanUseAbility(IServerPlayer player, long now, long lastUsed, int cooldown) =>
        player.Entity != null && player.Entity.Alive && player.WorldData.CurrentGameMode == EnumGameMode.Survival &&
        now - lastUsed >= cooldown && GetEquippedBracerSlot(player)?.Itemstack?.Collectible is ItemVigilFocus;

    private static bool TrySpendCharge(IServerPlayer player, int amount)
    {
        ItemSlot? slot = GetEquippedBracerSlot(player);
        if (slot?.Itemstack is not ItemStack stack || stack.Collectible is not ItemVigilFocus || ItemVigilFocus.GetCharge(stack) < amount) return false;
        ItemVigilFocus.SetCharge(stack, ItemVigilFocus.GetCharge(stack) - amount);
        slot.MarkDirty();
        return true;
    }

    internal static void TryAbsorbSevereDamage(IServerPlayer player, ref float damage)
    {
        if (damage < 8 || GetEquippedBracerSlot(player)?.Itemstack is not ItemStack stack || ItemVigilFocus.GetCharge(stack) < WardChargeCost) return;
        var system = player.Entity.Api.ModLoader.GetModSystem<VerdantVigilModSystem>();
        AbilityState state = system.GetAbilityState(player);
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - state.LastWardMs < WardCooldownMilliseconds || !TrySpendCharge(player, WardChargeCost)) return;
        damage *= 0.25f;
        state.LastWardMs = now;
        Notify(player, "ward-used");
    }

    private AbilityState GetAbilityState(IServerPlayer player)
    {
        if (!abilityStates.TryGetValue(player.PlayerUID, out AbilityState? state)) abilityStates[player.PlayerUID] = state = new AbilityState();
        return state;
    }

    private static void Notify(IServerPlayer player, string code, params object[] args) => player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.GetL(player.LanguageCode, $"verdantvigil:{code}", args), EnumChatType.Notification);

    private sealed class FlightState
    {
        public long LastChargeDrainMs { get; set; }
        public string AnimationCode { get; set; } = HoverAnimationCode;
    }

    private sealed class AbilityState
    {
        public long LandingCushionUntilMs { get; set; }
        public long LastDescentChargeMs { get; set; }
        public long LastDashMs { get; set; }
        public long LastStepMs { get; set; }
        public long LastSenseMs { get; set; }
        public long LastRecallMs { get; set; }
        public long LastWardMs { get; set; }
    }

    private string DetermineClientFlightAnimation()
    {
        if (capi?.World.Player.Entity.Controls.Forward != true)
        {
            return HoverAnimationCode;
        }

        return capi.World.Player.Entity.Controls.Sprint ? SprintAnimationCode : CruiseAnimationCode;
    }

    private void SetClientFlightAnimation(string animationCode)
    {
        if (capi == null || clientFlightAnimationCode == animationCode)
        {
            return;
        }

        if (clientFlightAnimationCode != null)
        {
            capi.World.Player.Entity.StopAnimation(clientFlightAnimationCode);
        }

        clientFlightAnimationCode = animationCode;
        capi.World.Player.Entity.StartAnimation(animationCode);
    }

    private static string DetermineServerFlightAnimation(IServerPlayer player)
    {
        if (!player.Entity.Controls.Forward)
        {
            return HoverAnimationCode;
        }

        return player.Entity.Controls.Sprint ? SprintAnimationCode : CruiseAnimationCode;
    }

    private static void StopFlightAnimations(Entity entity)
    {
        entity.StopAnimation(HoverAnimationCode);
        entity.StopAnimation(CruiseAnimationCode);
        entity.StopAnimation(SprintAnimationCode);
    }

}

[ProtoContract]
public sealed class FlightRequestPacket
{
    [ProtoMember(1)]
    public bool Desired { get; set; }

    [ProtoMember(2)]
    public uint Sequence { get; set; }
}

[ProtoContract]
public sealed class FlightStatePacket
{
    [ProtoMember(1)]
    public bool Granted { get; set; }

    [ProtoMember(2)]
    public uint Sequence { get; set; }

    [ProtoMember(3)]
    public string? Reason { get; set; }
}

[ProtoContract]
public sealed class BracerAbilityPacket
{
    [ProtoMember(1)]
    public int Ability { get; set; }
}

public enum BracerAbility
{
    Dash,
    Step,
    Sense,
    Recall
}
