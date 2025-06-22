using RimWorld;
using UnityEngine;
using Verse;

namespace AndroidConversion
{
	public class ITab_ConversionChamber : ITab
	{
		private const float TopAreaHeight = 35f;

		private ThingFilterUI.UIState state;

		private static readonly Vector2 WinSize = new Vector2(300f, 480f);

		private IStoreSettingsParent SelStoreSettingsParent => (IStoreSettingsParent)base.SelObject;

		public override bool IsVisible => SelStoreSettingsParent.StorageTabVisible;

		public ITab_ConversionChamber()
		{
			size = WinSize;
			labelKey = "AndroidTab";
			state = new ThingFilterUI.UIState();
			state.scrollPosition = default(Vector2);
		}

		protected override void FillTab()
		{
			IStoreSettingsParent selStoreSettingsParent = SelStoreSettingsParent;
			StorageSettings storeSettings = selStoreSettingsParent.GetStoreSettings();
			Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
			GUI.BeginGroup(rect);
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Rect rect2 = new Rect(rect);
			rect2.height = 32f;
			Widgets.Label(rect2, "AndroidTabTitle".Translate());
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			ThingFilter parentFilter = null;
			if (selStoreSettingsParent.GetParentStoreSettings() != null)
			{
				parentFilter = selStoreSettingsParent.GetParentStoreSettings().filter;
			}
			ThingFilterUI.DoThingFilterConfigWindow(new Rect(0f, 40f, rect.width, rect.height - 40f), state, storeSettings.filter, parentFilter, 8);
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.StorageTab, KnowledgeAmount.FrameDisplayed);
			GUI.EndGroup();
		}
	}
}