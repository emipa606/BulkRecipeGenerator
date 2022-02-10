using HarmonyLib;
using UnityEngine;
using Verse;

namespace Ad2mod;

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