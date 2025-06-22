using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace AndroidConversion
{
	public class UpgradeCommand_Hediff : UpgradeCommand
	{
		public List<Hediff> appliedHediffs = new List<Hediff>();

		public override void Apply(Pawn customTarget = null)
		{
			Pawn pawn = null;
			pawn = ((customTarget == null) ? customizationWindow.androidConverter.newPawn : customTarget);
			if (customTarget == null && customizationWindow == null)
			{
				Log.Error("customizationWindow is null! Impossible to add Hediffs without it.");
			}
			else
			{
				if (def.hediffToApply == null)
				{
					return;
				}
				if (def.partsToApplyTo != null)
				{
					// Cache the body parts lookup to avoid repeated calls
					var notMissingParts = pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside);

					foreach (BodyPartGroupDef item in def.partsToApplyTo)
					{
						// Filter parts once instead of checking each part individually
						var relevantParts = notMissingParts.Where(part =>
							part.IsInGroup(item) &&
							(def.partsDepth == BodyPartDepth.Undefined || part.depth == def.partsDepth));

						foreach (BodyPartRecord bodyPart in relevantParts)
						{
							Hediff hediff = HediffMaker.MakeHediff(def.hediffToApply, pawn, bodyPart);
							hediff.Severity = def.hediffSeverity;
							appliedHediffs.Add(hediff);
							pawn.health.AddHediff(hediff);
						}
					}
					return;
				}
				Hediff hediff2 = HediffMaker.MakeHediff(def.hediffToApply, pawn);
				hediff2.Severity = def.hediffSeverity;
				appliedHediffs.Add(hediff2);
				pawn.health.AddHediff(hediff2);
			}
		}

		public override string GetExplanation()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(def.hediffToApply.ConcreteExample.TipStringExtra);
			return stringBuilder.ToString();
		}

		public override void Undo()
		{
			if (customizationWindow == null)
			{
				Log.Error("customizationWindow is null! Impossible to remove Hediffs without it.");
				return;
			}

			// Remove all applied hediffs in batch
			foreach (Hediff appliedHediff in appliedHediffs)
			{
				customizationWindow.androidConverter.newPawn.health.RemoveHediff(appliedHediff);
			}
			appliedHediffs.Clear();
		}
	}
}