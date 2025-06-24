using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;

namespace AndroidConversion
{
	public class UpgradeCommand_Skin : UpgradeCommand_Hediff
	{
		public Color originalSkinColor;
		public Color originalSkinColorTwo;

		public override void Apply(Pawn customTarget = null)
		{
			base.Apply(customTarget);

			// Only change skin color if the setting allows it
			if (def.changeSkinColor)
			{
				Pawn pawn = null;
				pawn = ((customTarget == null) ? customizationWindow.androidConverter.newPawn : customTarget);

				AlienPartGenerator.AlienComp alienComp = pawn.TryGetComp<AlienPartGenerator.AlienComp>();
				if (alienComp != null)
				{
					originalSkinColor = alienComp.ColorChannels["skin"].first;
					originalSkinColorTwo = alienComp.ColorChannels["skin"].second;
					alienComp.ColorChannels["skin"].first = def.newSkinColor;
					alienComp.ColorChannels["skin"].second = def.newSkinColor;

					if (customizationWindow != null)
					{
						customizationWindow.refreshAndroidPortrait = true;
						return;
					}
					PortraitsCache.SetDirty(pawn);
					PortraitsCache.PortraitsCacheUpdate();
				}
				else
				{
					Log.Error("alienComp is null! Impossible to alter skin color without it.");
				}
			}
		}

		public override void Undo()
		{
			base.Undo();

			// Only restore skin color if it was originally changed
			if (def.changeSkinColor)
			{
				AlienPartGenerator.AlienComp alienComp = customizationWindow.androidConverter.newPawn.TryGetComp<AlienPartGenerator.AlienComp>();
				if (alienComp != null)
				{
					alienComp.ColorChannels["skin"].first = originalSkinColor;
					alienComp.ColorChannels["skin"].second = originalSkinColorTwo;
					customizationWindow.refreshAndroidPortrait = true;
				}
				else
				{
					Log.Error("alienComp is null! Impossible to alter skin color without it.");
				}
			}
		}
	}
}