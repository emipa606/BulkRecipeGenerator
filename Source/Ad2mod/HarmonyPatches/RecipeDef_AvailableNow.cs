using HarmonyLib;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.AvailableNow), MethodType.Getter)]
public static class RecipeDef_AvailableNow
{
    public static bool Postfix(bool __result, RecipeDef __instance)
    {
        if (__instance.IsSurgery || Ad2Mod.Settings.UseRightClickMenu)
        {
            return __result;
        }

        if (!__result)
        {
            return false;
        }

        var srcRecipe = Ad2.GetSrcRecipe(__instance);
        if (srcRecipe == null)
        {
            return true;
        }

        return !Ad2Mod.Settings.LimitToX5 || __instance == Ad2.GetNewRecipesList(srcRecipe)[0];
    }
}