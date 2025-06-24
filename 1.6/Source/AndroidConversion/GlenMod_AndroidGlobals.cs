using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidConversion
{
    public class GlenMod_AndroidGlobals
    {
        public static readonly bool GlenMod_expensiveAndroids = true;
        public static readonly bool GlenMod_expensiveAndroidsDroneNeedsPersonaCore = GlenMod_expensiveAndroids && false;
        public static readonly bool GlenMod_sizeCostScaling = false;
        public static readonly int GlenMod_printTimeMult = 1;
        public static readonly int GlenMod_basePrintTime = 60000;

        public static readonly int GlenMod_maxTraitsToPick = 7;
        public static readonly int GlenMod_upgradeBaseSize = 48;

        // Conversion permission settings
        public static readonly bool GlenMod_allowHostileConversion = false;
        public static readonly bool GlenMod_allowGuestConversion = false;
        public static readonly bool GlenMod_allowGuestPrisonerConversion = false;
    }
}
