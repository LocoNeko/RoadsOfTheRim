using Harmony;
using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System;

namespace RoadsOfTheRim
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        public static RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();

        static HarmonyPatches()
        {
            var harmony = HarmonyInstance.Create("Loconeko.Rimworld.RoadsOfTheRim");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            /* How I found the hidden methods :
            var methods = typeof(Tile).GetMethods();
            foreach (var method in methods)
            {
                Log.Message(method.Name);
            }
            */
        }
    }

    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public static class Patch_Caravan_GetGizmos
    {
        [HarmonyPostfix]
        public static void Postfix(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            bool isThereAConstructionSiteHere = Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), __instance.Tile);
            bool isTheCaravanWorkingOnASite = __instance.GetComponent<WorldObjectComp_Caravan>().currentlyWorkingOnSite;
            __result = __result.Concat(new Gizmo[] { RoadsOfTheRim.AddConstructionSite(__instance) })
                               .Concat(new Gizmo[] { RoadsOfTheRim.RemoveConstructionSite(__instance.Tile) });
            if (isThereAConstructionSiteHere & !isTheCaravanWorkingOnASite)
            {
                __result = __result.Concat(new Gizmo[] { RoadsOfTheRim.WorkOnSite(__instance) });
            }
            if (isTheCaravanWorkingOnASite)
            {
                __result = __result.Concat(new Gizmo[] { RoadsOfTheRim.StopWorkingOnSite(__instance) });
            }
        }
    }

    [HarmonyPatch(typeof(Caravan), "GetInspectString")]
    public static class Patch_Caravan_GetInspectString
    {
        [HarmonyPostfix]
        public static void Postfix(ref string __result, Caravan __instance)
        {
            try
            {
                WorldObjectComp_Caravan CaravanComp = __instance.GetComponent<WorldObjectComp_Caravan>();
                if (CaravanComp != null && CaravanComp.currentlyWorkingOnSite)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(__result);
                    stringBuilder.AppendLine();
                    stringBuilder.Append("RoadsOfTheRim_CaravanInspectStringWorkingOn".Translate(CaravanComp.getSite().fullName(), string.Format("{0:0.00}", CaravanComp.amountOfWork())));
                    __result = stringBuilder.ToString();
                }
            }
            catch
            {
                // lazy way out : the caravan can, on occasions (mainly debug teleport, though...), not have a site linked to the comp
            }
        }

    }

    [HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
    public static class Patch_FactionDialogMaker_FactionDialogFor
    {
        [HarmonyPostfix]
        public static void Postfix(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            // Allies can help build roads
            if (faction.PlayerRelationKind == FactionRelationKind.Ally)
            {
                __result.options.Insert(0, RoadsOfTheRim.HelpRoadConstruction(faction, negotiator));
            }
        }
    }

    /*
     * Patching roads so they cancel all or part of the Tile.biome.movementDifficulty
     * The actual rates are stored in static method RoadsOfTheRim.calculateBiomeModifier
     */
    [HarmonyPatch(typeof(WorldGrid), "GetRoadMovementDifficultyMultiplier")]
    public static class Patch_WorldGrid_GetRoadMovementDifficultyMultiplier
    {
        [HarmonyPostfix]
        public static void Postifx(ref float __result , WorldGrid __instance, ref int fromTile, ref int toTile, ref StringBuilder explanation)
        {
            List<Tile.RoadLink> roads = __instance.tiles[fromTile].Roads;
			if (roads == null)
			{
                return ;
			}
			if (toTile == -1)
			{
				toTile = __instance.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
			}
            float biomeCancellation = 0;
            for (int i = 0; i < roads.Count; i++)
			{
				if (roads[i].neighbor == toTile)
				{
                    // Calculate biome modifier, update explanation &  multiply result by biome modifier
                    float biomeModifier = RoadsOfTheRim.calculateBiomeModifier(roads[i].road, Find.WorldGrid[toTile].biome.movementDifficulty, out biomeCancellation);
                    // TO DO : Cancel part of Hilliness as well. See WorldPathGrid.HillinessMovementDifficultyOffset
                    __result *= biomeModifier ;
                    if (explanation != null) {
                        explanation.AppendLine ();
                        explanation.Append(String.Format("The road cancels {0:P0} of the biome's movement cost", biomeCancellation));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorldTargeter), "StopTargeting")]
    public static class Patch_WorldTargeter_StopTargeting
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting!=null)
            {
                RoadsOfTheRim.FinaliseConstructionSite(RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting);
                RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting = null;
            }
        }
    }

    /*
     * Below are attempts at preventing rocks from spawning on SterileTile, as a test.
     * Once sucessful, I can create a new type of road and prevent stuff from spawning on them
     * To control what type of ground is created where, I will have to XML patch roadGenSteps    
    [HarmonyPatch(typeof(GenStep_RockChunks), "GrowLowRockFormationFrom")]
    public static class Patch_GenStep_RockChunks_GrowLowRockFormationFrom
    {
        [HarmonyPrefix]
        public static void Prefix(ref IntVec3 root , Map map)
        {
            if (map.terrainGrid.TerrainAt(root).defName== "SterileTile")
            {
                root = new IntVec3(-999,0,-999);
                Log.Message("[RotR] - Placing rock on sterile tile");
                return;
            }
        }
    }
    */

    /*
    [HarmonyPatch(typeof(GenSpawn), "Spawn" , new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Map) , typeof(WipeMode) })]
    public static class Patch_GenSpawn_Spawn
    {
        [HarmonyPostfix]
        public static void Postfix(ref Thing __result , ref ThingDef def, ref IntVec3 loc, ref Map map , ref WipeMode wipeMode)
        {
            if (map.terrainGrid.TerrainAt(loc).defName == "BrokenAsphalt")
            {
                Log.Warning("[RotR] - DEBUG - Placing something on Broken Asphalt") ;
            }
        }
    }
    */
}