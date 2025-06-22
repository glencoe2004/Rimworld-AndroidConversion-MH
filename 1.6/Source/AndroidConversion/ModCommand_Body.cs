using AlienRace;
using Verse;

namespace AndroidConversion;

public class ModCommand_Body : ModCommand_Hediff
{
	public override void Apply(Pawn customTarget)
	{
		base.Apply(customTarget);
		ThingDef_AlienRace thingDef_AlienRace = customTarget.def as ThingDef_AlienRace;
		if (thingDef_AlienRace != null || thingDef_AlienRace.alienRace.generalSettings.alienPartGenerator.bodyTypes.Contains(def.newBodyType))
		{
			customTarget.story.bodyType = def.newBodyType;
		}
	}
}
