using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace AndroidConversion;

public class AndroidValueStatPart : StatPart
{
	public override string ExplanationPart(StatRequest req)
	{
		if (req.Thing is Pawn pawn)
		{
			IEnumerable<Hediff> relevantHediffs = GetRelevantHediffs(pawn);
			if (relevantHediffs == null)
			{
				return null;
			}
			List<Hediff> list = new List<Hediff>(relevantHediffs);
			if (list.Count == 0)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("AndroidMarketValueStatPartLabel".Translate());
			foreach (Hediff item in list)
			{
				stringBuilder.AppendLine("    " + item.LabelCap + ": +" + string.Format(parentStat.formatString, (float)Math.Ceiling(PriceUtility.PawnQualityPriceFactor(pawn) * CalculateMarketValueFromHediff(item, pawn.RaceProps.baseBodySize))));
			}
			return stringBuilder.ToString();
		}
		return null;
	}

	public override void TransformValue(StatRequest req, ref float val)
	{
		if (!(req.Thing is Pawn pawn))
		{
			return;
		}
		IEnumerable<Hediff> relevantHediffs = GetRelevantHediffs(pawn);
		if (relevantHediffs == null)
		{
			return;
		}
		foreach (Hediff item in new List<Hediff>(relevantHediffs))
		{
			val += (float)Math.Ceiling(PriceUtility.PawnQualityPriceFactor(pawn) * CalculateMarketValueFromHediff(item, pawn.RaceProps.baseBodySize));
		}
	}

	private float CalculateMarketValueFromHediff(Hediff hediff, float bodySize = 1f)
	{
		if (hediff == null)
		{
			Log.Error("Hediff is 'null'. This should not happen!");
			return 0f;
		}
		AndroidUpgradeHediffProperties modExtension = hediff.def.GetModExtension<AndroidUpgradeHediffProperties>();
		if (modExtension != null)
		{
			float num = 0f;
			if (modExtension.def == null)
			{
				Log.Error("Hediff '" + hediff.LabelCap + "' got 'null' properties despite having the 'AndroidUpgradeHediffProperties' DefModExtension!");
				return 0f;
			}
			foreach (ThingOrderRequest cost in modExtension.def.costList)
			{
				if (!cost.nutrition)
				{
					num = ((!modExtension.def.costsNotAffectedByBodySize.Contains(cost.thingDef)) ? (num + cost.thingDef.BaseMarketValue * cost.amount * bodySize) : (num + cost.thingDef.BaseMarketValue * cost.amount));
				}
			}
			return (float)Math.Ceiling(num);
		}
		return 0f;
	}

	private IEnumerable<Hediff> GetRelevantHediffs(Pawn pawn)
	{
		return pawn.health.hediffSet.hediffs.Where(delegate (Hediff hediff)
		{
			AndroidUpgradeHediffProperties modExtension = hediff.def.GetModExtension<AndroidUpgradeHediffProperties>();
			return modExtension != null && modExtension.def.costList.Count > 0 && (modExtension.def.costList.Count != 1 || !modExtension.def.costList[0].nutrition);
		});
	}
}
