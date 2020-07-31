using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{

	[StaticConstructorOnStartup]
	public static class WorldBuildRoad
	{
		public static readonly Texture2D BuildRoadTex = ContentFinder<Texture2D>.Get("UI/Commands/BuildroadTooltip");

		public static void BuildRoadOnGUI(ref float curBaseY)
		{
			BuildRoadOnGUI(new Vector2((float)UI.screenWidth - 10f - 32f, curBaseY - 10f - 32f));
			curBaseY -= 84f;
		}

		private static void BuildRoadOnGUI(Vector2 center)
		{
			Widgets.DrawTextureRotated(center, BuildRoadTex, 0f);
			Rect rect = new Rect(center.x - 32f, center.y - 32f, 64f, 64f);
			TooltipHandler.TipRegionByKey(rect, "RotR_BuildRoadTooltip");
			if (Event.current.type == EventType.KeyDown)
			{
				RoadsOfTheRim.DebugLog("Event on WorldBuildRoad");
			}
		}
	}
}