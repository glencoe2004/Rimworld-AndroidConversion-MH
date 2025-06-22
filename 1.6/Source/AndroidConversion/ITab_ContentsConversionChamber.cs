using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AndroidConversion;

public class ITab_ContentsConversionChamber : ITab_ContentsBase
{
	private List<Thing> listInt = new List<Thing>();

	public override IList<Thing> container
	{
		get
		{
			Building_ConversionChamber building_ConversionChamber = base.SelThing as Building_ConversionChamber;
			listInt.Clear();
			if (building_ConversionChamber != null && building_ConversionChamber.ContainedThing != null)
			{
				listInt.Add(building_ConversionChamber.ContainedThing);
			}
			return listInt;
		}
	}

	public ITab_ContentsConversionChamber()
	{
		labelKey = "TabConversionChamberContents";
		containedItemsKey = "ContainedItems";
		canRemoveThings = false;
	}
}
