using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AndroidConversion;

public class Building_ConversionChamber : Building, IThingHolder, IStoreSettingsParent
{
	protected bool contentsKnown;

	private CompPowerTrader _power;

	private CompPowerTrader powerComp;

	private CompFlickable flickableComp;

	public ThingOwner<Thing> ingredients = new ThingOwner<Thing>();

	public List<ModCommand> savedChanges = new List<ModCommand>();

	public ModdingStatus ChamberStatus;

	protected ThingOwner innerContainer;

	public ThingOrderProcessor orderProcessor;

	public int nextResourceTick;

	public int tickToMod;

	public int remainingTickTracker;

	private Sustainer soundSustainer;

	public StorageSettings inputSettings;

	public PawnConversionProperites conversionProperties;

	private Graphic cachedGraphicFull;

	public Pawn newPawn;

	public bool HasAnyContents => innerContainer.Count > 0;

	public bool CanOpen => HasAnyContents;

	public bool StorageTabVisible => true;

	public bool HumanToTX3 = false;

	public bool TX3ToTX4 = false;



	public Thing ContainedThing
	{
		get
		{
			if (innerContainer.Count != 0)
			{
				return innerContainer[0];
			}
			return null;
		}
	}

	public override Graphic Graphic
	{
		get
		{
			if (conversionProperties.hidePawnGraphic == null)
			{
				return base.Graphic;
			}
			if (!HasAnyContents)
			{
				return base.Graphic;
			}
			if (cachedGraphicFull == null)
			{
				cachedGraphicFull = conversionProperties.hidePawnGraphic.GraphicColoredFor(this);
			}
			return cachedGraphicFull;
		}
	}

	private CompPowerTrader PowerCompTrader => _power;

	public Pawn currentPawn => innerContainer.First() as Pawn;

	public void Notify_SettingsChanged()
	{
	}

