using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RoadsOfTheRim
{
    public class WorldObjectComp_ConstructionSite : WorldObjectComp
    {

        public RoadConstructionSite parentSite;

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

        public WorldObjectComp_ConstructionSite()
        {
            parentSite = this.parent as RoadConstructionSite;
        }

        public override void CompTick()
        {
            // Faction help must be handled here, since it's independent of whether or not a caravan is here.
            // Make it with a delay of 1/50 s compared to the CaravanComp so both functions end up playing nicely along each other
            // Don't work at night !
            if ((!CaravanNightRestUtility.RestingNowAt(parentSite.Tile)) && (Find.TickManager.TicksGame % 100 == 50))
            {
                float amountOfWork = parentSite.factionHelp();
                float percentOfWorkLeftToDoAfter = (work.left - amountOfWork) / work.cost;
                wood.reduceLeft((int)Math.Round(wood.left - (percentOfWorkLeftToDoAfter * wood.cost)));
                stone.reduceLeft((int)Math.Round(stone.left - (percentOfWorkLeftToDoAfter * stone.cost)));
                steel.reduceLeft((int)Math.Round(steel.left - (percentOfWorkLeftToDoAfter * steel.cost)));
                chemfuel.reduceLeft((int)Math.Round(chemfuel.left - (percentOfWorkLeftToDoAfter * chemfuel.cost)));
                UpdateProgress(amountOfWork);
            }
        }

        public CompProperties_RoadsOfTheRimConstructionSite properties
        {
            get
            {
                return (CompProperties_RoadsOfTheRimConstructionSite)props;
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
                parentSite = this.parent as RoadConstructionSite;
                Tile fromTile = Find.WorldGrid[parentSite.Tile];
                Tile toTile = Find.WorldGrid[parentSite.GetNextLeg().Tile];
                RoadBuildableDef roadToBuild = parentSite.roadToBuild;
                RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();

                // Cost increase from elevation : if elevation is above {CostIncreaseElevationThreshold} (default 1000m), cost is doubled every {ElevationCostDouble} (default 2000m)
                float elevationCostIncrease = ((fromTile.elevation <= settings.CostIncreaseElevationThreshold) ? 0 : (fromTile.elevation - settings.CostIncreaseElevationThreshold) / RoadsOfTheRimSettings.ElevationCostDouble);
                elevationCostIncrease += ((toTile.elevation <= settings.CostIncreaseElevationThreshold) ? 0 : (toTile.elevation - settings.CostIncreaseElevationThreshold) / RoadsOfTheRimSettings.ElevationCostDouble);

                // Hilliness and swampiness are the average between that of the from & to tiles
                // Hilliness is 0 on flat terrain, never negative. So it's between 0 and 4
                float hilliness = Math.Max((((float)fromTile.hilliness + (float)toTile.hilliness) / 2) - 1, 0);
                float swampiness = (fromTile.swampiness + toTile.swampiness) / 2;

                // Hilliness and swampiness double the costs when they equal {HillinessCostDouble} (default 4) and {SwampinessCostDouble} (default 0.5)
                float hillinessCostIncrease = hilliness / RoadsOfTheRimSettings.HillinessCostDouble;
                float swampinessCostIncrease = swampiness / RoadsOfTheRimSettings.SwampinessCostDouble;

                float bridgeCostIncrease = 0f;
                /* TO DO : River crossing
                List<int> fromTileNeighbors = new List<int>();
                Find.WorldGrid.GetTileNeighbors(parent.Tile, fromTileNeighbors);
                foreach (Tile.RiverLink aRiver in fromTile.Rivers )
                {
                    Log.Message("River in FROM tile : neighbor="+aRiver.neighbor+", river="+aRiver.river.ToString());
                }
                */

                // Total cost modifier
                float totalCostModifier = 1 + elevationCostIncrease + hillinessCostIncrease + swampinessCostIncrease + bridgeCostIncrease;

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

                totalCostModifier *= settings.BaseEffort;
                this.work.setCost((float)roadToBuild.work * totalCostModifier);
                this.wood.setCost((float)roadToBuild.wood * settings.BaseEffort);
                this.stone.setCost((float)roadToBuild.stone * settings.BaseEffort);
                this.steel.setCost((float)roadToBuild.steel * settings.BaseEffort);
                this.chemfuel.setCost((float)roadToBuild.chemfuel * settings.BaseEffort);
                parentSite.UpdateProgressBarMaterial();
            }
            catch (Exception e)
            {
                Log.Error("[Roads of the Rim] : Exception when setting constructionSite costs = " + e);
            }
        }

        /* 
         * Received a tick from a caravan with WorldObjectComp_Caravan
         * Returns TRUE if work finished
        */
        public bool doSomeWork(Caravan caravan)
        {
            WorldObjectComp_Caravan caravanComp = caravan.GetComponent<WorldObjectComp_Caravan>();

            if (DebugSettings.godMode)
            {
                return finishWork(parentSite, caravan);
            }

            if (!(caravanComp.CaravanCurrentState() == CaravanState.ReadyToWork))
            {
                Log.Message("[Roads of the Rim] DEBUG : doSomeWork() failed because the caravan can't work.");
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
            work.reduceLeft(amountOfWork);
            parentSite.UpdateProgressBarMaterial();

            // Work is done
            if (work.getLeft() <= 0)
            {
                return finishWork(parentSite, caravan);
            }
            return false;
        }

        /*
         * Build the road and move the construction site
         */
        public bool finishWork(RoadConstructionSite parentSite, Caravan caravan = null)
        {
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
            catch
            {
                Log.Error("[RotR] Exception while rendering new roads.");
            }

            // The Consutrction site and the caravan can move to the next leg
            RoadConstructionLeg nextLeg = parentSite.GetNextLeg();
            if (nextLeg != null)
            {
                parentSite.Tile = nextLeg.Tile;
                RoadConstructionLeg nextNextLeg = nextLeg.Next;
                if (nextNextLeg != null)
                {
                    nextNextLeg.Previous = null;
                    setCosts();
                    if (caravan != null)
                    {
                        caravan.pather.StartPath(nextLeg.Tile, new CaravanArrivalAction_StartWorkingOnRoad());
                    }
                }
                else
                {
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
                Find.World.worldObjects.Remove(nextLeg);
            }

            return true;
        }

        public string progressDescription()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Main".Translate(String.Format("{0:P1}", work.getPercentageDone())));
            if (parentSite.helpFromFaction != null)
            {
                stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_Help".Translate(parentSite.helpFromFaction.Name, (int)parentSite.helpAmount, String.Format("{0:0.0}", parentSite.helpWorkPerTick)));
                if (parentSite.helpFromTick > Find.TickManager.TicksGame)
                {
                    stringBuilder.Append("RoadsOfTheRim_ConstructionSiteDescription_HelpStartsWhen".Translate(String.Format("{0:0.00}", (float)(parentSite.helpFromTick - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay)));
                }
            }
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
            parentSite = this.parent as RoadConstructionSite;
        }
    }
}
