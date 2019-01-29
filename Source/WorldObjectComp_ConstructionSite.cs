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

        public struct Resource
        {
            public float cost;
            public float left;

            public void setCost(float f)
            {
                cost = f;
                left = f;
            }

            public void reduceLeft(float f)
            {
                left -= (f > left) ? left : f;
            }

            public float getCost() { return cost; }
            public float getLeft() { return left; }
            public float getPercentageDone()
            {
                if (cost <= 0)
                {
                    return 1f;
                }
                return 1f - (left / cost);
            }
        }

        private Resource work;
        private Resource wood;
        private Resource stone;
        private Resource steel;
        private Resource chemfuel;

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
        Returns the amount of each resource that can be saved when building a road, based on existing roads
         */
        /*
        public static void GetUpgradeModifiers(int fromTile_int , int toTile_int , RoadBuildableDef roadToBuildBuildableDef ,  ref float workRebate , ref float woodRebate , ref float stoneRebate , ref float steelRebate , ref float chemfuelRebate)
        {
            RoadDef bestExistingRoad = RoadsOfTheRim.BestExistingRoad(fromTile_int , toTile_int);

            // There is already a road here & the one to be built is better
            if (bestExistingRoad!=null && RoadsOfTheRim.isRoadBetter(DefDatabase<RoadDef>.GetNamed(roadToBuildBuildableDef.getRoadDef()) , bestExistingRoad))
            {
                // Give X% of the cost of the orignal road as a rebate for the new road
                string buildableDefName = "DirtPath" ;
                RoadBuildableDef existingRoadBuildableDef = DefDatabase<RoadBuildableDef>.GetNamed(buildableDefName) ;

            }
        }
        */

        public override void CompTick()
        {
            // Faction help must be handled here, since it's independent of whether or not a caravan is here.
            // Make it with a delay of 1/50 s compared to the CaravanComp so both functions end up playing nicely along each other
            // Don't work at night !
            try
            {
                if ((((RoadConstructionSite)parent).helpFromFaction != null) && (!CaravanNightRestUtility.RestingNowAt(((RoadConstructionSite)parent).Tile)) && (Find.TickManager.TicksGame % 100 == 50))
                {
                    ((RoadConstructionSite)parent).TryToSkipBetterRoads() ;  // No need to work if there's a better road here
                    float amountOfWork = ((RoadConstructionSite)parent).factionHelp();
                    float percentOfWorkLeftToDoAfter = (work.left - amountOfWork) / work.cost;
                    wood.reduceLeft((int)Math.Round(wood.left - (percentOfWorkLeftToDoAfter * wood.cost)));
                    stone.reduceLeft((int)Math.Round(stone.left - (percentOfWorkLeftToDoAfter * stone.cost)));
                    steel.reduceLeft((int)Math.Round(steel.left - (percentOfWorkLeftToDoAfter * steel.cost)));
                    chemfuel.reduceLeft((int)Math.Round(chemfuel.left - (percentOfWorkLeftToDoAfter * chemfuel.cost)));
                    UpdateProgress(amountOfWork);
                }
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog("Construction Site CompTick. parentsite = "+ ((RoadConstructionSite)parent), e);
            }
        }

        public bool resourcesAlreadyConsumed()
        {
            return ((wood.left < wood.cost) | (stone.left < stone.cost) | (steel.left < steel.cost) | (chemfuel.left < chemfuel.cost));
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
                float totalCostModifier = (1 + elevationModifier + hillinessModifier + swampinessModifier + bridgeModifier) * settings.BaseEffort;

                /* TO DO : This debug info should be shown properly on the site and/or caravan working on it
                Log.Message( string.Format(
                "From Tile: {0}m , {1} hilliness , {2} swampiness. To Tile: {3}m , {4} hilliness , {5} swampiness. Average {6} hilliness, {7} swampiness" , 
                    fromTile.elevation , (float)fromTile.hilliness , fromTile.swampiness, toTile.elevation , (float)toTile.hilliness , toTile.swampiness , hilliness , swampiness
                ));
                Log.Message( string.Format(
                "Work cost increase from elevation: {0:0.00%}, from hilliness {1:0.00%}, from swampiness {2:0.00%}, from bridge crossing {3:0.00%}. Total cost : {4:0.00%}" , 
                    elevationCostIncrease , hillinessCostIncrease , swampinessCostIncrease, bridgeCostIncrease, totalCostModifier
                ));
                */
                RoadBuildableDef roadToBuild = parentSite.roadToBuild;

                float workRebate = 0f;
                float woodRebate = 0f;
                float stoneRebate = 0f;
                float steelRebate = 0f;
                float chemfuelRebate = 0f;
                // TO DO once I have changed the roads to new road defs
                // GetUpgradeModifiers(parentSite.Tile , parentSite.GetNextLeg().Tile , roadToBuild , ref workRebate , ref woodRebate , ref stoneRebate , ref steelRebate , ref chemfuelRebate) ;

                this.work.setCost((float)(roadToBuild.work - workRebate) * totalCostModifier);
                this.wood.setCost((float)(roadToBuild.wood - woodRebate) * settings.BaseEffort);
                this.stone.setCost((float)(roadToBuild.stone - stoneRebate) * settings.BaseEffort);
                this.steel.setCost((float)(roadToBuild.steel - steelRebate) * settings.BaseEffort);
                this.chemfuel.setCost((float)(roadToBuild.chemfuel - chemfuelRebate) * settings.BaseEffort);
                parentSite.UpdateProgressBarMaterial();
            }
            catch (Exception e)
            {
                Log.Error("[RotR] : Exception when setting constructionSite costs = " + e);
            }
        }

        /* 
         * Received a tick from a caravan with WorldObjectComp_Caravan
         * Returns TRUE if work finished
        */
        public bool doSomeWork(Caravan caravan)
        {
            WorldObjectComp_Caravan caravanComp = caravan.GetComponent<WorldObjectComp_Caravan>();
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;

            if (DebugSettings.godMode)
            {
                return finishWork(caravan);
            }

            if (!(caravanComp.CaravanCurrentState() == CaravanState.ReadyToWork))
            {
                Log.Message("[RotR] DEBUG : doSomeWork() failed because the caravan can't work.");
                return false;
            }

            // Percentage of total work that can be done in this batch
            float amountOfWork = caravanComp.amountOfWork();

            if (amountOfWork > this.work.getLeft())
            {
                amountOfWork = this.work.getLeft();
            }

            // calculate material present in the caravan
            int available_wood = 0;
            int available_stone = 0;
            int available_steel = 0;
            int available_chemfuel = 0;

            // DEBUG - StringBuilder inventoryDescription = new StringBuilder();
            foreach (Thing aThing in CaravanInventoryUtility.AllInventoryItems(caravan))
            {
                if (aThing.def.ToString() == "WoodLog")
                {
                    available_wood += aThing.stackCount;
                }
                else if ((aThing.def.FirstThingCategory != null) && (aThing.def.FirstThingCategory.ToString() == "StoneBlocks"))
                {
                    available_stone += aThing.stackCount;
                }
                else if (aThing.def.IsMetal)
                {
                    available_steel += aThing.stackCount;
                }
                else if (aThing.def.ToString() == "Chemfuel")
                {
                    available_chemfuel += aThing.stackCount;
                }
                // DEBUG - inventoryDescription.Append(aThing.def.ToString() + " {"+ aThing.def.FirstThingCategory.ToString()+"}: " + aThing.stackCount+", ") ;
            }
            // DEBUG - Log.Message(inventoryDescription.ToString());
            // DEBUG - Log.Message("Resources : Wood="+available_wood+", Stone="+available_stone+", Metal="+available_steel+", Chemfuel="+available_chemfuel);

            // What percentage of work will remain after amountOfWork is done ?
            float percentOfWorkLeftToDoAfter = (work.left - amountOfWork) / work.cost;

            // The amount of each resource left to spend in total is : percentOfWorkLeftToDoAfter * {this resource cost}
            // Materials that would be needed to do that much work
            int needed_wood = (int)Math.Round(wood.left - (percentOfWorkLeftToDoAfter * wood.cost));
            int needed_stone = (int)Math.Round(stone.left - (percentOfWorkLeftToDoAfter * stone.cost));
            int needed_steel = (int)Math.Round(steel.left - (percentOfWorkLeftToDoAfter * steel.cost));
            int needed_chemfuel = (int)Math.Round(chemfuel.left - (percentOfWorkLeftToDoAfter * chemfuel.cost));
            // DEBUG - Log.Message("Needed: Wood=" + needed_wood + ", Stone=" + needed_stone + ", Metal=" + needed_steel + ", Chemfuel=" + needed_chemfuel);

            // Check if there's enough material to go through this batch. Materials with a cost of 0 are always OK
            float ratio_wood = (needed_wood == 0 ? 1f : Math.Min((float)available_wood / (float)needed_wood, 1f));
            float ratio_stone = (needed_stone == 0 ? 1f : Math.Min((float)available_stone / (float)needed_stone, 1f));
            float ratio_steel = (needed_steel == 0 ? 1f : Math.Min((float)available_steel / (float)needed_steel, 1f));
            float ratio_chemfuel = (needed_chemfuel == 0 ? 1f : Math.Min((float)available_chemfuel / (float)needed_chemfuel, 1f));

            //There's a shortage of materials
            float ratio_final = Math.Min(ratio_wood, Math.Min(ratio_stone, Math.Min(ratio_steel, ratio_chemfuel)));

            // The caravan didn't have enough resources for a full batch of work. Use as much as we can then stop working
            if (ratio_final < 1f)
            {
                Messages.Message("RoadsOfTheRim_CaravanNoResource".Translate(caravan.Name, parentSite.roadToBuild.label), MessageTypeDefOf.RejectInput);
                needed_wood = (int)(needed_wood * ratio_final);
                needed_stone = (int)(needed_stone * ratio_final);
                needed_steel = (int)(needed_steel * ratio_final);
                needed_chemfuel = (int)(needed_chemfuel * ratio_final);
                caravanComp.stopWorking();
            }

            // Consume resources from the caravan 
            foreach (Thing aThing in CaravanInventoryUtility.AllInventoryItems(caravan))
            {
                if ((aThing.def.ToString() == "WoodLog") && needed_wood > 0)
                {
                    int used_wood = (aThing.stackCount > needed_wood) ? needed_wood : aThing.stackCount;
                    aThing.stackCount -= used_wood;
                    needed_wood -= used_wood;
                    wood.reduceLeft(used_wood);
                }
                else if ((aThing.def.FirstThingCategory != null) && (aThing.def.FirstThingCategory.ToString() == "StoneBlocks") && needed_stone > 0)
                {
                    int used_stone = (aThing.stackCount > needed_stone) ? needed_stone : aThing.stackCount;
                    aThing.stackCount -= used_stone;
                    needed_stone -= used_stone;
                    stone.reduceLeft(used_stone);
                }
                else if ((aThing.def.IsMetal) && needed_steel > 0)
                {
                    int used_steel = (aThing.stackCount > needed_steel) ? needed_steel : aThing.stackCount;
                    aThing.stackCount -= used_steel;
                    needed_steel -= used_steel;
                    steel.reduceLeft(used_steel);
                }
                else if ((aThing.def.ToString() == "Chemfuel") && needed_chemfuel > 0)
                {
                    int used_chemfuel = (aThing.stackCount > needed_chemfuel) ? needed_chemfuel : aThing.stackCount;
                    aThing.stackCount -= used_chemfuel;
                    needed_chemfuel -= used_chemfuel;
                    chemfuel.reduceLeft(used_chemfuel);
                }
                if (aThing.stackCount == 0)
                {
                    aThing.Destroy();
                }
            }

            // Update amountOfWork based on the actual ratio worked & finally reducing the work & resources left
            amountOfWork = ratio_final * amountOfWork;
            return UpdateProgress(amountOfWork, caravan);
        }

        public bool UpdateProgress(float amountOfWork, Caravan caravan = null)
        {
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;

            work.reduceLeft(amountOfWork);
            parentSite.UpdateProgressBarMaterial();

            // Work is done
            if (work.getLeft() <= 0)
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
            RoadDef newRoadDef = DefDatabase<RoadDef>.GetNamed(parentSite.roadToBuild.getRoadDef());

            // Remove lesser roads, they don't deserve to live
            if (fromTile.potentialRoads != null)
            {
                foreach (Tile.RoadLink aLink in fromTile.potentialRoads.ToArray())
                {
                    if (aLink.neighbor == toTile_int & RoadsOfTheRim.isRoadBetter(newRoadDef, aLink.road))
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
                    if (aLink.neighbor == parentSite.Tile & RoadsOfTheRim.isRoadBetter(newRoadDef, aLink.road))
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
            fromTile.potentialRoads.Add(new Tile.RoadLink { neighbor = toTile_int, road = newRoadDef });
            toTile.potentialRoads.Add(new Tile.RoadLink { neighbor = fromTile_int, road = newRoadDef });
            try
            {
                Find.World.renderer.SetDirty<WorldLayer_Roads>();
                Find.World.renderer.SetDirty<WorldLayer_Paths>();
            }
            catch (Exception e)
            {
                Log.Error("[RotR] Exception : "+e);
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
                "RoadsOfTheRim_RoadBuiltLetterText".Translate(parentSite.roadToBuild.label, (caravan != null ? caravan.Label : "RoadsOfTheRim_RoadBuiltByAlly".Translate())),
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
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Main".Translate(String.Format("{0:P1}", work.getPercentageDone())));

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
                String.Format("{0:P}",elevationModifier + hillinessModifier + swampinessModifier + bridgeModifier) ,
                String.Format("{0:P}",elevationModifier) , String.Format("{0:P}",hillinessModifier) , String.Format("{0:P}",swampinessModifier) , String.Format("{0:P}",bridgeModifier)
            )) ;
            
            // Per resource : show costs & how much is left to do
            stringBuilder.AppendLine();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Resource".Translate("work", (int)work.getLeft(), (int)work.getCost()));
            stringBuilder.AppendLine();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Resource".Translate("wood", (int)wood.getLeft(), (int)wood.getCost()));
            stringBuilder.AppendLine();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Resource".Translate("stone", (int)stone.getLeft(), (int)stone.getCost()));
            stringBuilder.AppendLine();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Resource".Translate("steel", (int)steel.getLeft(), (int)steel.getCost()));
            stringBuilder.AppendLine();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Resource".Translate("chemfuel", (int)chemfuel.getLeft(), (int)chemfuel.getCost()));
            return stringBuilder.ToString();
        }

        public float percentageDone()
        {
            return work.getPercentageDone();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<float>(ref work.cost, "cost_work", 0, true);
            Scribe_Values.Look<float>(ref wood.cost, "cost_wood", 0, true);
            Scribe_Values.Look<float>(ref stone.cost, "cost_stone", 0, true);
            Scribe_Values.Look<float>(ref steel.cost, "cost_steel", 0, true);
            Scribe_Values.Look<float>(ref chemfuel.cost, "cost_chemfuel", 0, true);
            Scribe_Values.Look<float>(ref work.left, "left_work", 0, true);
            Scribe_Values.Look<float>(ref wood.left, "left_wood", 0, true);
            Scribe_Values.Look<float>(ref stone.left, "left_stone", 0, true);
            Scribe_Values.Look<float>(ref steel.left, "left_steel", 0, true);
            Scribe_Values.Look<float>(ref chemfuel.left, "left_chemfuel", 0, true);
        }
    }
}
