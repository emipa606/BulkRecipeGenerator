using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(FloatMenu), MethodType.Constructor, typeof(List<FloatMenuOption>))]
internal class FloatMenu_Constructor
{
    private static readonly List<FloatMenu> trackedFm = [];
    private static readonly List<FloatMenuOption> trackedFmo = [];

    public static bool IsTracked(FloatMenuOption fmo)
    {
        return trackedFmo.Contains(fmo);
    }

    public static bool IsTracked(FloatMenu fm)
    {
        return trackedFm.Contains(fm);
    }

    public static void Untrack(FloatMenuOption fmo)
    {
        trackedFmo.Remove(fmo);
    }

    public static void Untrack(FloatMenu fm)
    {
        trackedFm.Remove(fm);
    }

    private static void Postfix(FloatMenu __instance, List<FloatMenuOption> ___options)
    {
        if (!BillStack_DoListing.InDoListing)
        {
            return;
        }

        trackedFm.Add(__instance);
        foreach (var opt in ___options)
        {
            trackedFmo.Add(opt);
        }
    }
}