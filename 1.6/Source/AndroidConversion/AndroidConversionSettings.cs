using Verse;

namespace AndroidConversion
{
    public class AndroidConversionSettings : ModSettings
    {
        // Conversion permission settings (from original AndroidGlobals)
        public bool allowHostileConversion = false;
        public bool allowGuestConversion = false;
        public bool allowGuestPrisonerConversion = false;

        // Settings that match the existing AndroidGlobals variables
        public bool expensiveAndroids = true;
        public bool expensiveAndroidsDroneNeedsPersonaCore = false;
        public bool sizeCostScaling = false;
        public int printTimeMult = 1;
        public int basePrintTime = 60000;
        public int maxTraitsToPick = 7;
        public int upgradeBaseSize = 48;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref allowHostileConversion, "allowHostileConversion", false);
            Scribe_Values.Look(ref allowGuestConversion, "allowGuestConversion", false);
            Scribe_Values.Look(ref allowGuestPrisonerConversion, "allowGuestPrisonerConversion", false);
            Scribe_Values.Look(ref expensiveAndroids, "expensiveAndroids", true);
            Scribe_Values.Look(ref expensiveAndroidsDroneNeedsPersonaCore, "expensiveAndroidsDroneNeedsPersonaCore", false);
            Scribe_Values.Look(ref sizeCostScaling, "sizeCostScaling", false);
            Scribe_Values.Look(ref printTimeMult, "printTimeMult", 1);
            Scribe_Values.Look(ref basePrintTime, "basePrintTime", 60000);
            Scribe_Values.Look(ref maxTraitsToPick, "maxTraitsToPick", 7);
            Scribe_Values.Look(ref upgradeBaseSize, "upgradeBaseSize", 48);
            base.ExposeData();
            
            // After loading/saving, ensure global variables are updated
            if (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.LoadingVars)
            {
                UpdateGlobalVariablesFromSettings();
            }
        }
        
        private void UpdateGlobalVariablesFromSettings()
        {
            GlenMod_AndroidGlobals.GlenMod_allowHostileConversion = allowHostileConversion;
            GlenMod_AndroidGlobals.GlenMod_allowGuestConversion = allowGuestConversion;
            GlenMod_AndroidGlobals.GlenMod_allowGuestPrisonerConversion = allowGuestPrisonerConversion;
            GlenMod_AndroidGlobals.GlenMod_expensiveAndroids = expensiveAndroids;
            GlenMod_AndroidGlobals.GlenMod_expensiveAndroidsDroneNeedsPersonaCore = expensiveAndroidsDroneNeedsPersonaCore;
            GlenMod_AndroidGlobals.GlenMod_sizeCostScaling = sizeCostScaling;
            GlenMod_AndroidGlobals.GlenMod_printTimeMult = printTimeMult;
            GlenMod_AndroidGlobals.GlenMod_basePrintTime = basePrintTime;
            GlenMod_AndroidGlobals.GlenMod_maxTraitsToPick = maxTraitsToPick;
            GlenMod_AndroidGlobals.GlenMod_upgradeBaseSize = upgradeBaseSize;
        }
    }
}