using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AlienRace;
using RimWorld;
using UnityEngine;
using Verse;
using MechHumanlikes;

namespace AndroidConversion;

public class AndroidConversionWindow : Window
{
	public bool refreshAndroidPortrait;

	public bool changedRace;

	public Building_ConversionChamber androidConverter;

	public List<ThingOrderRequest> finalConversionItemCost = new List<ThingOrderRequest>();

	public int finalConversionTimeCost;

	public Vector2 modLeftScrollPosition;

	public Vector2 modRightScrollPosition;

	public List<ModCommand> appliedUpgradeCommands = new List<ModCommand>();

	public List<Trait> originalTraits = new List<Trait>();

	public static readonly float upgradesOffset = 640f;

	private static readonly Vector2 PawnPortraitSize = new Vector2(100f, 140f);

	public Vector2 traitsScrollPosition;

	private List<Trait> allTraits = new List<Trait>();

	public Trait replacedTrait;

	public Trait newTrait;

	// Add field to track TX3 to TX4 conversion
	public bool convertTX3ToTX4 = false;

	private bool doAndroidRecipes = true;


	public override Vector2 InitialSize => new Vector2(898f, 608f);

	public AndroidConversionWindow(Building_ConversionChamber caller)
	{
		androidConverter = caller;
		FindActiveMods();
		RefreshCosts();
	}

