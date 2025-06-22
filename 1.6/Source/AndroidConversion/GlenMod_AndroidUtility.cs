using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlienRace;
using RimWorld;
using Verse;

namespace AndroidConversion
{
    public static class AndroidUtility
    {
        public static void AddAndroidHediff(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("AndroidUtility.AddAndroidHediff: pawn is null");
                return;
            }

            // Check if pawn already has the android hediff
            HediffDef androidHediff = DefDatabase<HediffDef>.GetNamed("ChjAndroidLike", false);
            if (androidHediff == null)
            {
                Log.Error("AndroidUtility.AddAndroidHediff: Could not find HediffDef 'ChjAndroidLike'");
                return;
            }

            // Remove existing android hediff first to avoid duplicates
            Hediff existingAndroidHediff = pawn.health.hediffSet.GetFirstHediffOfDef(androidHediff);
            if (existingAndroidHediff != null)
            {
                pawn.health.RemoveHediff(existingAndroidHediff);
            }

            // Add the android hediff
            try
            {
                Hediff newAndroidHediff = HediffMaker.MakeHediff(androidHediff, pawn);
                pawn.health.AddHediff(newAndroidHediff);

                Log.Message($"Successfully added android hediff to {pawn.Name}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add android hediff to {pawn.Name}: {ex.Message}");
                return;
            }

            // Update portraits
            PortraitsCache.SetDirty(pawn);

            // Remove bad hediffs that shouldn't be on androids
            List<Hediff> hediffsToRemove = new List<Hediff>();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
            {
                if (hediff.def.isBad && hediff.def.defName != "ChjAndroidLike")
                {
                    hediffsToRemove.Add(hediff);
                }
            }

            foreach (Hediff hediff in hediffsToRemove)
            {
                try
                {
                    pawn.health.RemoveHediff(hediff);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Could not remove bad hediff {hediff.def.defName} from {pawn.Name}: {ex.Message}");
                }
            }

            // Notify health system of changes
            pawn.health.Notify_HediffChanged(null);
        }
    }
}