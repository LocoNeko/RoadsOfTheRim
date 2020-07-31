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
			Vector2 center= new Vector2((float)UI.screenWidth - 32f, 32f) ;
			Rect rect = new Rect(center, new Vector2(BuildRoadTex.width, BuildRoadTex.height));
			if (Widgets.ButtonImage(rect , BuildRoadTex))
            {
				RoadsOfTheRim.DebugLog("Event on WorldBuildRoad");
			}
			TooltipHandler.TipRegionByKey(rect, "RotR_BuildRoadTooltip");
		}
	}
}