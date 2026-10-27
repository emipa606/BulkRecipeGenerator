using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(WindowStack), nameof(WindowStack.TryRemove), typeof(Window), typeof(bool))]
public static class WindowStack_TryRemove
{
    public static void Postfix(bool __result, Window window)
    {
        if (window is not FloatMenu fm || !__result)
        {
            return;
        }

        if (!FloatMenu_Constructor.IsTracked(fm))
        {
            return;
        }

        var floatMenuOptions = (List<FloatMenuOption>)Traverse.Create(fm).Field("options").GetValue();
        FloatMenu_Constructor.Untrack(fm);
        foreach (var opt in floatMenuOptions)
        {
            FloatMenu_Constructor.Untrack(opt);
        }
    }
}