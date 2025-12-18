using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DanaTweaks;

public class BlockBehaviorOpenConnectedTrapdoors(Block block) : BlockBehavior(block)
{
    public override bool ClientSideOptional => true;

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        if (byPlayer == null || blockSel == null) return false;

        BlockPos startPos = blockSel.Position;
        BEBehaviorTrapDoor startDoor = GetTrapdoorAtPos(world, startPos);
        if (startDoor == null) return false;

        bool targetOpenState = !startDoor.Opened;
        List<BlockPos> positions = FloodFillTrapdoors(startDoor);

        foreach (BlockPos pos in positions)
        {
            BEBehaviorTrapDoor neighbor = GetTrapdoorAtPos(world, pos);
            if (neighbor == null) continue;

            if (neighbor.Opened == targetOpenState)
            {
                Toggle(world, byPlayer, pos, blockSel.Face, neighbor, open: !targetOpenState);
            }
        }
        return true;
    }

    private static List<BlockPos> FloodFillTrapdoors(BEBehaviorTrapDoor startDoor)
    {
        BlockPos startPos = startDoor.Pos.Copy();
        BlockFacing attachedFace = BlockFacing.ALLFACES[startDoor.AttachedFace];

        List<BlockPos> foundPositions = [];
        List<BlockPos> todo = [];
        HashSet<BlockPos> visited = [];

        foreach (BlockFacing face in BlockFacing.ALLFACES)
        {
            todo.Add(startPos.AddCopy(face));
        }

        visited.Add(startPos.Copy());

        while (todo.Count > 0)
        {
            BlockPos cur = todo[todo.Count - 1];
            todo.RemoveAt(todo.Count - 1);

            if (visited.Contains(cur)) continue;
            visited.Add(cur.Copy());

            if (cur.ManhattenDistance(startPos) > Core.ConfigServer.OpenConnectedTrapdoorsMaxBlocksDistance) continue;

            BEBehaviorTrapDoor curDoor = GetTrapdoorAtPos(startDoor.Api.World, cur);
            if (curDoor == null) continue;

            if (startDoor.facingWhenClosed != curDoor.facingWhenClosed) continue;

            BlockFacing curAttachedFace = BlockFacing.ALLFACES[curDoor.AttachedFace];
            bool isAttachedToVerticalFace = attachedFace.IsVertical && attachedFace == curAttachedFace;
            bool isAttachedToHorizontalFace = attachedFace.IsHorizontal && (attachedFace == curAttachedFace.Opposite || attachedFace == curAttachedFace);

            if (!isAttachedToVerticalFace && !isAttachedToHorizontalFace) continue;

            foundPositions.Add(cur.Copy());

            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                BlockPos next = cur.AddCopy(face);
                if (!visited.Contains(next))
                {
                    todo.Add(next);
                }
            }
        }
        return foundPositions;
    }

    private static BEBehaviorTrapDoor GetTrapdoorAtPos(IWorldAccessor world, BlockPos pos)
    {
        return world.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorTrapDoor>();
    }

    private static void Toggle(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, BlockFacing facing, BEBehaviorTrapDoor trapdoor, bool open)
    {
        Caller caller = new Caller() { Player = byPlayer };
        BlockSelection _blockSel = new BlockSelection(pos, facing, trapdoor.Block);
        TreeAttribute tree = new TreeAttribute();
        tree.SetBool("opened", open);
        trapdoor.Block.Activate(world, caller, _blockSel, tree);
    }
}