﻿using System;
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

        // OPTIMIZATION: Cache for reflection results to avoid repeated reflection calls
        private static readonly Dictionary<Type, FieldInfo[]> TypeFieldCache = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> TypeFieldLookupCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly Dictionary<string, bool> SkipFieldCache = new Dictionary<string, bool>();
        private static readonly Dictionary<Type, bool> CopyableTypeCache = new Dictionary<Type, bool>();

        // OPTIMIZATION: Pre-compiled field skip rules for faster checking
        private static readonly HashSet<string> SkippedFieldNames = new HashSet<string>
        {
            "loadID", "part", "pawn", "def", "comps", "tickAdded"
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

        // OPTIMIZATION: Get cached fields with lookup dictionary for O(1) access
        private static FieldInfo[] GetCachedFields(Type type)
        {
            if (!TypeFieldCache.TryGetValue(type, out FieldInfo[] fields))
            {
                fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                TypeFieldCache[type] = fields;
            }
            return fields;
        }

        // OPTIMIZATION: Get cached field lookup dictionary for O(1) field access by name
        private static Dictionary<string, FieldInfo> GetCachedFieldLookup(Type type)
        {
            if (!TypeFieldLookupCache.TryGetValue(type, out Dictionary<string, FieldInfo> lookup))
            {
                FieldInfo[] fields = GetCachedFields(type);
                lookup = fields.ToDictionary(f => f.Name, f => f);
                TypeFieldLookupCache[type] = lookup;
            }
            return lookup;
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

                // OPTIMIZATION: Use cached fields instead of reflection on every call
                FieldInfo[] fields = GetCachedFields(hediff.GetType());

                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        // OPTIMIZATION: Use cached skip check
                        if (ShouldSkipFieldCached(field.Name))
                            continue;

                        object value = field.GetValue(hediff);

                        // Handle special cases for complex objects
                        if (value != null && ShouldCopyValueCached(value))
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

        // OPTIMIZATION: Cached field skip check using HashSet for O(1) lookup
        private static bool ShouldSkipFieldCached(string fieldName)
        {
            if (!SkipFieldCache.TryGetValue(fieldName, out bool shouldSkip))
            {
                shouldSkip = SkippedFieldNames.Contains(fieldName);
                SkipFieldCache[fieldName] = shouldSkip;
            }
            return shouldSkip;
        }

        // OPTIMIZATION: Cached type checking for copyable values
        private static bool ShouldCopyValueCached(object value)
        {
            Type valueType = value.GetType();

            if (!CopyableTypeCache.TryGetValue(valueType, out bool shouldCopy))
            {
                shouldCopy = DetermineCopyability(valueType);
                CopyableTypeCache[valueType] = shouldCopy;
            }

            return shouldCopy;
        }

        // OPTIMIZATION: Separate method for type copyability determination to keep cache logic clean
        private static bool DetermineCopyability(Type valueType)
        {
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
                        // Create the hediff using the proper factory method to ensure loadID is properly assigned
                        Hediff newHediff = HediffMaker.MakeHediff(snapshot.HediffDef, pawn, targetPart);

                        // Copy all the stored field values (but skip the ones that should be auto-assigned)
                        RestoreHediffFields(newHediff, snapshot.FieldValues);

                        // Ensure critical fields are set correctly after field restoration
                        if (newHediff.pawn == null)
                        {
                            newHediff.pawn = pawn;
                        }

                        if (newHediff.def == null)
                        {
                            newHediff.def = snapshot.HediffDef;
                        }

                        if (newHediff.Part == null)
                        {
                            newHediff.Part = targetPart;
                        }

                        // Add to pawn's health - this will assign a proper loadID
                        pawn.health.AddHediff(newHediff);

                        Log.Message($"Successfully transferred {snapshot.HediffDef.defName} to {targetPart.Label} with loadID {newHediff.loadID}");
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

            // OPTIMIZATION: Use cached field lookup instead of reflection on every call
            Dictionary<string, FieldInfo> fieldLookup = GetCachedFieldLookup(hediffType);

            foreach (var kvp in fieldValues)
            {
                try
                {
                    // Skip fields that should not be restored to avoid conflicts
                    if (ShouldSkipFieldCached(kvp.Key))
                        continue;

                    if (fieldLookup.TryGetValue(kvp.Key, out FieldInfo field) &&
                        !field.IsInitOnly && !field.IsLiteral)
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

        // OPTIMIZATION: Cache for body part def lookups and pawn body parts
        private static readonly Dictionary<string, BodyPartDef> BodyPartDefCache = new Dictionary<string, BodyPartDef>();
        private static readonly Dictionary<Pawn, List<BodyPartRecord>> PawnBodyPartsCache = new Dictionary<Pawn, List<BodyPartRecord>>();

        private static BodyPartRecord FindAndroidBodyPart(Pawn pawn, string targetDefName, string originalLabel)
        {
            if (pawn?.RaceProps?.body?.AllParts == null)
                return null;

            Log.Message($"Looking for target part: {targetDefName} (from original: {originalLabel})");

            // OPTIMIZATION: Cache the body parts for this pawn's race to avoid repeated access
            List<BodyPartRecord> allParts;
            if (!PawnBodyPartsCache.TryGetValue(pawn, out allParts))
            {
                allParts = pawn.RaceProps.body.AllParts.ToList();
                PawnBodyPartsCache[pawn] = allParts;
            }

            // First, try to find exact match with similar labeling
            foreach (BodyPartRecord part in allParts)
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
            BodyPartRecord fallback = allParts.FirstOrDefault(part => part.def.defName == targetDefName);
            if (fallback != null)
            {
                Log.Message($"Using fallback match: {fallback.Label}");
                return fallback;
            }

            Log.Warning($"No match found for {targetDefName} (original: {originalLabel})");
            return null;
        }

        // OPTIMIZATION: Clear caches when appropriate to prevent memory leaks
        public static void ClearCaches()
        {
            PawnBodyPartsCache.Clear();
            Log.Message("HediffTransferUtility: Cleared pawn body parts cache");
        }

        // Call this on map change or when memory usage gets high
        public static void ClearAllCaches()
        {
            TypeFieldCache.Clear();
            TypeFieldLookupCache.Clear();
            SkipFieldCache.Clear();
            CopyableTypeCache.Clear();
            PawnBodyPartsCache.Clear();
            BodyPartDefCache.Clear();
            Log.Message("HediffTransferUtility: Cleared all caches");
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