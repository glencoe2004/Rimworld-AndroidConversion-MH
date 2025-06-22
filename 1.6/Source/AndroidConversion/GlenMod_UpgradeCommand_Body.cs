using AlienRace;
using RimWorld;
using Verse;

namespace AndroidConversion
{
	public class UpgradeCommand_Body : UpgradeCommand_Hediff
	{
		public BodyTypeDef originalBodyType;

		public override void Apply(Pawn customTarget = null)
		{
			base.Apply(customTarget);
			Pawn pawn = null;
			pawn = ((customTarget == null) ? customizationWindow.androidConverter.newPawn : customTarget);
			originalBodyType = pawn.story.bodyType;

			if (!(pawn.def is ThingDef_AlienRace thingDef_AlienRace) || thingDef_AlienRace.alienRace.generalSettings.alienPartGenerator.bodyTypes.Contains(def.newBodyType))
			{
				pawn.story.bodyType = def.newBodyType;
			}

			if (customizationWindow != null)
			{
				customizationWindow.refreshAndroidPortrait = true;
			}
		}

		public override void Undo()
		{
			base.Undo();
			customizationWindow.androidConverter.newPawn.story.bodyType = originalBodyType;
			customizationWindow.refreshAndroidPortrait = true;
		}
	}
}