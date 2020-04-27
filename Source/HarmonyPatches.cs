using HarmonyLib;
using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System;
using UnityEngine;

namespace RoadsOfTheRim
{
    [StaticConstructorOnStartup]
    public class HarmonyPatches
    {
        public static RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();

        static HarmonyPatches()
        {
            var harmony = new Harmony("Loconeko.Rimworld.RoadsOfTheRim");
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
                //RoadsOfTheRim.DebugLog("initialising roadDef " + thisDef);
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
            /*
            foreach (TerrainDef t in RoadsOfTheRim.builtRoadTerrains)
            {
                RoadsOfTheRim.DebugLog("builtRoadTerrains - Adding : " + t);
            }
            RoadsOfTheRim.DebugLog("[RofR] - Roads of the Rim loaded v20191010");
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
            if (isThereAConstructionSiteHere & !isTheCaravanWorkingOnASite && RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting == null)
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

    [HarmonyPatch(typeof(Alert_CaravanIdle), "GetExplanation")]
    public static class Patch_Alert_CaravanIdle_GetExplanation
    {
        [HarmonyPostfix]
        public static void Postfix(ref TaggedString __result)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                WorldObjectComp_Caravan caravanComp = caravan.GetComponent<WorldObjectComp_Caravan>();
                if (caravan.Spawned && caravan.IsPlayerControlled && !caravan.pather.MovingNow && !caravan.CantMove && !caravanComp.currentlyWorkingOnSite)
                {
                    stringBuilder.AppendLine("  - " + caravan.Label);
                }
            }
            __result = "CaravanIdleDesc".Translate(stringBuilder.ToString());
        }
    }

    [HarmonyPatch(typeof(Alert_CaravanIdle), "GetReport")]
    public static class Patch_Alert_CaravanIdle_GetReport
    {
        [HarmonyPostfix]
        public static void Postfix(ref AlertReport __result)
        {
            List<Caravan> newList = new List<Caravan>();
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                WorldObjectComp_Caravan caravanComp = caravan.GetComponent<WorldObjectComp_Caravan>();
                if (caravan.Spawned && caravan.IsPlayerControlled && !caravan.pather.MovingNow && !caravan.CantMove && !caravanComp.currentlyWorkingOnSite)
                {
                    newList.Add(caravan);
                }
            }
            __result = AlertReport.CulpritsAre(newList);
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
        public static void Postifx(ref float __result, WorldGrid __instance, ref int fromTile, ref int toTile, ref StringBuilder explanation)
        {
            List<Tile.RoadLink> roads = __instance.tiles[fromTile].Roads;
            if (roads == null)
            {
                return;
            }
            if (toTile == -1)
            {
                toTile = __instance.FindMostReasonableAdjacentTileForDisplayedPathCost(fromTile);
            }
            float BiomeModifier = 0;
            float HillModifier = 0;
            float WinterModifier = 0;
            for (int i = 0; i < roads.Count; i++)
            {
                if (roads[i].neighbor == toTile)
                {
                    float HillinessOffset = (float)HillinessMovementDifficultyOffset.Invoke(null, new object[] { Find.WorldGrid[toTile].hilliness });
                    if (HillinessOffset > 12f) { HillinessOffset = 12f; }

                    // Calculate biome, Hillines & winter modifiers, update explanation &  multiply result by biome modifier
                    float RoadModifier = RoadsOfTheRim.calculateRoadModifier(
                        roads[i].road,
                        Find.WorldGrid[toTile].biome.movementDifficulty,
                        HillinessOffset,
                        WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(toTile),
                        out BiomeModifier,
                        out HillModifier,
                        out WinterModifier
                    );

                    __result *= RoadModifier;
                    if (explanation != null) {
                        explanation.AppendLine();
                        explanation.Append(String.Format(
                            "The road cancels {0:P0} of the biome ({3:##.###}), {1:P0} of the hills ({4:##.###}) & {2:P0} of winter movement costs",
                            BiomeModifier, HillModifier, WinterModifier,
                            Find.WorldGrid[toTile].biome.movementDifficulty, HillinessOffset
                        ));
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorldPathGrid), "CalculatedMovementDifficultyAt")]
    static class Patch_WorldPathGrid_CalculatedMovementDifficultyAt
    {
        [HarmonyPostfix]
        public static void PostFix(ref float __result, int tile, bool perceivedStatic, int? ticksAbs, StringBuilder explanation)
        {
            if (__result > 999f)
            {
                try
                {
                    if (Find.WorldGrid.InBounds(tile))
                    {
                        Tile tile2 = Find.WorldGrid.tiles[tile];
                        List<Tile.RoadLink> roads = tile2.Roads;
                        if (roads?.Count > 0)
                        {
                            RoadDef BestRoad = null;
                            for (int i = 0; i < roads.Count; i++)
                            {
                                if (BestRoad == null)
                                {
                                    BestRoad = roads[i].road;
                                }
                                else
                                {
                                    if (BestRoad.movementCostMultiplier < roads[i].road.movementCostMultiplier)
                                    {
                                        BestRoad = roads[i].road;
                                    }
                                }
                            }
                            if (BestRoad != null)
                            {
                                DefModExtension_RotR_RoadDef roadDefExtension = BestRoad.GetModExtension<DefModExtension_RotR_RoadDef>();
                                if (roadDefExtension != null && ((tile2.biome.impassable && roadDefExtension.biomeModifier > 0) || (tile2.hilliness == Hilliness.Impassable)))
                                {
                                    __result = 12f;
                                    RoadsOfTheRim.DebugLog(String.Format("[RotR] - Impassable Tile {0} movement difficulty patched", tile));
                                }
                            }

                        }
                    }
                    else
                    {
                        RoadsOfTheRim.DebugLog("[RotR] - CalculatedMovementDifficultyAt Patch - Tile out of bounds");
                    }
                }
                catch (Exception e)
                {
                    RoadsOfTheRim.DebugLog("[RotR] - CalculatedMovementDifficultyAt Patch - Catastrophic failure", e);
                    return;
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
            if (RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting != null)
            {
                //RoadsOfTheRim.DebugLog("StopTargeting");
                RoadsOfTheRim.FinaliseConstructionSite(RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting);
                RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting = null;
            }
        }
    }

    // Pure debug
    [HarmonyPatch(typeof(Dialog_FormCaravan), "AddToTransferables")]
    public static class Patch_Dialog_FormCaravan_AddToTransferables
    {
        [HarmonyPostfix]
        public static void Postfix(Thing t)
        {
            RoadsOfTheRim.DebugLog("Forming caravan, adding to transferable: " + t.Label);
        }
    }
    // Pure debug
    [HarmonyPatch(typeof(TabDrawer), "DrawTabs")]
    public static class Patch_TabDrawer_DrawTabs
    {
        [HarmonyPostfix]
        public static void Postfix(Rect baseRect , List<TabRecord> tabs)
        {
            List<TabRecord> list = tabs.ListFullCopy();
            foreach (TabRecord item in list)
            {
                RoadsOfTheRim.DebugLog("Forming caravan, adding to transferable: " + item.label);
            }
        }
    }
}