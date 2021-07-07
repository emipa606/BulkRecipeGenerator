using System.Collections.Generic;
using HarmonyLib;
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
}