	public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Medium;
		Pawn currentPawn = androidConverter.currentPawn;
		string text = string.Format("{0} - {1} ({2})", "ConversionMenuTitle".Translate(), currentPawn.Name.ToStringShort, currentPawn.def.LabelCap);
		float num = Text.CalcHeight(text, inRect.width);
		Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, num), text);
		Rect location = new Rect(0f, num + 10f, 268f, inRect.height - (num + 10f));
		Rect location2 = new Rect(inRect.width - 268f, num + 10f, 268f, inRect.height - (num + 10f));
		float x = (inRect.width - PawnPortraitSize.x) / 2f;
		Rect location3 = new Rect(x, num + 10f, PawnPortraitSize.x, PawnPortraitSize.y);
		Rect location4 = new Rect(location.width + 20f, location3.y + location3.height + 50f, inRect.width - (location2.width + 20f) * 2f, 58f);
		Rect location5 = new Rect(location.width + 20f, location4.y + location4.height + 20f, location4.width, 32f);
		new Rect(location.width + 20f, location3.y + location3.height + 20f, location5.width, 32f);
		Rect location6 = new Rect(location.width + 20f, location5.y + location5.height + 50f, location5.width, 32f);
		DisplayMods(physical: true, location, ref modLeftScrollPosition);
		DisplayMods(physical: false, location2, ref modRightScrollPosition);
		DisplayPawn(location3);
		DisplayCost(location4);
		DisplayButtons(location5);
		DisplayRace(location6, androidConverter.currentPawn);
		Text.Anchor = TextAnchor.UpperLeft;
	}

	public void DisplayTraits(Rect location, Rect inRect, Rect r2)
	{
		Pawn tempPawn = androidConverter.newPawn;
		if (tempPawn == null)
		{
			tempPawn = androidConverter.currentPawn;
		}
		Rect skillRerollRect = r2;
		skillRerollRect.y += r2.width + 30f;
		float row = inRect.y;
		Text.Anchor = TextAnchor.MiddleLeft;
		Text.Font = GameFont.Medium;
		Widgets.DrawTitleBG(location);
		Widgets.Label(location.ContractedBy(2f), "AndroidCustomizationTraitsLabel".Translate());
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.MiddleCenter;
		Trait traitToBeRemoved = null;
		float traitRowWidth = 256f;
		float traitRowHeight = 24f;
		float innerTraitsRectHeight = (float)(tempPawn.story.traits.allTraits.Count + 1) * traitRowHeight;
		Rect outerTraitsRect = new Rect(location);
		outerTraitsRect.y += 16f;
		outerTraitsRect.height = inRect.height - outerTraitsRect.y;
		outerTraitsRect.width += 12f;
		Rect innerTraitsRect = new Rect(outerTraitsRect);
		innerTraitsRect.height = innerTraitsRectHeight + 8f;
		Widgets.BeginScrollView(outerTraitsRect, ref traitsScrollPosition, innerTraitsRect);
		foreach (Trait trait in tempPawn.story.traits.allTraits)
		{
			Rect rowRect2 = new Rect(skillRerollRect.xMax, skillRerollRect.y, traitRowWidth, traitRowHeight);
			Widgets.DrawBox(rowRect2);
			Widgets.DrawHighlightIfMouseover(rowRect2);
			Rect traitLabelRect2 = new Rect(rowRect2);
			traitLabelRect2.width -= traitLabelRect2.height;
			Rect removeButtonRect = new Rect(rowRect2);
			removeButtonRect.width = removeButtonRect.height;
			removeButtonRect.x = traitLabelRect2.xMax;
			if (originalTraits.Any((Trait otherTrait) => otherTrait.def == trait.def && otherTrait.Degree == trait.Degree))
			{
				Widgets.Label(traitLabelRect2, "<" + trait.LabelCap + ">");
			}
			else
			{
				Widgets.Label(traitLabelRect2, trait.LabelCap);
			}
			if (Widgets.ButtonInvisible(traitLabelRect2))
			{
				PickTraitMenu(trait, tempPawn);
			}
			if (Widgets.ButtonImage(removeButtonRect, TexCommand.ForbidOn))
			{
				traitToBeRemoved = trait;
			}
			row += 26f;
		}
		Text.Anchor = TextAnchor.MiddleRight;
		Rect rowRect = new Rect(skillRerollRect.xMax, row, traitRowWidth, traitRowHeight);
		Rect traitLabelRect = new Rect(rowRect);
		traitLabelRect.width -= traitLabelRect.height;
		Rect addButtonRect = new Rect(rowRect);
		addButtonRect.width = addButtonRect.height;
		addButtonRect.x = traitLabelRect.xMax;
		Widgets.Label(traitLabelRect, "AndroidCustomizationAddTraitLabel".Translate(tempPawn.story.traits.allTraits.Count, GlenMod_AndroidGlobals.GlenMod_maxTraitsToPick));
		if (Widgets.ButtonImage(addButtonRect, TexCommand.Install) && tempPawn.story.traits.allTraits.Count < GlenMod_AndroidGlobals.GlenMod_maxTraitsToPick)
		{
			PickTraitMenu(null, tempPawn);
		}
		Widgets.EndScrollView();
		Text.Anchor = TextAnchor.UpperLeft;
		if (traitToBeRemoved != null)
		{
			tempPawn.story.traits.allTraits.Remove(traitToBeRemoved);
			RefreshSkills(tempPawn);
			traitToBeRemoved = null;
		}
	}

	public void RefreshSkills(Pawn pawn, bool addBackstoryBonuses = false)
	{
		List<SkillDef> allDefsListForReading = DefDatabase<SkillDef>.AllDefsListForReading;
		for (int i = 0; i < allDefsListForReading.Count; i++)
		{
			SkillDef skillDef = allDefsListForReading[i];
			SkillRecord srn = pawn.skills.GetSkill(skillDef);
			SkillRecord src = androidConverter.currentPawn.skills.GetSkill(skillDef);
			srn.Level = src.Level;
			srn.passion = src.passion;
			srn.xpSinceLastLevel = src.xpSinceLastLevel;
			for (int j = 0; j < pawn.story.traits.allTraits.Count; j++)
			{
				pawn.story.traits.allTraits[i].CurrentData.skillGains.FindAll((SkillGain gain) => gain.skill == skillDef).ForEach(delegate(SkillGain gain)
				{
					srn.Level += gain.amount;
				});
			}
		}
	}

	public void PickTraitMenu(Trait oldTrait, Pawn pawn)
	{
		allTraits.Clear();
		foreach (TraitDef def in DefDatabase<TraitDef>.AllDefsListForReading)
		{
			foreach (TraitDegreeData degree in def.degreeDatas)
			{
				Trait trait2 = new Trait(def, degree.degree);
				allTraits.Add(trait2);
			}
		}
		if (pawn.def is ThingDef_AlienRace alienRaceDef)
		{
			List<TraitDef> disallowedTraits = alienRaceDef?.alienRace?.generalSettings?.disallowedTraits?.Select((AlienChanceEntry<TraitWithDegree> trait) => trait.entry.def).ToList();
			if (disallowedTraits != null)
			{
				foreach (TraitDef trait4 in disallowedTraits)
				{
					allTraits.RemoveAll((Trait thisTrait) => trait4.defName == thisTrait.def.defName);
				}
			}
		}
		foreach (Trait trait3 in pawn.story.traits.allTraits)
		{
			allTraits.RemoveAll((Trait aTrait) => aTrait.def == trait3.def);
			allTraits.RemoveAll((Trait aTrait) => trait3.def.conflictingTraits.Contains(aTrait.def));
		}
		FloatMenuUtility.MakeMenu(allTraits, (Trait labelTrait) => originalTraits.Any((Trait originalTrait) => originalTrait.def == labelTrait.def && originalTrait.Degree == labelTrait.Degree) ? ((string)"AndroidCustomizationOriginalTraitFloatMenu".Translate(labelTrait.LabelCap)) : labelTrait.LabelCap, (Trait theTrait) => delegate
		{
			Trait trait5 = oldTrait;
			replacedTrait = trait5;
			newTrait = theTrait;
		});
	}

	public void DisplayPawn(Rect location)
	{
		Pawn currentPawn = ((!changedRace) ? androidConverter.currentPawn : androidConverter.newPawn);
		GUI.DrawTexture(location, PortraitsCache.Get(currentPawn, PawnPortraitSize, Rot4.South));
	}

	public void DisplayMods(bool physical, Rect location, ref Vector2 scrollPos)
	{
		float num = 32f;
		float num2 = num + 35f;
		float width = location.width;
		float height = 32f;
		float num3 = 0f;
		Rect modSize = new Rect(0f, 0f, GlenMod_AndroidGlobals.GlenMod_upgradeBaseSize, GlenMod_AndroidGlobals.GlenMod_upgradeBaseSize);
		int num4 = (int)Math.Floor(width / modSize.width);
		List<AndroidModGroupDef> list = new List<AndroidModGroupDef>();
		foreach (AndroidModGroupDef androidModGroupDef in DefDatabase<AndroidModGroupDef>.AllDefs)
		{
			if (androidModGroupDef.physical == physical)
			{
				num3 += androidModGroupDef.calculateNeededHeight(modSize, width);
				num3 += 52f;
				list.Add(androidModGroupDef);
			}
		}
		Rect rect = new Rect(location.x, num, width, height);
		Rect rect2 = new Rect(location.x, num2, width, num3);
		Rect outRect = new Rect(location.x, num2, width, location.height);
		Text.Font = GameFont.Medium;
		Text.Anchor = TextAnchor.MiddleCenter;
		if (physical)
		{
			Widgets.Label(rect, "AndroidPhysicsMods".Translate());
		}
		else
		{
			Widgets.Label(rect, "AndroidMentalMods".Translate());
		}
		Widgets.DrawLineHorizontal(rect.x, rect.y + 32f, rect.width);
		float num5 = num2;
		Text.Font = GameFont.Small;
		Text.Anchor = TextAnchor.UpperLeft;
		Widgets.BeginScrollView(outRect, ref scrollPos, rect2);
		foreach (AndroidModGroupDef androidModGroupDef2 in from modGroup in list.AsEnumerable()
			orderby modGroup.orderID
			select modGroup)
		{
			Rect rect3 = new Rect(rect);
			rect3.y = num5;
			rect3.height = 22f;
			num5 += 30f;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.DrawTitleBG(rect3);
			Widgets.Label(rect3, androidModGroupDef2.label);
			Widgets.DrawLineHorizontal(rect3.x, rect3.y + 22f, rect3.width);
			Text.Anchor = TextAnchor.UpperLeft;
			androidModGroupDef2.calculateNeededHeight(modSize, width);
			int num6 = 0;
			foreach (AndroidModDef modCurrent in androidModGroupDef2.Upgrades.OrderBy((AndroidModDef upgradeSubGroup) => upgradeSubGroup.orderID))
			{
				if (!modCurrent.enabled)
				{
					continue;
				}
				if (num6 >= num4)
				{
					num6 = 0;
					num5 += modSize.height;
				}
				Rect rect4 = new Rect(rect.x + modSize.width * (float)num6, num5, modSize.width, modSize.height);
				if (Mouse.IsOver(rect4))
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine(modCurrent.label);
					stringBuilder.AppendLine();
					stringBuilder.AppendLine(modCurrent.description);
					stringBuilder.AppendLine();
					if (modCurrent.newBodyType != null)
					{
						stringBuilder.AppendLine("AndroidCustomizationChangeBodyType".Translate());
						stringBuilder.AppendLine();
					}
					if (modCurrent.changeSkinColor)
					{
						stringBuilder.AppendLine("AndroidCustomizationChangeSkinColor".Translate());
						stringBuilder.AppendLine();
					}
					stringBuilder.AppendLine("AndroidCustomizationTimeCost".Translate() + ": " + modCurrent.extraPrintingTime.ToStringTicksToPeriodVerbose());
					if (modCurrent.requiredResearch != null && !modCurrent.requiredResearch.IsFinished)
					{
						stringBuilder.AppendLine();
						stringBuilder.AppendLine("AndroidCustomizationRequiredResearch".Translate() + ": " + modCurrent.requiredResearch.LabelCap);
					}
					TooltipHandler.TipRegion(rect4, stringBuilder.ToString());
				}
				bool flag = true;
				if (modCurrent.requiredResearch != null && !modCurrent.requiredResearch.IsFinished)
				{
					flag = false;
				}
				if (flag)
				{
					flag = !appliedUpgradeCommands.Any((ModCommand appUpgrade) => appUpgrade.def != modCurrent && (!appUpgrade.isActive || !appUpgrade.removing) && appUpgrade.def.exclusivityGroups.Any((string group) => modCurrent.exclusivityGroups.Contains(group)));
				}
				if (!flag)
				{
					Widgets.DrawRectFast(rect4, Color.red);
				}
				else if (appliedUpgradeCommands.Any((ModCommand modCommand) => modCommand.def == modCurrent))
				{
					Widgets.DrawRectFast(rect4, Color.white);
				}
				if (modCurrent.iconTexturePath != null)
				{
					Widgets.DrawTextureFitted(rect4.ContractedBy(3f), ContentFinder<Texture2D>.Get(modCurrent.iconTexturePath), 1f);
				}
				Widgets.DrawHighlightIfMouseover(rect4);
				ModCommand modCommand2 = appliedUpgradeCommands.FirstOrDefault((ModCommand ModCommand) => ModCommand.def == modCurrent);
				if (flag && Widgets.ButtonInvisible(rect4))
				{
					if (modCommand2 != null)
					{
						if (modCommand2.isActive)
						{
							modCommand2.removing = !modCommand2.removing;
						}
						else
						{
							appliedUpgradeCommands.Remove(modCommand2);
						}
					}
					else
					{
						ModCommand item = ModMaker.Make(modCurrent);
						appliedUpgradeCommands.Add(item);
					}
					RefreshCosts();
				}
				num6++;
			}
			num5 += modSize.height;
		}
		Widgets.EndScrollView();
	}

	public void DisplayCost(Rect location)
	{
		float x = location.x;
		float num = location.y;
		Text.Anchor = TextAnchor.MiddleLeft;
		Text.Font = GameFont.Medium;
		Rect rect = new Rect(x, num, 256f, 32f);
		Widgets.DrawTitleBG(rect);
		Widgets.Label(rect.ContractedBy(2f), "AndroidConversionCostLabel".Translate());
		num += rect.height;
		int num2 = 0;
		Text.Font = GameFont.Tiny;
		Text.Anchor = TextAnchor.LowerLeft;
		Rect rect3 = new Rect(x + 3f, num, 26f, 26f);
		Widgets.DrawTextureFitted(rect3, ContentFinder<Texture2D>.Get("UI/TimeControls/TimeSpeedButton_Superfast"), 1f);
		TooltipHandler.TipRegion(rect3, "AndroidConversionTimeCost".Translate() + ": " + (androidConverter.tickToMod + finalConversionTimeCost).ToStringTicksToPeriodVerbose());
		Widgets.DrawHighlightIfMouseover(rect3);
		Widgets.Label(rect3.ExpandedBy(8f), (androidConverter.tickToMod + finalConversionTimeCost).ToStringTicksToPeriodVerbose() ?? "");
		num2++;
		Text.Anchor = TextAnchor.LowerRight;
		foreach (ThingOrderRequest thingOrderRequest in finalConversionItemCost)
		{
			Rect rect2 = new Rect(x + 3f + (float)num2 * 32f, num, 26f, 26f);
			if (thingOrderRequest.nutrition)
			{
				Widgets.DefIcon(rect2, RimWorld.ThingDefOf.Meat_Human);
				TooltipHandler.TipRegion(rect2, "AndroidNutrition".Translate());
			}
			else
			{
				Widgets.DefIcon(rect2, thingOrderRequest.thingDef);
				TooltipHandler.TipRegion(rect2, thingOrderRequest.thingDef.LabelCap);
			}
			Widgets.DrawHighlightIfMouseover(rect2);
			Widgets.Label(rect2, thingOrderRequest.amount.ToString() ?? "");
			num2++;
		}
	}

	public void DisplayButtons(Rect location)
	{
		Rect rect3 = new Rect(location.x, location.y, location.width, 32f);
		Rect rect2 = new Rect(location.x, location.y + 32f, location.width, 32f);
		Text.Font = GameFont.Medium;
		if (Widgets.ButtonText(rect3, "AndroidConversionStart".Translate()))
		{
			androidConverter.savedChanges = appliedUpgradeCommands;
			StartConversion();
			Close();
		}
		if (Widgets.ButtonText(rect2, "AndroidConversionCancel".Translate()))
		{
			Close();
		}
	}

	public void DisplayRace(Rect location, Pawn current)
	{
		Text.Anchor = TextAnchor.MiddleCenter;

		// Check if current pawn is TX3
		if (current.IsTX3())
		{
			// Make it a clickable button for TX3 androids
			string buttonText = convertTX3ToTX4 ? "TX4 Android (Selected)" : "Upgrade to TX4";

			// Change button color based on selection state
			Color oldColor = GUI.color;
			if (convertTX3ToTX4)
			{
				GUI.color = Color.green;
			}

			if (Widgets.ButtonText(location, buttonText))
			{
				convertTX3ToTX4 = !convertTX3ToTX4;
				RefreshCosts(); // Refresh costs when toggling TX4 upgrade
			}

			GUI.color = oldColor;
		}
		else
		{
			// For non-TX3 pawns, just display the race name (non-interactive)
			Widgets.DrawBox(location);
			Widgets.Label(location, current.kindDef.race.LabelCap);
		}

		Text.Anchor = TextAnchor.UpperLeft; // Reset text anchor to default
	}

	private void FindActiveMods()
	{
		Pawn currentPawn = androidConverter.currentPawn;
		foreach (AndroidModGroupDef allDef in DefDatabase<AndroidModGroupDef>.AllDefs)
		{
			foreach (AndroidModDef item in allDef.Upgrades.OrderBy((AndroidModDef upgradeSubGroup) => upgradeSubGroup.orderID))
			{
				ModCommand modCommand = ModMaker.Make(item);
				if (modCommand.Active(currentPawn))
				{
					modCommand.isActive = true;
					appliedUpgradeCommands.Add(modCommand);
				}
			}
		}
	}

	private void AddCostsFromThingDef(ThingDef thingDef, bool addWorkAmount, bool isTX3ToTX4, params string[] excludeDefNames)
	{
		if (thingDef?.costList == null)
			return;

		foreach (ThingDefCountClass cost in thingDef.costList)
		{
			// Skip excluded items if any are specified
			if (excludeDefNames != null && excludeDefNames.Length > 0 &&
				excludeDefNames.Contains(cost.thingDef?.defName))
			{
				continue;
			}

			int adjustedAmount = isTX3ToTX4 ? (cost.count + 1) / 2 : cost.count;

			var thingOrderRequest = new ThingOrderRequest()
			{
				amount = adjustedAmount,
				nutrition = false,
				thingDef = cost.thingDef
			};

			finalConversionItemCost.Add(thingOrderRequest);
		}

		// Add work amount if requested and available
		if (addWorkAmount && thingDef.recipeMaker?.workAmount != null)
		{
			int workAmount = (int)thingDef.recipeMaker.workAmount;
			finalConversionTimeCost += isTX3ToTX4 ? (workAmount + 1) / 2 : workAmount;
		}
	}

	public void RefreshCosts()
	{
		finalConversionItemCost.Clear();
		finalConversionTimeCost = 0;
		if (!androidConverter.IsPawnAndroid())
		{
			PawnConversionProperites conversionProperties = androidConverter.conversionProperties;
			foreach (ThingOrderRequest thingOrderRequest in conversionProperties.costList)
			{
				ThingOrderRequest thingOrderRequest2 = new ThingOrderRequest();
				thingOrderRequest2.amount = thingOrderRequest.amount;
				thingOrderRequest2.nutrition = thingOrderRequest.nutrition;
				thingOrderRequest2.thingDef = thingOrderRequest.thingDef;
				finalConversionItemCost.Add(thingOrderRequest2);
			}
			finalConversionTimeCost += conversionProperties.ticksToConvert;
		}
		List<ThingDef> list = new List<ThingDef>();
		foreach (ModCommand modCommand in appliedUpgradeCommands)
		{
			if (!modCommand.isActive)
			{
				foreach (ThingOrderRequest costToApply in modCommand.def.costList)
				{
					ThingOrderRequest thingOrderRequest3 = finalConversionItemCost.FirstOrDefault((ThingOrderRequest finalCost) => finalCost.thingDef == costToApply.thingDef || (finalCost.nutrition && costToApply.nutrition));
					if (thingOrderRequest3 != null)
					{
						thingOrderRequest3.amount += costToApply.amount;
						continue;
					}
					thingOrderRequest3 = new ThingOrderRequest();
					thingOrderRequest3.amount = costToApply.amount;
					thingOrderRequest3.nutrition = costToApply.nutrition;
					thingOrderRequest3.thingDef = costToApply.thingDef;
					finalConversionItemCost.Add(thingOrderRequest3);
				}
				list.AddRange(modCommand.def.costsNotAffectedByBodySize);
				finalConversionTimeCost += modCommand.def.extraPrintingTime;
			}
			else if (modCommand.removing)
			{
				finalConversionTimeCost += modCommand.def.removalPrintingTime;
			}
		}
		if (list.Count > 0)
		{
			list = new List<ThingDef>(list.Distinct());
		}
		finalConversionTimeCost = (int)((float)finalConversionTimeCost * GlenMod_AndroidGlobals.GlenMod_printTimeMult);
		if (list.Count > 0)
		{
			list = new List<ThingDef>(list.Distinct());
		}
		if (GlenMod_AndroidGlobals.GlenMod_expensiveAndroids)
		{
			ThingOrderRequest uranium = new ThingOrderRequest();
			uranium.amount = 80f;
			uranium.nutrition = false;
			uranium.thingDef = RimWorld.ThingDefOf.Uranium;
			finalConversionItemCost.Add(uranium);
			ThingOrderRequest gold = new ThingOrderRequest();
			gold.amount = 40f;
			gold.nutrition = false;
			gold.thingDef = RimWorld.ThingDefOf.Gold;
			finalConversionItemCost.Add(gold);
			if (appliedUpgradeCommands.SingleOrDefault((ModCommand c) => c.def.defName == "Upgrade_DroneCore") == null && GlenMod_AndroidGlobals.GlenMod_expensiveAndroidsDroneNeedsPersonaCore)
			{
				ThingOrderRequest aiCore = new ThingOrderRequest();
				aiCore.amount = 1f;
				aiCore.nutrition = false;
				aiCore.thingDef = RimWorld.ThingDefOf.AIPersonaCore;
				finalConversionItemCost.Add(aiCore);
			}
		}
		Pawn currentPawn = androidConverter.currentPawn;
		if (!currentPawn.IsTX3() && currentPawn.IsHuman()) // No confirm for this - upgrading human to TX3 is not optional
		{
			// Use cached DefOf instead of DefDatabase lookup
			ThingDef tx3GeneratorDef = AndroidConversionDefOf.ATPP_TX3AndroidGenerator;
			if (tx3GeneratorDef != null && doAndroidRecipes)
			{
				AddCostsFromThingDef(tx3GeneratorDef, true, false, "Meat_Human");
			}
			else
			{
				// Fallback to hardcoded values if ThingDef not found
				Log.Warning("ATPP_TX3AndroidGenerator ThingDef not found in DefOf cache, using fallback costs for TX3 conversion");

				finalConversionTimeCost += 28500;

				// Plasteel
				ThingOrderRequest plasteel = new()
				{
					amount = 180f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.Plasteel
				};
				finalConversionItemCost.Add(plasteel);

				// Uranium
				ThingOrderRequest uranium = new()
				{
					amount = 80f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.Uranium
				};
				finalConversionItemCost.Add(uranium);

				// MHC_CoolantPack
				ThingOrderRequest coolantPack = new()
				{
					amount = 3f,
					nutrition = false,
					thingDef = MHC_ThingDefOf.MHC_CoolantPack
				};
				finalConversionItemCost.Add(coolantPack);

				// MHC_LubricationPack
				ThingOrderRequest lubricationPack = new()
				{
					amount = 3f,
					nutrition = false,
					thingDef = MHC_ThingDefOf.MHC_LubricationPack
				};
				finalConversionItemCost.Add(lubricationPack);

				// Gold
				ThingOrderRequest gold = new()
				{
					amount = 40f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.Gold
				};
				finalConversionItemCost.Add(gold);

				// ComponentIndustrial
				ThingOrderRequest componentIndustrial = new()
				{
					amount = 20f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.ComponentIndustrial
				};
				finalConversionItemCost.Add(componentIndustrial);

				// ComponentSpacer
				ThingOrderRequest componentSpacer = new()
				{
					amount = 12f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.ComponentSpacer
				};
				finalConversionItemCost.Add(componentSpacer);

				// MedicineIndustrial
				ThingOrderRequest medicineIndustrial = new()
				{
					amount = 4f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.MedicineIndustrial
				};
				finalConversionItemCost.Add(medicineIndustrial);
			}
		}
		// Convert TX3 to TX4
		if (currentPawn.IsTX3() && convertTX3ToTX4)
		{
			// Use cached DefOf instead of DefDatabase lookup
			ThingDef tx4GeneratorDef = AndroidConversionDefOf.ATPP_TX4AndroidGenerator;
			if (tx4GeneratorDef != null && doAndroidRecipes)
			{
				AddCostsFromThingDef(tx4GeneratorDef, true, true, "Meat_Human");
			}
			else
			{
				// Fallback to hardcoded values if ThingDef not found
				Log.Warning("ATPP_TX4AndroidGenerator ThingDef not found in DefOf cache, using fallback costs for TX4 conversion");

				finalConversionTimeCost += 18250;

				// Plasteel
				ThingOrderRequest plasteel = new()
				{
					amount = 120f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.Plasteel
				};
				finalConversionItemCost.Add(plasteel);

				// MHC_CoolantPack
				ThingOrderRequest coolantPack = new()
				{
					amount = 2f,
					nutrition = false,
					thingDef = MHC_ThingDefOf.MHC_CoolantPack
				};
				finalConversionItemCost.Add(coolantPack);

				// MHC_LubricationPack
				ThingOrderRequest lubricationPack = new()
				{
					amount = 2f,
					nutrition = false,
					thingDef = MHC_ThingDefOf.MHC_LubricationPack
				};
				finalConversionItemCost.Add(lubricationPack);

				// Gold
				ThingOrderRequest gold = new()
				{
					amount = 20f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.Gold
				};
				finalConversionItemCost.Add(gold);

				// ComponentIndustrial
				ThingOrderRequest componentIndustrial = new()
				{
					amount = 10f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.ComponentIndustrial
				};
				finalConversionItemCost.Add(componentIndustrial);

				// ComponentSpacer
				ThingOrderRequest componentSpacer = new()
				{
					amount = 10f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.ComponentSpacer
				};
				finalConversionItemCost.Add(componentSpacer);

				// AIPersonaCore
				ThingOrderRequest AIPersonaCore = new()
				{
					amount = 1f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.AIPersonaCore
				};
				finalConversionItemCost.Add(AIPersonaCore);

				// MedicineIndustrial
				ThingOrderRequest medicineIndustrial = new()
				{
					amount = 4f,
					nutrition = false,
					thingDef = RimWorld.ThingDefOf.MedicineIndustrial
				};
				finalConversionItemCost.Add(medicineIndustrial);
			}
		}
		if (!GlenMod_AndroidGlobals.GlenMod_sizeCostScaling)
		{
			return;
		}

		foreach (ThingOrderRequest item in finalConversionItemCost)
		{
			item.amount = (float)Math.Ceiling(item.amount * androidConverter.newPawn.def.race.baseBodySize);
		}
	}

	public void StartConversion()
	{
		androidConverter.orderProcessor.requestedItems = finalConversionItemCost;
		androidConverter.remainingTickTracker = (androidConverter.tickToMod = finalConversionTimeCost);
		androidConverter.ChamberStatus = ModdingStatus.Filling;

		Pawn cerPawn = androidConverter.currentPawn;

		if (!cerPawn.IsTX3() && cerPawn.IsHuman()) { // Convert human to TX3
			androidConverter.HumanToTX3 = true;
		}

		if (convertTX3ToTX4) { // Convert TX3 to TX4
			androidConverter.TX3ToTX4 = true;
		}
	}
}
