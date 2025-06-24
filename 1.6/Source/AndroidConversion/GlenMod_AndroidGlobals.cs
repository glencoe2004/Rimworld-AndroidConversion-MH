using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndroidConversion
{
    public class GlenMod_AndroidGlobals
    {
        // Conversion permission settings (now modifiable by settings)
        public static bool GlenMod_allowHostileConversion = false;
        public static bool GlenMod_allowGuestConversion = false;
        public static bool GlenMod_allowGuestPrisonerConversion = false;

        // These are now modifiable by the settings system
        public static bool GlenMod_expensiveAndroids = true;
        public static bool GlenMod_expensiveAndroidsDroneNeedsPersonaCore = false;
        public static bool GlenMod_sizeCostScaling = false;
        public static int GlenMod_printTimeMult = 1;
        public static int GlenMod_basePrintTime = 60000;
        public static int GlenMod_maxTraitsToPick = 7;
        public static int GlenMod_upgradeBaseSize = 48;

        // Static constructor to initialize from settings
        static GlenMod_AndroidGlobals()
        {
            // Values will be updated when the settings handler initializes
        }
    }
}