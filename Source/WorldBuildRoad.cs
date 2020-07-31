using RimWorld.Planet;
using UnityEngine;
using Verse;

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
				WorldComponent_RoadBuildingState f = Find.World.GetComponent(typeof(WorldComponent_RoadBuildingState)) as WorldComponent_RoadBuildingState;
				RoadsOfTheRim.DebugLog("Event on WorldBuildRoad");
				Find.WorldTargeter.BeginTargeting(delegate (GlobalTargetInfo target)
					{
						return WorldBuildRoadActionOnTile(target.Tile);
					},
					true, RotR_StaticConstructorOnStartup.ConstructionLeg_MouseAttachment , false, null ,
					delegate (GlobalTargetInfo target)
					{
						return "RoadsOfTheRim_BuildToHere".Translate();
				});
			}
			TooltipHandler.TipRegionByKey(rect, "RotR_BuildRoadTooltip");
		}

		public static bool WorldBuildRoadActionOnTile(int Tile)
        {
			RoadsOfTheRim.DebugLog("Clicked on tile "+Find.WorldGrid.tiles[Tile].ToString());
			return false;
        }
	}
}