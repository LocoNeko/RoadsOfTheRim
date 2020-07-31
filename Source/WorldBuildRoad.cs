using RimWorld.Planet;
using UnityEngine;
using Verse;
using RimWorld;

namespace RoadsOfTheRim
{

	[StaticConstructorOnStartup]
	public static class WorldBuildRoad
	{
		public static readonly Texture2D BuildRoadTex = ContentFinder<Texture2D>.Get("UI/Commands/BuildroadTooltip");


		public static void BuildRoadOnGUI()
		{
			//Vector2 center= new Vector2((float)UI.screenWidth - 64f, 32f) ;
			Rect rect = new Rect(new Vector2(32f , 32f) , new Vector2(BuildRoadTex.width, BuildRoadTex.height));
			if (Widgets.ButtonImage(rect , BuildRoadTex))
            {
				WorldComponent_RoadBuildingState roadBuildingState = Find.World.GetComponent(typeof(WorldComponent_RoadBuildingState)) as WorldComponent_RoadBuildingState;
				// target only if : not currnetly targeting, not already picking site
				if (!roadBuildingState.PickingSiteTile && roadBuildingState.CurrentlyTargeting==null)
                {
					Find.WorldTargeter.BeginTargeting(delegate (GlobalTargetInfo target)
					{
						return WorldBuildRoadActionOnTile(target.Tile);
					},
						true, RotR_StaticConstructorOnStartup.ConstructionLeg_MouseAttachment, false, null,
						delegate (GlobalTargetInfo target)
						{
							return "RotR_BuildFromHere".Translate();
						});
				}
			}
			TooltipHandler.TipRegionByKey(rect, "RotR_BuildRoadTooltip");
		}

		public static bool WorldBuildRoadActionOnTile(int tile)
        {
			RoadsOfTheRim.DebugLog("Clicked on tile "+tile+". I should now show the road construction menu.");
			WorldComponent_RoadBuildingState roadBuildingState = Find.World.GetComponent(typeof(WorldComponent_RoadBuildingState)) as WorldComponent_RoadBuildingState;
			Find.WorldTargeter.StopTargeting();
			roadBuildingState.PickingSiteTile = false;

			// ALl of that below already exist in ModMain. Re-use code where possible !
			RoadConstructionSite constructionSite = (RoadConstructionSite)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true));
			constructionSite.Tile = tile;
			Find.WorldObjects.Add(constructionSite);

			ConstructionMenu menu = new ConstructionMenu(constructionSite, null , tile);

			if (menu.CountBuildableRoads() == 0)
			{
				Find.WorldObjects.Remove(constructionSite);
				Messages.Message("RoadsOfTheRim_NoBetterRoadCouldBeBuilt".Translate(), MessageTypeDefOf.RejectInput);
			}
			else
			{
				menu.closeOnClickedOutside = true;
				menu.forcePause = true;
				Find.WindowStack.Add(menu);
			}
			return false;
        }
	}
}