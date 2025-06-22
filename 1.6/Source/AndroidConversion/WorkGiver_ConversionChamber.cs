using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AndroidConversion;

internal class WorkGiver_ConversionChamber : WorkGiver_HaulToBiosculpterPod
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDefOf.DekConversionChamber);

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (!(t is Building_ConversionChamber { ChamberStatus: ModdingStatus.Filling } building_ConversionChamber))
		{
			return false;
		}
		if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced))
		{
			return false;
		}
		if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
		{
			return false;
		}
		IEnumerable<ThingOrderRequest> enumerable = building_ConversionChamber.orderProcessor.PendingRequests();
		bool flag = false;
		if (enumerable != null)
		{
			foreach (ThingOrderRequest request in enumerable)
			{
				if (FindIngredient(pawn, building_ConversionChamber, request) != null)
				{
					flag = true;
					break;
				}
			}
		}
		return flag;
	}

	public override Job JobOnThing(Pawn pawn, Thing printerThing, bool forced = false)
	{
		Building_ConversionChamber building_ConversionChamber = printerThing as Building_ConversionChamber;
		IEnumerable<ThingOrderRequest> enumerable = building_ConversionChamber.orderProcessor.PendingRequests();
		if (enumerable != null)
		{
			foreach (ThingOrderRequest thingOrderRequest in enumerable)
			{
				Thing thing = FindIngredient(pawn, building_ConversionChamber, thingOrderRequest);
				if (thing != null)
				{
					if (!thingOrderRequest.nutrition)
					{
						return new Job(JobDefOf.DekFillConversionChamber, thing, printerThing)
						{
							count = (int)thingOrderRequest.amount
						};
					}
					int num = (int)Math.Ceiling(thingOrderRequest.amount / thing.def.ingestible.CachedNutrition);
					if (num > 0)
					{
						return new Job(JobDefOf.DekFillConversionChamber, thing, printerThing)
						{
							count = num
						};
					}
				}
			}
		}
		return null;
	}

	private Thing FindIngredient(Pawn pawn, Building_ConversionChamber androidPrinter, ThingOrderRequest request)
	{
		if (request != null)
		{
			Predicate<Thing> extraPredicate = request.ExtraPredicate();
			Predicate<Thing> predicate = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x) && extraPredicate(x);
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, request.Request(), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, predicate, null, 0, -1, forceAllowGlobalSearch: false, RegionType.Normal);
		}
		return null;
	}
}
