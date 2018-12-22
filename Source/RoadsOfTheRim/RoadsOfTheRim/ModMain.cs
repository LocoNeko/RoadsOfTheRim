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
        // Constants
        public const int MinBaseEffort = 1;
        public const int DefaultBaseEffort = 5;
        public const int MaxBaseEffort = 10;
        public const float ElevationCostDouble = 2000f ;
        public const float HillinessCostDouble = 4f;
        public const float SwampinessCostDouble = 0.5f;


        public int BaseEffort = DefaultBaseEffort;
        public bool OverrideCosts = true;
        public float CostIncreaseElevationThreshold = 1000 ;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref BaseEffort, "BaseEffort", DefaultBaseEffort, true);
            Scribe_Values.Look<bool>(ref OverrideCosts, "OverrideCosts", true, true);
            Scribe_Values.Look<float>(ref CostIncreaseElevationThreshold, "CostIncreaseElevationThreshold", 1000 , true);
        }
    }

    public class RoadsOfTheRim : Mod
    {
        public static RoadsOfTheRimSettings settings;

        public RoadsOfTheRim(ModContentPack content) : base(content)
        {
            settings = GetSettings<RoadsOfTheRimSettings>();
            /* Patching the Caravan's Gizmos to add "BuildRoad" */
            var harmony = HarmonyInstance.Create("Loconeko.Rimworld.RoadsOfTheRim");
            harmony.Patch(typeof(Caravan).GetMethod("GetGizmos") , null , new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("GetGizmosPostfix")) , null);
            /*
             * I need to postfix the function that calls WorldObject.GetInspectString() to display whether or not the caravan is working on a road
             */
        }

        public override string SettingsCategory() => "RoadsOfTheRimSettingsCategoryLabel".Translate();

        public override void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            listing_Standard.Label("RoadsOfTheRimSettingsBaseEffort".Translate() + ": " + settings.BaseEffort);
            listing_Standard.Gap();
            settings.BaseEffort = (int)listing_Standard.Slider(settings.BaseEffort, RoadsOfTheRimSettings.MinBaseEffort, RoadsOfTheRimSettings.MaxBaseEffort);
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("RoadsOfTheRimSettingsOverrideCosts".Translate() + ": ", ref settings.OverrideCosts);
            listing_Standard.End();
            settings.Write();
        }

        public static void GetGizmosPostfix(ref IEnumerable<Gizmo> __result, Caravan __instance)
        {
            bool isThereAConstructionSiteHere = Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), __instance.Tile);
            bool isTheCaravanWorkingOnASite = __instance.GetComponent<WorldObjectComp_Caravan>().currentlyWorkingOnSite;
            // TO DO : Add comms console dialog to ask for help on a construction site
            // See https://github.com/erdelf/PrisonerRansom/blob/master/Source/PrisonerRansom/ReplacementCode.cs
            // method of interest : FactionDialogMaker , FactionDialogFor
            __result = __result.Concat(new Gizmo[] { AddConstructionSite(__instance) })
                               .Concat(new Gizmo[] { RemoveConstructionSite(__instance) });
            if (isThereAConstructionSiteHere & !isTheCaravanWorkingOnASite)
            {
                __result = __result.Concat(new Gizmo[] { WorkOnSite(__instance) });
            }
            if (isTheCaravanWorkingOnASite)
            {
                __result = __result.Concat(new Gizmo[] { StopWorkingOnSite(__instance) });
            }
        }

        /*
        Add a construction site :
        - pick a neighbouring tile (fail if the tile clicked was not a neighbour)
         */
        public static Command AddConstructionSite(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimAddConstructionSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimAddConstructionSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite", true);
            command_Action.action = delegate ()
            {
                // Find neighbours of caravan tile
                List<int> neighbouringTiles = new List<int>();
                Find.WorldGrid.GetTileNeighbors(caravan.Tile, neighbouringTiles);

                // Find clicked tile
                Find.WorldTargeter.BeginTargeting(delegate (GlobalTargetInfo target)
                {
                    if (neighbouringTiles.Contains(target.Tile))
                    {
                        CreateConstructionSite(caravan, target.Tile);
                    }
                    else
                    {
                        Messages.Message("RoadsOfTheRim_MustPickNeighbouringTile".Translate(), MessageTypeDefOf.RejectInput);
                    };
                    return true;
                },
                true, null, false, null, delegate (GlobalTargetInfo target)
                {
                    return "RoadsOfTheRim_BuildToHere".Translate();
                });
            };

            // Test when the AddConstructionSite action should be disabled : when there's already a construction site here
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

        /*
        Create a new Construction site
        - Show a list of buildable roads between the From and To tile
        - Exclude already existing similar or lesser roads
        - Fail if no road can be built (there's already an asphalt road here)
         */
        public static void CreateConstructionSite(Caravan caravan , int toTile_int)
        {
            Tile fromTile = Find.WorldGrid[caravan.Tile] ;
            Tile toTile =  Find.WorldGrid[toTile_int] ;
            
            // Check best existing roads
            RoadDef bestExistingRoad = (RoadDef)null ;
            if (fromTile.potentialRoads != null)
            {
                foreach (Tile.RoadLink aLink in fromTile.potentialRoads)
                {
                    if (aLink.neighbor == toTile_int & isRoadBetter(aLink.road , bestExistingRoad))
                    {
                        bestExistingRoad = aLink.road ;
                    }
                }
            }

            if (toTile.potentialRoads != null)
            {
                foreach (Tile.RoadLink aLink in toTile.potentialRoads)
                {
                    if (aLink.neighbor == caravan.Tile  & isRoadBetter(aLink.road , bestExistingRoad))
                    {
                        bestExistingRoad = aLink.road ;
                    }
                }
            }

            /* Go through all the RoadBuildableDefs and show them in a float menu when creating construction site*/
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (RoadBuildableDef thisRoadBuildableDef in DefDatabase<RoadBuildableDef>.AllDefs)
            {

                if (bestExistingRoad == null || isRoadBetter(DefDatabase<RoadDef>.GetNamed(thisRoadBuildableDef.roadDef, true), bestExistingRoad))
                {
                    list.Add(new FloatMenuOption(
                        thisRoadBuildableDef.label + " (movement :" + thisRoadBuildableDef.movementCostMultiplier + ")",
                        delegate ()
                        {
                            SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, null);
                            FinaliseConstructionSite(caravan.Tile, toTile_int, thisRoadBuildableDef);
                        }
                    ));
                }
            }

            if (list.Count > 0)
            {
                FloatMenu floatMenu = new FloatMenu(list);
                floatMenu.vanishIfMouseDistant = true;
                Find.WindowStack.Add(floatMenu);
            }
            else
            {
                Messages.Message("RoadsOfTheRim_NoBetterRoadCouldBeBuilt".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        /*
        Finalise creation of construction site
         */
        public static bool FinaliseConstructionSite(int fromTile_int , int toTile_int , RoadBuildableDef roadBuildableDef)
        {
            RoadConstructionSite constructionSite = (RoadConstructionSite)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true));
            constructionSite.Tile = fromTile_int;
            constructionSite.setDestination(toTile_int);
            constructionSite.roadToBuild = roadBuildableDef;
            Find.WorldObjects.Add(constructionSite);
            return true;
        }

        /*
        Work on  Site
         */
        public static Command WorkOnSite(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimWorkOnSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimWorkOnSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite", true);
            command_Action.action = delegate ()
            {
                SoundStarter.PlayOneShotOnCamera(SoundDefOf.Click, null);
                caravan.GetComponent<WorldObjectComp_Caravan>().startWorking();
            };
            return command_Action;
        }

        /*
        Stop working on  Site
         */
        public static Command StopWorkingOnSite(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimStopWorkingOnSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimStopWorkingOnSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/RemoveConstructionSite", true);
            command_Action.action = delegate ()
            {
                SoundStarter.PlayOneShotOnCamera(SoundDefOf.CancelMode, null);
                caravan.GetComponent<WorldObjectComp_Caravan>().stopWorking() ;
            };
            return command_Action;
        }

        /*
        Remove Construction Site
         */
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
            // Test when the RemoveConstructionSite action should be disabled (i.e. there's already a construction site here)
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
        Delete Construction Site
         */
        public static void DeleteConstructionSite(Caravan caravan)
        {
            RoadConstructionSite ConstructionSite = (RoadConstructionSite) Find.WorldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), caravan.Tile) ;
            if (ConstructionSite.resourcesAlreadyConsumed())
            {
                Messages.Message("RoadsOfTheRim_CantDestroyResourcesAlreadyConsumed".Translate(), MessageTypeDefOf.RejectInput);
            }
            else
            {
                Find.WorldObjects.Remove(ConstructionSite) ;
            }
        }

        // Compares the movement cost multiplier of 2 roaddefs, returns TRUE if roadA is better or roadB is null. returns FALSE in all other cases
        public static bool isRoadBetter(RoadDef roadA , RoadDef roadB)
        {
            if (roadA == null) 
            {
                return false ;
            }
            else if (roadB == null) 
            {
                return true ;
            }
            return (roadA.movementCostMultiplier < roadB.movementCostMultiplier) ;
        }
    }
}