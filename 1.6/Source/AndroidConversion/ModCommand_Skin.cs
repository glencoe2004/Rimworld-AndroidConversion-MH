using AlienRace;
using Verse;

namespace AndroidConversion;

public class ModCommand_Skin : ModCommand_Hediff
{
	public override void Apply(Pawn customTarget)
	{
		base.Apply(customTarget);
		AlienPartGenerator.AlienComp alienComp = customTarget.TryGetComp<AlienPartGenerator.AlienComp>();
		if (alienComp != null)
		{
			alienComp.ColorChannels["skin"].first = def.newSkinColor;
			alienComp.ColorChannels["skin"].second = def.newSkinColor;
		}
		else
		{
			Log.Error("alienComp is null! Impossible to alter skin color without it.");
		}
	}
}
