using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AndroidConversion;

internal class JobDriver_FillConversionChamber : JobDriver
{
	private const TargetIndex CarryThingIndex = TargetIndex.A;

	private const TargetIndex DestIndex = TargetIndex.B;

	private Building_ConversionChamber conversionChamber => (Building_ConversionChamber)(Thing)job.GetTarget(TargetIndex.B);

	public override string GetReport()
	{
		Thing thing = ((pawn.carryTracker.CarriedThing == null) ? base.TargetThingA : pawn.carryTracker.CarriedThing);
		return "ReportHaulingTo".Translate(thing.LabelCap, job.targetB.Thing.LabelShort);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (!pawn.CanReserve(base.TargetA))
		{
			return false;
		}
		if (!pawn.CanReserve(base.TargetB))
		{
			return false;
		}
		pawn.Reserve(base.TargetA, job, 1, -1, null, errorOnFailed);
		pawn.Reserve(base.TargetB, job, 1, -1, null, errorOnFailed);
		return true;
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
		this.FailOn(() => conversionChamber.ChamberStatus != ModdingStatus.Filling);
		yield return Toils_Reserve.Reserve(TargetIndex.A);
		yield return Toils_Reserve.ReserveQueue(TargetIndex.A);
		yield return Toils_Reserve.Reserve(TargetIndex.B);
		yield return Toils_Reserve.ReserveQueue(TargetIndex.B);
		Toil getToHaulTarget = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		yield return getToHaulTarget;
		yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: true);
		yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue(getToHaulTarget, TargetIndex.A);
		yield return Toils_Haul.CarryHauledThingToContainer();
	}
}
