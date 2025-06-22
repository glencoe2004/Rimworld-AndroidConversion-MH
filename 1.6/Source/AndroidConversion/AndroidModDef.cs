using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AndroidConversion;

public class AndroidModDef : Def
{
	public AndroidModGroupDef modGroup;

	public bool enabled = true;

	public Type modType = typeof(ModCommand_Hediff);

	public int removalPrintingTime;

	public bool removable = true;

	public int orderID;

	public string iconTexturePath;

	public List<ThingOrderRequest> costList = new List<ThingOrderRequest>();

	public List<ThingDef> costsNotAffectedByBodySize = new List<ThingDef>();

	public int extraPrintingTime;

	public List<HediffApplication> hediffs = new List<HediffApplication>();

	public HediffDef hediffToApply;

	public float hediffSeverity = 1f;

	public List<BodyPartGroupDef> partsToApplyTo;

	public BodyPartDepth partsDepth;

	public List<string> exclusivityGroups = new List<string>();

	public BodyTypeDef newBodyType;

	public bool changeSkinColor;

	public Color newSkinColor = new Color(1f, 1f, 1f);

	public ResearchProjectDef requiredResearch;

	public List<string> spawnInBackstories = new List<string>();

	public bool invasive = true;
}
