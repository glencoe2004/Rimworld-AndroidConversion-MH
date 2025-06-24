using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace AndroidConversion
{
    [UsedImplicitly]
    public class AndroidConversionSettingsHandler : Mod
    {
        public static AndroidConversionSettings Settings;

        public AndroidConversionSettingsHandler(ModContentPack content) : base(content)
        {
            // Initialize Harmony
            Harmony harmony = new Harmony("glencoe2004.MHAndroidConversion");
            harmony.PatchAll();

            // Initialize settings
            Settings = GetSettings<AndroidConversionSettings>();

            // Update global variables from settings
            UpdateGlobalVariables();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Conversion Permission Settings
            listing.Label("Conversion Permissions:");
            listing.CheckboxLabeled("Allow Hostile Conversion", ref Settings.allowHostileConversion,
                "Allows converting hostile pawns (enemies).");
            listing.CheckboxLabeled("Allow Guest Conversion", ref Settings.allowGuestConversion,
                "Allows converting quest lodgers and guests.");
            listing.CheckboxLabeled("Allow Guest Prisoner Conversion", ref Settings.allowGuestPrisonerConversion,
                "Allows converting guest prisoners from other factions.");

            listing.Gap(12f);

            // Balance Settings
            listing.Label("Balance Settings:");

            // Expensive Androids
            listing.CheckboxLabeled("Expensive Androids", ref Settings.expensiveAndroids,
                "Makes android conversion more expensive, requiring additional rare resources.");

            // Drone Needs Persona Core (only if expensive androids is enabled)
            if (Settings.expensiveAndroids)
            {
                listing.CheckboxLabeled("Drone Core Requires Persona Core", ref Settings.expensiveAndroidsDroneNeedsPersonaCore,
                    "When enabled, selecting the Drone Core upgrade will require a persona core.");
            }

            // Size Cost Scaling
            listing.CheckboxLabeled("Size-Based Cost Scaling", ref Settings.sizeCostScaling,
                "Scales resource costs based on the size of the pawn being converted.");

            listing.Gap(12f);

            // Print Time Multiplier
            listing.Label("Print Time Multiplier: " + Settings.printTimeMult + "x");
            Settings.printTimeMult = (int)listing.Slider(Settings.printTimeMult, 1, 10);

            // Base Print Time (in hours for display)
            float hours = Settings.basePrintTime / 2500f;
            listing.Label("Base Print Time: " + hours.ToString("F1") + " hours");
            hours = listing.Slider(hours, 0.5f, 10f);
            Settings.basePrintTime = (int)(hours * 2500f);

            listing.Gap(12f);

            // Max Traits to Pick
            listing.Label("Maximum Traits to Pick: " + Settings.maxTraitsToPick);
            Settings.maxTraitsToPick = (int)listing.Slider(Settings.maxTraitsToPick, 1, 15);

            // Upgrade Icon Size
            listing.Label("Upgrade Icon Size: " + Settings.upgradeBaseSize + "px");
            Settings.upgradeBaseSize = (int)listing.Slider(Settings.upgradeBaseSize, 24, 128);

            listing.Gap(12f);

            // Apply button
            if (listing.ButtonText("Apply Changes"))
            {
                UpdateGlobalVariables();
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        private void UpdateGlobalVariables()
        {
            GlenMod_AndroidGlobals.GlenMod_allowHostileConversion = Settings.allowHostileConversion;
            GlenMod_AndroidGlobals.GlenMod_allowGuestConversion = Settings.allowGuestConversion;
            GlenMod_AndroidGlobals.GlenMod_allowGuestPrisonerConversion = Settings.allowGuestPrisonerConversion;
            GlenMod_AndroidGlobals.GlenMod_expensiveAndroids = Settings.expensiveAndroids;
            GlenMod_AndroidGlobals.GlenMod_expensiveAndroidsDroneNeedsPersonaCore = Settings.expensiveAndroidsDroneNeedsPersonaCore;
            GlenMod_AndroidGlobals.GlenMod_sizeCostScaling = Settings.sizeCostScaling;
            GlenMod_AndroidGlobals.GlenMod_printTimeMult = Settings.printTimeMult;
            GlenMod_AndroidGlobals.GlenMod_basePrintTime = Settings.basePrintTime;
            GlenMod_AndroidGlobals.GlenMod_maxTraitsToPick = Settings.maxTraitsToPick;
            GlenMod_AndroidGlobals.GlenMod_upgradeBaseSize = Settings.upgradeBaseSize;
        }

        public override string SettingsCategory()
        {
            return "Android Conversion";
        }
    }
}