using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{

	public static class BuildRoadWidget
	{
		private const float Padding = 10f;

		private const float Size = 64f;

		private static readonly Texture2D CompassTex = ContentFinder<Texture2D>.Get("UI/Command/AddConstructionSite");

		public static void BuildRoadOnGUI(ref float curBaseY)
		{
			BuildRoadOnGUI(new Vector2((float)UI.screenWidth - 10f - 32f, curBaseY - 10f - 32f));
			curBaseY -= 84f;
		}

		private static void BuildRoadOnGUI(Vector2 center)
		{
			Widgets.DrawTextureRotated(center, CompassTex, 0f);
			Rect rect = new Rect(center.x - 32f, center.y - 32f, 64f, 64f);
			TooltipHandler.TipRegionByKey(rect, "RotR_BuildRoadTooltip");
		}
	}
}