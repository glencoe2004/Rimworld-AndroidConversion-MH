using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AndroidConversion;

public class AndroidUpgradeGroupDef : Def
{
	public int orderID;

	[Unsaved(false)]
	private List<AndroidUpgradeDef> intCachedUpgrades;

	public IEnumerable<AndroidUpgradeDef> Upgrades
	{
		get
		{
			if (intCachedUpgrades == null)
			{
				intCachedUpgrades = new List<AndroidUpgradeDef>();
				foreach (AndroidUpgradeDef allDef in DefDatabase<AndroidUpgradeDef>.AllDefs)
				{
					if (allDef.upgradeGroup == this)
					{
						intCachedUpgrades.Add(allDef);
					}
				}
			}
			return intCachedUpgrades;
		}
	}

	public float calculateNeededHeight(Rect upgradeSize, float rowWidth)
	{
		int num = (int)Math.Floor(rowWidth / upgradeSize.width);
		return upgradeSize.height * (float)Math.Ceiling((double)Upgrades.Count() / (double)num);
	}
}
