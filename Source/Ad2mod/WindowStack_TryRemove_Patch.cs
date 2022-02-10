using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Ad2mod;

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

        var fmos = (List<FloatMenuOption>)Traverse.Create(fm).Field("options").GetValue();
        PatchFloatMenu.Untrack(fm);
        foreach (var opt in fmos)
        {
            PatchFloatMenu.Untrack(opt);
        }

        //Log.Message($"WindowStack.TryRemove {PatchFloatMenu.trackedFM.Count}  {PatchFloatMenu.trackedFMO.Count}");
    }
}