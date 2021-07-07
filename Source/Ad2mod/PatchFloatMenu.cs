using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Ad2mod
{
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
}