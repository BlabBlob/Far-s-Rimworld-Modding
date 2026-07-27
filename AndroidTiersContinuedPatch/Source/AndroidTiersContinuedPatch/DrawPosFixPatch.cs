using System;
using HarmonyLib;
using Verse;
using UnityEngine;

namespace AndroidTiersContinuedPatch
{
    [HarmonyPatch(typeof(Pawn_DrawTracker), "get_DrawPos")]
    public static class DrawPosFixPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn_DrawTracker __instance, Pawn ___pawn, ref Vector3 __result)
        {
            if (__instance == null || ___pawn == null)
            {
                __result = Vector3.zero;
                return false;
            }
            if (!___pawn.Spawned || ___pawn.Map == null)
            {
                __result = ___pawn.PositionHeld.ToVector3Shifted();
                return false;
            }
            return true;
        }

        [HarmonyFinalizer]
        public static Exception Finalizer(Exception __exception, Pawn_DrawTracker __instance, Pawn ___pawn, ref Vector3 __result)
        {
            if (__exception is NullReferenceException)
            {
                if (___pawn != null)
                {
                    if (___pawn.Spawned)
                    {
                        __result = ___pawn.Position.ToVector3Shifted();
                    }
                    else
                    {
                        __result = ___pawn.PositionHeld.ToVector3Shifted();
                    }
                }
                else
                {
                    __result = Vector3.zero;
                }
                return null; // Suppress exception cleanly so rendering and UI do not crash
            }
            return __exception;
        }
    }
}
