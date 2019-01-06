using Harmony;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{

    public class RoadConstructionSite : WorldObject
    {
        public RoadBuildableDef roadToBuild;

        public int toTile;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
                g.disabledReason = null;
            }
            yield break;
        }

        public RoadConstructionSite()
        {
            /*
             * Removed since I overwrote Draw instead of patching it           
            var harmony = HarmonyInstance.Create("Loconeko.Rimworld.RoadsOfTheRim");
            MethodInfo method = typeof(WorldObject).GetMethod("Draw");
            HarmonyMethod prefix = null;
            HarmonyMethod postfix = new HarmonyMethod(typeof(RoadsOfTheRim).GetMethod("DrawPostfix")); ;
            harmony.Patch(method, prefix, postfix);
            */           
        }

        /*
        The construction site costs are set here
         */
        public override void PostAdd()
        {
            // TO DO : Set the real costs here. They must come from the RoadBuildableDef roadToBuild
            this.GetComponent<CompRoadsOfTheRimConstructionSite>().setCosts(Find.WorldGrid[Tile] , Find.WorldGrid[toTile] , roadToBuild);
        }

        public void setDestination(int destination)
        {
            toTile = destination ;
        }

        public bool resourcesAlreadyConsumed()
        {
            return this.GetComponent<CompRoadsOfTheRimConstructionSite>().resourcesAlreadyConsumed() ;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("Building: " + roadToBuild.label + " (movement difficulty: " + roadToBuild.movementCostMultiplier + ")");
            stringBuilder.AppendLine();
            stringBuilder.Append(this.GetComponent<CompRoadsOfTheRimConstructionSite>().progressDescription()) ;
            return stringBuilder.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look<RoadBuildableDef>(ref this.roadToBuild, "roadToBuild");
            Scribe_Values.Look<int>(ref this.toTile, "toTile", 0 , false);
        }

        /*
        Check WorldObject Draw method to find why the construction site icon is rotated strangely when expanded
         */
        public override void Draw()
        {
            base.Draw();
            WorldGrid worldGrid = Find.WorldGrid;
            Vector3 fromPos = worldGrid.GetTileCenter(this.Tile);
            Vector3 toPos = worldGrid.GetTileCenter(this.toTile);
            float d = 0.05f;
            fromPos += fromPos.normalized * d;
            toPos += toPos.normalized * d;
            GenDraw.DrawWorldLineBetween(fromPos, toPos);
            // TO DO : Progress bar
            //this.GetComponent<CompRoadsOfTheRimConstructionSite>().percentageDone() ;
        }

        // Tile : caravan.Tile
        // IncidentWorker_QuestPeaceTalks : shows me a good way to create a worldObject
    }

    /*
    A construction site comp :
    - Keeps track of all costs
    - Keeps track of how much work is left to do
    - Applies the effects of work done by a caravan
    - Creates the road once work is done
     */
    public class CompProperties_RoadsOfTheRimConstructionSite : WorldObjectCompProperties
    {
        public CompProperties_RoadsOfTheRimConstructionSite()
        {
            this.compClass = typeof(CompRoadsOfTheRimConstructionSite);
        }
    }

    public class CompRoadsOfTheRimConstructionSite : WorldObjectComp
    {
        public struct Resource
        {
            public float cost ;
            public float left ;

            public void setCost(float f)
            {
                cost = f ;
                left = f ;
            }

            public void reduceLeft(float f)
            {
                left -= (f>left) ? left : f ;
            }

            public float getCost() {return cost ;}
            public float getLeft() {return left ;}
            public float getPercentageDone() {
                if (cost<=0)
                {
                    return 1f;
                }
                return 1f - (left / cost) ;
            }
        }

        private Resource work ;
        private Resource wood ;
        private Resource stone ;
        private Resource steel ;
        private Resource chemfuel ;

        public CompProperties_RoadsOfTheRimConstructionSite properties
        {
            get
            {
                return (CompProperties_RoadsOfTheRimConstructionSite)props;
            }
        }

        public bool resourcesAlreadyConsumed()
        {
            return ( (wood.left<wood.cost) | (stone.left<stone.cost) | (steel.left<steel.cost) | (chemfuel.left<chemfuel.cost) ) ;
        }

        public void setCosts(Tile fromTile , Tile toTile , RoadBuildableDef roadToBuild)
        {
            try
            {
                RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();
                // Cost increase from elevation : if elevation is above {CostIncreaseElevationThreshold} (default 1000m), cost is doubled every {ElevationCostDouble} (default 2000m)
                float elevationCostIncrease = ( (fromTile.elevation <= settings.CostIncreaseElevationThreshold) ? 0 : (fromTile.elevation - settings.CostIncreaseElevationThreshold)/RoadsOfTheRimSettings.ElevationCostDouble ) ;
                elevationCostIncrease += ( (toTile.elevation <= settings.CostIncreaseElevationThreshold) ? 0 : (toTile.elevation - settings.CostIncreaseElevationThreshold)/RoadsOfTheRimSettings.ElevationCostDouble ) ;

                // Hilliness and swampiness are the average between that of the from & to tiles
                // Hilliness is 0 on flat terrain, never negative. So it's between 0 and 4
                float hilliness = Math.Max ( ( ((float)fromTile.hilliness + (float)toTile.hilliness)/2 ) - 1 , 0 ) ;
                float swampiness = (fromTile.swampiness + toTile.swampiness) /2 ;

                // Hilliness and swampiness double the costs when they equal {HillinessCostDouble} (default 4) and {SwampinessCostDouble} (default 0.5)
                float hillinessCostIncrease = hilliness / RoadsOfTheRimSettings.HillinessCostDouble ;
                float swampinessCostIncrease = swampiness /  RoadsOfTheRimSettings.SwampinessCostDouble ;

                // TO DO : river crossing
                float bridgeCostIncrease = 0f ;
                List<int> fromTileNeighbors = new List<int>();
                Find.WorldGrid.GetTileNeighbors(parent.Tile, fromTileNeighbors);
                StringBuilder desc = new StringBuilder();
                foreach (int neighbor in fromTileNeighbors)
                {
                    desc.Append(neighbor+",");
                }
                // DEBUG - Log.Message("Neigbors of From tile :" + desc);

                List<int> toTileNeighbors = new List<int>();
                RoadConstructionSite TheSite = (RoadConstructionSite) parent;
                int toTile_int = TheSite.toTile;
                Find.WorldGrid.GetTileNeighbors(toTile_int, toTileNeighbors);
                StringBuilder desc2 = new StringBuilder();
                foreach (int neighbor in toTileNeighbors)
                {
                    desc2.Append(neighbor + ",");
                }
                // DEBUG - Log.Message("Neigbors of To tile :" + desc2);

                /*
                foreach (Tile.RiverLink aRiver in fromTile.Rivers )
                {
                    Log.Message("River in FROM tile : neighbor="+aRiver.neighbor+", river="+aRiver.river.ToString());
                }
                */

                // Total cost modifier
                float totalCostModifier = 1 + elevationCostIncrease + hillinessCostIncrease + swampinessCostIncrease + bridgeCostIncrease ;

                /*
                 * DEBUG               
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
                this.work.setCost(roadToBuild.work * totalCostModifier) ;
                this.wood.setCost((float)roadToBuild.wood * settings.BaseEffort) ;
                this.stone.setCost((float)roadToBuild.stone * settings.BaseEffort) ;
                this.steel.setCost((float)roadToBuild.steel * settings.BaseEffort) ;
                this.chemfuel.setCost((float)roadToBuild.chemfuel * settings.BaseEffort) ;
            }
            catch
            {
                Log.Message("[Roads of the Rim] : error setting constructionSite costs");
            }
        }

        /* 
         * Received a tick from a caravan with WorldObjectComp_Caravan
         * Returns TRUE if work finished
        */
        public bool doSomeWork(Caravan caravan)
        {
            WorldObjectComp_Caravan caravanComp = caravan.GetComponent<WorldObjectComp_Caravan>() ;
            RoadConstructionSite parentSite = this.parent as RoadConstructionSite;

            if (DebugSettings.godMode)
            {
                return finishWork(caravan, parentSite);
            }

            if (!caravanComp.CaravanCanWork())
            {
                Log.Message("[Roads of the Rim] : doSomeWork() failed because the caravan can't work.");
                return false;
            }

            // Percentage of total work that can be done in this batch
            float amountOfWork = (float)caravanComp.amountOfWork() ;
            if (amountOfWork> this.work.getLeft())
            {
                amountOfWork = this.work.getLeft();
            }
            float thisBatch = (amountOfWork / this.work.getCost());

            //DEBUG -Log.Message("Amount of Work : " + amountOfWork + " = " + thisBatch*100 + "%. Caravan inventory : ");

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
                    available_wood += aThing.stackCount ;
                }
                else if (aThing.def.FirstThingCategory.ToString()=="StoneBlocks")
                {
                    available_stone += aThing.stackCount ;
                }
                else if (aThing.def.IsMetal)
                {
                    available_steel += aThing.stackCount ;
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

            // Check if there's enough material to go through this batch
            float ratio_wood = Math.Min((float)available_wood / needed_wood, 1f);
            float ratio_stone = Math.Min((float)available_stone / needed_stone, 1f);
            float ratio_steel = Math.Min((float)available_steel / needed_steel, 1f);
            float ratio_chemfuel = Math.Min((float)available_chemfuel / needed_chemfuel, 1f);

            //There's a shortage of materials
            float ratio_final = Math.Min(ratio_wood , Math.Min(ratio_stone , Math.Min(ratio_steel , ratio_chemfuel))) ;

            // The caravan didn't have enough resources for a full batch of work. Use as much as we can then stop working
            if (ratio_final<1f)
            {
                Messages.Message("RoadsOfTheRim_CaravanNoResource".Translate(caravan.Name, parentSite.roadToBuild.label), MessageTypeDefOf.RejectInput);
                needed_wood = (int)(needed_wood * ratio_final);
                needed_stone = (int)(needed_stone * ratio_final);
                needed_steel = (int)(needed_steel * ratio_final);
                needed_chemfuel = (int)(needed_chemfuel * ratio_final);
                caravanComp.stopWorking() ;
            }

            // Consume resources from the caravan 
            foreach (Thing aThing in CaravanInventoryUtility.AllInventoryItems(caravan))
            {
                if ((aThing.def.ToString() == "WoodLog") && needed_wood >0)
                {
                    int used_wood = (aThing.stackCount > needed_wood) ? needed_wood : aThing.stackCount ;
                    aThing.stackCount -= used_wood ;
                    needed_wood -= used_wood ;
                    wood.reduceLeft(used_wood);
                }
                else if ((aThing.def.FirstThingCategory.ToString() == "StoneBlocks") && needed_stone > 0 )
                {
                    int used_stone = (aThing.stackCount > needed_stone) ? needed_stone : aThing.stackCount ;
                    aThing.stackCount -= used_stone ;
                    needed_stone -= used_stone ;
                    stone.reduceLeft(used_stone);
                }
                else if ((aThing.def.IsMetal) && needed_steel > 0 )
                {
                    int used_steel = (aThing.stackCount > needed_steel) ? needed_steel : aThing.stackCount ;
                    aThing.stackCount -= used_steel ;
                    needed_steel -= used_steel ;
                    steel.reduceLeft(used_steel);
                }
                else if ((aThing.def.ToString() == "Chemfuel") && needed_chemfuel > 0 )
                {
                    int used_chemfuel = (aThing.stackCount > needed_chemfuel) ? needed_chemfuel : aThing.stackCount;
                    aThing.stackCount -= used_chemfuel ;
                    needed_chemfuel -= used_chemfuel ;
                    chemfuel.reduceLeft(used_chemfuel);
                }
                if (aThing.stackCount==0)
                {
                    aThing.Destroy();
                }
            }
            // Finally reducing the work & resources left
            work.reduceLeft(amountOfWork);

            // Work is done
            if (work.getLeft()<=0)
            {
                return finishWork(caravan, parentSite);
            }
            return false;
        }

        public bool finishWork(Caravan caravan , RoadConstructionSite parentSite)
        {
            /*
             * Build the road and remove the construction site
             * NOTE : using .parent here, because parent is a WorldObject, I then cast it as a RoadConstructionSite (otherwise it would be just a WorldObject)
             */
            Tile fromTile = Find.WorldGrid[parentSite.Tile];
            Tile toTile = Find.WorldGrid[parentSite.toTile];
            RoadDef newRoadDef = DefDatabase<RoadDef>.GetNamed(parentSite.roadToBuild.getRoadDef());

            // Remove lesser roads, they don't deserve to live
            if (fromTile.potentialRoads != null)
            {
                foreach (Tile.RoadLink aLink in fromTile.potentialRoads.ToArray())
                {
                    if (aLink.neighbor == parentSite.toTile & RoadsOfTheRim.isRoadBetter(newRoadDef, aLink.road))
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
                    if (aLink.neighbor == caravan.Tile & RoadsOfTheRim.isRoadBetter(newRoadDef, aLink.road))
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
            fromTile.potentialRoads.Add(new Tile.RoadLink { neighbor = parentSite.toTile, road = newRoadDef });
            toTile.potentialRoads.Add(new Tile.RoadLink { neighbor = parentSite.Tile, road = newRoadDef });
            Find.World.renderer.RegenerateAllLayersNow();

            // Send letter
            Find.LetterStack.ReceiveLetter(
                "RoadsOfTheRim_RoadBuilt".Translate(),
                "RoadsOfTheRim_RoadBuiltLetterText".Translate(parentSite.roadToBuild.label, caravan.Label),
                LetterDefOf.PositiveEvent,
                new GlobalTargetInfo(caravan)
            );

            return true;
        }

        public string progressDescription() {
            StringBuilder stringBuilder = new StringBuilder();
            //DEBUG - stringBuilder.Append("[Mvmt difficulty="+ WorldPathGrid.CalculatedMovementDifficultyAt(parent.Tile , true) + "] - Needs: ");
            stringBuilder.Append("Needs: ");
            stringBuilder.AppendLine();
            stringBuilder.Append("Work    : " + (int)work.getLeft() + " / " + (int)work.getCost());
            stringBuilder.AppendLine();
            stringBuilder.Append("Wood    : " + (int)wood.getLeft() + " / " + (int)wood.getCost());
            stringBuilder.AppendLine();
            stringBuilder.Append("Stone   : " + (int)stone.getLeft() + " / " + (int)stone.getCost());
            stringBuilder.AppendLine();
            stringBuilder.Append("Steel   : " + (int)steel.getLeft() + " / " + (int)steel.getCost());
            stringBuilder.AppendLine();
            stringBuilder.Append("Chemfuel: " + (int)chemfuel.getLeft() + " / " + (int)chemfuel.getCost());
            return stringBuilder.ToString();

        }

        public float percentageDone()
        {
            return work.getPercentageDone() ;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<float>(ref work.cost ,    "cost_work" , 0 , true);
            Scribe_Values.Look<float>(ref wood.cost ,    "cost_wood", 0 , true);
            Scribe_Values.Look<float>(ref stone.cost,    "cost_stone", 0 , true);
            Scribe_Values.Look<float>(ref steel.cost,    "cost_steel", 0 , true);
            Scribe_Values.Look<float>(ref chemfuel.cost, "cost_chemfuel", 0 , true);
            Scribe_Values.Look<float>(ref work.left,     "left_work", 0 , true);
            Scribe_Values.Look<float>(ref wood.left,     "left_wood", 0 , true);
            Scribe_Values.Look<float>(ref stone.left,    "left_stone", 0 , true);
            Scribe_Values.Look<float>(ref steel.left,    "left_steel", 0 , true);
            Scribe_Values.Look<float>(ref chemfuel.left, "left_chemfuel", 0 , true);
        }
    }
}

/*
 * Useful stuff :
 * WorldGrid.TilesToRawData
 * 
 * WITab_Terrain : shows movement difficulty
 * 
 */
