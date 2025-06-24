using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AndroidConversion;

public class FloatMenuOptionProvider_CarryingPawnToConversionChamber : FloatMenuOptionProvider
{
	protected override bool Drafted => true;
	protected override bool Undrafted => false;
	protected override bool Multiselect => false;
	protected override bool RequiresManipulation => true;

	protected override bool AppliesInt(FloatMenuContext context)
	{
		return context.FirstSelectedPawn.IsCarryingPawn();
	}

	public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
	{
		if (!(clickedThing is Building_ConversionChamber conversionChamber))
		{
			yield break;
		}

		Pawn carriedPawn = (Pawn)context.FirstSelectedPawn.carryTracker.CarriedThing;

		if (CarryToConversionChamber(conversionChamber, context, carriedPawn, out var option))
		{
			yield return option;
		}
	}

    private bool CarryToConversionChamber(Building_ConversionChamber conversionChamber, FloatMenuContext context, Pawn carriedPawn, out FloatMenuOption option)
    {
        option = null;

        if (conversionChamber.HasAnyContents)
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "ConversionChamberOccupied".Translate(), null);
            return true;
        }

        if (!conversionChamber.CanEnter(carriedPawn))
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "CannotBeConverted".Translate(), null);
            return true;
        }

        if (context.FirstSelectedPawn.HostileTo(carriedPawn))
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "CarriedPawnHostile".Translate().CapitalizeFirst(), null);
            return true;
        }

        if (!context.FirstSelectedPawn.CanReach(conversionChamber, PathEndMode.InteractionCell, Danger.Deadly))
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            return true;
        }

        // Check quest lodgers using AndroidGlobals setting
        if (carriedPawn.IsQuestLodger() && !GlenMod_AndroidGlobals.GlenMod_allowGuestConversion)
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "ConversionChamberGuestsNotAllowed".Translate(), null);
            return true;
        }

        // Check guest prisoners using AndroidGlobals setting
        if (carriedPawn.GetExtraHostFaction() != null && !GlenMod_AndroidGlobals.GlenMod_allowGuestPrisonerConversion)
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "ConversionChamberGuestPrisonersNotAllowed".Translate(), null);
            return true;
        }

        // Check hostile pawns using AndroidGlobals setting
        if (carriedPawn.HostileTo(Faction.OfPlayer) && !GlenMod_AndroidGlobals.GlenMod_allowHostileConversion)
        {
            option = new FloatMenuOption("CannotPlaceIn".Translate(carriedPawn, conversionChamber) + ": " + "CarriedPawnHostile".Translate(), null);
            return true;
        }

        option = FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption("PlaceIn".Translate(carriedPawn, conversionChamber), delegate
            {
                conversionChamber.SetForbidden(value: false, warnOnFail: false);
                Job job = JobMaker.MakeJob(AndroidConversionDefOf.DekCarryToConversionChamberDrafted, context.FirstSelectedPawn.carryTracker.CarriedThing, conversionChamber);
                job.count = 1;
                context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }),
            context.FirstSelectedPawn, conversionChamber);
        return true;
    }
}