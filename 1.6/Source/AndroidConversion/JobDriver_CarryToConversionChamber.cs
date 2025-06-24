using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AndroidConversion;

public class JobDriver_CarryToConversionChamber : JobDriver
{
	private const TargetIndex TakeeInd = TargetIndex.A;
	private const TargetIndex ConversionChamberInd = TargetIndex.B;

	protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;
	protected Building_ConversionChamber ConversionChamber => (Building_ConversionChamber)job.GetTarget(TargetIndex.B).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed))
		{
			return pawn.Reserve(ConversionChamber, job, 1, -1, null, errorOnFailed);
		}
		return false;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnDestroyedOrNull(TargetIndex.B);
		this.FailOnAggroMentalState(TargetIndex.A);
		this.FailOn(() => !ConversionChamber.Accepts(Takee));

		Toil goToTakee = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
			.FailOnDestroyedNullOrForbidden(TargetIndex.A)
			.FailOnDespawnedNullOrForbidden(TargetIndex.B)
			.FailOn(() => ConversionChamber.HasAnyContents)
			.FailOn(() => !pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly))
			.FailOnSomeonePhysicallyInteracting(TargetIndex.A);

		Toil startCarryingTakee = Toils_Haul.StartCarryThing(TargetIndex.A);
		Toil goToThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);

		yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));
		yield return goToTakee;
		yield return startCarryingTakee;
		yield return goToThing;

		Toil waitToil = Toils_General.Wait(500, TargetIndex.B);
		waitToil.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
		waitToil.WithProgressBarToilDelay(TargetIndex.B);
		yield return waitToil;

        Toil placeInChamber = new Toil();
        placeInChamber.initAction = delegate
        {
            Pawn carriedPawn = Takee as Pawn;

            // Check if the carried pawn's faction is neutral and make it hostile before placing
            if (carriedPawn != null &&
                carriedPawn.Faction != null &&
                carriedPawn.Faction != Faction.OfPlayer &&
                carriedPawn.Faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Neutral)
            {
                // Make the faction hostile to the player
                if (carriedPawn.Faction.HasGoodwill)
                {
                    // For factions with goodwill system, reduce goodwill to hostile level
                    int currentGoodwill = carriedPawn.Faction.GoodwillWith(Faction.OfPlayer);
                    int goodwillChangeNeeded = -75 - currentGoodwill - 1; // -1 to ensure it goes below -75
                    Faction.OfPlayer.TryAffectGoodwillWith(carriedPawn.Faction, goodwillChangeNeeded, canSendMessage: true, canSendHostilityLetter: true);
                    Log.Message($"Neutral pawn {carriedPawn.Name} being placed in conversion chamber - reduced {carriedPawn.Faction.Name} goodwill to hostile level");
                }
                else
                {
                    // For factions without goodwill, use direct relation setting
                    carriedPawn.Faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, canSendHostilityLetter: true, reason: "Converted {0}");
                    Log.Message($"Neutral pawn {carriedPawn.Name} being placed in conversion chamber - set faction {carriedPawn.Faction.Name} to hostile");
                }
            }

            // Original logic to handle the transfer properly
            if (pawn.carryTracker.CarriedThing == Takee)
            {
                // Transfer directly from carry tracker to conversion chamber
                if (pawn.carryTracker.innerContainer.TryTransferToContainer(Takee, ConversionChamber.innerContainer))
                {
                    // Set faction if needed (from TryAcceptThing logic)
                    if (Takee.Faction != null && Takee.Faction.IsPlayer)
                    {
                        ConversionChamber.contentsKnown = true;
                    }

                    // Set the chamber status to idle (from TryAcceptThing logic)
                    ConversionChamber.ChamberStatus = ModdingStatus.Idle;

                    // Notify the conversion chamber that a pawn has entered
                    ConversionChamber.Notify_PawnEntered();
                }
            }
            else
            {
                // Fallback if not carried (shouldn't happen but just in case)
                ConversionChamber.TryAcceptThing(Takee);
            }
        };
        placeInChamber.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return placeInChamber;
	}

	public override object[] TaleParameters()
	{
		return new object[2] { pawn, Takee };
	}
}