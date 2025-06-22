using System;
using System.Collections.Generic;
using Verse;

namespace AndroidConversion;

public class PawnConversionProperites : DefModExtension
{
	public List<ThingOrderRequest> costList;

	public int resourceTick;

	public int ticksToConvert;

	public SoundDef craftingSound;

	public GraphicData hidePawnGraphic;

	public float ResourceTicks()
	{
		return (float)Math.Ceiling((double)ticksToConvert / (double)resourceTick);
	}
}
