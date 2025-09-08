using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.ServerMods;

namespace DanaTweaks;

public class Recipes : ModSystem
{
    public GridRecipeLoader GridRecipeLoader { get; set; }

    public override double ExecuteOrder() => 1.1;

    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Server)
        {
            return;
        }

        GridRecipeLoader = api.ModLoader.GetModSystem<GridRecipeLoader>();

        foreach (GridRecipe recipe in api.World.GridRecipes)
        {
            if (Core.ConfigServer.FourPlanksFromLog)
            {
                if (recipe.Output.ResolvedItemstack != null && recipe.HasLogAsIngredient() && recipe.Output.IsPlank())
                {
                    recipe.Output.ResolvedItemstack.StackSize = 4;
                }
            }
        }
    }
}