using Verse;

namespace AndroidConversion
{
	public class Hediff_MechaniteHive : HediffWithComps
	{
		public override string TipStringExtra => "AndroidMechaniteHive".Translate();

		public override void Tick()
		{
			base.Tick();
			if (!pawn.IsHashIntervalTick(2000))
			{
				return;
			}
			foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
			{
				if (hediff is Hediff_Injury { Bleeding: not false } hediff_Injury)
				{
					hediff_Injury.Tended(1f, 1f);
				}
			}
		}
	}
}