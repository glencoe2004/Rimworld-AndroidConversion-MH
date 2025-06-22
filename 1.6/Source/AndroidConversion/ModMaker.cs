using System;

namespace AndroidConversion;

public static class ModMaker
{
	public static ModCommand Make(AndroidModDef def)
	{
		ModCommand modCommand = (ModCommand)Activator.CreateInstance(def.modType);
		if (modCommand != null)
		{
			modCommand.def = def;
		}
		return modCommand;
	}
}
