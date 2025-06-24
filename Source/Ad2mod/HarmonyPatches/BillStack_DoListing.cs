using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(BillStack), nameof(BillStack.DoListing))]
public class BillStack_DoListing
{
    public static bool InDoListing;
    public static BillStack LastBillStack;

    public static void Prefix(ref Func<List<FloatMenuOption>> recipeOptionsMaker, BillStack __instance)
    {
        if (__instance.billGiver is Pawn)
        {
            return;
        }

        InDoListing = true;
        LastBillStack = __instance;
        if (!Ad2Mod.Settings.UseRightClickMenu)
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
        InDoListing = false;
    }
}