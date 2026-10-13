using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectRegrowth
{
    public class RegrowthSettings : ModSettings
    {
        public float cycleDurationDays = 35f;
        public float powerConsumption = 600f;
        
        // Biological cycle ingredients
        public int bioMedicineCount = 5;
        public int bioNeutroamineCount = 15;
        public int bioPlasteelCount = 10;
        public int bioSpacerComponentCount = 0;
        public int bioIndustrialComponentCount = 0;

        // Mechanical cycle ingredients
        public int androidNanokitCount = 5;
        public int androidSpacerComponentCount = 5;
        public int androidPlasteelCount = 25;
        public int androidIndustrialComponentCount = 0;
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cycleDurationDays, "cycleDurationDays", 35f);
            Scribe_Values.Look(ref powerConsumption, "powerConsumption", 600f);
            
            Scribe_Values.Look(ref bioMedicineCount, "bioMedicineCount", 5);
            Scribe_Values.Look(ref bioNeutroamineCount, "bioNeutroamineCount", 15);
            Scribe_Values.Look(ref bioPlasteelCount, "bioPlasteelCount", 10);
            Scribe_Values.Look(ref bioSpacerComponentCount, "bioSpacerComponentCount", 0);
            Scribe_Values.Look(ref bioIndustrialComponentCount, "bioIndustrialComponentCount", 0);

            Scribe_Values.Look(ref androidNanokitCount, "androidNanokitCount", 5);
            Scribe_Values.Look(ref androidSpacerComponentCount, "androidSpacerComponentCount", 5);
            Scribe_Values.Look(ref androidPlasteelCount, "androidPlasteelCount", 25);
            Scribe_Values.Look(ref androidIndustrialComponentCount, "androidIndustrialComponentCount", 0);
        }
    }

    public class RegrowthMod : Mod
    {
        public static RegrowthSettings Settings;

        public RegrowthMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RegrowthSettings>();
        }

        public override string SettingsCategory()
        {
            return "Project Regrowth";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, 400f, 35f), "Project Regrowth Settings");
            Text.Font = GameFont.Small;
            
            GUI.color = Color.gray;
            Widgets.Label(new Rect(inRect.x, inRect.y + 35f, 600f, 25f), "Configure cycle duration, power draw, and extra resource costs for advanced bioregeneration.");
            GUI.color = Color.white;
            
            Widgets.DrawLineHorizontal(inRect.x, inRect.y + 60f, inRect.width);

            // General Panel
            Rect generalRect = new Rect(inRect.x, inRect.y + 75f, inRect.width, 110f);
            Widgets.DrawMenuSection(generalRect);
            Rect generalInner = generalRect.ContractedBy(10f);
            
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(generalInner.x, generalInner.y, 200f, 15f), "GENERAL & POWER");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float durationY = generalInner.y + 15f;
            DrawSettingSlider(new Rect(generalInner.x, durationY, (generalInner.width - 20f) / 2f, 40f), "Cycle Duration", ref Settings.cycleDurationDays, 1f, 120f, "days");
            DrawSettingSlider(new Rect(generalInner.x + (generalInner.width - 20f) / 2f + 20f, durationY, (generalInner.width - 20f) / 2f, 40f), "Biosculpter Power Draw", ref Settings.powerConsumption, 50f, 3000f, "W");

            // Columns
            float colY = generalRect.yMax + 15f;
            float colWidth = (inRect.width - 20f) / 2f;
            float colHeight = inRect.height - colY - 10f;

            // Left Column (Biological)
            Rect bioRect = new Rect(inRect.x, colY, colWidth, colHeight);
            Widgets.DrawMenuSection(bioRect);
            Rect bioInner = bioRect.ContractedBy(10f);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(bioInner.x, bioInner.y, 250f, 15f), "BIOLOGICAL CYCLE INGREDIENTS");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float itemY = bioInner.y + 20f;
            float spacing = 45f;
            DrawSettingSlider(new Rect(bioInner.x, itemY, bioInner.width, 40f), "Glitterworld Medicine", ref Settings.bioMedicineCount, 0, 50, "");
            itemY += spacing;
            DrawSettingSlider(new Rect(bioInner.x, itemY, bioInner.width, 40f), "Neutroamine", ref Settings.bioNeutroamineCount, 0, 100, "");
            itemY += spacing;
            DrawSettingSlider(new Rect(bioInner.x, itemY, bioInner.width, 40f), "Plasteel", ref Settings.bioPlasteelCount, 0, 200, "");
            itemY += spacing;
            DrawSettingSlider(new Rect(bioInner.x, itemY, bioInner.width, 40f), "Spacer Components", ref Settings.bioSpacerComponentCount, 0, 50, "");
            itemY += spacing;
            DrawSettingSlider(new Rect(bioInner.x, itemY, bioInner.width, 40f), "Industrial Components", ref Settings.bioIndustrialComponentCount, 0, 50, "");

            // Right Column (Mechanical)
            Rect mechRect = new Rect(inRect.x + colWidth + 20f, colY, colWidth, colHeight);
            Widgets.DrawMenuSection(mechRect);
            Rect mechInner = mechRect.ContractedBy(10f);

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(mechInner.x, mechInner.y, 250f, 15f), "MECHANICAL CYCLE INGREDIENTS");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            bool androidTiersActive = DefDatabase<ThingDef>.GetNamed("ATPP_AndroidNanokitAdvanced", false) != null;
            if (androidTiersActive)
            {
                float mY = mechInner.y + 20f;
                DrawSettingSlider(new Rect(mechInner.x, mY, mechInner.width, 40f), "Advanced Nanokits", ref Settings.androidNanokitCount, 0, 50, "");
                mY += spacing;
                DrawSettingSlider(new Rect(mechInner.x, mY, mechInner.width, 40f), "Spacer Components", ref Settings.androidSpacerComponentCount, 0, 50, "");
                mY += spacing;
                DrawSettingSlider(new Rect(mechInner.x, mY, mechInner.width, 40f), "Plasteel", ref Settings.androidPlasteelCount, 0, 200, "");
                mY += spacing;
                DrawSettingSlider(new Rect(mechInner.x, mY, mechInner.width, 40f), "Industrial Components", ref Settings.androidIndustrialComponentCount, 0, 50, "");
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(mechInner.x, mechInner.y + colHeight / 3f, mechInner.width, 50f), "Android Tiers mod is not active.\nMechanical cycle settings are disabled.");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ApplySettings();
        }

        public static void ApplySettings()
        {
            var podDef = DefDatabase<ThingDef>.GetNamed("BiosculpterPod", false);
            if (podDef != null && podDef.comps != null)
            {
                foreach (var compProps in podDef.comps)
                {
                    if (compProps is CompProperties_BiosculpterPod_HealingCycle healingProps)
                    {
                        if (healingProps.key == "limbRegrowth")
                        {
                            healingProps.durationDays = Settings.cycleDurationDays;
                            healingProps.extraRequiredIngredients.Clear();
                            
                            AddIngredient(healingProps.extraRequiredIngredients, "MedicineUltratech", Settings.bioMedicineCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "Neutroamine", Settings.bioNeutroamineCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "Plasteel", Settings.bioPlasteelCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "ComponentSpacer", Settings.bioSpacerComponentCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "ComponentIndustrial", Settings.bioIndustrialComponentCount);
                        }
                        else if (healingProps.key == "androidRegrowth")
                        {
                            healingProps.durationDays = Settings.cycleDurationDays;
                            healingProps.extraRequiredIngredients.Clear();

                            AddIngredient(healingProps.extraRequiredIngredients, "ATPP_AndroidNanokitAdvanced", Settings.androidNanokitCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "ComponentSpacer", Settings.androidSpacerComponentCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "Plasteel", Settings.androidPlasteelCount);
                            AddIngredient(healingProps.extraRequiredIngredients, "ComponentIndustrial", Settings.androidIndustrialComponentCount);
                        }
                    }
                }
            }
        }

        private static void AddIngredient(System.Collections.Generic.List<ThingDefCountClass> list, string defName, int count)
        {
            if (count > 0)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamed(defName, false);
                if (def != null)
                {
                    list.Add(new ThingDefCountClass(def, count));
                }
            }
        }

        private void DrawSettingSlider(Rect rect, string label, ref float value, float min, float max, string suffix)
        {
            Rect labelRect = new Rect(rect.x, rect.y, rect.width - 80f, 20f);
            Rect valRect = new Rect(rect.x + rect.width - 70f, rect.y, 70f, 20f);
            Rect sliderRect = new Rect(rect.x, rect.y + 20f, rect.width, 18f);

            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(valRect, string.IsNullOrEmpty(suffix) ? $"{value:F0}" : $"{value:F0} {suffix}");
            Text.Anchor = TextAnchor.UpperLeft;
            
            value = Widgets.HorizontalSlider(sliderRect, value, min, max, true, null, null, null, 1f);
        }

        private void DrawSettingSlider(Rect rect, string label, ref int value, int min, int max, string suffix)
        {
            float valFloat = value;
            DrawSettingSlider(rect, label, ref valFloat, min, max, suffix);
            value = Mathf.RoundToInt(valFloat);
        }
    }
}
