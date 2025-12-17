using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DanaTweaks;

public class Hotkeys : ModSystem
{
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Input.RegisterHotKey(OpenCloseLidHotkey, OpenCloseLidName, GlKeys.O, HotkeyType.GUIOrOtherControls, ctrlPressed: true);
        api.Input.SetHotKeyHandler(OpenCloseLidHotkey, _ => OpenCloseLid(api));

        api.Input.RegisterHotKey(RemoveOrAddLabelHotkey, RemoveOrAddLabelName, GlKeys.L, HotkeyType.GUIOrOtherControls, ctrlPressed: true);
        api.Input.SetHotKeyHandler(RemoveOrAddLabelHotkey, _ => RemoveOrAddLabel(api));
    }

    public static bool OpenCloseLid(ICoreClientAPI api)
    {
        if (!api.IsCrate()) return false;
        api.Network.GetChannel("danatweaks").SendPacket(new ClientRequest_OpenCloseCrate( ));
        return true;
    }

    public static bool RemoveOrAddLabel(ICoreClientAPI api)
    {
        if (!api.IsCrate()) return false;
        api.Network.GetChannel("danatweaks").SendPacket(new ClientRequest_AttachRemoveCrateLabel( ));
        return true;
    }
}

[ProtoContract]
public class ClientRequest_OpenCloseCrate;

[ProtoContract]
public class ClientRequest_AttachRemoveCrateLabel;

[ProtoContract]
public class ServerResponse_TriggerError
{
    [ProtoMember(1)]
    public string errorMessage;
}

public class TweaksNetwork : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.Network
            .RegisterChannel("danatweaks")
            .RegisterMessageType(typeof(ClientRequest_OpenCloseCrate))
            .RegisterMessageType(typeof(ClientRequest_AttachRemoveCrateLabel))
            .RegisterMessageType(typeof(ServerResponse_TriggerError));
    }

    #region Client

    IClientNetworkChannel clientChannel;
    ICoreClientAPI clientApi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        clientChannel = api.Network.GetChannel("danatweaks")
        .SetMessageHandler<ServerResponse_TriggerError>(OnServerResponse_TriggerError);
    }

    private void OnServerResponse_TriggerError(ServerResponse_TriggerError packet)
    {
        if (!string.IsNullOrEmpty(packet.errorMessage))
        {
            clientApi.TriggerIngameError(this, "", packet.errorMessage);
        }
    }

    #endregion

    #region Server

    IServerNetworkChannel serverChannel;
    ICoreServerAPI serverApi;

    public override void StartServerSide(ICoreServerAPI api)
    {
        serverApi = api;
        serverChannel = api.Network
            .GetChannel("danatweaks")
            .SetMessageHandler<ClientRequest_OpenCloseCrate>(OnClientRequest_OpenCloseCrate)
            .SetMessageHandler<ClientRequest_AttachRemoveCrateLabel>(OnClientRequest_AttachRemoveCrateLabel);
    }

    private void OnClientRequest_OpenCloseCrate(IServerPlayer fromPlayer, ClientRequest_OpenCloseCrate packet)
    {
        if (!Extensions.IsCrate(fromPlayer.CurrentBlockSelection, serverApi))
        {
            serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = NoCrate }, fromPlayer);
            return;
        }

        BlockPos pos = fromPlayer?.CurrentBlockSelection?.Position;

        if (pos == null || fromPlayer.Entity.World.BlockAccessor.GetBlockEntityExt<BlockEntityCrate>(pos) is not BlockEntityCrate becrate)
        {
            serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = NoCrate }, fromPlayer);
            return;
        }

        if (!fromPlayer.Entity.Api.World.Claims.TryAccess(fromPlayer, pos, EnumBlockAccessFlags.Use))
        {
            return;
        }

        becrate.preferredLidState = becrate.preferredLidState switch
        {
            "opened" => "closed",
            "closed" => "opened",
            _ => "opened"
        };

        becrate.MarkDirty(redrawOnClient: true);
    }

    private void OnClientRequest_AttachRemoveCrateLabel(IServerPlayer fromPlayer, ClientRequest_AttachRemoveCrateLabel packet)
    {
        if (!Extensions.IsCrate(fromPlayer.CurrentBlockSelection, serverApi))
        {
            serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = NoLabel }, fromPlayer);
            return;
        }

        BlockPos pos = fromPlayer?.CurrentBlockSelection?.Position;

        if (pos == null)
        {
            serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = NoCrate }, fromPlayer);
            return;
        }

        ItemSlot activeSlot = fromPlayer.InventoryManager.ActiveHotbarSlot;

        BlockEntityCrate becrate = fromPlayer.Entity.World.BlockAccessor.GetBlockEntityExt<BlockEntityCrate>(pos);

        if (becrate == null)
        {
            serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = NoCrate }, fromPlayer);
            return;
        }

        if (!fromPlayer.Entity.Api.World.Claims.TryAccess(fromPlayer, pos, EnumBlockAccessFlags.Use))
        {
            return;
        }
        
        ItemStack LabelStack = new(serverApi.World.GetItem(new AssetLocation(ParchmentCode)));

        if (becrate.label == DefaultLabel)
        {
            if (!fromPlayer.InventoryManager.TryGiveItemstack(LabelStack.Clone(), true))
            {
                becrate.Api.World.SpawnItemEntity(LabelStack.Clone(), fromPlayer.Entity.ServerPos.XYZ);
            }

            becrate.label = null;
            becrate.MarkDirty(redrawOnClient: true);
            return;
        }
        
        if (becrate.Labelled)
        {
            serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = HasDiffLabel }, fromPlayer);
            return;
        }

        if (activeSlot.IsCorrectLabel(LabelStack))
        {
            fromPlayer.Entity.ActiveHandItemSlot.TakeOut(1);
            fromPlayer.Entity.ActiveHandItemSlot.MarkDirty();

            becrate.label = DefaultLabel;
            becrate.MarkDirty(redrawOnClient: true);
            return;
        }

        serverChannel.SendPacket(new ServerResponse_TriggerError() { errorMessage = NoLabel }, fromPlayer);
    }

    #endregion
}