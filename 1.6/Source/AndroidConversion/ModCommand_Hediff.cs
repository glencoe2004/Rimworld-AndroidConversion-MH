using System.Linq;
using System.Text;
using Verse;

namespace AndroidConversion;

public class ModCommand_Hediff : ModCommand
{
	public override bool Active(Pawn customTarget)
	{
		if (def.hediffToApply != null)
		{
			// Use Any() instead of FirstOrDefault() for better performance when we only need to check existence
			return customTarget.health.hediffSet.hediffs.Any(hediff => hediff.def == def.hediffToApply);
		}
		return false;
	}

	public override void Apply(Pawn customTarget)
	{
		if (customTarget == null)
		{
			Log.Error("customTarget is null! Impossible to add Hediffs without it.");
		}
		else
		{
			if (def.hediffToApply == null)
			{
				return;
			}
			if (def.partsToApplyTo != null)
			{
				foreach (BodyPartGroupDef bodyPartGroupDef in def.partsToApplyTo)
				{
					foreach (BodyPartRecord bodyPartRecord in customTarget.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside))
					{
						if (bodyPartRecord.IsInGroup(bodyPartGroupDef))
						{
							_ = def.partsDepth;
							if (bodyPartRecord.depth == def.partsDepth)
							{
								Hediff hediff = HediffMaker.MakeHediff(def.hediffToApply, customTarget, bodyPartRecord);
								hediff.Severity = def.hediffSeverity;
								customTarget.health.AddHediff(hediff);
							}
						}
					}
				}
				return;
			}
			Hediff hediff2 = HediffMaker.MakeHediff(def.hediffToApply, customTarget);
			hediff2.Severity = def.hediffSeverity;
			customTarget.health.AddHediff(hediff2);
		}
	}

	public override void Remove(Pawn customTarget)
	{
		if (customTarget == null)
		{
			Log.Error("customTarget is null! Impossible to remove Hediffs without it.");
		}
		else
		{
			if (def.hediffToApply == null)
			{
				return;
			}

			// Collect all hediffs to remove first, then remove them
			// This avoids modifying the collection while iterating
			var hediffsToRemove = customTarget.health.hediffSet.hediffs
				.Where(hediff => hediff.def == def.hediffToApply)
				.ToList();

			foreach (Hediff hediff in hediffsToRemove)
			{
				customTarget.health.RemoveHediff(hediff);
			}
		}
	}

	public override string GetExplanation()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(def.hediffToApply.ConcreteExample.TipStringExtra);
		return stringBuilder.ToString();
	}
}