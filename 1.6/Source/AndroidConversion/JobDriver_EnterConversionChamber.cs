using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace AndroidConversion;

public class JobDriver_EnterConversionChamber : JobDriver
{
	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
	}

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        Toil toil = Toils_General.Wait(20);
        toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        toil.WithProgressBarToilDelay(TargetIndex.A);
        yield return toil;

        Toil enter = new Toil();
        enter.initAction = delegate
        {
            Pawn actor = this.pawn; // Instead of the mangled reference
            Building_ConversionChamber conversionChamber = (Building_ConversionChamber)actor.CurJob.targetA.Thing;

            Action action = delegate
            {
                actor.DeSpawn(DestroyMode.Vanish);
                conversionChamber.TryAcceptThing(actor);
                conversionChamber.Notify_PawnEntered();
            };

            if (conversionChamber.def.building.isPlayerEjectable)
            {
                action();
            }
            else if (base.Map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount <= 1)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "CasketWarning".Translate(actor.Named("PAWN")).AdjustedFor(actor),
                    action));
            }
            else
            {
                action();
            }
        };
        enter.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return enter;
    }
}
