using UnityEngine;
using Verse;

namespace AndroidConversion;

[StaticConstructorOnStartup]
public class Gizmo_AbortMod : Command
{
	public Building_ConversionChamber modChamber;

	public static Texture2D initIcon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject");

	public string labelAbortMod = "AndroidGizmoAbortModLabel";

	public string labelAbortConvert = "AndroidGizmoAbortModLabel";

	public string descriptionAbortMod = "AndroidGizmoAbortModDescription";

	public string descriptionAbortConvert = "AndroidGizmoAbortModDescription";

	public Gizmo_AbortMod(Building_ConversionChamber chamber)
	{
		modChamber = chamber;
		if (modChamber.IsPawnAndroid())
		{
			defaultLabel = labelAbortMod.Translate();
			defaultDesc = descriptionAbortMod.Translate();
		}
		else
		{
			defaultLabel = labelAbortConvert.Translate();
			defaultDesc = descriptionAbortConvert.Translate();
		}
		icon = initIcon;
	}

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
		modChamber.EjectPawn();
	}
}
