using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace ProjectRegrowth
{
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            Log.Message("[ProjectRegrowth] Initializing Harmony Patches...");
            var harmony = new Harmony("meast.ProjectRegrowth");
            harmony.PatchAll();
            Log.Message("[ProjectRegrowth] Harmony Patches applied successfully.");

            try
            {
                RegrowthMod.ApplySettings();
                Log.Message("[ProjectRegrowth] Mod settings applied to Defs successfully.");
            }
            catch (Exception ex)
            {
                Log.Error("[ProjectRegrowth] Failed to apply settings to Defs: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(CompBiosculpterPod), "SetPower")]
    public static class SetPower_Patch
    {
        public static void Postfix(CompBiosculpterPod __instance)
        {
            // If the pod is currently operating/processing a cycle
            if (__instance.State == BiosculpterPodState.Occupied && __instance.CurrentCycle != null)
            {
                // Check if the current cycle matches our custom high-power cycles
                string cycleKey = __instance.CurrentCycle.Props.key;
                if (cycleKey == "limbRegrowth" || cycleKey == "androidRegrowth")
                {
                    // Set power consumption from settings (negative represents drawing power in RimWorld)
                    var powerTrader = __instance.parent.GetComp<CompPowerTrader>();
                    if (powerTrader != null)
                    {
                        powerTrader.PowerOutput = -RegrowthMod.Settings.powerConsumption;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CompBiosculpterPod_HealingCycle), "WillHeal")]
    public static class WillHeal_Patch
    {
        public static void Postfix(Hediff hediff, ref bool __result)
        {
            if (__result && hediff != null && hediff.def != null)
            {
                string defName = hediff.def.defName;
                if (defName == "ATPP_HediffVX0Chip" ||
                    defName == "ATPP_HediffVX1Chip" ||
                    defName == "ATPP_HediffVX2Chip" ||
                    defName == "ATPP_HediffVX3Chip" ||
                    defName == "ATPP_HediffRXChip")
                {
                    __result = false;
                }
            }
        }
    }
}
