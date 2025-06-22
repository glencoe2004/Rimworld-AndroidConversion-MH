using System;
using System.Xml;
using RimWorld;
using Verse;

namespace AndroidConversion;

public class ThingOrderRequest : IExposable
{
	public ThingDef thingDef;

	public bool nutrition;

	public ThingFilter thingFilter;

	public float amount;

	public ThingRequest Request()
	{
		if (thingDef != null)
		{
			return ThingRequest.ForDef(thingDef);
		}
		if (nutrition)
		{
			return ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree);
		}
		return ThingRequest.ForUndefined();
	}

	public Predicate<Thing> ExtraPredicate()
	{
		if (nutrition)
		{
			if (thingFilter == null)
			{
				return delegate (Thing thing)
				{
					ThingDef def = thing.def;
					return def != null && !def.ingestible.IsMeal && thing.def.IsNutritionGivingIngestible;
				};
			}
			return (Thing thing) => thingFilter.Allows(thing) && thing.def.IsNutritionGivingIngestible && ((!(thing is Corpse t) || !t.IsDessicated()) ? true : false);
		}
		return (Thing thing) => true;
	}

	public void LoadDataFromXmlCustom(XmlNode xmlRoot)
	{
		if (xmlRoot.ChildNodes.Count != 1)
		{
			Log.Error("Misconfigured ThingOrderRequest: " + xmlRoot.OuterXml);
			return;
		}
		if (xmlRoot.Name.ToLower() == "nutrition")
		{
			nutrition = true;
		}
		else
		{
			DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
		}
		amount = (float)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(float));
	}

	public void ExposeData()
	{
		Scribe_Defs.Look(ref thingDef, "thingDef");
		Scribe_Values.Look(ref nutrition, "nutrition", defaultValue: false);
		Scribe_Values.Look(ref amount, "amount", 0f);
	}
}
