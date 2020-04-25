using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RoadsOfTheRim
{

    public class CompProperties_RoadsOfTheRimConstructionSite : WorldObjectCompProperties
    {
        public CompProperties_RoadsOfTheRimConstructionSite()
        {
            this.compClass = typeof(WorldObjectComp_ConstructionSite);
        }
    }

    public class WorldObjectComp_ConstructionSite : WorldObjectComp
    {

        public CompProperties_RoadsOfTheRimConstructionSite properties
        {
            get
            {
                return (CompProperties_RoadsOfTheRimConstructionSite)props;
            }
        }

        // TO DO : Make those 2 private
        public Dictionary<string , int> costs = new Dictionary<string, int>() ; 

        public Dictionary<string , float> left = new Dictionary<string, float>() ;

        // Used for ExposeData()
        private List<string> costs_Keys = new List<string>();
        private List<int> costs_Values = new List<int>();
        private List<string> left_Keys = new List<string>();
        private List<float> left_Values = new List<float>();

        public int GetCost(string name)
        {
            int value = 0 ;
            if (!costs.TryGetValue(name , out value))
            {
                return 0 ; // TO DO : Throwing an excepion would be bettah
            }
            return value ;
        }

        public float GetLeft(string name)
        {
            float value = 0 ;
            if (!left.TryGetValue(name , out value))
            {
                return 0 ; // TO DO : Throwing an excepion would be bettah
            }
            return value ;
        }

        public void ReduceLeft (string name, float amount)
        {
            float value =0 ;
            if (left.TryGetValue(name, out value))
            {
                left[name] -= (amount > value) ? value : amount ;
            }
        }

        public float GetPercentageDone(string name)
        {
            if (!costs.TryGetValue(name, out int costTotal) & !left.TryGetValue(name, out float leftTotal))
            {
                return 0;
            }
            return (float)(costTotal - leftTotal) / (float)costTotal;
        }

        /*
        Returns the cost modifiers for building a road from one tile to another, based on Elevation, Hilliness, Swampiness & river crossing
         */
        public static void GetCostsModifiers(int fromTile_int , int toTile_int , ref float elevationModifier , ref float hillinessModifier , ref float swampinessModifier , ref float bridgeModifier)
        {
            try 
            {
                RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();
                Tile fromTile = Find.WorldGrid[fromTile_int];
                Tile toTile = Find.WorldGrid[toTile_int];

                // Cost increase from elevation : if elevation is above {CostIncreaseElevationThreshold} (default 1000m), cost is doubled every {ElevationCostDouble} (default 2000m)
                elevationModifier = ((fromTile.elevation <= settings.CostIncreaseElevationThreshold) ? 0 : (fromTile.elevation - settings.CostIncreaseElevationThreshold) / RoadsOfTheRimSettings.ElevationCostDouble);
                elevationModifier += ((toTile.elevation <= settings.CostIncreaseElevationThreshold) ? 0 : (toTile.elevation - settings.CostIncreaseElevationThreshold) / RoadsOfTheRimSettings.ElevationCostDouble);

                // Hilliness and swampiness are the average between that of the from & to tiles
                // Hilliness is 0 on flat terrain, never negative. It's between 0 (flat) and 5(Impassable)
                float hilliness = Math.Max((((float)fromTile.hilliness + (float)toTile.hilliness) / 2) - 1, 0);
                float swampiness = (fromTile.swampiness + toTile.swampiness) / 2;

                // Hilliness and swampiness double the costs when they equal {HillinessCostDouble} (default 4) and {SwampinessCostDouble} (default 0.5)
                hillinessModifier = hilliness / RoadsOfTheRimSettings.HillinessCostDouble;
                swampinessModifier = swampiness / RoadsOfTheRimSettings.SwampinessCostDouble;

                bridgeModifier = 0f;
                /* TO DO : River crossing
                List<int> fromTileNeighbors = new List<int>();
                Find.WorldGrid.GetTileNeighbors(parent.Tile, fromTileNeighbors);
                foreach (Tile.RiverLink aRiver in fromTile.Rivers )
                {
                    Log.Message("River in FROM tile : neighbor="+aRiver.neighbor+", river="+aRiver.river.ToString());
                }
                */
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog(null , e);
            }
        }

        /*
         * For resources (including work) that are part of the cost of both the road to build and the best existing road, 
         * grant CostUpgradeRebate% (default 30%) of the best existing road build costs as a rebate on the costs of the road to be built
         * i.e. the exisitng road cost 300 stones, the new road cost 600 stones, the rebate is 300*30% = 90 stones
         */
        public static void GetUpgradeModifiers(int fromTile_int , int toTile_int , RoadDef roadToBuild , out Dictionary<string , int> rebate)
        {
            rebate = new Dictionary<string , int>() ;
            RoadDef bestExistingRoad = RoadsOfTheRim.BestExistingRoad(fromTile_int, toTile_int) ;
            if (bestExistingRoad!=null)
            {
                DefModExtension_RotR_RoadDef bestExistingRoadDefModExtension = bestExistingRoad.GetModExtension<DefModExtension_RotR_RoadDef>() ;
                DefModExtension_RotR_RoadDef roadToBuildRoadDefModExtension = roadToBuild.GetModExtension<DefModExtension_RotR_RoadDef>() ;
                if (bestExistingRoadDefModExtension!=null && roadToBuildRoadDefModExtension!=null && RoadsOfTheRim.isRoadBetter(roadToBuild , bestExistingRoad))
                {
                    foreach (string resourceName in DefModExtension_RotR_RoadDef.allResourcesAndWork)
                    {
                        int existingCost = bestExistingRoadDefModExtension.GetCost(resourceName) ;
                        int toBuildCost = roadToBuildRoadDefModExtension.GetCost(resourceName) ;
                        if (existingCost!=0 && toBuildCost!=0)
                        {
                            if ( (int)(existingCost * (float)RoadsOfTheRim.settings.CostUpgradeRebate / 100) > toBuildCost)
                            {
                                rebate[resourceName] = toBuildCost ;
                            }
                            else
                            {
                                rebate[resourceName] = (int)(existingCost * (float)RoadsOfTheRim.settings.CostUpgradeRebate / 100) ; 
                            }
                        }
                    }
                }
            }
        }

        /*
         * Faction help must be handled here, since it's independent of whether or not a caravan is here.
         * Make it with a delay of 1/50 s compared to the CaravanComp so both functions end up playing nicely along each other
         * Don't work at night !
         */       
        public override void CompTick()
        {
            try
            {
                if ((((RoadConstructionSite)parent).helpFromFaction != null) && (!CaravanNightRestUtility.RestingNowAt(((RoadConstructionSite)parent).Tile)) && (Find.TickManager.TicksGame % 100 == 50))
                {
                    ((RoadConstructionSite)parent).TryToSkipBetterRoads() ;  // No need to work if there's a better road here
                    float amountOfWork = ((RoadConstructionSite)parent).factionHelp();

                    float percentOfWorkLeftToDoAfter = ((float)GetLeft("Work") - amountOfWork) / (float)GetCost("Work");
                    foreach (string resourceName in DefModExtension_RotR_RoadDef.allResources)
                    {
                        ReduceLeft ( resourceName, (int)Math.Round( (float)GetLeft(resourceName) - (percentOfWorkLeftToDoAfter * (float)GetCost(resourceName)) ) );
                    }
                    UpdateProgress(amountOfWork);
                }
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog("Construction Site CompTick. parentsite = "+ ((RoadConstructionSite)parent), e);
            }
        }

        public string ResourcesAlreadyConsumed()
        {
            List<String> l = new List<string>() ;
            try
            {
                foreach (string resourceName in DefModExtension_RotR_RoadDef.allResourcesAndWork)
                {
                    if (GetCost(resourceName) > 0 && GetLeft(resourceName) < GetCost(resourceName))
                    {
                        l.Add(String.Format("{0} {1}", GetCost(resourceName) - GetLeft(resourceName), resourceName));
                    }
                }
            }
            catch
            {
                RoadsOfTheRim.DebugLog("resourcesAlreadyConsumed failed. This will happen after upgrading to the 20190207 version") ;
            }
            return String.Join(", " , l.ToArray());
        }

        public void setCosts()
        {
            try
            {
                RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();
                RoadConstructionSite parentSite = this.parent as RoadConstructionSite;

                float elevationModifier = 0f;
                float hillinessModifier = 0f;
                float swampinessModifier = 0f;
                float bridgeModifier = 0f;
                GetCostsModifiers(parentSite.Tile , parentSite.GetNextLeg().Tile , ref elevationModifier , ref hillinessModifier , ref swampinessModifier , ref bridgeModifier) ;

                // Total cost modifier
                float totalCostModifier = (1 + elevationModifier + hillinessModifier + swampinessModifier + bridgeModifier) * ((float)settings.BaseEffort / 10);

                DefModExtension_RotR_RoadDef roadDefExtension = parentSite.roadDef.GetModExtension<DefModExtension_RotR_RoadDef>();

                // Check existing roads for potential rebates when upgrading
                GetUpgradeModifiers(parentSite.Tile , parentSite.GetNextLeg().Tile , parentSite.roadDef , out Dictionary<string , int> rebate) ;

                List<string> s = new List<string>();
                foreach (string resourceName in DefModExtension_RotR_RoadDef.allResourcesAndWork)
                {
                    if (roadDefExtension.GetCost(resourceName)>0)
                    {
                        int thisRebate = 0;
                        rebate.TryGetValue(resourceName, out thisRebate);
                        costs[resourceName] = (int)((roadDefExtension.GetCost(resourceName) - thisRebate) * totalCostModifier);
                        left[resourceName] = costs[resourceName];
                        if (thisRebate>0)
                        {
                            s.Add("RoadsOfTheRim_UpgradeRebateDetail".Translate((int)(thisRebate * totalCostModifier) , resourceName));
                        }
                    }
                }

                if (s.Count > 0)
                {
                    Messages.Message("RoadsOfTheRim_UpgradeRebate".Translate(parentSite.roadDef.label, string.Join(", ", s.ToArray())), MessageTypeDefOf.PositiveEvent);
                }

                parentSite.UpdateProgressBarMaterial();
            }
            catch (Exception e)
            {
                Log.Error("[RotR] : Exception when setting constructionSite costs = " + e);
            }
        }

        public bool UpdateProgress(float amountOfWork, Caravan caravan = null)
        {
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;

            ReduceLeft("Work", amountOfWork);

            parentSite.UpdateProgressBarMaterial();

            // Work is done
            if (GetLeft("Work") <= 0)
            {
                return finishWork(caravan);
            }
            return false;
        }

        /*
         * Build the road and move the construction site
         */
        public bool finishWork(Caravan caravan = null)
        {
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;
            int fromTile_int = parentSite.Tile;
            int toTile_int = parentSite.GetNextLeg().Tile;
            Tile fromTile = Find.WorldGrid[fromTile_int];
            Tile toTile = Find.WorldGrid[toTile_int];

            // Remove lesser roads, they don't deserve to live
            if (fromTile.potentialRoads != null)
            {
                foreach (Tile.RoadLink aLink in fromTile.potentialRoads.ToArray())
                {
                    if (aLink.neighbor == toTile_int & RoadsOfTheRim.isRoadBetter(parentSite.roadDef, aLink.road))
                    {
                        fromTile.potentialRoads.Remove(aLink);
                    }
                }
            }
            else
            {
                fromTile.potentialRoads = new List<Tile.RoadLink>();
            }

            if (toTile.potentialRoads != null)
            {
                foreach (Tile.RoadLink aLink in toTile.potentialRoads.ToArray())
                {
                    if (aLink.neighbor == parentSite.Tile & RoadsOfTheRim.isRoadBetter(parentSite.roadDef, aLink.road))
                    {
                        toTile.potentialRoads.Remove(aLink);
                    }
                }
            }
            else
            {
                toTile.potentialRoads = new List<Tile.RoadLink>();
            }

            // Add the road to fromTile & toTile
            fromTile.potentialRoads.Add(new Tile.RoadLink { neighbor = toTile_int, road = parentSite.roadDef });
            toTile.potentialRoads.Add(new Tile.RoadLink { neighbor = fromTile_int, road = parentSite.roadDef });
            try
            {
                Find.World.renderer.SetDirty<WorldLayer_Roads>();
                Find.World.renderer.SetDirty<WorldLayer_Paths>();
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(fromTile_int);
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(toTile_int);
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog("[RotR] Exception : ", e);
            }

            // The Construction site and the caravan can move to the next leg
            RoadConstructionLeg nextLeg = parentSite.GetNextLeg();
            if (nextLeg != null)
            {
                int CurrentTile = parentSite.Tile;
                parentSite.Tile = nextLeg.Tile;
                RoadConstructionLeg nextNextLeg = nextLeg.Next;
                // TO DO Here : Check if there's an existing road that is the same or better as the one being built. If there is, skip the next leg
                if (nextNextLeg != null)
                {
                    nextNextLeg.Previous = null;
                    setCosts();
                    parentSite.MoveWorkersToNextLeg(CurrentTile); // Move any caravans working on this site to the next leg, and delay faction help if any
                }
                else
                {
                    EndConstruction(caravan) ; // We have built the last leg. Notify & remove the site
                }
                Find.World.worldObjects.Remove(nextLeg);
            }

            return true;
        }

        public void EndConstruction(Caravan caravan = null)
        {
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;
            // On the last leg, send letter & remove the construction site
            Find.LetterStack.ReceiveLetter(
                "RoadsOfTheRim_RoadBuilt".Translate(),
                "RoadsOfTheRim_RoadBuiltLetterText".Translate(parentSite.roadDef.label, (caravan != null ? (TaggedString)caravan.Label : "RoadsOfTheRim_RoadBuiltByAlly".Translate())),
                LetterDefOf.PositiveEvent,
                new GlobalTargetInfo(parentSite.Tile)
            );
            Find.World.worldObjects.Remove(parentSite);
            if (parentSite.helpFromFaction != null)
            {
                RoadsOfTheRim.factionsHelp.helpFinished(parentSite.helpFromFaction);
            }
        }

        public string progressDescription()
        {
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Main".Translate(String.Format("{0:P1}", GetPercentageDone("Work"))));

            // Description of ally's help, if any
            if (parentSite.helpFromFaction != null)
            {
                stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Help".Translate(parentSite.helpFromFaction.Name, (int)parentSite.helpAmount, String.Format("{0:0.0}", parentSite.helpWorkPerTick)));
                if (parentSite.helpFromTick > Find.TickManager.TicksGame)
                {
                    stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_HelpStartsWhen".Translate(String.Format("{0:0.00}", (float)(parentSite.helpFromTick - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay)));
                }
            }
            // Show total cost modifiers
            float elevationModifier = 0f;
            float hillinessModifier = 0f;
            float swampinessModifier = 0f;
            float bridgeModifier = 0f;
            GetCostsModifiers(parentSite.Tile , parentSite.GetNextLeg().Tile , ref elevationModifier , ref hillinessModifier , ref swampinessModifier , ref bridgeModifier) ;
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_CostModifiers".Translate(
                String.Format("{0:P0}",elevationModifier + hillinessModifier + swampinessModifier + bridgeModifier) ,
                String.Format("{0:P0}",elevationModifier) , String.Format("{0:P0}",hillinessModifier) , String.Format("{0:P0}",swampinessModifier) , String.Format("{0:P0}",bridgeModifier)
            )) ;


            List<Caravan> AllCaravansHere = new List<Caravan>() ;
            Find.WorldObjects.GetPlayerControlledCaravansAt(parentSite.Tile , AllCaravansHere) ;
            int ISR2G = 0;
            foreach (Caravan c in AllCaravansHere)
            {
                int caravanISR2G = c.GetComponent<WorldObjectComp_Caravan>().useISR2G();
                if (caravanISR2G > ISR2G)
                {
                    ISR2G = caravanISR2G;
                }
            }

            // Per resource : show costs & how much is left to do
            foreach (string resourceName in DefModExtension_RotR_RoadDef.allResourcesAndWork)
            {
                if (GetCost(resourceName) > 0)
                {
                    stringBuilder.AppendLine();
                    string ISR2Gmsg = "";
                    if (ISR2G>0)
                    {
                        if (resourceName=="Work")
                        {
                            ISR2Gmsg = (ISR2G == 1 ? "RoadsOfTheRim_ConstructionSiteDescription_ISR2Gwork".Translate() : "RoadsOfTheRim_ConstructionSiteDescription_AISR2Gwork".Translate()) ;
                        }
                        else if (DefModExtension_RotR_RoadDef.GetInSituModifier(resourceName, ISR2G))
                        {
                            ISR2Gmsg = (ISR2G == 1 ? "RoadsOfTheRim_ConstructionSiteDescription_ISR2GFree".Translate() : "RoadsOfTheRim_ConstructionSiteDescription_AISR2GFree".Translate()) ;
                        }
                    }
                    stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Resource".Translate(
                        resourceName,
                        String.Format((resourceName=="Work" ? "{0:N2}" : "{0:N0}"), GetLeft(resourceName)), // Only Work should be shown with 2 decimals
                        (int)GetCost(resourceName) ,
                        ISR2Gmsg
                    ));
                }
            }

            return stringBuilder.ToString();
        }

        public float percentageDone()
        {
            return GetPercentageDone("Work");
        }

        public override void PostExposeData()
        {
            Scribe_Collections.Look<string, int>(ref costs, "RotR_site_costs", LookMode.Value, LookMode.Value, ref costs_Keys , ref costs_Values);
            Scribe_Collections.Look<string, float>(ref left, "RotR_site_left", LookMode.Value, LookMode.Value, ref left_Keys, ref left_Values);
        }
    }
}