	public Building_ConversionChamber()
	{
		innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);
		ingredients = new ThingOwner<Thing>();
		_power = GetComp<CompPowerTrader>();
	}

	public ThingOwner GetDirectlyHeldThings()
	{
		return ingredients;
	}

	public void GetChildHolders(List<IThingHolder> outChildren)
	{
		ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
	}

	public void EjectContents()
	{
		innerContainer.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
		contentsKnown = true;
		Notify_PawnEntered();
	}

	public void Open()
	{
		if (HasAnyContents)
		{
			EjectContents();
			tickToMod = 0;
		}
	}

	public virtual bool Accepts(Thing thing)
	{
		return innerContainer.CanAcceptAnyOf(thing);
	}

	public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
	{
		Pawn pawn = thing as Pawn;
		if (pawn == null)
		{
			Log.Error(base.ThingID + " accepted non pawn " + pawn.ThingID + "/" + pawn.GetType().Name + "! this should never happen");
			return false;
		}
		if (innerContainer.TryAdd(thing))
		{
			if (thing.Faction != null && thing.Faction.IsPlayer)
			{
				contentsKnown = true;
			}
			ChamberStatus = ModdingStatus.Idle;
			return true;
		}
		Log.Warning("Could not add to container");
		return false;
	}

	public StorageSettings GetStoreSettings()
	{
		return inputSettings;
	}

	public StorageSettings GetParentStoreSettings()
	{
		return def.building.fixedStorageSettings;
	}

	public void Notify_PawnEntered()
	{
		base.Map.mapDrawer.MapMeshDirty(base.Position, 0uL);
	}

	public void AdjustPowerNeed()
	{
		if (flickableComp != null && (flickableComp == null || !flickableComp.SwitchIsOn))
		{
			powerComp.PowerOutput = 0f;
		}
		else if (ChamberStatus == ModdingStatus.Modding)
		{
			powerComp.PowerOutput = 0f - powerComp.Props.PowerConsumption;
		}
		else
		{
			powerComp.PowerOutput = (0f - powerComp.Props.PowerConsumption) * 0.1f;
		}
	}

	private void ResetProcess()
	{
		ChamberStatus = ModdingStatus.WaitingForPawn;
		orderProcessor.requestedItems.Clear();
	}

	public bool IsPawnAndroid()
	{
		return currentPawn.IsAndroid();
	}

	public bool IsPawnHuman()
	{
		return currentPawn.IsHuman();
	}

	public bool IsPawnTX3()
	{
		return currentPawn.IsTX3();
	}

	public bool IsPawnTX4()
	{
		return currentPawn.IsTX4();
	}

	public void InitiatePawnModing()
	{
		newPawn = (Pawn)currentPawn.CloneObjectShallowly();
		Find.WindowStack.Add(new AndroidConversionWindow(this));
	}

	public void EjectPawn()
	{
		ingredients.TryDropAll(InteractionCell, base.Map, ThingPlaceMode.Near);
		Open();
		ResetProcess();
	}

	public void CompleteConversion()
	{
		if (newPawn == null)
		{
			Log.Error("CompleteConversion: newPawn is null");
			Open();
			ResetProcess();
			return;
		}

		// Store the old position and map before modification
		Map currentMap = currentPawn.Map;
		IntVec3 currentPos = currentPawn.Position;
		bool wasSpawned = currentPawn.Spawned;
		Faction originalFaction = currentPawn.Faction;

		// Despawn the pawn temporarily to avoid conflicts
		if (wasSpawned)
		{
			currentPawn.DeSpawn(DestroyMode.Vanish);
		}

		try
		{
			// Apply visual changes first (before race change)
			currentPawn.gender = newPawn.gender;
			currentPawn.story.headType = newPawn.story.headType;
			currentPawn.story.bodyType = newPawn.story.bodyType;
			currentPawn.style = newPawn.style;
			currentPawn.story.hairDef = newPawn.story.hairDef;
			currentPawn.story.SkinColorBase = newPawn.story.SkinColor;

			// Store age data before race change
			long ageBiological = currentPawn.ageTracker.AgeBiologicalTicks;
			long ageChronological = currentPawn.ageTracker.AgeChronologicalTicks;

			// Determine if this is a human to android conversion (for hediff transfer)
			bool isHumanToAndroid = currentPawn.IsHuman() && (HumanToTX3 || TX3ToTX4);

			// Store hediff snapshots BEFORE race change (while we still have human body structure)
			List<HediffTransferUtility.HediffSnapshot> hediffSnapshots = null;
			if (isHumanToAndroid)
			{
				hediffSnapshots = HediffTransferUtility.PrepareHediffTransfer(currentPawn);
			}

			// CRITICAL: Change race definition PROPERLY
			if (HumanToTX3)
			{
				// Use cached DefOf instead of DefDatabase lookup
				if (AndroidConversionDefOf.ATPP_Android3TX != null && AndroidConversionDefOf.ATPP_Android3TXKind != null)
				{
					currentPawn.def = AndroidConversionDefOf.ATPP_Android3TX;
					currentPawn.kindDef = AndroidConversionDefOf.ATPP_Android3TXKind;

					// Force regenerate components for new race
					currentPawn.ageTracker = new Pawn_AgeTracker(currentPawn);
					currentPawn.ageTracker.AgeBiologicalTicks = ageBiological;
					currentPawn.ageTracker.AgeChronologicalTicks = ageChronological;

					// Regenerate other critical components
					if (currentPawn.needs != null)
					{
						currentPawn.needs = new Pawn_NeedsTracker(currentPawn);
					}
				}
				else
				{
					Log.Error("Could not find TX3 ThingDef or PawnKindDef in DefOf cache");
				}
			}


			if (TX3ToTX4)
			{
				// Use cached DefOf instead of DefDatabase lookup
				if (AndroidConversionDefOf.ATPP_Android4TX != null && AndroidConversionDefOf.ATPP_Android4TXKind != null)
				{
					currentPawn.def = AndroidConversionDefOf.ATPP_Android4TX;
					currentPawn.kindDef = AndroidConversionDefOf.ATPP_Android4TXKind;

					// Force regenerate components for new race
					currentPawn.ageTracker = new Pawn_AgeTracker(currentPawn);
					currentPawn.ageTracker.AgeBiologicalTicks = ageBiological;
					currentPawn.ageTracker.AgeChronologicalTicks = ageChronological;

					// Regenerate other critical components
					if (currentPawn.needs != null)
					{
						currentPawn.needs = new Pawn_NeedsTracker(currentPawn);
					}
				}
				else
				{
					Log.Error("Could not find TX4 ThingDef or PawnKindDef in DefOf cache");
				}
			}

			// Apply android hediff AFTER race change but BEFORE mod applications
			if (HumanToTX3 || TX3ToTX4)
			{
				AndroidUtility.AddAndroidHediff(currentPawn);
			}

			// Apply transferred hediff snapshots AFTER race change is complete
			if (isHumanToAndroid && hediffSnapshots != null)
			{
				HediffTransferUtility.ApplyTransferredHediffs(currentPawn, hediffSnapshots);
			}

			// Apply saved modifications AFTER android hediff and transfers
			foreach (ModCommand modCommand in savedChanges)
			{
				try
				{
					if (modCommand.isActive)
					{
						if (modCommand.removing)
						{
							modCommand.Remove(currentPawn);
						}
					}
					else
					{
						modCommand.Apply(currentPawn);
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Error applying mod command {modCommand?.def?.defName}: {ex.Message}");
				}
			}

			// Remove bad hediffs for new androids (after all modifications)
			if (HumanToTX3)
			{
				List<Hediff> hediffsToRemove = new List<Hediff>();
				foreach (Hediff hediff in currentPawn.health.hediffSet.hediffs)
				{
					// Use cached DefOf instead of string comparison
					if (hediff.def.isBad && hediff.def != AndroidConversionDefOf.ChjAndroidLike)
					{
						hediffsToRemove.Add(hediff);
					}
				}
				foreach (Hediff hediff in hediffsToRemove)
				{
					currentPawn.health.RemoveHediff(hediff);
				}
			}

			// Ensure faction is maintained
			if (originalFaction != null)
			{
				currentPawn.SetFaction(originalFaction);
			}

			// Force complete graphics refresh
			currentPawn.Drawer.renderer.SetAllGraphicsDirty();
			PortraitsCache.SetDirty(currentPawn);

			// Respawn the pawn if it was spawned before
			if (wasSpawned && currentMap != null)
			{
				GenSpawn.Spawn(currentPawn, currentPos, currentMap);
			}

			Log.Message($"Successfully completed conversion for {currentPawn.Name}. New race: {currentPawn.def.defName}");
		}
		catch (Exception ex)
		{
			Log.Error($"Error during conversion: {ex.Message}\n{ex.StackTrace}");

			// Try to respawn the pawn even if conversion failed
			if (wasSpawned && currentMap != null && !currentPawn.Spawned)
			{
				try
				{
					GenSpawn.Spawn(currentPawn, currentPos, currentMap);
				}
				catch (Exception spawnEx)
				{
					Log.Error($"Failed to respawn pawn after conversion error: {spawnEx.Message}");
				}
			}
		}
		finally
		{
			// Reset conversion flags
			HumanToTX3 = false;
			TX3ToTX4 = false;

			// Clean up
			Open();
			ResetProcess();
		}
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		List<Gizmo> list = new List<Gizmo>(base.GetGizmos());
		if (ChamberStatus == ModdingStatus.Idle)
		{
			list.Insert(0, new Gizmo_StartMod(this));
			list.Insert(0, new Gizmo_AbortMod(this));
		}
		else if (ChamberStatus == ModdingStatus.Filling)
		{
			list.Insert(0, new Gizmo_AbortMod(this));
		}
		if (DebugSettings.godMode && (ChamberStatus == ModdingStatus.Filling || ChamberStatus == ModdingStatus.Modding))
		{
			list.Insert(0, new Command_Action
			{
				defaultLabel = "DEBUG: Finish modding.",
				defaultDesc = "Finishes modding the pawn.",
				action = delegate
				{
					ChamberStatus = ModdingStatus.Finished;
				}
			});
		}
		return list;
	}

	public bool CanEnter(Pawn testPawn)
	{
		string pawnRaceName = testPawn.kindDef.race.defName;
		if ((pawnRaceName == "Human") || GlenMod_RaceUtility.AlienRaceKinds.Any((PawnKindDef kind) => kind.race.defName == pawnRaceName))
		{
			return Accepts(testPawn);
		}
		return false;
	}

	public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
	{
		if (myPawn.IsQuestLodger())
		{
			yield return new FloatMenuOption("CannotUseReason".Translate("CryptosleepCasketGuestsNotAllowed".Translate()), null);
			yield break;
		}
		foreach (FloatMenuOption floatMenuOption in base.GetFloatMenuOptions(myPawn))
		{
			yield return floatMenuOption;
		}
		if (innerContainer.Count != 0)
		{
			yield break;
		}
		if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
		{
			yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
			yield break;
		}
		if (!CanEnter(myPawn))
		{
			yield return new FloatMenuOption("CannotBeConverted".Translate(), null);
			yield break;
		}
		// Use cached DefOf instead of DefDatabase lookup
		JobDef jobDef = AndroidConversionDefOf.DekEnterConversionChamber;
		string text = "EnterConversionChamber".Translate();
		Action action = delegate
		{
			Job job = JobMaker.MakeJob(jobDef, this);
			myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		};
		yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, action), myPawn, this);
	}

	public override void SpawnSetup(Map map, bool respawningAfterLoad)
	{
		base.SpawnSetup(map, respawningAfterLoad);
		powerComp = GetComp<CompPowerTrader>();
		flickableComp = GetComp<CompFlickable>();
		if (inputSettings == null)
		{
			inputSettings = new StorageSettings(this);
			if (def.building.defaultStorageSettings != null)
			{
				inputSettings.CopyFrom(def.building.defaultStorageSettings);
			}
		}
		conversionProperties = def.GetModExtension<PawnConversionProperites>();
		if (!respawningAfterLoad)
		{
			if (conversionProperties == null)
			{
				conversionProperties = new PawnConversionProperites();
			}
			orderProcessor = new ThingOrderProcessor(ingredients, inputSettings);
		}
		AdjustPowerNeed();
	}

	public override void PostMake()
	{
		base.PostMake();
		inputSettings = new StorageSettings(this);
		if (def.building.defaultStorageSettings != null)
		{
			inputSettings.CopyFrom(def.building.defaultStorageSettings);
		}
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.Look(ref ingredients, "ingredients");
		Scribe_Values.Look(ref ChamberStatus, "printerStatus", ModdingStatus.Idle);
		Scribe_Values.Look(ref remainingTickTracker, "printingTicksLeft", 0);
		Scribe_Values.Look(ref nextResourceTick, "nextResourceTick", 0);
		Scribe_Deep.Look(ref inputSettings, "inputSettings");
		Scribe_Deep.Look(ref orderProcessor, "orderProcessor", ingredients, inputSettings);
		Scribe_Values.Look(ref tickToMod, "totaltimeCost", 0);
		Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
		Scribe_Values.Look(ref contentsKnown, "contentsKnown", defaultValue: false);
		Scribe_Collections.Look(ref savedChanges, "savedChanges", LookMode.Deep);

		// Save conversion flags to ensure they persist
		Scribe_Values.Look(ref HumanToTX3, "HumanToTX3", false);
		Scribe_Values.Look(ref TX3ToTX4, "TX3ToTX4", false);

		// Ensure proper initialization after loading
		if (Scribe.mode == LoadSaveMode.PostLoadInit)
		{
			// Reset chamber status if it was stuck on finished
			if (ChamberStatus == ModdingStatus.Finished)
			{
				ChamberStatus = ModdingStatus.Idle;
				HumanToTX3 = false;
				TX3ToTX4 = false;
			}

			// Ensure orderProcessor exists
			if (orderProcessor == null)
			{
				orderProcessor = new ThingOrderProcessor(ingredients, inputSettings);
			}

			// Validate conversion properties
			if (conversionProperties == null)
			{
				conversionProperties = def.GetModExtension<PawnConversionProperites>();
				if (conversionProperties == null)
				{
					conversionProperties = new PawnConversionProperites();
				}
			}
		}
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if ((int)mode > 0)
		{
			ingredients.TryDropAll(base.PositionHeld, base.MapHeld, ThingPlaceMode.Near);
		}
		base.Destroy(mode);
	}

	public override string GetInspectString()
	{
		if (base.ParentHolder != null && !(base.ParentHolder is Map))
		{
			return base.GetInspectString();
		}
		StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());
		stringBuilder.AppendLine();
		string text = "AndroidModdingStatus";
		string str = "AndroidModdingStatusEnum";
		int chamberStatus = (int)ChamberStatus;
		stringBuilder.AppendLine(text.Translate((str + chamberStatus).Translate()));
		if (ChamberStatus == ModdingStatus.Modding)
		{
			stringBuilder.AppendLine("AndroidModdingProgress".Translate((((float)tickToMod - (float)remainingTickTracker) / (float)tickToMod).ToStringPercent()));
		}
		if (ChamberStatus == ModdingStatus.Filling)
		{
			bool flag = true;
			stringBuilder.Append(FormatIngredientCosts(out flag, orderProcessor.requestedItems));
			if (!flag)
			{
				stringBuilder.AppendLine();
			}
		}
		if (ingredients.Count > 0)
		{
			stringBuilder.Append("AndroidPrinterMaterials".Translate() + " ");
		}
		foreach (Thing thing in ingredients)
		{
			stringBuilder.Append(thing.LabelCap + "; ");
		}
		return stringBuilder.ToString().TrimEndNewlines();
	}

	protected override void Tick()
	{
		base.Tick();
		AdjustPowerNeed();
		if (!powerComp.PowerOn && soundSustainer != null && !soundSustainer.Ended)
		{
			soundSustainer.End();
		}
		if (flickableComp != null && (flickableComp == null || !flickableComp.SwitchIsOn))
		{
			return;
		}
		switch (ChamberStatus)
		{
		case ModdingStatus.Filling:
			handleFillingTick();
			return;
		case ModdingStatus.Modding:
			handleModdingTick();
			return;
		case ModdingStatus.Finished:
			handleFinalTick();
			return;
		}
		if (soundSustainer != null && !soundSustainer.Ended)
		{
			soundSustainer.End();
		}
	}

	public void handleFillingTick()
	{
		if (powerComp.PowerOn && Current.Game.tickManager.TicksGame % 300 == 0)
		{
			FleckMaker.ThrowSmoke(base.Position.ToVector3(), base.Map, 1f);
		}
		IEnumerable<ThingOrderRequest> enumerable = orderProcessor.PendingRequests();
		bool flag = enumerable == null;
		if (!flag && enumerable.Count() == 0)
		{
			flag = true;
		}
		if (flag)
		{
			ChamberStatus = ModdingStatus.Modding;
		}
	}

	public void handleModdingTick()
	{
		if (!powerComp.PowerOn)
		{
			return;
		}
		if (Current.Game.tickManager.TicksGame % 100 == 0)
		{
			FleckMaker.ThrowSmoke(base.Position.ToVector3(), base.Map, 1.33f);
		}
		if (Current.Game.tickManager.TicksGame % 250 == 0)
		{
			for (int i = 0; i < 3; i++)
			{
				FleckMaker.ThrowMicroSparks(base.Position.ToVector3() + new Vector3(Rand.Range(-1, 1), 0f, Rand.Range(-1, 1)), base.Map);
			}
		}
		if (soundSustainer == null || soundSustainer.Ended)
		{
			SoundDef craftingSound = conversionProperties.craftingSound;
			if (craftingSound != null && craftingSound.sustain)
			{
				SoundInfo soundInfo = SoundInfo.InMap(this, MaintenanceType.PerTick);
				soundSustainer = craftingSound.TrySpawnSustainer(soundInfo);
			}
		}
		if (soundSustainer != null && !soundSustainer.Ended)
		{
			soundSustainer.Maintain();
		}
		nextResourceTick--;
		if (nextResourceTick <= 0)
		{
			nextResourceTick = conversionProperties.resourceTick;
			foreach (ThingOrderRequest thingOrderRequest in orderProcessor.requestedItems)
			{
				if (thingOrderRequest.nutrition)
				{
					if (!(CountNutrition() > 0f))
					{
						continue;
					}
					Thing thing4 = ingredients.First((Thing thing) => thing.def.IsIngestible);
					if (thing4 == null)
					{
						continue;
					}
					int num = Math.Min((int)Math.Ceiling((double)thingOrderRequest.amount / ((double)tickToMod / (double)conversionProperties.resourceTick)), thing4.stackCount);
					Thing thing2 = null;
					if (thing4 is Corpse corpse)
					{
						if (corpse.IsDessicated())
						{
							ingredients.TryDrop(corpse, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out thing2);
							continue;
						}
						ingredients.TryDrop(corpse, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out thing2);
						Pawn innerPawn = corpse.InnerPawn;
						if (innerPawn != null)
						{
							innerPawn.equipment?.DropAllEquipment(InteractionCell, forbid: false);
							innerPawn.apparel?.DropAll(InteractionCell, forbid: false);
						}
						thing4.Destroy();
					}
					else
					{
						ingredients.Take(thing4, num).Destroy();
					}
				}
				else if (ingredients.Any((Thing thing) => thing.def == thingOrderRequest.thingDef))
				{
					Thing thing3 = ingredients.First((Thing thing) => thing.def == thingOrderRequest.thingDef);
					if (thing3 != null)
					{
						int num2 = Math.Min((int)Math.Ceiling(thingOrderRequest.amount / ((float)tickToMod / (float)conversionProperties.resourceTick)), thing3.stackCount);
						ingredients.Take(thing3, num2).Destroy();
					}
				}
			}
		}
		if (remainingTickTracker > 0)
		{
			remainingTickTracker--;
		}
		else
		{
			ChamberStatus = ModdingStatus.Finished;
		}
	}

	public void handleFinalTick()
	{
		ingredients.ClearAndDestroyContents();
		FilthMaker.TryMakeFilth(InteractionCell, base.Map, RimWorld.ThingDefOf.Filth_Slime, 5);
		ChoiceLetter choiceLetter = ((!IsPawnAndroid()) ? LetterMaker.MakeLetter("AndroidModLetterLabel".Translate(currentPawn.Name.ToStringShort), "AndroidModLetterDescription".Translate(currentPawn.Name.ToStringFull), LetterDefOf.PositiveEvent, currentPawn) : LetterMaker.MakeLetter("AndroidConvertLetterLabel".Translate(currentPawn.Name.ToStringShort), "AndroidConvertLetterDescription".Translate(currentPawn.Name.ToStringFull), LetterDefOf.PositiveEvent, currentPawn));
		Find.LetterStack.ReceiveLetter(choiceLetter);
		CompleteConversion();
	}

	public float CountNutrition()
	{
		float num = 0f;
		foreach (Thing thing in ingredients)
		{
			if (thing is Corpse corpse)
			{
				if (!corpse.IsDessicated())
				{
					num += FoodUtility.GetBodyPartNutrition(corpse, corpse.InnerPawn.RaceProps.body.corePart);
				}
			}
			else if (thing.def.IsIngestible)
			{
				num += (thing.def?.ingestible.CachedNutrition ?? 1f) * (float)thing.stackCount;
			}
		}
		return num;
	}

	public string FormatIngredientCosts(out bool needsFulfilled, IEnumerable<ThingOrderRequest> requestedItems, bool deductCosts = true)
	{
		StringBuilder stringBuilder = new StringBuilder();
		needsFulfilled = true;
		foreach (ThingOrderRequest thingOrderRequest in requestedItems)
		{
			if (thingOrderRequest.nutrition)
			{
				float num = CountNutrition();
				if (deductCosts)
				{
					float num2 = thingOrderRequest.amount - num;
					if (num2 > 0f)
					{
						stringBuilder.Append("AndroidPrinterNeed".Translate(num2, "AndroidNutrition".Translate()) + " ");
						needsFulfilled = false;
					}
				}
				else
				{
					stringBuilder.Append("AndroidPrinterNeed".Translate(thingOrderRequest.amount, "AndroidNutrition".Translate()) + " ");
				}
				continue;
			}
			int num3 = ingredients.TotalStackCountOfDef(thingOrderRequest.thingDef);
			if (deductCosts)
			{
				if ((float)num3 < thingOrderRequest.amount)
				{
					stringBuilder.Append("AndroidPrinterNeed".Translate(thingOrderRequest.amount - (float)num3, thingOrderRequest.thingDef.LabelCap) + " ");
					needsFulfilled = false;
				}
			}
			else
			{
				stringBuilder.Append("AndroidPrinterNeed".Translate(thingOrderRequest.amount, thingOrderRequest.thingDef.LabelCap) + " ");
			}
		}
		return stringBuilder.ToString();
	}
}
