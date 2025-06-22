using System;

namespace AndroidConversion
{
	public static class UpgradeMaker
	{
		public static UpgradeCommand Make(AndroidUpgradeDef def, AndroidConversionWindow customizationWindow = null)
		{
			UpgradeCommand upgradeCommand = (UpgradeCommand)Activator.CreateInstance(def.commandType);
			if (upgradeCommand != null)
			{
				upgradeCommand.def = def;
				upgradeCommand.customizationWindow = customizationWindow;
			}
			return upgradeCommand;
		}
	}
}