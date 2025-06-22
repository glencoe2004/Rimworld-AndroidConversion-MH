using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AndroidConversion;

public class AndroidModGroupDef : Def
{
	public int orderID;

	[Unsaved(false)]
	private List<AndroidModDef> intCachedUpgrades;

	public bool physical;

	public IEnumerable<AndroidModDef> Upgrades
	{
		get
		{
			if (intCachedUpgrades == null)
			{
				intCachedUpgrades = new List<AndroidModDef>();
				foreach (AndroidModDef androidModDef in DefDatabase<AndroidModDef>.AllDefs)
				{
					if (androidModDef.modGroup == this)
					{
						intCachedUpgrades.Add(androidModDef);
					}
				}
			}
			return intCachedUpgrades;
		}
	}

	public float calculateNeededHeight(Rect modSize, float rowWidth)
	{
		int num = (int)Math.Floor(rowWidth / modSize.width);
		int num2 = Upgrades.Count((AndroidModDef mod) => mod.enabled);
		return modSize.height * (float)Math.Ceiling((double)num2 / (double)num);
	}
}
