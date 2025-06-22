using RimWorld;
using Verse;

namespace AndroidConversion
{
	public class Hediff_VanometricCell : HediffWithComps
	{
		public override string TipStringExtra => "AndroidHediffVanometricCell".Translate();

		public override void Tick()
		{
			base.Tick();
			Need_Food need_Food = pawn?.needs?.food;
			if (need_Food != null)
			{
				need_Food.CurLevel = need_Food.MaxLevel;
			}

		}
	}
}