using System;
using UnityEngine;
using Verse;

namespace AndroidTiersContinuedPatch
{
    public class AndroidTiersPatchSettings : ModSettings
    {
        public float productionMultiplier = 1.0f;
        public float batteryConsumptionMultiplier = 1.0f;
        public bool autoEquipWeapons = false;
        public bool autoEquipApparel = false;
        public bool allowAndroidPsycasts = false;

        // Legacy compatibility
        public bool autoEquipEnabled
        {
            get => autoEquipWeapons || autoEquipApparel;
            set
            {
                autoEquipWeapons = value;
                autoEquipApparel = value;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref productionMultiplier, "productionMultiplier", 1.0f);
            Scribe_Values.Look(ref batteryConsumptionMultiplier, "batteryConsumptionMultiplier", 1.0f);
            Scribe_Values.Look(ref autoEquipWeapons, "autoEquipWeapons", false);
            Scribe_Values.Look(ref autoEquipApparel, "autoEquipApparel", false);
            Scribe_Values.Look(ref allowAndroidPsycasts, "allowAndroidPsycasts", false);
        }
    }

    public class AndroidTiersPatchMod : Mod
    {
        public static AndroidTiersPatchSettings settings;

        public AndroidTiersPatchMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<AndroidTiersPatchSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Endless Growth Detection Label
            if (ModsConfig.IsActive("Slime.EndlessGrowth"))
            {
                GUI.color = Color.green;
                listingStandard.Label("Detected: Endless Growth");
                GUI.color = Color.white;
                listingStandard.Gap();
            }

            // Auto-Equip Checkboxes
            listingStandard.CheckboxLabeled("Enable Auto-Equip Weapons", ref settings.autoEquipWeapons, "Enables androids to automatically equip superior weapons found on the map. Disable if using Combat Extended ammo policies.");
            listingStandard.CheckboxLabeled("Enable Auto-Equip Apparel/Armor", ref settings.autoEquipApparel, "Enables androids to automatically wear superior apparel and armor found on the map.");
            listingStandard.Gap();

            // Psycasts Checkbox
            listingStandard.CheckboxLabeled("Allow Android Psycasts", ref settings.allowAndroidPsycasts, "Toggles whether androids (T1-T5) can have psychic sensitivity, acquire Psylink levels, and use Psycasts.");
            listingStandard.Gap();

            // Production Multiplier Slider
            listingStandard.Label(string.Format("Ani-Droid Production Multiplier: {0}x", settings.productionMultiplier.ToString("F2")));
            float newProd = (float)Math.Round(listingStandard.Slider(settings.productionMultiplier, 0.25f, 10.0f) * 4f) / 4f;
            if (newProd != settings.productionMultiplier)
            {
                settings.productionMultiplier = newProd;
            }
            listingStandard.Gap();

            // Battery Consumption Multiplier Slider
            listingStandard.Label(string.Format("Ani-Droid Battery Consumption Multiplier: {0}x", settings.batteryConsumptionMultiplier.ToString("F2")));
            float newBat = (float)Math.Round(listingStandard.Slider(settings.batteryConsumptionMultiplier, 0.25f, 10.0f) * 4f) / 4f;
            if (newBat != settings.batteryConsumptionMultiplier)
            {
                settings.batteryConsumptionMultiplier = newBat;
            }

            listingStandard.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // Re-apply dynamically when settings are closed
            AnimalDietPatch.ApplySettings();
        }

        public override string SettingsCategory()
        {
            return "Android Tiers Continued Patch";
        }
    }
}
