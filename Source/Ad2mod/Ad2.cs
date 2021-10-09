using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ad2mod
{
    [StaticConstructorOnStartup]
    public class Ad2
    {
        private const int thresholdLimit = 120;
        private static readonly int[] mulFactors = { 5, 10, 25, 50 };

        //  old:new
        private static readonly Dictionary<RecipeDef, List<RecipeDef>> dictON =
            new Dictionary<RecipeDef, List<RecipeDef>>();

        //  new:old
        private static readonly Dictionary<RecipeDef, RecipeDef> dictNO = new Dictionary<RecipeDef, RecipeDef>();

        //  recipe.LabelCap : RecipeDef
        private static readonly Dictionary<string, RecipeDef> dictLR = new Dictionary<string, RecipeDef>();

        static Ad2()
        {
            var harmony = new Harmony("com.local.anon.ad2");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //RecipeIconsCompatibility(harmony);
            GenRecipes();
        }

        private static string TransformRecipeLabel(string s)
        {
            if (s.NullOrEmpty())
            {
                s = "(missing label)";
            }

            return s.TrimEnd();
        }

        private static void RememberRecipeLabel(RecipeDef r)
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

        private static void RememberNewRecipe(RecipeDef src, RecipeDef n)
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
            dictNO.TryGetValue(recipe, out var res);
            return res;
        }

        public static List<RecipeDef> GetNewRecipesList(RecipeDef recipe)
        {
            dictON.TryGetValue(recipe, out var res);
            return res;
        }

        public static RecipeDef GetRecipeByLabel(string label)
        {
            dictLR.TryGetValue(label, out var res);
            return res;
        }

        public static List<Bill> FindRecipesUses()
        {
            if (Find.Maps == null)
            {
                throw new Exception("Find.Maps == null");
            }

            var res = new List<Bill>();
            foreach (var map in Find.Maps)
            {
                foreach (var wt in map.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>())
                {
                    foreach (var bill in wt.BillStack)
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

        private static void RecipeIconsCompatibility(Harmony harmony)
        {
            try
            {
                ((Action)(() =>
                {
                    if (!LoadedModManager.RunningModsListForReading.Any(x => x.Name == "Recipe icons"))
                    {
                        return;
                    }

                    var methodForPatch = AccessTools.Method("RecipeIcons.RecipeTooltip:DoGUI");
                    if (methodForPatch == null)
                    {
                        Log.Warning(
                            "BulkRecipeGenerator: LoadedModManager said that RecipeIcons enabled, but methodForPatch == null," +
                            " so RecipeIcons patch will not be applied.");
                        return;
                    }

                    harmony.Patch(methodForPatch,
                        postfix: new HarmonyMethod(typeof(FloatMenuOption_DoGUI_Patch), "Postfix"));
                }))();
            }
            catch (TypeLoadException)
            {
            }
        }

        private static RecipeDef MkNewRecipe(RecipeDef rd, int factor)
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
            if (!Ad2Mod.settings.useSameQualityForAll && rd.products[0].thingDef.HasComp(typeof(CompQuality)))
            {
                for (var index = 0; index < factor; index++)
                {
                    r.products.Add(new ThingDefCountClass(rd.products[0].thingDef, rd.products[0].count));
                }
            }
            else
            {
                r.products.Add(new ThingDefCountClass(rd.products[0].thingDef, rd.products[0].count * factor));
            }

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

            var IVGClass = (Type)Traverse.Create(rd).Field("ingredientValueGetterClass").GetValue();
            Traverse.Create(r).Field("ingredientValueGetterClass").SetValue(IVGClass);

            //if (rd.unfinishedThingDef != null)
            //    Log.Message(rd.label + " uses unfinishedThingDef " + rd.unfinishedThingDef.label+"  an it is removed");
            return r;
        }

        private static void GenRecipes()
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
                        Log.Warning(
                            $"BulkRecipeGenerator: newRecipe == null  on {recipe.label}, this should not happen normally");
                        continue;
                    }


                    newRecipe.ResolveReferences();

                    DefDatabase<RecipeDef>.Add(newRecipe);

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

                    if (Ad2Mod.settings.limitToX5)
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