using Verse;

namespace AndroidConversion
{
    public class Hediff_MechaniteHive : HediffWithComps
    {
        // Cached values for performance
        private int ticksSinceLastHeal = 0;
        private const int HEALING_INTERVAL_BASE = 2000; // Base healing interval in ticks

        public override string TipStringExtra => "AndroidMechaniteHive".Translate();

        // Legacy Tick method - kept minimal for compatibility
        public override void Tick()
        {
            base.Tick();

            // Keep the original hash interval check for compatibility
            // but most logic moved to TickInterval for VTR optimization
            if (!pawn.IsHashIntervalTick(HEALING_INTERVAL_BASE))
            {
                return;
            }

            // Fallback healing for edge cases where TickInterval might not run
            PerformHealingCheck();
        }

        // VTR method - handles healing logic with delta timing
        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            // Accumulate ticks for healing interval
            ticksSinceLastHeal += delta;

            // Check if enough time has passed for healing attempt
            // Use the same interval as the original for consistent behavior
            if (ticksSinceLastHeal >= HEALING_INTERVAL_BASE)
            {
                PerformHealingCheck();
                ticksSinceLastHeal = 0;
            }
        }

        private void PerformHealingCheck()
        {
            // Perform the actual healing logic - moved from Tick() for reuse
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_Injury { Bleeding: not false } hediff_Injury)
                {
                    hediff_Injury.Tended(1f, 1f);
                }
            }
        }

        // Ensure proper saving/loading of VTR state
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksSinceLastHeal, "ticksSinceLastHeal", 0);
        }
    }
}