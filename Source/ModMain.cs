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
using System;

namespace RoadsOfTheRim
{
    [StaticConstructorOnStartup]
    static class RotR_StaticConstructorOnStartup
    {
        public static readonly Texture2D ConstructionLeg_MouseAttachment = ContentFinder<Texture2D>.Get("UI/Overlays/ConstructionLeg", true);

        public static Material ConstructionLegLast_Material = MaterialPool.MatFrom("World/WorldObjects/ConstructionLegLast", ShaderDatabase.WorldOverlayTransparentLit , WorldMaterials.DynamicObjectRenderQueue ) ;
    }

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
        }


        public static WorldComponent_FactionRoadConstructionHelp factionsHelp
        {
            get 
            {
               WorldComponent_FactionRoadConstructionHelp f = Find.World.GetComponent(typeof(WorldComponent_FactionRoadConstructionHelp)) as WorldComponent_FactionRoadConstructionHelp;
               if (f != null)
               {
                   return f;
               }
               Log.Warning("[RotR] - ERROR, couldn't find WorldComponent_FactionRoadConstructionHelp");
               return null;
            }
        }

        public static WorldComponent_RoadBuildingState RoadBuildingState
        {
            get
            {
                WorldComponent_RoadBuildingState f = Find.World.GetComponent(typeof(WorldComponent_RoadBuildingState)) as WorldComponent_RoadBuildingState;
                if (f != null)
                {
                    return f;
                }
                Log.Message("[RotR] - ERROR, couldn't find WorldComponent_RoadBuildingState");
                return null;
            }
        }

        public static void DebugLog(String message = null , Exception e = null)
        {
            if (message!=null)
            {
                Log.Warning("[RotR] - " + message);
            }
            if (Prefs.DevMode && e!=null)
            {
                Log.Error(
                "[RotR] Exception :\n" + e + "\n=====\n" +
                "Stack trace :\n" + e.StackTrace +"\n=====\n"+
                "Data : " + e.Data
                );
            }
        }

        public static float calculateBiomeModifier(RoadDef roadDef, float biomeMovementDifficulty, out float biomeCancellation)
        {
            biomeCancellation = 0;
            try
            {
                if (roadDef.defName == "DirtRoad")
                {
                    biomeCancellation = 0.25f;
                }
                if (roadDef.defName == "StoneRoad")
                {
                    biomeCancellation = 0.75f;
                }
                if (roadDef.defName == "AncientAsphaltRoad")
                {
                    biomeCancellation = 1f;
                }
                // Roads cancel biome movement difficulty
                // e.g : Biome is at 3, effect is at 0.75 : we get a multiplier of .5, combined with the biome of 3, we'll only get 1.5 for biome
                // i.e. : effect is at 0, we always get a multiplier of 1 (no effect)
                // effect is at 1, we always get a multiplier of 1/biome, which effectively cancels biome effects
                return (1 + (biomeMovementDifficulty - 1) * (1 - biomeCancellation)) / biomeMovementDifficulty;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        public static float calculateRoadModifier(RoadDef roadDef, float BiomeMovementDifficulty , float HillinessOffset , float WinterOffset , out float biomeCoef, out float HillModifier)
        {
            biomeCoef = 0f ;
            HillModifier = 1f;
            if (roadDef.defName == "DirtRoad")
            {
                biomeCoef = 0.25f;
                HillModifier = 0.8f;
            }
            if (roadDef.defName == "StoneRoad")
            {
                biomeCoef = 0.75f;
                HillModifier = 0.6f;
            }
            if (roadDef.defName == "AncientAsphaltRoad" || roadDef.defName == "AncientAsphaltHighway")
            {
                biomeCoef = 1f;
                HillModifier = 0.4f;
            }
            float BiomeModifier = (1 + (BiomeMovementDifficulty-1) * (1-biomeCoef)) / BiomeMovementDifficulty ;
            return ((BiomeModifier*BiomeMovementDifficulty) + (HillModifier*HillinessOffset) + WinterOffset ) / (BiomeMovementDifficulty + HillinessOffset + WinterOffset) ;
        }

        /*
        Returns the road with the best movement cost multiplier between 2 neighbouring tiles.
        returns null if there's no road or if the tiles are not neighbours
         */
        public static RoadDef BestExistingRoad(int fromTile_int , int toTile_int)
        {
            RoadDef bestExistingRoad = null;
            try
            {
                WorldGrid worldGrid = Find.WorldGrid ;
                Tile fromTile = worldGrid[fromTile_int];
                Tile toTile = worldGrid[toTile_int];

                if (fromTile.potentialRoads != null)
                {
                    foreach (Tile.RoadLink aLink in fromTile.potentialRoads)
                    {
                        if (aLink.neighbor == toTile_int & RoadsOfTheRim.isRoadBetter(aLink.road , bestExistingRoad))
                        {
                            bestExistingRoad = aLink.road ;
                        }
                    }
                }
                if (toTile.potentialRoads != null)
                {
                    foreach (Tile.RoadLink aLink in toTile.potentialRoads)
                    {
                        if (aLink.neighbor == fromTile_int & RoadsOfTheRim.isRoadBetter(aLink.road , bestExistingRoad))
                        {
                            bestExistingRoad = aLink.road ;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DebugLog(null , e) ;
            }
            
            return bestExistingRoad ;
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
                }
                catch
                {
                    // Ugly. I should just check if the WorldPathGrid exists.
                }
            }
        }

        public static Command AddConstructionSite(Caravan caravan)
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimAddConstructionSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimAddConstructionSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite", true);
            command_Action.action = delegate ()
            {
                RoadConstructionSite constructionSite = (RoadConstructionSite)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true));
                constructionSite.Tile = caravan.Tile;
                Find.WorldObjects.Add(constructionSite);

                ConstructionMenu menu = new ConstructionMenu(constructionSite);
                if (menu.CountBuildableRoads() == 0)
                {
                    Find.WorldObjects.Remove(constructionSite);
                    Messages.Message("RoadsOfTheRim_NoBetterRoadCouldBeBuilt".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    menu.closeOnClickedOutside = true;
                    menu.forcePause = true;
                    Find.WindowStack.Add(menu);
                }
            };
            
            // Disable if there's already a construction site here
            if (Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), caravan.Tile))
            {
                command_Action.Disable("RoadsOfTheRimBuildConstructionSiteAlreadyHere".Translate());
            }
            
            // disable if the caravan can't work OR if the site is not ready
            if (caravan.GetComponent<WorldObjectComp_Caravan>().CaravanCurrentState() != CaravanState.ReadyToWork)
            {
                command_Action.Disable("RoadsOfTheRimBuildWorkOnSiteCantWork".Translate());
            }

            // Disable on biomes that don't allow roads
            BiomeDef biomeHere = Find.WorldGrid.tiles[caravan.Tile].biome ;
            if (!biomeHere.allowRoads)
            {
                command_Action.Disable("RoadsOfTheRim_BiomePreventsConstruction".Translate(biomeHere.label));
            }
            return command_Action;
        }

        public static void FinaliseConstructionSite(RoadConstructionSite site)
        {
            // Log.Warning("[RotR] - FinaliseConstructionSite");
            if (site.GetNextLeg()!=null)
            {
                site.GetComponent<WorldObjectComp_ConstructionSite>().setCosts();
            }
            else
            {
                RoadConstructionSite.DeleteSite(site);
            }
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
            // disable if the caravan can't work OR if the site is not ready
            if (caravan.GetComponent<WorldObjectComp_Caravan>().CaravanCurrentState() != CaravanState.ReadyToWork)
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
        public static Command RemoveConstructionSite(int tile)
        {
            // TO DO : Refactor this so we find the site first, to pass it to Deleteconstructionsite directly, or even get rid of that function all together
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = "RoadsOfTheRimRemoveConstructionSite".Translate();
            command_Action.defaultDesc = "RoadsOfTheRimRemoveConstructionSiteDescription".Translate();
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/RemoveConstructionSite", true);
            command_Action.action = delegate ()
            {
                SoundStarter.PlayOneShotOnCamera(SoundDefOf.CancelMode, null);
                DeleteConstructionSite(tile);
            };
            // Test when the RemoveConstructionSite action should be disabled (i.e. there's no construction site here)
            bool ConstructionSiteAlreadyHere = false;
            try
            {
                ConstructionSiteAlreadyHere = Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), tile);
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
        public static void DeleteConstructionSite(int tile)
        {
            RoadConstructionSite ConstructionSite = (RoadConstructionSite) Find.WorldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), tile) ;
            if (ConstructionSite!=null)
            {
                if (ConstructionSite.resourcesAlreadyConsumed())
                {
                    Messages.Message("RoadsOfTheRim_CantDestroyResourcesAlreadyConsumed".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    if (ConstructionSite.helpFromFaction != null)
                    {
                        RoadsOfTheRim.factionsHelp.helpFinished(ConstructionSite.helpFromFaction);
                    }
                    RoadConstructionSite.DeleteSite(ConstructionSite);
                }
            }
        }

        public static DiaOption HelpRoadConstruction(Faction faction, Pawn negotiator)
        {
            DiaOption dialog = new DiaOption("RoadsOfTheRim_commsAskHelp".Translate());

            // If the faction is already helping, it must be disabled
            if (RoadsOfTheRim.factionsHelp.getCurrentlyHelping(faction)) dialog.Disable("RoadsOfTheRim_commsAlreadyHelping".Translate());

            // If the faction is in construction cooldown, it must be disabled
            if (RoadsOfTheRim.factionsHelp.inCooldown(faction)) dialog.Disable("RoadsOfTheRim_commsHasHelpedRecently".Translate(string.Format("{0:0.0}", RoadsOfTheRim.factionsHelp.daysBeforeFactionCanHelp(faction))));

            // Find all construction sites on the world map
            IEnumerable<WorldObject> constructionSites = Find.WorldObjects.AllWorldObjects.Cast<WorldObject>().Where(site => site.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true)).ToArray() ;
            // If none : option should be disabled
            if (!constructionSites.Any()) dialog.Disable("RoadsOfTheRim_commsNoSites".Translate());

            DiaNode diaNode = new DiaNode("RoadsOfTheRim_commsSitesList".Translate());
            foreach (RoadConstructionSite site in constructionSites)
            {
                DiaOption diaOption = new DiaOption(site.fullName())
                {
                    // TO DO  disable sites that already receive help (only one faction can help per site)

                    action = delegate
                    {
                        RoadsOfTheRim.factionsHelp.startHelping(faction , site , negotiator) ;
                    }
                };
                // Disable sites that do not have a settlement of this faction close enough (as defined by ConstructionSite.maxTicksToNeighbour)
                if (site.closestSettlementOfFaction(faction)==null)
                {
                    diaOption.Disable("RoadsOfTheRim_commsNotClose".Translate(faction.Name));
                }
                if (site.helpFromFaction!=null)
                {
                    diaOption.Disable("RoadsOfTheRim_commsAnotherFactionIsHelping".Translate(site.helpFromFaction));
                }
                diaNode.options.Add(diaOption);
                diaOption.resolveTree = true ;
            }
            // Cancel option (needed when all sites are disabled for one of the above reason)
            DiaOption cancelOption = new DiaOption("(" + "RoadsOfTheRim_commsCancel".Translate() + ")");
            diaNode.options.Add(cancelOption);
            cancelOption.resolveTree = true;

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