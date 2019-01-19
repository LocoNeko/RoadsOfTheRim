using Harmony;
using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
            // TO DO : Add comms console dialog to ask for help on a construction site
            // See https://github.com/erdelf/PrisonerRansom/blob/master/Source/PrisonerRansom/ReplacementCode.cs
            // method of interest : FactionDialogMaker , FactionDialogFor

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
        public static void Postifx(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            // Allies can help build roads
            if (faction.PlayerRelationKind == FactionRelationKind.Ally)
            {
                __result.options.Insert(0, RoadsOfTheRim.HelpRoadConstruction(faction, negotiator));
            }
        }
    }

    [HarmonyPatch(typeof(Tile), "get_Roads")]
    public static class Patch_Tile_get_Roads
    {
        [HarmonyPostfix]
        public static void Postifx(ref List<Tile.RoadLink> __result, Tile __instance)
        {
            RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();
            if (__result != null)
            {
                bool overrideCosts = settings.OverrideCosts;
                List<Tile.RoadLink> patchedRoads = new List<Tile.RoadLink>();
                foreach (Tile.RoadLink aLink in __result)
                {
                    Tile.RoadLink aRoadLink = new Tile.RoadLink();
                    RoadDef aRoad = aLink.road;

                    float biomeMovementDifficultyEffect = 0; // To be taken from road type : 1 (best) , 0.75 , 0.25 , 0 (worst)
                    /*
                        foreach (RoadBuildableDef thisRoadBuildableDef in DefDatabase<RoadBuildableDef>.AllDefs)
                    {
                        if (thisRoadBuildableDef.roadDef == aRoad.defName)
                        {
                            biomeMovementDifficultyEffect = thisRoadBuildableDef.biomeMovementDifficultyEffect;
                            Log.Message("RotR DEBUG biomeMovementDifficultyEffect = " + biomeMovementDifficultyEffect);
                        }
                    }
                    */
                    // Roads cancel biome movement difficulty
                    // e.g : Biome is at 3, effect is at 0.75 : we get a multiplier of .5, combined with the biome of 3, we'll only get 1.5 for biome
                    // i.e. : effect is at 0, we always get a multiplier of 1 (no effect)
                    // effect is at 1, we always get a multiplier of 1/biome, which effectively cancels biome effects
                    float biomeMovementDifficultyCancellation = (1 + (__instance.biome.movementDifficulty - 1) * (1 - biomeMovementDifficultyEffect)) / __instance.biome.movementDifficulty;
                    // If the settings are set to override default costs, apply them, otherwise use default (0.5) , multiply by how much the road cancels the biome movement difficulty
                    aRoad.movementCostMultiplier = (settings.OverrideCosts ? aLink.road.movementCostMultiplier : 0.5f) * biomeMovementDifficultyCancellation;
                    aRoadLink.neighbor = aLink.neighbor;
                    aRoadLink.road = aRoad;
                    patchedRoads.Add(aRoadLink);
                }
                __result = patchedRoads;
            }
        }
    }
}