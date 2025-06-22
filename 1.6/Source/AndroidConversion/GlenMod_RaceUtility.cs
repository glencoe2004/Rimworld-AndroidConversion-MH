using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using MechHumanlikes;
using AlienRace;

namespace AndroidConversion
{
    public static class GlenMod_RaceUtility
    {
		private static List<PawnKindDef> alienRaceKindsint = new List<PawnKindDef>();

		private static bool alienRaceKindSearchDoneint = false;

		private static bool alienRacesFoundint = false;

		public static bool AlienRacesExist => alienRacesFoundint;

		public static IEnumerable<PawnKindDef> AlienRaceKinds
		{
			get
			{
				if (!alienRaceKindSearchDoneint)
				{
					foreach (ThingDef_AlienRace alienDef in DefDatabase<ThingDef_AlienRace>.AllDefs)
					{
						PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.AllDefs.FirstOrDefault((PawnKindDef def) => def.race == alienDef);
						if (pawnKindDef != null)
						{
							alienRaceKindsint.Add(pawnKindDef);
						}
					}
					alienRaceKindsint.RemoveAll((PawnKindDef def) => def.race.defName == "Human");
					foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
					{
						continue;
					}
					if (alienRaceKindsint.Count > 1)
					{
						alienRacesFoundint = true;
					}
					alienRaceKindSearchDoneint = true;
				}
				return alienRaceKindsint;
			}
		}

		public static bool IsAndroid(this Pawn pawn)
		{
			// If pawn has a mechanical pawn extension or has the android hediff, they are an android
			MHC_MechanicalPawnExtension pawnExtension = pawn.def.GetModExtension<MHC_MechanicalPawnExtension>();

			if (pawnExtension != null)
				return true;

			// Use DefDatabase instead of DefOf to avoid initialization timing issues
			HediffDef androidHediff = DefDatabase<HediffDef>.GetNamed("ChjAndroidLike", false);
			return androidHediff != null && pawn.health.hediffSet.HasHediff(androidHediff);
		}

		/// <summary>
		/// Determines if a pawn is a human (not an android, not an alien race)
		/// </summary>
		/// <param name="pawn">The pawn to check</param>
		/// <returns>True if the pawn is a human, false otherwise</returns>
		public static bool IsHuman(this Pawn pawn)
		{
			// First check if it's an android - androids are not human
			if (pawn.IsAndroid())
			{
				return false;
			}

			// Check if the pawn's race is specifically "Human"
			if (pawn.def.defName == "Human")
			{
				return true;
			}

			// Additional check: if it's not in the alien races list and not an android,
			// and the race defName is Human, then it's human
			return pawn.kindDef != null &&
				   pawn.kindDef.race.defName == "Human" &&
				   !AlienRaceKinds.Any(alienKind => alienKind == pawn.kindDef);
		}


		/// <summary>
		/// Determines if a pawn is a TX3 android
		/// </summary>
		/// <param name="pawn">The pawn to check</param>
		/// <returns>True if the pawn is a TX3, false otherwise</returns>
		public static bool IsTX3(this Pawn pawn)
		{
			// First check if the pawn isn't an android
			if (!pawn.IsAndroid())
			{
				return false;
			}
			// Check if the pawn's race is specifically "ATPP_Android3TX"
			if (pawn.def.defName == "ATPP_Android3TX")
			{
				return true;
			}
			// Additional check: if it's in the alien races list,
			// and the race defName is ATPP_Android3TX, then it's a TX3
			return pawn.kindDef != null &&
				   pawn.kindDef.race.defName == "ATPP_Android3TX" &&
				   AlienRaceKinds.Any(alienKind => alienKind == pawn.kindDef);
		}

		/// <summary>
		/// Determines if a pawn is a TX4 android
		/// </summary>
		/// <param name="pawn">The pawn to check</param>
		/// <returns>True if the pawn is a TX4, false otherwise</returns>
		public static bool IsTX4(this Pawn pawn)
		{
			// First check if the pawn isn't an android
			if (!pawn.IsAndroid())
			{
				return false;
			}
			// Check if the pawn's race is specifically "ATPP_Android4TX"
			if (pawn.def.defName == "ATPP_Android4TX")
			{
				return true;
			}
			// Additional check: if it's in the alien races list,
			// and the race defName is ATPP_Android4TX, then it's a TX4
			return pawn.kindDef != null &&
				   pawn.kindDef.race.defName == "ATPP_Android4TX" &&
				   AlienRaceKinds.Any(alienKind => alienKind == pawn.kindDef);
		}
	}
}
