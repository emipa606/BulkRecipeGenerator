using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(BillStack))]
[HarmonyPatch("DoListing")]
public class BillStack_DoListing_Patch
{
    public static bool inDoListing;
    public static BillStack lastBillStack;

    public static void Prefix(ref Func<List<FloatMenuOption>> recipeOptionsMaker, BillStack __instance)
    {
        if (__instance.billGiver is Pawn)
        {
            return;
        }

        inDoListing = true;
        lastBillStack = __instance;
        if (!Ad2Mod.settings.useRightClickMenu)
        {
            return;
        }

        var list = recipeOptionsMaker();
        recipeOptionsMaker = delegate
        {
            var newList = new List<FloatMenuOption>();
            foreach (var opt in list)
            {
                var recipe = Ad2.GetRecipeByLabel(opt.Label);
                if (recipe == null || !Ad2.IsNewRecipe(recipe))
                {
                    newList.Add(opt);
                }
            }

            return newList;
        };
    }

    public static void Postfix()
    {
        inDoListing = false;
    }
}