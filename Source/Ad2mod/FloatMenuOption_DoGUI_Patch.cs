using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ad2mod
{
    [HarmonyPatch(typeof(FloatMenuOption))]
    [HarmonyPatch("DoGUI")]
    public class FloatMenuOption_DoGUI_Patch
    {
        private static List<FloatMenuOption> RecipeOptionsMaker(List<RecipeDef> recipesList)
        {
            var list = new List<FloatMenuOption>();
            if (!(BillStack_DoListing_Patch.lastBillStack.billGiver is Building_WorkTable table))
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
            if (!Ad2Mod.settings.useRightClickMenu || !PatchFloatMenu.IsTracked(__instance))
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

            var nlst = Ad2.GetNewRecipesList(recipe);
            if (nlst == null)
            {
                return;
            }

            Find.WindowStack.Add(new FloatMenu(RecipeOptionsMaker(nlst)));
        }
    }
}