using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AndroidConversion
{
	public class ThingOrderProcessor : IExposable
	{
		public ThingOwner thingHolder;

		public StorageSettings storageSettings;

		public List<ThingOrderRequest> requestedItems = new List<ThingOrderRequest>();

		public ThingOrderProcessor()
		{
		}

		public ThingOrderProcessor(ThingOwner thingHolder, StorageSettings storageSettings)
		{
			this.thingHolder = thingHolder;
			this.storageSettings = storageSettings;
		}

		public IEnumerable<ThingOrderRequest> PendingRequests()
		{
			foreach (ThingOrderRequest requestedItem in requestedItems)
			{
				if (requestedItem.nutrition)
				{
					float num = CountNutrition();
					if (num < requestedItem.amount)
					{
						ThingOrderRequest thingOrderRequest = new ThingOrderRequest();
						thingOrderRequest.nutrition = true;
						thingOrderRequest.amount = requestedItem.amount - num;
						thingOrderRequest.thingFilter = storageSettings.filter;
						yield return thingOrderRequest;
					}
				}
				else
				{
					float num2 = thingHolder.TotalStackCountOfDef(requestedItem.thingDef);
					if (num2 < requestedItem.amount)
					{
						ThingOrderRequest thingOrderRequest2 = new ThingOrderRequest();
						thingOrderRequest2.thingDef = requestedItem.thingDef;
						thingOrderRequest2.amount = requestedItem.amount - num2;
						yield return thingOrderRequest2;
					}
				}
			}
		}

		public float CountNutrition()
		{
			float num = 0f;
			foreach (Thing item in (IEnumerable<Thing>)thingHolder)
			{
				if (item is Corpse corpse)
				{
					num += FoodUtility.GetBodyPartNutrition(corpse, corpse.InnerPawn.RaceProps.body.corePart);
				}
				else if (item.def.IsIngestible)
				{
					num += (item.def?.ingestible.CachedNutrition ?? 0.05f) * (float)item.stackCount;
				}
			}
			return num;
		}

		public void ExposeData()
		{
			Scribe_Collections.Look(ref requestedItems, "requestedItems", LookMode.Deep);
		}
	}

}
