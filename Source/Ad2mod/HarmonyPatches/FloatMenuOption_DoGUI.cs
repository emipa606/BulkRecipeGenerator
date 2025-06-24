using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(FloatMenuOption), nameof(FloatMenuOption.DoGUI))]
public class FloatMenuOption_DoGUI
{
    private static List<FloatMenuOption> recipeOptionsMaker(List<RecipeDef> recipesList)
    {
        var list = new List<FloatMenuOption>();
        if (BillStack_DoListing.LastBillStack.billGiver is not Building_WorkTable table)
        {
            list.Add(new FloatMenuOption("table == null", delegate { }));
            return list;
        }

        foreach (var recipe in recipesList)
        {
            if (!recipe.AvailableNow)
            {
                continue;
            }

            list.Add(new FloatMenuOption(recipe.LabelCap, delegate
            {
                if (!table.Map.mapPawns.FreeColonists.Any(col => recipe.PawnSatisfiesSkillRequirements(col)))
                {
                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                }

                var bill2 = recipe.MakeNewBill();
                table.billStack.AddBill(bill2);
            }));
        }

        return list;
    }

    public static void Postfix(ref bool __result, FloatMenuOption __instance)
    {
        if (!Ad2Mod.Settings.UseRightClickMenu || !FloatMenu_Constructor.IsTracked(__instance))
        {
            return;
        }

        if (!__result || Event.current.button != 1)
        {
            return;
        }

        __result = false;
        var recipe = Ad2.GetRecipeByLabel(__instance.Label);
        if (recipe == null || !Ad2.IsSrcRecipe(recipe))
        {
            return;
        }

        var newList = Ad2.GetNewRecipesList(recipe);
        if (newList == null)
        {
            return;
        }

        Find.WindowStack.Add(new FloatMenu(recipeOptionsMaker(newList)));
    }
}