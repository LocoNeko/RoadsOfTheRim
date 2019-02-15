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

            // Initialise the list of terrains that are specific to built roads. Doing it here is hacky, but this is a quick way to use defs after they were loaded
            foreach (RoadDef thisDef in DefDatabase<RoadDef>.AllDefs)
            {
                RoadsOfTheRim.DebugLog("initiliasing roadDef " + thisDef);
                if (thisDef.HasModExtension<DefModExtension_RotR_RoadDef>() && thisDef.GetModExtension<DefModExtension_RotR_RoadDef>().built) // Only add RoadDefs that are buildable, based on DefModExtension_RotR_RoadDef.built
                {
                    foreach (RoadDefGenStep_Place aStep in thisDef.roadGenSteps.OfType<RoadDefGenStep_Place>()) // Only get RoadDefGenStep_Place
                    {
                        TerrainDef t = (TerrainDef)aStep.place; // Cast the buildableDef into a TerrainDef
                        if (!RoadsOfTheRim.builtRoadTerrains.Contains(t))
                        {
                            RoadsOfTheRim.builtRoadTerrains.Add(t);
                        }
                    }
                }
            }
            foreach (TerrainDef t in RoadsOfTheRim.builtRoadTerrains)
            {
                RoadsOfTheRim.DebugLog("builtRoadTerrains - Adding : " + t);
            }
            RoadsOfTheRim.DebugLog("Roads of the Rim loaded");
        }
    }

    [HarmonyPatch(typeof(Caravan), "GetGizmos")]
    public static class Patch_Caravan_GetGizmos
    {
        [HarmonyPostfix]
        public static void Postfix(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            bool isThereAConstructionSiteHere = Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), __instance.Tile);
            bool isTheCaravanWorkingOnASite = true;
            try
            {
                isTheCaravanWorkingOnASite = __instance.GetComponent<WorldObjectComp_Caravan>().currentlyWorkingOnSite;
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog(null, e);
            }
            __result = __result.Concat(new Gizmo[] { RoadsOfTheRim.AddConstructionSite(__instance) })
                               .Concat(new Gizmo[] { RoadsOfTheRim.RemoveConstructionSite(__instance.Tile) });
            if (isThereAConstructionSiteHere & !isTheCaravanWorkingOnASite && RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting==null)
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
     * Patching roads so they cancel all or part of the Tile.biome.movementDifficulty and Hilliness
     * The actual rates are stored in static method RoadsOfTheRim.calculateRoadModifier
     */
    [HarmonyPatch(typeof(WorldGrid), "GetRoadMovementDifficultyMultiplier")]
    public static class Patch_WorldGrid_GetRoadMovementDifficultyMultiplier
    {
        private static readonly MethodInfo HillinessMovementDifficultyOffset = AccessTools.Method(typeof(WorldPathGrid), "HillinessMovementDifficultyOffset", new Type[] { typeof(Hilliness) });

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
            float BiomeModifier = 0 ;
            float HillModifier = 0 ;
            float WinterModifier = 0;
            for (int i = 0; i < roads.Count; i++)
			{
                if (roads[i].neighbor == toTile)
				{

                    // Calculate biome, Hillines & winter modifiers, update explanation &  multiply result by biome modifier
                    float RoadModifier = RoadsOfTheRim.calculateRoadModifier(
                        roads[i].road , 
                        Find.WorldGrid[toTile].biome.movementDifficulty ,
                        (float)HillinessMovementDifficultyOffset.Invoke(null , new object[] { Find.WorldGrid[toTile].hilliness }),
                        WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(toTile) ,
                        out BiomeModifier,
                        out HillModifier ,
                        out WinterModifier
                    );

                    __result *= RoadModifier ;
                    if (explanation != null) {
                        explanation.AppendLine ();
                        explanation.Append(String.Format("The road cancels {0:P0} of the biome, {1:P0} of the hills & {2:P0} of winter movement costs", BiomeModifier, HillModifier , WinterModifier));
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
                //RoadsOfTheRim.DebugLog("StopTargeting");
                RoadsOfTheRim.FinaliseConstructionSite(RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting);
                RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting = null;
            }
        }
    }
}
