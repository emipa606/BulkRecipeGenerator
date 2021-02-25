using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ad2mod
{
    [HarmonyPatch(typeof(ThingDef))]
    [HarmonyPatch("AllRecipes", MethodType.Getter)]
    public class ThingDef_AllRecipes_Getter_Patch
    {
        public static void Prefix(ref bool __state, List<RecipeDef> ___allRecipesCached)
        {
            __state = ___allRecipesCached == null;
        }

        public static List<RecipeDef> Postfix(List<RecipeDef> __result, ref List<RecipeDef> ___allRecipesCached,
            bool __state)
        {
            if (!__state)
            {
                return __result;
            }

            var res = new List<RecipeDef>();
            foreach (var r in __result)
            {
                if (Ad2.IsNewRecipe(r))
                {
                    continue;
                }

                res.Add(r);
                var newRList = Ad2.GetNewRecipesList(r);
                if (newRList == null)
                {
                    continue;
                }

                foreach (var nr in newRList)
                {
                    res.Add(nr);
                }
            }

            ___allRecipesCached = res;
            //Log.Message("___allRecipesCached = res");

            return res;
        }
    }


    [HarmonyPatch(typeof(RecipeDef))]
    [HarmonyPatch("AvailableNow", MethodType.Getter)]
    public class RecipeDef_AvailableNow_Getter_Patch
    {
        public static bool Postfix(bool __result, RecipeDef __instance)
        {
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

            if (Ad2Mod.settings.limitToX5 && __instance != Ad2.GetNewRecipesList(srcRecipe)[0])
            {
                return false;
            }

            if (__instance.workAmount > 1.5 * Ad2Mod.settings.defaultThreshold * 60)
            {
                //Log.Message(__instance.label + " hidden with src workAmount " + __instance.WorkAmountTotal(null)/60);
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(BillStack))]
    [HarmonyPatch("DoListing")]
    public class BillStack_DoListing_Patch
    {
        public static bool inDoListing;
        public static BillStack lastBillStack;

        public static void Prefix(ref Func<List<FloatMenuOption>> recipeOptionsMaker, BillStack __instance)
        {
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


    [HarmonyPatch(typeof(FloatMenu), MethodType.Constructor)]
    [HarmonyPatch(new[] {typeof(List<FloatMenuOption>)})]
    internal class PatchFloatMenu
    {
        private static readonly List<FloatMenu> trackedFM = new List<FloatMenu>();
        private static readonly List<FloatMenuOption> trackedFMO = new List<FloatMenuOption>();

        public static bool IsTracked(FloatMenuOption fmo)
        {
            return trackedFMO.Contains(fmo);
        }

        public static bool IsTracked(FloatMenu fm)
        {
            return trackedFM.Contains(fm);
        }

        public static void Untrack(FloatMenuOption fmo)
        {
            trackedFMO.Remove(fmo);
        }

        public static void Untrack(FloatMenu fm)
        {
            trackedFM.Remove(fm);
        }

        private static void Postfix(FloatMenu __instance, List<FloatMenuOption> ___options)
        {
            if (!BillStack_DoListing_Patch.inDoListing)
            {
                return;
            }

            trackedFM.Add(__instance);
            foreach (var opt in ___options)
            {
                trackedFMO.Add(opt);
            }
        }
    }


    [HarmonyPatch(typeof(WindowStack))]
    [HarmonyPatch("TryRemove", typeof(Window), typeof(bool))]
    public class WindowStack_TryRemove_Patch
    {
        public static void Postfix(bool __result, Window window)
        {
            if (!(window is FloatMenu fm) || __result == false)
            {
                return;
            }

            if (!PatchFloatMenu.IsTracked(fm))
            {
                return;
            }

            var fmos = (List<FloatMenuOption>) Traverse.Create(fm).Field("options").GetValue();
            PatchFloatMenu.Untrack(fm);
            foreach (var opt in fmos)
            {
                PatchFloatMenu.Untrack(opt);
            }

            //Log.Message($"WindowStack.TryRemove {PatchFloatMenu.trackedFM.Count}  {PatchFloatMenu.trackedFMO.Count}");
        }
    }


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


    [HarmonyPatch(typeof(FloatMenuOption))]
    [HarmonyPatch("Chosen")]
    public class FloatMenuOption_Chosen_Patch
    {
        public static bool Prefix(FloatMenuOption __instance)
        {
            if (Ad2Mod.settings.useRightClickMenu && Event.current.button == 1 && PatchFloatMenu.IsTracked(__instance))
            {
                return false;
            }

            return true;
        }
    }
}