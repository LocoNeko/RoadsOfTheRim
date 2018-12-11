using Harmony;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using System.Reflection;
using RimWorld.Planet;
using Verse.Sound;
using UnityEngine;

namespace RoadsOfTheRim
{
    public class RoadsOfTheRimSettings : ModSettings
    {
        /* Mod setting : the basic effort needed to build a road must be between 1 and 10 */
        public const int MinBaseEffort = 1;
        public const int DefaultBaseEffort = 5;
        public const int MaxBaseEffort = 10;
        public static int BaseEffort = DefaultBaseEffort;
        public static bool OverrideCosts = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref BaseEffort, "BaseEffort", DefaultBaseEffort, true);
            Scribe_Values.Look<bool>(ref OverrideCosts, "OverrideCosts", true, true);
        }
    }

    public class RoadsOfTheRim : Mod
    {
        public static RoadsOfTheRimSettings settings;

        public RoadsOfTheRim(ModContentPack content) : base(content)
        {
            settings = GetSettings<RoadsOfTheRimSettings>();
            var harmony = HarmonyInstance.Create("Loconeko.Rimworld.RoadsOfTheRim");
            /* Patching the Caravan's Gizmos to add "BuildRoad" */
            MethodInfo method = typeof(Caravan).GetMethod("GetGizmos");
            HarmonyMethod prefix = null;
            HarmonyMethod postfix = new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("GetGizmosPostfix")); ;
            harmony.Patch(method, prefix, postfix, null);

            //Log.Message("[RoadsOfTheRim] Loaded");
        }

        public override string SettingsCategory() => "RoadsOfTheRimSettingsCategoryLabel".Translate();

        public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            listing_Standard.Label("RoadsOfTheRimSettingsBaseEffort".Translate() + ": " + RoadsOfTheRimSettings.BaseEffort);
            listing_Standard.Gap();
            RoadsOfTheRimSettings.BaseEffort = (int)listing_Standard.Slider((float)RoadsOfTheRimSettings.BaseEffort, (float)RoadsOfTheRimSettings.MinBaseEffort, (float)RoadsOfTheRimSettings.MaxBaseEffort);
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("RoadsOfTheRimSettingsOverrideCosts".Translate() + ": ", ref RoadsOfTheRimSettings.OverrideCosts);
            listing_Standard.End();
            settings.Write();
        }

        public static void GetGizmosPostfix(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            __result = __result.Concat(new Gizmo[] { AddConstructionSite(__instance) })
                               .Concat(new Gizmo[] { RemoveConstructionSite(__instance) });
            // TO DO next one should be : work on road which will cost time and ressource and eventually ends up with the road built
        }

        public static Command AddConstructionSite(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimAddConstructionSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimAddConstructionSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite", true);
            command_Action.action = delegate ()
            {
                /* Go through all the RoadBuildableDefs and show them in a float menu when creating construction site*/
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (RoadBuildableDef thisRoadBuildableDef in DefDatabase<RoadBuildableDef>.AllDefs)
                {
                    list.Add(new FloatMenuOption(
                        thisRoadBuildableDef.label + " (movement :" + thisRoadBuildableDef.movementCostMultiplier + ")",
                        delegate ()
                        {
                            SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, null);
                            CreateConstructionSite(caravan, thisRoadBuildableDef);
                        }
                    ));
                }

                if (list.Count > 0)
                {
                    FloatMenu floatMenu = new FloatMenu(list);
                    floatMenu.vanishIfMouseDistant = true;
                    Find.WindowStack.Add(floatMenu);
                }
            };
            // Test when the AddConstructionSite action should be disabled
            // - Check whether there's already a construction site here
            // - TO DO : Check whether there's already a road here
            bool ConstructionSiteAlreadyHere = false;
            try
            {
                ConstructionSiteAlreadyHere = Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), caravan.Tile);
            }
            catch
            {

            }
            if (ConstructionSiteAlreadyHere)
            {
                command_Action.Disable("RoadsOfTheRimBuildConstructionSiteAlreadyHere".Translate());
            }
            return command_Action;
        }

        public static Command RemoveConstructionSite(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimRemoveConstructionSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimRemoveConstructionSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/RemoveConstructionSite", true);
            command_Action.action = delegate ()
            {
                SoundStarter.PlayOneShotOnCamera(SoundDefOf.CancelMode, null);
                DeleteConstructionSite(caravan);
            };
            // Test when the RemoveConstructionSite action should be disabled
            // Check whether there's already a construction site here
            bool ConstructionSiteAlreadyHere = false;
            try
            {
                ConstructionSiteAlreadyHere = Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), caravan.Tile);
            }
            catch
            {

            }
            if (!ConstructionSiteAlreadyHere)
            {
                command_Action.Disable("RoadsOfTheRimBuildConstructionSiteNotAlreadyHere".Translate());
            }
            return command_Action;
        }

        /*
         * Creates a road construction site on the world map, where the Caravan is.
         * The site will track the amount of work already done
         * The site is considered the "From" tile and is linked to one neighbouring tile : the toTile 
         * later : The Caravan must provide the material
         * later : The Caravan also has a construction score based on number of people, construction skills, animals
         */
        public static void CreateConstructionSite(Caravan caravan, RoadBuildableDef roadBuildableDef)
        {
            WorldGrid worldGrid = Find.WorldGrid;
            RoadConstructionSite constructionSite = (RoadConstructionSite)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true));
            constructionSite.Tile = caravan.Tile;
            constructionSite.roadToBuild = roadBuildableDef;
            /* TO DO : Instead of placing the construction site directly, make the player pick a target tile
             * One way to do it : as soon as the roadToBuild is selected, show a little dot in every neighbouring tile. Clicking on the dot is what will create the construction site
             */
            constructionSite.toTile = null;
            List<int> neighbouringTiles = new List<int>();
            Find.WorldGrid.GetTileNeighbors(caravan.Tile, neighbouringTiles);
            Find.WorldObjects.Add(constructionSite);


            /* TO DO : create the road on the world map. Use WorldGrid ?
            Useful : RoutePlannerWaypoint allows you to point and click on the map to find a path
            CaravanTile.potentialRoads.Add(); ?
            WorldGrid.OverlayRoad (int fromTile, int toTile, RoadDef roadDef)
            Find.WorldGrid.OverlayRoad; ?
            Find.World.renderer.RegenerateAllLayersNow();
            */
            /*
            **Test : first, simply make a road to the nearest neighbour**
            int toTile = worldGrid.FindMostReasonableAdjacentTileForDisplayedPathCost(caravan.Tile);
            RoadDef roadDef = worldGrid.GetRoadDef(caravan.Tile , toTile);
            RoadDef roadDef2 = worldGrid.GetRoadDef(caravan.Tile, toTile, false);
            Tile tile = worldGrid[caravan.Tile];
            Tile tile2 = worldGrid[toTile];
            if (roadDef2 != null)
            {
                tile.potentialRoads.RemoveAll((Tile.RoadLink rl) => rl.neighbor == toTile);
                tile2.potentialRoads.RemoveAll((Tile.RoadLink rl) => rl.neighbor == caravan.Tile);
            }
            if (tile.potentialRoads == null)  { tile.potentialRoads = new List<Tile.RoadLink>(); }
            if (tile2.potentialRoads == null) { tile2.potentialRoads = new List<Tile.RoadLink>(); }
            tile.potentialRoads.Add(new Tile.RoadLink
            {
                neighbor = toTile,
                road = roadDef
            });
            tile2.potentialRoads.Add(new Tile.RoadLink
            {
                neighbor = caravan.Tile,
                road = roadDef
            });
            Find.World.renderer.RegenerateAllLayersNow();
            */
        }

        public static void DeleteConstructionSite(Caravan caravan)
        {
            Find.WorldObjects.Remove(Find.WorldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), caravan.Tile));
        }
    }
}