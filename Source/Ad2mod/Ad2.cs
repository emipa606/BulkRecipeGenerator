﻿using HarmonyLib;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;
using System;
using System.Collections.Generic;


namespace Ad2mod
{
    class Util
    {
        public static int Clamp(int val, int a, int b)
        {
            return (val < a) ? a : ((val > b) ? b : val);
        }
    }


    public class Ad2Settings : ModSettings
    {
        public int defaultThreshold = 60;
        public bool limitToX5 = false;
        public bool useRightClickMenu = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultThreshold, "defaultThreshold", 60);
            Scribe_Values.Look(ref limitToX5, "limitToX5", false);
            Scribe_Values.Look(ref useRightClickMenu, "useRightClickMenu", true);
            base.ExposeData();
        }
    }

    class Ad2Mod : Mod
    {
        public static Ad2Settings settings;
        public static Ad2Mod instance;
        readonly NumField defaultThresholdField = new NumField();
        readonly NumField thresholdField = new NumField();
        //Game lastGame;

        class NumField
        {
            string buffer;
            readonly int min, max;

            public NumField(int min = 0, int max = 120)
            {
                this.min = min;
                this.max = max;
            }
            public void Reset()
            {
                buffer = null;
            }
            public bool DoField(float y, string label, ref int val)
            {
                var LH = Text.LineHeight;
                if (buffer == null)
                {
                    buffer = val.ToString();
                }

                float x = 0;
                TextAnchor anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(new Rect(x, y, 200, LH), label);
                Text.Anchor = anchor;
                x += 200;
                buffer = Widgets.TextField(new Rect(x, y, 60, LH), buffer);
                x += 60;
                if (Widgets.ButtonText(new Rect(x, y, 100, LH), "Apply"))
                {
                    if (int.TryParse(buffer, out var resInt))
                    {
                        val = Util.Clamp(resInt, min, max);
                        buffer = val.ToString();
                        return true;
                    }
                    buffer = val.ToString();
                }
                return false;
            }
        }

        public Ad2Mod(ModContentPack content) : base(content)
        {
            instance = this;
            settings = GetSettings<Ad2Settings>();
        }

        public override string SettingsCategory()
        {
            return "Bulk recipe generator";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var x = inRect.x;
            var y = inRect.y;
            var LH = Text.LineHeight;
            y += LH;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "Global settings");
            y += Text.LineHeight + 2;
            Text.Font = GameFont.Small;
            if (defaultThresholdField.DoField(y, "Default target time", ref settings.defaultThreshold))
            {
                Messages.Message("Default target time changed to " + settings.defaultThreshold, MessageTypeDefOf.NeutralEvent);
            }

            TooltipHandler.TipRegion(new Rect(x, y, 360, LH), "Has no effect if 'Put recipes in context menu' is checked");

            y += LH;
            var r1 = new Rect(x, y, 360, LH);
            Widgets.CheckboxLabeled(r1, "Limit to x5 recipes", ref settings.limitToX5);
            TooltipHandler.TipRegion(r1, "Add only x5 recipes");
            y += LH;
            var r2 = new Rect(x, y, 360, LH);
            Widgets.CheckboxLabeled(r2, "Put recipes in context menu", ref settings.useRightClickMenu);
            TooltipHandler.TipRegion(r2, "Put recipes to context menu of source recipe instead of adding after it in the same list.");

            y += (2 * LH) + 2;

            if (Current.Game == null)
            {
                //lastGame = null;
                return;
            }
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "World settings");
            y += Text.LineHeight + 2;
            Text.Font = GameFont.Small;

            List<Bill> bills = Ad2.FindRecipesUses();
            var s = $"Remove modded recipes from save ({bills.Count} found)";
            var w = Text.CalcSize(s).x + 64;
            if (Widgets.ButtonText(new Rect(x, y, w, 32), s))
            {
                foreach (Bill bill in bills)
                {
                    bill.billStack.Delete(bill);
                }

                Messages.Message(bills.Count.ToString() + " bills removed", MessageTypeDefOf.NeutralEvent);
            }
        }
    }


    [StaticConstructorOnStartup]
    public class Ad2
    {
        const int thresholdLimit = 120;
        static readonly int[] mulFactors = { 5, 10, 25, 50 };

        //  old:new
        static readonly Dictionary<RecipeDef, List<RecipeDef>> dictON = new Dictionary<RecipeDef, List<RecipeDef>>();
        //  new:old
        static readonly Dictionary<RecipeDef, RecipeDef> dictNO = new Dictionary<RecipeDef, RecipeDef>();
        //  recipe.LabelCap : RecipeDef
        static readonly Dictionary<string, RecipeDef> dictLR = new Dictionary<string, RecipeDef>();

        static string TransformRecipeLabel(string s)
        {
            if (s.NullOrEmpty())
            {
                s = "(missing label)";
            }
            return s.TrimEnd(new char[0]);
        }

        static void RememberRecipeLabel(RecipeDef r)
        {
            var label = TransformRecipeLabel(r.LabelCap);
            if (!dictLR.ContainsKey(label))
            {
                dictLR.Add(label, r);
            }
            else if (dictLR[label] != r)
            {
                Log.Warning($"Ambiguous recipe label: {label}. Right click menu will be disabled for this one.");
                dictLR[label] = null;
            }
        }
        static void RememberNewRecipe(RecipeDef src, RecipeDef n)
        {
            if (!dictON.ContainsKey(src))
            {
                dictON.Add(src, new List<RecipeDef>());
            }

            dictON[src].Add(n);

            if (dictNO.ContainsKey(n))
            {
                Log.Error($"BulkRecipeGenerator: dictNO already contains {n.defName} ({n.label})");
                return;
            }
            dictNO.Add(n, src);

            RememberRecipeLabel(src);
            RememberRecipeLabel(n);
        }

        public static bool IsSrcRecipe(RecipeDef recipe)
        {
            return dictON.ContainsKey(recipe);
        }

        public static bool IsNewRecipe(RecipeDef recipe)
        {
            return dictNO.ContainsKey(recipe);
        }

        public static RecipeDef GetSrcRecipe(RecipeDef recipe)
        {
            dictNO.TryGetValue(recipe, out RecipeDef res);
            return res;
        }
        public static List<RecipeDef> GetNewRecipesList(RecipeDef recipe)
        {
            dictON.TryGetValue(recipe, out List<RecipeDef> res);
            return res;
        }
        public static RecipeDef GetRecipeByLabel(string label)
        {
            dictLR.TryGetValue(label, out RecipeDef res);
            return res;
        }

        public static List<Bill> FindRecipesUses()
        {
            if (Find.Maps == null)
            {
                throw new Exception("Find.Maps == null");
            }

            var res = new List<Bill>();
            foreach (Map map in Find.Maps)
            {
                foreach (Building_WorkTable wt in map.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>())
                {
                    foreach (Bill bill in wt.BillStack)
                    {
                        if (IsNewRecipe(bill.recipe))
                        {
                            res.Add(bill);
                        }
                    }
                }
            }
            return res;
        }

        static void RecipeIconsCompatibility(Harmony harmony)
        {
            try
            {
                ((Action)(() =>
                {
                    if (LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Recipe icons"))
                    {
                        MethodInfo methodForPatch = AccessTools.Method("RecipeIcons.FloatMenuOptionLeft:DoGUI");
                        if (methodForPatch == null)
                        {
                            Log.Warning("BulkRecipeGenerator: LoadedModManager said that RecipeIcons enabled, but methodForPatch == null," +
                                " so RecipeIcons patch will not be applied.");
                            return;
                        }
                        harmony.Patch(methodForPatch, postfix: new HarmonyMethod(typeof(FloatMenuOption_DoGUI_Patch), "Postfix"));
                    }
                }))();
            }
            catch (TypeLoadException) { }
        }

        static Ad2()
        {
            var harmony = new Harmony("com.local.anon.ad2");
            if (harmony == null)
            {
                Log.Error("BulkRecipeGenerator: harmony == null, mod will not work");
                return;
            }
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            RecipeIconsCompatibility(harmony);
            GenRecipes();
        }

        static RecipeDef MkNewRecipe(RecipeDef rd, int factor)
        {
            if (rd.ingredients.Count == 0 || rd.products.Count != 1)
            {
                return null;
            }

            var r = new RecipeDef
            {
                defName = rd.defName + $"_{factor}x",
                label = rd.label + $" x{factor}",
                description = rd.description + $" (x{factor})",
                jobString = rd.jobString,
                modContentPack = rd.modContentPack,
                workSpeedStat = rd.workSpeedStat,
                efficiencyStat = rd.efficiencyStat,
                fixedIngredientFilter = rd.fixedIngredientFilter,
                productHasIngredientStuff = rd.productHasIngredientStuff,
                workSkill = rd.workSkill,
                workSkillLearnFactor = rd.workSkillLearnFactor,
                skillRequirements = rd.skillRequirements.ListFullCopyOrNull(),
                recipeUsers = rd.recipeUsers.ListFullCopyOrNull(),
                unfinishedThingDef = null, //rd.unfinishedThingDef,
                effectWorking = rd.effectWorking,
                soundWorking = rd.soundWorking,
                allowMixingIngredients = rd.allowMixingIngredients,
                defaultIngredientFilter = rd.defaultIngredientFilter,
                researchPrerequisite = rd.researchPrerequisite,
                factionPrerequisiteTags = rd.factionPrerequisiteTags
            };
            r.products.Add(new ThingDefCountClass(rd.products[0].thingDef, rd.products[0].count * factor));
            var new_ingredients = new List<IngredientCount>();
            foreach (var oic in rd.ingredients)
            {
                var nic = new IngredientCount();
                nic.SetBaseCount(oic.GetBaseCount() * factor);
                nic.filter = oic.filter;
                new_ingredients.Add(nic);
            }
            r.ingredients = new_ingredients;
            r.workAmount = rd.WorkAmountTotal(null) * factor;

            Type IVGClass;
            IVGClass = (Type)Traverse.Create(rd).Field("ingredientValueGetterClass").GetValue();
            Traverse.Create(r).Field("ingredientValueGetterClass").SetValue(IVGClass);

            //if (rd.unfinishedThingDef != null)
            //    Log.Message(rd.label + " uses unfinishedThingDef " + rd.unfinishedThingDef.label+"  an it is removed");
            return r;
        }

        static void GenRecipes()
        {
            var allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading;
            var srcs = new List<RecipeDef>();
            foreach (var recipe in allRecipes)
            {
                if (recipe.products.Count != 1)
                {
                    continue;
                }

                if (recipe.ingredients.Count == 0)
                {
                    continue;
                }

                if (recipe.WorkAmountTotal(null) > thresholdLimit * 60)
                {
                    continue;
                }

                srcs.Add(recipe);
                //Log.Message(recipe.label + "\t" + recipe.defName + "\t" + recipe.WorkAmountTotal(null)/60);
            }
            var RecipesUsers = new List<ThingDef>();
            foreach (var recipe in srcs)
            {
                var lastOne = false;
                foreach (var factor in mulFactors)
                {
                    if (factor * recipe.WorkAmountTotal(null) > thresholdLimit * 60)
                    {
                        lastOne = true;
                    }

                    var newRecipe = MkNewRecipe(recipe, factor);
                    if (newRecipe == null)
                    {
                        Log.Warning($"BulkRecipeGenerator: newRecipe == null  on {recipe.label}, this should not happen normally");
                        continue;
                    }
                    newRecipe.ResolveReferences();
                    DefDatabase<RecipeDef>.Add(def: newRecipe);

                    RememberNewRecipe(recipe, newRecipe);

                    foreach (var ru in recipe.AllRecipeUsers)
                    {
                        if (!RecipesUsers.Contains(ru))
                        {
                            RecipesUsers.Add(ru);
                        }

                        if (newRecipe.recipeUsers == null)
                        {
                            ru.recipes.Add(newRecipe);
                        }
                    }
                    if (lastOne)
                    {
                        break;
                    }
                }
            }
            foreach (var ru in RecipesUsers)
            {
                Traverse.Create(ru).Field("allRecipesCached").SetValue(null);
            }
        }
    }

}
