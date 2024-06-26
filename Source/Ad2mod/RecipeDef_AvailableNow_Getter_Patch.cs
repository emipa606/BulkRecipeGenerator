﻿using HarmonyLib;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.AvailableNow), MethodType.Getter)]
public class RecipeDef_AvailableNow_Getter_Patch
{
    public static bool Postfix(bool __result, RecipeDef __instance)
    {
        if (__instance.IsSurgery)
        {
            return __result;
        }

        if (Ad2Mod.settings.useRightClickMenu)
        {
            return __result;
        }

        if (__result == false)
        {
            return false;
        }

        var srcRecipe = Ad2.GetSrcRecipe(__instance);
        if (srcRecipe == null)
        {
            return true;
        }

        return !Ad2Mod.settings.limitToX5 || __instance == Ad2.GetNewRecipesList(srcRecipe)[0];
        //if (__instance.workAmount > 1.5 * Ad2Mod.settings.defaultThreshold * 60)
        //{
        //    //Log.Message(__instance.label + " hidden with src workAmount " + __instance.WorkAmountTotal(null)/60);
        //    return false;
        //}
    }
}