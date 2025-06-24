using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace AndroidConversion;

public class FloatMenuOptionProvider_CarryToConversionChamber : FloatMenuOptionProvider
{
	protected override bool Drafted => true;
	protected override bool Undrafted => true;
	protected override bool Multiselect => false;
	protected override bool RequiresManipulation => true;

	protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
	{
		if (!clickedPawn.Downed)
		{
			return null;
		}

		if (!context.FirstSelectedPawn.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations: true))
		{
			return null;
		}

		Building_ConversionChamber conversionChamber = FindConversionChamberFor(clickedPawn, context.FirstSelectedPawn, ignoreOtherReservations: true);
		if (conversionChamber == null)
		{
			return null;
		}

		TaggedString taggedString = "CarryToConversionChamber".Translate(clickedPawn.LabelCap, clickedPawn);

		if (clickedPawn.IsQuestLodger())
		{
			return FloatMenuUtility.DecoratePrioritizedTask(
				new FloatMenuOption(taggedString + " (" + "ConversionChamberGuestsNotAllowed".Translate() + ")", null, MenuOptionPriority.Default, null, clickedPawn),
				context.FirstSelectedPawn, clickedPawn);
		}

		if (clickedPawn.GetExtraHostFaction() != null)
		{
			return FloatMenuUtility.DecoratePrioritizedTask(
				new FloatMenuOption(taggedString + " (" + "ConversionChamberGuestPrisonersNotAllowed".Translate() + ")", null, MenuOptionPriority.Default, null, clickedPawn),
				context.FirstSelectedPawn, clickedPawn);
		}

		if (!conversionChamber.CanEnter(clickedPawn))
		{
			return FloatMenuUtility.DecoratePrioritizedTask(
				new FloatMenuOption(taggedString + " (" + "CannotBeConverted".Translate() + ")", null, MenuOptionPriority.Default, null, clickedPawn),
				context.FirstSelectedPawn, clickedPawn);
		}

		Action action = delegate
		{
			Building_ConversionChamber chamber = FindConversionChamberFor(clickedPawn, context.FirstSelectedPawn);
			if (chamber == null)
			{
				chamber = FindConversionChamberFor(clickedPawn, context.FirstSelectedPawn, ignoreOtherReservations: true);
			}
			if (chamber == null)
			{
				Messages.Message("CannotCarryToConversionChamber".Translate() + ": " + "NoConversionChamber".Translate(), clickedPawn, MessageTypeDefOf.RejectInput, historical: false);
			}
			else
			{
				Job job = JobMaker.MakeJob(AndroidConversionDefOf.DekCarryToConversionChamber, clickedPawn, chamber);
				job.count = 1;
				context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			}
		};

		return FloatMenuUtility.DecoratePrioritizedTask(
			new FloatMenuOption(taggedString, action, MenuOptionPriority.Default, null, clickedPawn),
			context.FirstSelectedPawn, clickedPawn);
	}

	public static Building_ConversionChamber FindConversionChamberFor(Pawn pawn, Pawn traveler, bool ignoreOtherReservations = false)
	{
		foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs)
		{
			if (thingDef.thingClass == typeof(Building_ConversionChamber))
			{
				foreach (Building building in pawn.Map.listerBuildings.AllBuildingsColonistOfDef(thingDef))
				{
					Building_ConversionChamber conversionChamber = building as Building_ConversionChamber;
					if (conversionChamber != null &&
						!conversionChamber.HasAnyContents &&
						conversionChamber.CanEnter(pawn) &&
						traveler.CanReserveAndReach(conversionChamber, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations))
					{
						return conversionChamber;
					}
				}
			}
		}
		return null;
	}
}