using RimWorld;
using Verse;

namespace AndroidConversion
{
    [DefOf]
    public static class AndroidConversionDefOf
    {
        // ThingDefs
        public static ThingDef DekConversionChamber;
        public static ThingDef ATPP_Android3TX;
        public static ThingDef ATPP_Android4TX;
        public static ThingDef ATPP_TX3AndroidGenerator;
        public static ThingDef ATPP_TX4AndroidGenerator;

        // PawnKindDefs
        public static PawnKindDef ATPP_Android3TXKind;
        public static PawnKindDef ATPP_Android4TXKind;

        // HediffDefs
        public static HediffDef ChjAndroidLike;
        public static HediffDef ChjAndroidUpgrade_TechprofAIModule;
        public static HediffDef ChjAndroidUpgrade_ArchotechAIModule;
        public static HediffDef ChjAndroidUpgrade_DroneCore;
        public static HediffDef ChjAndroidUpgrade_Hulking;
        public static HediffDef ChjAndroidUpgrade_Agile;
        public static HediffDef ChjAndroidUpgrade_Super;
        public static HediffDef ChjAndroidUpgrade_HyperweaveSkin;
        public static HediffDef ChjAndroidUpgrade_PlasteelSkin;
        public static HediffDef ChjAndroidUpgrade_DragonScales;
        public static HediffDef ChjAndroidUpgrade_FighterModule;
        public static HediffDef ChjAndroidUpgrade_MedicalModule;
        public static HediffDef ChjAndroidUpgrade_ConstructionModule;
        public static HediffDef ChjAndroidUpgrade_CrafterModule;
        public static HediffDef ChjAndroidUpgrade_AgriculturalModule;
        public static HediffDef ChjAndroidUpgrade_ScienceModule;
        public static HediffDef ChjAndroidUpgrade_DiplomacyModule;
        public static HediffDef ChjAndroidUpgrade_VanometricCell;
        public static HediffDef ChjAndroidUpgrade_MechaniteHive;
        public static HediffDef ChjAndroidUpgrade_PsychicAttunement;

        // JobDefs
        public static JobDef DekFillConversionChamber;
        public static JobDef DekEnterConversionChamber;

        // ResearchProjectDefs
        public static ResearchProjectDef ChJAndroidUpgrade_Hub;
        public static ResearchProjectDef ChJAndroidUpgrade_Physique;
        public static ResearchProjectDef ChJAndroidUpgrade_Skin;
        public static ResearchProjectDef ChJAndroidUpgrade_Utility;
        public static ResearchProjectDef ChJAndroidUpgrade_Proficency;
        public static ResearchProjectDef ChJAndroidUpgrade_Archotech;
        public static ResearchProjectDef DekAndroidConversion;

        // AndroidModDefs and AndroidUpgradeDefs will be cached separately since they're custom defs

        static AndroidConversionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AndroidConversionDefOf));
        }
    }
}