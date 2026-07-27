using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidTiersContinuedPatch
{
    [HarmonyPatch(typeof(Pawn_AgeTracker), "get_DevelopmentalStage")]
    public static class AndroidDevelopmentalStagePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_AgeTracker __instance, ref DevelopmentalStage __result, Pawn ___pawn)
        {
            if (___pawn != null && ___pawn.def != null)
            {
                string defName = ___pawn.def.defName;
                if (defName.StartsWith("Android") || 
                    defName == "M7Mech" || 
                    defName == "M8Mech" || 
                    defName == "AT_HellUnit" ||
                    defName.StartsWith("Robotic"))
                {
                    __result = DevelopmentalStage.Adult;
                }
            }
        }
    }
}
