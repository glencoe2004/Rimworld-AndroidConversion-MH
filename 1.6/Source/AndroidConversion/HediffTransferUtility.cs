using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace AndroidConversion
{
    public static class HediffTransferUtility
    {
        // Human to Android body part mapping table
        private static readonly Dictionary<string, string> HumanToAndroidBodyParts = new Dictionary<string, string>
        {
            // Core Body
            { "Torso", "ATR_MechanicalThorax" },
            { "Ribcage", "ATR_Framework" },
            { "Sternum", "ATR_Framework" },
            { "Pelvis", "ATR_Framework" },
            { "Spine", "ATR_Framework" },
            
            // Internal Organs
            { "Heart", "ATR_InternalCorePump" },
            { "Lung", "ATR_MechanicalHeatsink" },
            { "Kidney", "ATR_MechanicalHeatsink" },
            { "Liver", "ATR_InternalGenerator" },
            { "Stomach", "ATR_InternalBattery" },
            
            // Head & Neck
            { "Neck", "ATR_MechanicalNeck" },
            { "Head", "ATR_MechanicalHead" },
            { "Skull", "ATR_MechanicalHead" },
            { "Brain", "ATR_ArtificialBrain" },
            { "Eye", "ATR_MechanicalVisualSensor" },
            { "Ear", "ATR_MechanicalAudioSensor" },
            { "Nose", "ATR_SmellSensor" },
            { "Jaw", "ATR_VoiceSynthesizer" },
            { "Tongue", "ATR_VoiceSynthesizer" },
            
            // Limbs
            { "Shoulder", "ATR_MechanicalShoulder" },
            { "Clavicle", "ATR_MechanicalShoulder" },
            { "Arm", "ATR_MechanicalArm" },
            { "Humerus", "ATR_MechanicalArm" },
            { "Radius", "ATR_MechanicalArm" },
            { "Hand", "ATR_MechanicalHand" },
            { "Finger", "ATR_MechanicalFinger" },
            
            // Legs & Feet
            { "Waist", "ATR_MechanicalWaist" },
            { "Leg", "ATR_MechanicalLeg" },
            { "Femur", "ATR_MechanicalLeg" },
            { "Tibia", "ATR_MechanicalLeg" },
            { "Foot", "ATR_MechanicalFoot" },
            { "Toe", "ATR_MechanicalFoot" }
        };

        public static List<HediffSnapshot> PrepareHediffTransfer(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                Log.Warning("PrepareHediffTransfer: Invalid pawn or health data");
                return null;
            }

            if (!pawn.IsHuman())
            {
                Log.Message("Skipping hediff transfer - not a human pawn");
                return null;
            }

            Log.Message($"Preparing hediff transfer for {pawn.Name} (Human to Android)");

            // Get all current hediffs that have body parts
            List<Hediff> hediffsToTransfer = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
            {
                if (hediff.Part != null)
                {
                    hediffsToTransfer.Add(hediff);
                }
            }

            Log.Message($"Found {hediffsToTransfer.Count} hediffs with body parts to potentially transfer");

            // Store complete hediff snapshots
            List<HediffSnapshot> snapshots = new List<HediffSnapshot>();

            foreach (Hediff hediff in hediffsToTransfer)
            {
                try
                {
                    string targetBodyPartDef = GetAndroidBodyPartMapping(hediff.Part);
                    if (!string.IsNullOrEmpty(targetBodyPartDef))
                    {
                        HediffSnapshot snapshot = CreateHediffSnapshot(hediff, targetBodyPartDef);
                        if (snapshot != null)
                        {
                            snapshots.Add(snapshot);
                            Log.Message($"Captured complete snapshot: {hediff.def.defName} from {hediff.Part.Label} to {targetBodyPartDef}");
                        }
                    }
                    else
                    {
                        Log.Message($"No mapping found for body part: {hediff.Part.def.defName} ({hediff.Part.Label})");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing hediff {hediff.def.defName}: {ex.Message}");
                }
            }

            // Remove old hediffs that have body parts (they'll be replaced)
            foreach (Hediff hediff in hediffsToTransfer)
            {
                try
                {
                    pawn.health.RemoveHediff(hediff);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not remove hediff {hediff.def.defName}: {ex.Message}");
                }
            }

            return snapshots;
        }

        private static HediffSnapshot CreateHediffSnapshot(Hediff hediff, string targetBodyPartDef)
        {
            try
            {
                HediffSnapshot snapshot = new HediffSnapshot
                {
                    HediffType = hediff.GetType(),
                    HediffDef = hediff.def,
                    SourceBodyPartLabel = hediff.Part.Label,
                    TargetBodyPartDef = targetBodyPartDef,
                    FieldValues = new Dictionary<string, object>()
                };

                // Get all fields using reflection
                FieldInfo[] fields = hediff.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        // Skip fields we'll recalculate
                        if (ShouldSkipField(field.Name))
                            continue;

                        object value = field.GetValue(hediff);

                        // Handle special cases for complex objects
                        if (value != null && ShouldCopyValue(value))
                        {
                            snapshot.FieldValues[field.Name] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Could not copy field {field.Name} from hediff {hediff.def.defName}: {ex.Message}");
                    }
                }

                return snapshot;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create snapshot for hediff {hediff.def.defName}: {ex.Message}");
                return null;
            }
        }

        private static bool ShouldSkipField(string fieldName)
        {
            // Fields that should be recalculated on the new pawn
            return fieldName == "loadID" ||
                   fieldName == "part" ||  // Skip the backing field for Part property
                   fieldName == "pawn" ||
                   fieldName == "def" ||   // We set this manually
                   fieldName == "comps";  // Comps will be regenerated
        }

        private static bool ShouldCopyValue(object value)
        {
            Type valueType = value.GetType();

            // Copy primitives, strings, enums
            if (valueType.IsPrimitive || valueType == typeof(string) || valueType.IsEnum)
                return true;

            // Copy Defs (they're static references)
            if (typeof(Def).IsAssignableFrom(valueType))
                return true;

            // Copy simple value types
            if (valueType.IsValueType)
                return true;

            // Skip complex reference types that might cause issues
            // (like pawn references, comp lists, etc.)
            return false;
        }

        public static void ApplyTransferredHediffs(Pawn pawn, List<HediffSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return;
            }

            Log.Message($"Applying {snapshots.Count} transferred hediff snapshots to {pawn.Name}");

            foreach (HediffSnapshot snapshot in snapshots)
            {
                try
                {
                    BodyPartRecord targetPart = FindAndroidBodyPart(pawn, snapshot.TargetBodyPartDef, snapshot.SourceBodyPartLabel);

                    if (targetPart != null)
                    {
                        Hediff newHediff = (Hediff)Activator.CreateInstance(snapshot.HediffType);

                        // Set pawn and def first, then part
                        newHediff.pawn = pawn;
                        newHediff.def = snapshot.HediffDef;
                        newHediff.Part = targetPart;

                        // Copy all the stored field values
                        RestoreHediffFields(newHediff, snapshot.FieldValues);

                        // Ensure Part is still set correctly after field restoration
                        if (newHediff.Part == null)
                        {
                            newHediff.Part = targetPart;
                            Log.Warning($"Had to re-set Part for {snapshot.HediffDef.defName} after field restoration");
                        }

                        // Add to pawn's health
                        pawn.health.AddHediff(newHediff);

                        Log.Message($"Successfully transferred {snapshot.HediffDef.defName} to {targetPart.Label}");
                    }
                    else
                    {
                        Log.Warning($"Could not find target body part {snapshot.TargetBodyPartDef} for hediff {snapshot.HediffDef.defName}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to apply transferred hediff {snapshot.HediffDef.defName}: {ex.Message}");
                }
            }
        }

        private static void RestoreHediffFields(Hediff hediff, Dictionary<string, object> fieldValues)
        {
            Type hediffType = hediff.GetType();

            foreach (var kvp in fieldValues)
            {
                try
                {
                    FieldInfo field = hediffType.GetField(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && !field.IsInitOnly && !field.IsLiteral)
                    {
                        field.SetValue(hediff, kvp.Value);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not restore field {kvp.Key} on hediff {hediff.def.defName}: {ex.Message}");
                }
            }
        }

        private static string GetAndroidBodyPartMapping(BodyPartRecord humanPart)
        {
            if (humanPart?.def?.defName == null)
                return null;

            string partDefName = humanPart.def.defName;

            // Direct mapping check
            if (HumanToAndroidBodyParts.TryGetValue(partDefName, out string androidPart))
            {
                return androidPart;
            }

            // No mapping found
            return null;
        }

        private static BodyPartRecord FindAndroidBodyPart(Pawn pawn, string targetDefName, string originalLabel)
        {
            if (pawn?.RaceProps?.body?.AllParts == null)
                return null;

            Log.Message($"Looking for target part: {targetDefName} (from original: {originalLabel})");

            // First, try to find exact match with similar labeling
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part.def.defName == targetDefName)
                {
                    Log.Message($"Found matching def: {part.def.defName} with label: {part.Label}");

                    // For parts that should maintain left/right distinction
                    if (originalLabel.Contains("left") && part.Label.Contains("left"))
                    {
                        Log.Message($"Matched left part: {part.Label}");
                        return part;
                    }
                    else if (originalLabel.Contains("right") && part.Label.Contains("right"))
                    {
                        Log.Message($"Matched right part: {part.Label}");
                        return part;
                    }
                    // For specific finger mappings
                    else if (targetDefName == "ATR_MechanicalFinger")
                    {
                        if ((originalLabel.Contains("pinky") && part.Label.Contains("pinky")) ||
                            (originalLabel.Contains("ring") && part.Label.Contains("ring")) ||
                            (originalLabel.Contains("middle") && part.Label.Contains("middle")) ||
                            (originalLabel.Contains("index") && part.Label.Contains("index")) ||
                            (originalLabel.Contains("thumb") && part.Label.Contains("thumb")))
                        {
                            Log.Message($"Matched finger: {part.Label}");
                            return part;
                        }
                    }
                    // For parts without left/right distinction, take the first match
                    else if (!originalLabel.Contains("left") && !originalLabel.Contains("right") &&
                             !part.Label.Contains("left") && !part.Label.Contains("right"))
                    {
                        Log.Message($"Matched singular part: {part.Label}");
                        return part;
                    }
                }
            }

            // Fallback: just find the first part with the target def name
            BodyPartRecord fallback = pawn.RaceProps.body.AllParts.FirstOrDefault(part => part.def.defName == targetDefName);
            if (fallback != null)
            {
                Log.Message($"Using fallback match: {fallback.Label}");
                return fallback;
            }

            Log.Warning($"No match found for {targetDefName} (original: {originalLabel})");
            return null;
        }

        public class HediffSnapshot
        {
            public Type HediffType;
            public HediffDef HediffDef;
            public string SourceBodyPartLabel;
            public string TargetBodyPartDef;
            public Dictionary<string, object> FieldValues;
        }
    }
}