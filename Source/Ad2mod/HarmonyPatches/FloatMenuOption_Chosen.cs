using HarmonyLib;
using UnityEngine;
using Verse;

namespace Ad2mod;

[HarmonyPatch(typeof(FloatMenuOption), nameof(FloatMenuOption.Chosen))]
public class FloatMenuOption_Chosen
{
    public static bool Prefix(FloatMenuOption __instance)
    {
        return !Ad2Mod.Settings.UseRightClickMenu || Event.current.button != 1 ||
               !FloatMenu_Constructor.IsTracked(__instance);
    }
}