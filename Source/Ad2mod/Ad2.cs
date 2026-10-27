using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Ad2mod;

[StaticConstructorOnStartup]
public static class Ad2
{
    private static readonly int[] mulFactors = [5, 10, 25, 50];

    //  old:new
    private static readonly Dictionary<RecipeDef, List<RecipeDef>> dictOn = new();

    //  new:old
    private static readonly Dictionary<RecipeDef, RecipeDef> dictNo = new();

    //  recipe.LabelCap : RecipeDef
    private static readonly Dictionary<string, RecipeDef> dictLr = new();

    private static readonly ThingDef GeneratedUnfinishedBulkItems;

    static Ad2()
    {
        GeneratedUnfinishedBulkItems = ThingDef.Named("GeneratedUnfinishedBulkItems");
        new Harmony("com.local.anon.ad2").PatchAll(Assembly.GetExecutingAssembly());
        genRecipes();
    }

    private static string transformRecipeLabel(string s)
    {
        if (s.NullOrEmpty())
        {
            s = "(missing label)";
        }

        return s.TrimEnd();
    }

    private static void logMessage(string message, bool forced = false, bool warning = false)
    {
        if (!forced && !Ad2Mod.Settings.VerboseLogging)
        {
            return;
        }

        if (warning)
        {
            Log.Warning($"[BulkRecipeGenerator]: {message}");
            return;
        }

        Log.Message($"[BulkRecipeGenerator]: {message}");
    }

    private static void rememberRecipeLabel(RecipeDef r)
    {
        var label = transformRecipeLabel(r.LabelCap);
        if (dictLr.TryAdd(label, r))
        {
            return;
        }

        if (dictLr[label] == r)
        {
            return;
        }

        logMessage($"Ambiguous recipe label: {label}. Right click menu will be disabled for this one.", false,
            true);
        dictLr[label] = null;
    }

    private static void rememberNewRecipe(RecipeDef src, RecipeDef n)
    {
        if (!dictOn.ContainsKey(src))
        {
            dictOn.Add(src, []);
        }

        dictOn[src].Add(n);

        if (!dictNo.TryAdd(n, src))
        {
            Log.Error($"BulkRecipeGenerator: dictNO already contains {n.defName} ({n.label})");
            return;
        }

        rememberRecipeLabel(src);
        rememberRecipeLabel(n);
    }

    public static bool IsSrcRecipe(RecipeDef recipe)
    {
        return dictOn.ContainsKey(recipe);
    }

    public static bool IsNewRecipe(RecipeDef recipe)
    {
        return dictNo.ContainsKey(recipe);
    }

    public static RecipeDef GetSrcRecipe(RecipeDef recipe)
    {
        dictNo.TryGetValue(recipe, out var res);
        return res;
    }

    public static List<RecipeDef> GetNewRecipesList(RecipeDef recipe)
    {
        dictOn.TryGetValue(recipe, out var res);
        return res;
    }

    public static RecipeDef GetRecipeByLabel(string label)
    {
        dictLr.TryGetValue(label, out var res);
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

    private static RecipeDef mkNewRecipe(RecipeDef rd, int factor)
    {
        if (rd.ingredients.Count == 0 || rd.products.Count != 1)
        {
            return null;
        }

        var r = new RecipeDef
        {
            defName = $"{rd.defName}_{factor}x",
            label = $"{rd.label} x{factor}",
            description = $"{rd.description} (x{factor})",
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
            unfinishedThingDef = GeneratedUnfinishedBulkItems, //rd.unfinishedThingDef,
            effectWorking = rd.effectWorking,
            soundWorking = rd.soundWorking,
            allowMixingIngredients = rd.allowMixingIngredients,
            defaultIngredientFilter = rd.defaultIngredientFilter,
            researchPrerequisite = rd.researchPrerequisite,
            factionPrerequisiteTags = rd.factionPrerequisiteTags
        };
        if (!Ad2Mod.Settings.UseSameQualityForAll && rd.products[0].thingDef.HasComp(typeof(CompQuality)))
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

        return r;
    }

    private static void genRecipes()
    {
        var allRecipes = DefDatabase<RecipeDef>.AllDefsListForReading;
        var recipes = new List<RecipeDef>();
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

            if (!Ad2Mod.Settings.MakeBulkForQuality && recipe.products[0].thingDef.HasComp(typeof(CompQuality)))
            {
                continue;
            }

            if (recipe.WorkAmountTotal(null) > Ad2Mod.Settings.MaxTimeCutoff * 60)
            {
                continue;
            }

            recipes.Add(recipe);
        }

        var recipesUsers = new List<ThingDef>();
        foreach (var recipe in recipes)
        {
            var lastOne = false;
            foreach (var factor in mulFactors)
            {
                if (factor * recipe.WorkAmountTotal(null) > Ad2Mod.Settings.MaxTimeCutoff * 60)
                {
                    lastOne = true;
                }

                var newRecipe = mkNewRecipe(recipe, factor);
                if (newRecipe == null)
                {
                    logMessage(
                        $"BulkRecipeGenerator: newRecipe == null  on {recipe.label}, this should not happen normally",
                        false, true);
                    continue;
                }


                newRecipe.ResolveReferences();

                DefDatabase<RecipeDef>.Add(newRecipe);

                rememberNewRecipe(recipe, newRecipe);

                foreach (var ru in recipe.AllRecipeUsers)
                {
                    if (!recipesUsers.Contains(ru))
                    {
                        recipesUsers.Add(ru);
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

                if (Ad2Mod.Settings.LimitToX5)
                {
                    break;
                }
            }
        }

        foreach (var ru in recipesUsers)
        {
            Traverse.Create(ru).Field("allRecipesCached").SetValue(null);
        }
    }
}