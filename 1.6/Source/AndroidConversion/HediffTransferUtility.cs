using System;
using System.Collections.Generic;
using System.Linq;
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

        public static List<HediffTransferData> PrepareHediffTransfer(Pawn pawn)
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

            // Store hediff data for transfer
            List<HediffTransferData> transferData = new List<HediffTransferData>();

            foreach (Hediff hediff in hediffsToTransfer)
            {
                try
                {
                    string targetBodyPartDef = GetAndroidBodyPartMapping(hediff.Part);
                    if (!string.IsNullOrEmpty(targetBodyPartDef))
                    {
                        transferData.Add(new HediffTransferData
                        {
                            HediffDef = hediff.def,
                            Severity = hediff.Severity,
                            SourceBodyPartLabel = hediff.Part.Label,
                            TargetBodyPartDef = targetBodyPartDef,
                            IsPermanent = hediff.IsPermanent(),
                            SourceDef = hediff.sourceDef,
                            SourceLabel = hediff.sourceLabel
                        });

                        Log.Message($"Queued transfer: {hediff.def.defName} from {hediff.Part.Label} to {targetBodyPartDef}");
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

            return transferData;
        }

        public static void ApplyTransferredHediffs(Pawn pawn, List<HediffTransferData> transferData)
        {
            if (transferData == null || transferData.Count == 0)
            {
                return;
            }

            Log.Message($"Applying {transferData.Count} transferred hediffs to {pawn.Name}");

            foreach (HediffTransferData data in transferData)
            {
                try
                {
                    BodyPartRecord targetPart = FindAndroidBodyPart(pawn, data.TargetBodyPartDef, data.SourceBodyPartLabel);

                    if (targetPart != null)
                    {
                        Hediff newHediff = HediffMaker.MakeHediff(data.HediffDef, pawn, targetPart);
                        newHediff.Severity = data.Severity;

                        // Preserve source information if applicable
                        if (data.SourceDef != null)
                        {
                            newHediff.sourceDef = data.SourceDef;
                        }
                        if (!string.IsNullOrEmpty(data.SourceLabel))
                        {
                            newHediff.sourceLabel = data.SourceLabel;
                        }

                        pawn.health.AddHediff(newHediff);

                        Log.Message($"Successfully transferred {data.HediffDef.defName} to {targetPart.Label}");
                    }
                    else
                    {
                        Log.Warning($"Could not find target body part {data.TargetBodyPartDef} for hediff {data.HediffDef.defName}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to apply transferred hediff {data.HediffDef.defName}: {ex.Message}");
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

            // Debug: Log all available body parts
            Log.Message($"DEBUG: Available body parts for {pawn.def.defName}:");
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                Log.Message($"  - {part.def.defName} ({part.Label})");
            }

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

        public class HediffTransferData
        {
            public HediffDef HediffDef;
            public float Severity;
            public string SourceBodyPartLabel;
            public string TargetBodyPartDef;
            public bool IsPermanent;
            public ThingDef SourceDef;
            public string SourceLabel;
        }
    }
}