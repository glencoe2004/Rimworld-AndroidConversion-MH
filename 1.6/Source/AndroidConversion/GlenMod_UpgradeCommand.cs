using UnityEngine;
using Verse;

namespace AndroidConversion
{
	public abstract class UpgradeCommand
	{
		public AndroidUpgradeDef def;

		public AndroidConversionWindow customizationWindow;

		public abstract void Apply(Pawn customTarget = null);

		public abstract void Undo();

		public virtual void Notify_UpgradeAdded()
		{
		}

		public virtual void ExtraOnGUI(Rect inRect)
		{
		}

		public abstract string GetExplanation();
	}
}