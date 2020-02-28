using RimWorld;
using Verse;
using HarmonyLib;

/*
 * This file contains all C# related to placing & removing Concrete bridges
 */
namespace RoadsOfTheRim
{
    [DefOf]
    public static class TerrainDefOf
    {
        public static TerrainDef StoneRecent;
        public static TerrainDef AsphaltRecent ;
        public static TerrainDef GlitterRoad;
        public static TerrainDef ConcreteBridge ;
        public static TerrainDef MarshyTerrain;
        public static TerrainDef Mud;
    }

    [DefOf]
    public static class TerrainAffordanceDefOf
    {
        public static TerrainAffordanceDef Bridgeable;
        public static TerrainAffordanceDef BridgeableAny;
    }

    [HarmonyPatch(typeof(Designator_RemoveBridge), "CanDesignateCell")]
    public static class Patch_Designator_RemoveBridge_CanDesignateCell
    {
        [HarmonyPostfix]
        public static void Postfix(ref AcceptanceReport __result, Designator_RemoveBridge __instance, IntVec3 c)
        {
            if (c.InBounds(__instance.Map) && c.GetTerrain(__instance.Map) == TerrainDefOf.ConcreteBridge)
            {
                __result = true ;
                RoadsOfTheRim.DebugLog(c.GetTerrain(__instance.Map).label);
            }
        }
    }

    [HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintAt")]
    public static class Patch_GenConstruct_CanPlaceBlueprintAt
    {
        [HarmonyPostfix]
        public static void Postfix(ref AcceptanceReport __result, BuildableDef entDef, IntVec3 center, Rot4 rot , Map map , bool godMode = false, Thing thingToIgnore = null)
        {
            if (entDef == TerrainDefOf.ConcreteBridge && map.terrainGrid.TerrainAt(center).affordances.Contains(TerrainAffordanceDefOf.Bridgeable)) // ConcreteBridge on normal water (bridgeable)
            {
                __result = AcceptanceReport.WasAccepted;
            }
        }
    }
    /*
     * Both Concrete bridges, Stone Roads, and Asphalt roads must check the terrain they're placed on and :
     * - Change it (Marsh & marshy soil to be removed when a "good" road was placed
     * - Be placed despite affordance (Concrete bridges on top of normal bridgeable water)    
     */

    [HarmonyPatch(typeof(RoadDefGenStep_Place), "Place")]
    public static class Patch_RoadDefGenStep_Place_Place
    {
        public static bool isGoodTerrain(TerrainDef terrain)
        {
            return ((terrain == TerrainDefOf.Mud) || (terrain == TerrainDefOf.MarshyTerrain));
        }

        [HarmonyPostfix]
        public static void Postfix(ref RoadDefGenStep_Place __instance, Map map, IntVec3 position, TerrainDef rockDef, IntVec3 origin, GenStep_Roads.DistanceElement[,] distance)
        {
            if (__instance.place == TerrainDefOf.ConcreteBridge && position.GetTerrain(map).IsWater)
            {
                map.terrainGrid.SetTerrain(position, TerrainDefOf.ConcreteBridge) ;
            }
            if (__instance.place == TerrainDefOf.GlitterRoad && (isGoodTerrain(position.GetTerrain(map)) || position.GetTerrain(map).IsWater))
            {
                map.terrainGrid.SetTerrain(position, TerrainDefOf.GlitterRoad);
            }
            if (__instance.place == TerrainDefOf.AsphaltRecent && isGoodTerrain(position.GetTerrain(map)))
            {
                map.terrainGrid.SetTerrain(position, TerrainDefOf.AsphaltRecent);
            }
            if (__instance.place == TerrainDefOf.StoneRecent && isGoodTerrain(position.GetTerrain(map)))
            {
                map.terrainGrid.SetTerrain(position, TerrainDefOf.StoneRecent);
            }
        }
    }

    [HarmonyPatch(typeof(GenConstruct), "CanBuildOnTerrain")]
    public static class Patch_GenConstruct_CanBuildOnTerrain
    {
        [HarmonyPostfix]
        public static void Postfix (ref bool __result , BuildableDef entDef, IntVec3 c, Map map)
        {
            if (entDef == TerrainDefOf.ConcreteBridge || entDef == TerrainDefOf.AsphaltRecent || entDef == TerrainDefOf.GlitterRoad)
            {
                if (map.terrainGrid.TerrainAt(c).IsWater)
                {
                    __result = true;
                }
            }
        }
    }
}
