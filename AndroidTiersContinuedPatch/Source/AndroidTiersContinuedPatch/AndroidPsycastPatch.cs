using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidTiersContinuedPatch
{
    [HarmonyPatch(typeof(StatWorker), "GetValueUnfinalized")]
    public static class AndroidPsycastSensitivityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(StatWorker __instance, StatRequest req, ref float __result)
        {
            if (AndroidTiersPatchMod.settings != null && AndroidTiersPatchMod.settings.allowAndroidPsycasts)
            {
                if (req.HasThing && req.Thing is Pawn pawn && pawn.def != null)
                {
                    StatDef stat = Traverse.Create(__instance).Field("stat").GetValue<StatDef>();
                    if (stat == StatDefOf.PsychicSensitivity)
                    {
                        string defName = pawn.def.defName;
                        if (defName.StartsWith("Android") || defName == "M7Mech" || defName == "M8Mech" || defName == "AT_HellUnit" || defName.StartsWith("Robotic"))
                        {
                            if (__result <= 0f)
                            {
                                __result = 1.0f;
                            }
                        }
                    }
                }
            }
        }
    }
}
