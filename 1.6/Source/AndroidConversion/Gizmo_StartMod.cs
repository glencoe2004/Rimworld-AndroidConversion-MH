using UnityEngine;
using Verse;

namespace AndroidConversion;

[StaticConstructorOnStartup]
public class Gizmo_StartMod : Command
{
	public Building_ConversionChamber modChamber;

	public static Texture2D initIcon = ContentFinder<Texture2D>.Get("Icons/Widgets/vitruvian-man");

	public string labelInitMod = "AndroidGizmoInitModLabel";

	public string labelInitConvert = "AndroidGizmoInitConvertLabel";

	public string descriptionInitMod = "AndroidGizmoInitModDescription";

	public string descriptionInitConvert = "AndroidGizmoInitConvertDescription";

	public Gizmo_StartMod(Building_ConversionChamber chamber)
	{
		modChamber = chamber;
		if (modChamber.IsPawnAndroid())
		{
			defaultLabel = labelInitMod.Translate();
			defaultDesc = descriptionInitMod.Translate();
		}
		else
		{
			defaultLabel = labelInitConvert.Translate();
			defaultDesc = descriptionInitConvert.Translate();
		}
		icon = initIcon;
	}

	public override void ProcessInput(Event ev)
	{
		base.ProcessInput(ev);
		modChamber.InitiatePawnModing();
	}
}
