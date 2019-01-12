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
        // Constants
        public const float MinBaseEffort = .1f;
        public const float DefaultBaseEffort = 1f;
        public const float MaxBaseEffort = 1f;
        public const float ElevationCostDouble = 2000f ;
        public const float HillinessCostDouble = 4f;
        public const float SwampinessCostDouble = 0.5f;
        public float BaseEffort = DefaultBaseEffort;
        public bool OverrideCosts = true;
        public float CostIncreaseElevationThreshold = 1000 ;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref BaseEffort, "BaseEffort", DefaultBaseEffort, true);
            Scribe_Values.Look<bool>(ref OverrideCosts, "OverrideCosts", true, true);
            Scribe_Values.Look<float>(ref CostIncreaseElevationThreshold, "CostIncreaseElevationThreshold", 1000 , true);
        }
    }

    public class RoadsOfTheRim : Mod
    {
        public static RoadsOfTheRimSettings settings;

        /*
        For construction help from ally , I will need a dictionary whenCanFactionHelp <Faction , int> that stores the ticks when that faction can help again 
         */

        public RoadsOfTheRim(ModContentPack content) : base(content)
        {
            settings = GetSettings<RoadsOfTheRimSettings>();
            HarmonyInstance harmony = HarmonyInstance.Create("Loconeko.Rimworld.RoadsOfTheRim");
            // Patching the Caravan's Gizmos to add "Add construction Site" , "Remove construction Site" , "Work", "Stop working"
            harmony.Patch(typeof(Caravan).GetMethod("GetGizmos") , null , new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("GetGizmosPostfix")) , null);
            harmony.Patch(typeof(Caravan).GetMethod("GetInspectString"), null, new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("Caravan_GetInspectStringPostfix")), null);
            harmony.Patch(typeof(Tile).GetMethod("get_Roads"), null, new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("get_RoadsPostifx")), null);
            harmony.Patch(typeof(FactionDialogMaker).GetMethod("FactionDialogFor"), null, new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("FactionDialogForPostifx")), null);

            /* How I found the hidden methods :
            var methods = typeof(Tile).GetMethods();
            foreach (var method in methods)
            {
                Log.Message(method.Name);
            }
            */
        }

        public override string SettingsCategory() => "RoadsOfTheRimSettingsCategoryLabel".Translate();

        public override void DoSettingsWindowContents(Rect rect)
        {
            bool CurrentOverOverrideCosts = settings.OverrideCosts;
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(rect);
            listing_Standard.Label("RoadsOfTheRimSettingsBaseEffort".Translate() + ": " + string.Format("{0:0%}", settings.BaseEffort));
            listing_Standard.Gap();
            settings.BaseEffort = (float)listing_Standard.Slider(settings.BaseEffort, RoadsOfTheRimSettings.MinBaseEffort, RoadsOfTheRimSettings.MaxBaseEffort);
            listing_Standard.Gap();
            listing_Standard.CheckboxLabeled("RoadsOfTheRimSettingsOverrideCosts".Translate() + ": ", ref settings.OverrideCosts);
            listing_Standard.End();
            settings.Write();
            if (CurrentOverOverrideCosts != settings.OverrideCosts)
            {
                try
                {
                    Find.WorldPathGrid.RecalculateAllPerceivedPathCosts();
                    Find.World.renderer.RegenerateAllLayersNow();
                }
                catch
                {
                }
            }
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

        public static void Caravan_GetInspectStringPostfix(ref string __result, Caravan __instance)
        {
            try
            {
                WorldObjectComp_Caravan CaravanComp = __instance.GetComponent<WorldObjectComp_Caravan>();
                if (CaravanComp != null && CaravanComp.currentlyWorkingOnSite)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(__result);
                    stringBuilder.AppendLine();
                    stringBuilder.Append("RoadsOfTheRim_CaravanInspectStringWorkingOn".Translate(CaravanComp.getSite().fullName()));
                    __result = stringBuilder.ToString();
                }
            }
            catch
            {
                // lazy way out : the caravan can, on occasions (mainly debug teleport, though...), not have a site linked to the comp
            }
        }

        public static void get_RoadsPostifx(ref List<Tile.RoadLink> __result , Tile __instance)
        {
            if (__result!=null)
            {
                bool overrideCosts = settings.OverrideCosts ;
                List<Tile.RoadLink> patchedRoads = new List<Tile.RoadLink>();
                foreach (Tile.RoadLink aLink in __result)
                {
                    Tile.RoadLink aRoadLink = new Tile.RoadLink();
                    RoadDef aRoad = aLink.road;
                    float biomeMovementDifficultyEffect = 0 ; // To be taken from road type : 1 (best) , 0.75 , 0.25 , 0 (worst)
                    // Roads cancel biome movement difficulty
                    // e.g : Biome is at 3, effect is at 0.75 : we get a multiplier of .5, combined with the biome of 3, we'll only get 1.5 for biome
                    // i.e. : effect is at 0, we always get a multiplier of 1 (no effect)
                    // effect is at 1, we always get a multiplier of 1/biome, which effectively cancels biome effects
                    float biomeMovementDifficultyCancellation = (1 + (__instance.biome.movementDifficulty - 1) * (1-biomeMovementDifficultyEffect)) / __instance.biome.movementDifficulty ;
                    // If the settings are set to override default costs, apply them, otherwise use default (0.5) , multiply by how much the road cancels the biome movement difficulty
                    aRoad.movementCostMultiplier = (settings.OverrideCosts ? aLink.road.movementCostMultiplier : 0.5f) * biomeMovementDifficultyCancellation ;
                    aRoadLink.neighbor = aLink.neighbor;
                    aRoadLink.road = aRoad;
                    patchedRoads.Add(aRoadLink);
                }
                __result = patchedRoads;
            }
        }

        public static void FactionDialogForPostifx(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            if (faction.PlayerRelationKind == FactionRelationKind.Ally)
            {
                __result.options.Insert(0, HelpRoadConstruction(faction, negotiator));
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

            /*
            TO DO : If I want to change this to a nice looking cartridge-style option picker, I can start by looking at DingoDjango/DeepOreIdentifier
            this tells me quite a lot about how to draw on the UI
            Layout : 1 vertical cartridge per type of buidalble road (no need to show anything if empty)
            Each cartridge has :
            - A square image at the top, representing the typoe of road
            - The name of the road below
            - A list of costs, one per line :
            > Work
            > Wood
            > Stone
            > Steel
            > Chemfuel
            Each cost could be represented by the icon of the resource (need to think of a work icon)
            For resources with a cost of 0, don't display them

            Upon hover, the cartridge should be highligthed
            Upon clicking outside, the cartridge should disappear
            Upon clicking on it, we cna finally call FinaliseConstructionSite(caravan.Tile, toTile_int, thisRoadBuildableDef);

            Check, among others :
            * Widgets many methods
            */
            /* Go through all the RoadBuildableDefs and show them in a float menu when creating construction site*/
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (RoadBuildableDef thisRoadBuildableDef in DefDatabase<RoadBuildableDef>.AllDefs)
            {

                if (bestExistingRoad == null || isRoadBetter(DefDatabase<RoadDef>.GetNamed(thisRoadBuildableDef.roadDef, true), bestExistingRoad))
                {
                    list.Add(new FloatMenuOption(
                        "RoadsOfTheRim_CreateConstructionSiteMenuOption".Translate(thisRoadBuildableDef.label , thisRoadBuildableDef.movementCostMultiplier),
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
            // disable based on : __instance.GetComponent<WorldObjectComp_Caravan>().CaravanCanWork();
            if (!caravan.GetComponent<WorldObjectComp_Caravan>().CaravanCanWork())
            {
                command_Action.Disable("RoadsOfTheRimBuildWorkOnSiteCantWork".Translate());
            }
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

        private static DiaOption HelpRoadConstruction(Faction faction, Pawn negotiator)
        {
            // Find all construction sites on the world map
            IEnumerable<WorldObject> constructionSites = Find.WorldObjects.AllWorldObjects.Cast<WorldObject>().Where(site => site.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true)).ToArray() ;
            DiaOption dialog = new DiaOption("RoadsOfTheRim_commsAskHelp".Translate());

            // If none : option should be disabled
            if (!constructionSites.Any()) dialog.Disable("RoadsOfTheRim_commsNoSites".Translate());
            // TO DO : 
            // If the Faction is currently in construction cooldown, this should be disabled
            // Construction cooldown needs to be added to factions

            DiaNode diaNode = new DiaNode("RoadsOfTheRim_commsSitesList".Translate());
            foreach (RoadConstructionSite site in constructionSites)
            {
                DiaOption diaOption = new DiaOption(site.fullName())
                {
                    action = delegate
                    {
                        // TO DO
                        // Here : test success or failure (maybe even partial success
                        // Calculate how much a Faction can help based on nearby settlements
                        // trigger an event that will help construction of that site, with a delay, and for a certain amount of time. This can be put in the construction site (tick from where help starts, + amount of help)
                        // Make sure the faction has a cooldown for construction, to ensure proper MTB
                        // Remember to lower goodwill by 10
                        // Also, work should stop in the event the faction is not an ally any more : must patch faction.FactionTick()
                    }
                };
                diaNode.options.Add(diaOption) ;
                diaOption.resolveTree = true ;
            }
            dialog.link = diaNode ;
            return dialog ;
        }

        // Compares the movement cost multiplier of 2 roaddefs, returns TRUE if roadA is better or roadB is null. returns FALSE in all other cases
        public static bool isRoadBetter(RoadDef roadA , RoadDef roadB)
        {
            if (roadA == null) 
            {
                return false ;
            }
            if (roadB == null)
            {
                return true ;
            }
            return (roadA.movementCostMultiplier < roadB.movementCostMultiplier) ;
        }
    }
}