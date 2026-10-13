using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using MOARANDROIDS;

namespace AndroidTiersContinuedPatch
{
    [HarmonyPatch(typeof(MOARANDROIDS.Utils), nameof(MOARANDROIDS.Utils.PermutePawn), new[] { typeof(Pawn), typeof(Pawn), typeof(bool) })]
    public static class PermutePawnFixPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn p1, Pawn p2, bool allowSettingsPermutation = true)
        {
            try
            {
                if (p1 == null || p2 == null || p1 == p2)
                    return false;

                // ====================================================================
                // 1. STORY SWAPPING
                // ====================================================================
                if (p1.story != null && p2.story != null)
                {
                    Pawn_StoryTracker st1 = new Pawn_StoryTracker(p2);
                    // Copy physical attributes from p2 (destination body)
                    st1.HairColor = p2.story.HairColor;
                    st1.headType = p2.story.headType;
                    st1.hairDef = p2.story.hairDef;
                    st1.bodyType = p2.story.bodyType;
                    st1.furDef = p2.story.furDef;
                    st1.skinColorOverride = p2.story.skinColorOverride;
                    st1.favoriteColor = p2.story.favoriteColor;

                    // Copy backstory and traits from p1 (mind)
                    st1.Adulthood = p1.story.Adulthood;
                    st1.Childhood = p1.story.Childhood;
                    st1.birthLastName = p1.story.birthLastName;

                    if (p1.story.traits?.allTraits != null)
                    {
                        foreach (var trait in p1.story.traits.allTraits)
                        {
                            if (trait?.def != null)
                            {
                                Trait nt = new Trait(trait.def, trait.Degree, false);
                                GainDirectTrait(st1, nt);
                            }
                        }
                    }
                    st1.title = p2.story.title;

                    Pawn_StoryTracker st2 = new Pawn_StoryTracker(p1);
                    // Copy physical attributes from p1 (destination body)
                    st2.HairColor = p1.story.HairColor;
                    st2.headType = p1.story.headType;
                    st2.hairDef = p1.story.hairDef;
                    st2.bodyType = p1.story.bodyType;
                    st2.furDef = p1.story.furDef;
                    st2.skinColorOverride = p1.story.skinColorOverride;
                    st2.favoriteColor = p1.story.favoriteColor;

                    // Copy backstory and traits from p2 (mind)
                    st2.Adulthood = p2.story.Adulthood;
                    st2.Childhood = p2.story.Childhood;
                    st2.birthLastName = p2.story.birthLastName;

                    if (p2.story.traits?.allTraits != null)
                    {
                        foreach (var trait in p2.story.traits.allTraits)
                        {
                            if (trait?.def != null)
                            {
                                Trait nt = new Trait(trait.def, trait.Degree, false);
                                GainDirectTrait(st2, nt);
                            }
                        }
                    }
                    st2.title = p1.story.title;

                    string vhg1 = (string)Traverse.Create(p1.story).Field("headGraphicPath").GetValue();
                    string vhg2 = (string)Traverse.Create(p2.story).Field("headGraphicPath").GetValue();

                    p1.story = st2;
                    p2.story = st1;

                    Traverse.Create(p1.story).Field("headGraphicPath").SetValue(vhg1);
                    Traverse.Create(p2.story).Field("headGraphicPath").SetValue(vhg2);
                }

                // ====================================================================
                // 2. ROYAL TITLES
                // ====================================================================
                Pawn_RoyaltyTracker tmpRoyalty = p1.royalty;
                if (p1.royalty != null)
                    p1.royalty.pawn = p2;
                if (p2.royalty != null)
                    p2.royalty.pawn = p1;

                p1.royalty = p2.royalty;
                p2.royalty = tmpRoyalty;

                if (p1.royalty != null)
                {
                    p1.royalty.UpdateAvailableAbilities();
                    p1.needs?.AddOrRemoveNeedsAsAppropriate();
                    p1.abilities?.Notify_TemporaryAbilitiesChanged();
                }
                if (p2.royalty != null)
                {
                    p2.royalty.UpdateAvailableAbilities();
                    p2.needs?.AddOrRemoveNeedsAsAppropriate();
                    p2.abilities?.Notify_TemporaryAbilitiesChanged();
                }

                // ====================================================================
                // 3. TRAITS & INCAPABILITIES
                // ====================================================================
                SafeNotifTraitsChanged(p1);
                SafeNotifTraitsChanged(p2);

                SafeResetCachedIncapableOf(p1);
                SafeResetCachedIncapableOf(p2);

                SafeInvertRelations(p1, p2);

                // ====================================================================
                // 4. SKILLS & TRAINING
                // ====================================================================
                if (p1.skills != null && p2.skills != null)
                {
                    Pawn_SkillTracker ps1 = new Pawn_SkillTracker(p1);
                    Pawn_SkillTracker ps2 = new Pawn_SkillTracker(p2);

                    List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
                    for (int i = 0; i < allDefsListForReading.Count; i++)
                    {
                        SkillDef skillDef = allDefsListForReading[i];
                        SkillRecord skillSource1 = p1.skills.GetSkill(skillDef);
                        SkillRecord skillSource2 = p2.skills.GetSkill(skillDef);
                        SkillRecord targetSkill1 = ps1.GetSkill(skillDef);
                        SkillRecord targetSkill2 = ps2.GetSkill(skillDef);

                        if (targetSkill2 != null && skillSource1 != null)
                        {
                            targetSkill2.levelInt = skillSource1.levelInt;
                            targetSkill2.passion = skillSource1.passion;
                            targetSkill2.xpSinceLastLevel = skillSource1.xpSinceLastLevel;
                            targetSkill2.xpSinceMidnight = skillSource1.xpSinceMidnight;
                        }

                        if (targetSkill1 != null && skillSource2 != null)
                        {
                            targetSkill1.levelInt = skillSource2.levelInt;
                            targetSkill1.passion = skillSource2.passion;
                            targetSkill1.xpSinceLastLevel = skillSource2.xpSinceLastLevel;
                            targetSkill1.xpSinceMidnight = skillSource2.xpSinceMidnight;
                        }
                    }

                    p1.skills = ps1;
                    p2.skills = ps2;
                }

                Pawn_TrainingTracker ptt = p1.training;
                p1.training = p2.training;
                p2.training = ptt;

                // ====================================================================
                // 5. PLAY LOG ENTRIES
                // ====================================================================
                if (Find.PlayLog?.AllEntries != null)
                {
                    foreach (var log in Find.PlayLog.AllEntries)
                    {
                        if (log != null && (log.Concerns(p1) || log.Concerns(p2)))
                        {
                            Traverse tlog = Traverse.Create(log);
                            Pawn initiator = tlog.Field("initiator").GetValue<Pawn>();
                            Pawn recipient = tlog.Field("recipient").GetValue<Pawn>();

                            if (initiator == p1)
                                initiator = p2;
                            else if (initiator == p2)
                                initiator = p1;

                            if (recipient == p1)
                                recipient = p2;
                            else if (recipient == p2)
                                recipient = p1;

                            tlog.Field("initiator").SetValue(initiator);
                            tlog.Field("recipient").SetValue(recipient);
                        }
                    }
                }

                // ====================================================================
                // 6. MEMORIES & MOOD (Fully Null-Safe)
                // ====================================================================
                Pawn_NeedsTracker pn1 = new Pawn_NeedsTracker(p1);
                Pawn_NeedsTracker pn2 = new Pawn_NeedsTracker(p2);

                try
                {
                    FleshTypeDef androidFlesh = DefDatabase<FleshTypeDef>.GetNamedSilentFail("AndroidTier");
                    if (androidFlesh != null)
                    {
                        if (p1.RaceProps?.FleshType == androidFlesh)
                            pn1.rest = new Need_Rest_Fake(p1);
                        if (p2.RaceProps?.FleshType == androidFlesh)
                            pn2.rest = new Need_Rest_Fake(p2);
                    }
                }
                catch (Exception) { }

                // Swap personal thought memories between p1 and p2
                if (p1.needs?.mood?.thoughts?.memories?.Memories != null && pn2.mood?.thoughts?.memories?.Memories != null)
                {
                    foreach (var x in p1.needs.mood.thoughts.memories.Memories)
                    {
                        if (x != null)
                        {
                            if (x.otherPawn == p2)
                                x.otherPawn = p1;

                            pn2.mood.thoughts.memories.Memories.Add(x);
                        }
                    }
                }

                if (p2.needs?.mood?.thoughts?.memories?.Memories != null && pn1.mood?.thoughts?.memories?.Memories != null)
                {
                    foreach (var x in p2.needs.mood.thoughts.memories.Memories)
                    {
                        if (x != null)
                        {
                            if (x.otherPawn == p1)
                                x.otherPawn = p2;

                            pn1.mood.thoughts.memories.Memories.Add(x);
                        }
                    }
                }

                // Update memories for map pawns
                if (Find.Maps != null)
                {
                    foreach (var map in Find.Maps)
                    {
                        if (map?.mapPawns?.AllPawns == null) continue;
                        foreach (var colon in map.mapPawns.AllPawns)
                        {
                            if (colon == null || colon == p1 || colon == p2 || colon.needs?.mood?.thoughts?.memories?.Memories == null)
                                continue;

                            foreach (var mem in colon.needs.mood.thoughts.memories.Memories)
                            {
                                if (mem?.otherPawn != null)
                                {
                                    if (mem.otherPawn == p1)
                                        mem.otherPawn = p2;
                                    else if (mem.otherPawn == p2)
                                        mem.otherPawn = p1;
                                }
                            }
                        }
                    }
                }

                // Update memories for world pawns (null-safe against world pawns without needs)
                if (Find.WorldPawns?.AllPawnsAlive != null)
                {
                    foreach (var wp in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (wp == null || wp.needs?.mood?.thoughts?.memories?.Memories == null)
                            continue;

                        foreach (var mem in wp.needs.mood.thoughts.memories.Memories)
                        {
                            if (mem?.otherPawn != null)
                            {
                                if (mem.otherPawn == p1)
                                    mem.otherPawn = p2;
                                else if (mem.otherPawn == p2)
                                    mem.otherPawn = p1;
                            }
                        }
                    }
                }

                // ====================================================================
                // 7. NEEDS VALUES
                // ====================================================================
                if (p1.needs != null && p2.needs != null)
                {
                    if (p1.needs.food != null && p2.needs.food != null && pn1.food != null && pn2.food != null)
                    {
                        pn1.food.CurLevel = p1.needs.food.CurLevel;
                        pn2.food.CurLevel = p2.needs.food.CurLevel;
                    }
                    if (p1.needs.joy != null && p2.needs.joy != null && pn1.joy != null && pn2.joy != null)
                    {
                        pn1.joy.CurLevel = p1.needs.joy.CurLevel;
                        pn2.joy.CurLevel = p2.needs.joy.CurLevel;
                    }
                    if (p1.needs.comfort != null && p2.needs.comfort != null && pn1.comfort != null && pn2.comfort != null)
                    {
                        pn1.comfort.CurLevel = p1.needs.comfort.CurLevel;
                        pn2.comfort.CurLevel = p2.needs.comfort.CurLevel;
                    }
                    if (p1.needs.roomsize != null && p2.needs.roomsize != null && pn1.roomsize != null && pn2.roomsize != null)
                    {
                        pn1.roomsize.CurLevel = p1.needs.roomsize.CurLevel;
                        pn2.roomsize.CurLevel = p2.needs.roomsize.CurLevel;
                    }
                    if (p1.needs.rest != null && p2.needs.rest != null && pn1.rest != null && pn2.rest != null)
                    {
                        pn1.rest.CurLevel = p1.needs.rest.CurLevel;
                        pn2.rest.CurLevel = p2.needs.rest.CurLevel;
                    }
                    if (p1.needs.mood != null && p2.needs.mood != null && pn1.mood != null && pn2.mood != null)
                    {
                        pn1.mood.CurLevel = p2.needs.mood.CurLevel;
                        pn2.mood.CurLevel = p1.needs.mood.CurLevel;
                    }
                }

                p1.needs = pn1;
                p2.needs = pn2;

                pn1.AddOrRemoveNeedsAsAppropriate();
                pn2.AddOrRemoveNeedsAsAppropriate();

                // ====================================================================
                // 8. NAME SWAPPING & GRAPHICS RESOLUTION
                // ====================================================================
                Name nam = p1.Name;
                p1.Name = p2.Name;
                p2.Name = nam;

                try
                {
                    if (p1.Drawer?.renderer != null)
                    {
                        p1.Drawer.renderer.SetAllGraphicsDirty();
                        p1.Drawer.renderer.renderTree?.SetDirty();
                    }
                    if (p2.Drawer?.renderer != null)
                    {
                        p2.Drawer.renderer.SetAllGraphicsDirty();
                        p2.Drawer.renderer.renderTree?.SetDirty();
                    }
                    PortraitsCache.SetDirty(p1);
                    PortraitsCache.SetDirty(p2);
                    Find.ColonistBar?.MarkColonistsDirty();
                }
                catch (Exception) { }

                // ====================================================================
                // 9. WORK SETTINGS INITIALIZATION
                // ====================================================================
                if (p1.workSettings == null)
                {
                    p1.workSettings = new Pawn_WorkSettings(p1);
                    p1.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                }

                if (p2.workSettings == null)
                {
                    p2.workSettings = new Pawn_WorkSettings(p2);
                    p2.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
                }

                // ====================================================================
                // 10. SETTINGS, RESTRICTIONS & POLICIES PERMUTATION
                // ====================================================================
                if (allowSettingsPermutation && p1.Faction == Faction.OfPlayer && p2.Faction == Faction.OfPlayer)
                {
                    try
                    {
                        // Copy work settings from p1
                        Pawn_WorkSettings p1WS = new Pawn_WorkSettings(p1);
                        p1WS.EnableAndInitializeIfNotAlreadyInitialized();
                        if (p1.workSettings != null)
                        {
                            foreach (var el in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                            {
                                if (!p1.WorkTypeIsDisabled(el))
                                    p1WS.SetPriority(el, p1.workSettings.GetPriority(el));
                            }
                        }

                        // Copy player settings from p1
                        Area p1Area = p1.playerSettings?.AreaRestrictionInPawnCurrentMap;
                        HostilityResponseMode? p1Hostility = p1.playerSettings?.hostilityResponse;

                        // Copy timetable from p1
                        Pawn_TimetableTracker p1TT = null;
                        if (p1.timetable != null)
                        {
                            p1TT = new Pawn_TimetableTracker(p1);
                            for (int i = 0; i < 24; i++)
                            {
                                p1TT.SetAssignment(i, p1.timetable.GetAssignment(i));
                            }
                        }

                        // Copy policies from p1
                        FoodPolicy p1FR = p1.foodRestriction?.CurrentFoodPolicy;
                        DrugPolicy p1DR = p1.drugs?.CurrentPolicy;
                        ApparelPolicy p1O = p1.outfits?.CurrentApparelPolicy;

                        // Apply P2 -> P1
                        if (p2.workSettings != null && p1.workSettings != null)
                        {
                            foreach (var el in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                            {
                                if (!p1.WorkTypeIsDisabled(el))
                                    p1.workSettings.SetPriority(el, p2.workSettings.GetPriority(el));
                            }
                        }
                        if (p1.playerSettings != null && p2.playerSettings != null)
                        {
                            p1.playerSettings.AreaRestrictionInPawnCurrentMap = p2.playerSettings.AreaRestrictionInPawnCurrentMap;
                            p1.playerSettings.hostilityResponse = p2.playerSettings.hostilityResponse;
                        }
                        if (p1.timetable != null && p2.timetable != null)
                        {
                            for (int i = 0; i < 24; i++)
                            {
                                p1.timetable.SetAssignment(i, p2.timetable.GetAssignment(i));
                            }
                        }
                        if (p1.foodRestriction != null && p2.foodRestriction != null)
                        {
                            p1.foodRestriction.CurrentFoodPolicy = p2.foodRestriction.CurrentFoodPolicy;
                        }
                        if (p1.drugs != null && p2.drugs != null)
                        {
                            p1.drugs.CurrentPolicy = p2.drugs.CurrentPolicy;
                        }
                        if (p1.outfits != null && p2.outfits != null)
                        {
                            p1.outfits.CurrentApparelPolicy = p2.outfits.CurrentApparelPolicy;
                        }

                        // Apply P1 -> P2
                        if (p2.workSettings != null)
                        {
                            foreach (var el in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                            {
                                if (!p2.WorkTypeIsDisabled(el))
                                    p2.workSettings.SetPriority(el, p1WS.GetPriority(el));
                            }
                        }
                        if (p2.playerSettings != null)
                        {
                            if (p1Area != null)
                                p2.playerSettings.AreaRestrictionInPawnCurrentMap = p1Area;
                            if (p1Hostility.HasValue)
                                p2.playerSettings.hostilityResponse = p1Hostility.Value;
                        }
                        if (p2.timetable != null && p1TT != null)
                        {
                            for (int i = 0; i < 24; i++)
                            {
                                p2.timetable.SetAssignment(i, p1TT.GetAssignment(i));
                            }
                        }
                        if (p2.foodRestriction != null && p1FR != null)
                        {
                            p2.foodRestriction.CurrentFoodPolicy = p1FR;
                        }
                        if (p2.drugs != null && p1DR != null)
                        {
                            p2.drugs.CurrentPolicy = p1DR;
                        }
                        if (p2.outfits != null && p1O != null)
                        {
                            p2.outfits.CurrentApparelPolicy = p1O;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("[AndroidTiersContinuedPatch] PermutePawn: error transferring settings: " + ex.Message);
                    }
                }

                // ====================================================================
                // 11. IDEOLOGY PERMUTATION
                // ====================================================================
                if (p1.ideo != null || p2.ideo != null)
                {
                    Pawn_IdeoTracker pit = p1.ideo;
                    p1.ideo = p2.ideo;
                    p2.ideo = pit;

                    if (p1.ideo != null)
                    {
                        Traverse.Create(p1.ideo).Field("pawn").SetValue(p1);
                        if (p2.ideo?.Ideo?.RolesListForReading != null)
                        {
                            foreach (var precept in p2.ideo.Ideo.RolesListForReading)
                            {
                                if (precept != null && precept.IsAssigned(p2))
                                {
                                    precept.Unassign(p2, false);
                                    precept.Assign(p1, false);
                                }
                            }
                        }
                    }
                    if (p2.ideo != null)
                    {
                        Traverse.Create(p2.ideo).Field("pawn").SetValue(p2);
                        if (p2.ideo?.Ideo?.RolesListForReading != null)
                        {
                            foreach (var precept in p2.ideo.Ideo.RolesListForReading)
                            {
                                if (precept != null && precept.IsAssigned(p1))
                                {
                                    precept.Unassign(p1, false);
                                    precept.Assign(p2, false);
                                }
                            }
                        }
                    }
                }

                // ====================================================================
                // 12. ABILITIES PERMUTATION
                // ====================================================================
                Pawn_AbilityTracker pat = p1.abilities;
                p1.abilities = p2.abilities;
                p2.abilities = pat;

                if (p1.abilities != null)
                {
                    p1.abilities.pawn = p1;
                    p1.abilities.Notify_TemporaryAbilitiesChanged();
                }
                if (p2.abilities != null)
                {
                    p2.abilities.pawn = p2;
                    p2.abilities.Notify_TemporaryAbilitiesChanged();
                }

                Log.Message($"[AndroidTiersContinuedPatch] PermutePawn successfully completed between {p1.LabelShortCap} and {p2.LabelShortCap}.");
            }
            catch (Exception e)
            {
                Log.Error("[AndroidTiersContinuedPatch] Exception in PermutePawn prefix: " + e.Message + " - " + e.StackTrace);
            }

            return false; // Skip original buggy PermutePawn
        }

        public static void GainDirectTrait(Pawn_StoryTracker tr, Trait trait)
        {
            if (tr?.traits == null || trait?.def == null || tr.traits.HasTrait(trait.def))
                return;

            tr.traits.allTraits.Add(trait);
        }

        public static void SafeNotifTraitsChanged(Pawn pawn)
        {
            if (pawn == null) return;
            pawn.Notify_DisabledWorkTypesChanged();

            if (pawn.skills != null)
            {
                pawn.skills.Notify_SkillDisablesChanged();
            }
            if (!pawn.Dead && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            {
                pawn.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
            }
        }

        public static void SafeResetCachedIncapableOf(Pawn pawn)
        {
            if (pawn == null) return;
            pawn.ClearCachedDisabledWorkTypes();
            pawn.ClearCachedDisabledSkillRecords();
        }

        public static void SafeInvertRelations(Pawn p1, Pawn p2)
        {
            try
            {
                if (p1 == null || p2 == null) return;

                Pawn_RelationsTracker pro1 = p1.relations;
                Pawn_RelationsTracker pro2 = p2.relations;

                Pawn_RelationsTracker pr1 = new Pawn_RelationsTracker(p1);
                Pawn_RelationsTracker pr2 = new Pawn_RelationsTracker(p2);
                p1.relations = pr1;
                p2.relations = pr2;
                Pawn tmp1;

                if (Find.WorldPawns?.AllPawnsAlive != null)
                {
                    foreach (var wp in Find.WorldPawns.AllPawnsAlive)
                    {
                        if (wp == null || wp.relations?.DirectRelations == null)
                            continue;

                        List<DirectPawnRelation> list = wp.relations.DirectRelations.ToList();
                        for (int i1 = 0; i1 < list.Count; i1++)
                        {
                            DirectPawnRelation rel = list[i1];
                            if (rel != null && rel.otherPawn != null && rel.def != null)
                            {
                                if (rel.otherPawn == p1)
                                {
                                    wp.relations.TryRemoveDirectRelation(rel.def, p1);
                                    wp.relations.AddDirectRelation(rel.def, p2);
                                }
                                else if (rel.otherPawn == p2)
                                {
                                    wp.relations.TryRemoveDirectRelation(rel.def, p2);
                                    wp.relations.AddDirectRelation(rel.def, p1);
                                }
                            }
                        }
                    }
                }

                if (pro2?.DirectRelations != null)
                {
                    List<DirectPawnRelation> list1 = pro2.DirectRelations.ToList();
                    for (int i = 0; i < list1.Count; i++)
                    {
                        DirectPawnRelation rel = list1[i];
                        if (rel != null && rel.otherPawn != null && rel.def != null)
                        {
                            if (rel.otherPawn.relations != null && rel.otherPawn != p1 && rel.otherPawn != p2)
                            {
                                rel.otherPawn.relations.TryRemoveDirectRelation(rel.def, p2);
                            }

                            if (rel.otherPawn == p1)
                                tmp1 = p2;
                            else
                                tmp1 = rel.otherPawn;

                            pr1.AddDirectRelation(rel.def, tmp1);
                        }
                    }
                }

                if (pro1?.DirectRelations != null)
                {
                    List<DirectPawnRelation> list2 = pro1.DirectRelations.ToList();
                    for (int i = 0; i < list2.Count; i++)
                    {
                        DirectPawnRelation rel = list2[i];
                        if (rel != null && rel.otherPawn != null && rel.def != null)
                        {
                            if (rel.otherPawn.relations != null && rel.otherPawn != p1 && rel.otherPawn != p2)
                            {
                                rel.otherPawn.relations.TryRemoveDirectRelation(rel.def, p1);
                            }

                            if (rel.otherPawn == p2)
                                tmp1 = p1;
                            else
                                tmp1 = rel.otherPawn;

                            pr2.AddDirectRelation(rel.def, tmp1);
                        }
                    }
                }

                if (p1.relations != null) p1.relations.everSeenByPlayer = true;
                if (p2.relations != null) p2.relations.everSeenByPlayer = true;

                if (Find.Maps != null)
                {
                    foreach (var map in Find.Maps)
                    {
                        if (map?.mapPawns?.AllPawns == null) continue;
                        foreach (var colon in map.mapPawns.AllPawns)
                        {
                            if (colon == null || colon == p1 || colon == p2)
                                continue;

                            if (colon.playerSettings != null)
                            {
                                if (colon.playerSettings.Master != null && colon.playerSettings.Master == p1)
                                    colon.playerSettings.Master = p2;
                                else if (colon.playerSettings.Master != null && colon.playerSettings.Master == p2)
                                    colon.playerSettings.Master = p1;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("[AndroidTiersContinuedPatch] SafeInvertRelations: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(MOARANDROIDS.Utils), nameof(MOARANDROIDS.Utils.invertRelations), new[] { typeof(Pawn), typeof(Pawn) })]
    public static class InvertRelationsFixPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn p1, Pawn p2)
        {
            PermutePawnFixPatch.SafeInvertRelations(p1, p2);
            return false; // Skip original
        }
    }
}
