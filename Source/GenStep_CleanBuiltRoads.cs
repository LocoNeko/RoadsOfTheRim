using System.Collections.Generic ;
using RimWorld ;
using Verse;
using System;

namespace RoadsOfTheRim
{
    /*
    This GenStep should be called at the very end when generating a map
    It aims at pushing away things that spawned on recently built roads and have no place there : Rocks, walls, ruins...
    This should make the roads much more believable
     */
    public class GenStep_CleanBuiltRoads : GenStep
    {
        public override int SeedPart
		{
			get
			{
                return 314159265;
			}
		}

		public override void Generate(Map map, GenStepParams parms)
		{
            //RoadsOfTheRim.DebugLog("Cleaning up roads if I can");
            try
            {
                TerrainGrid terrainGrid = map.terrainGrid;
                foreach (IntVec3 current in map.AllCells)
			    {
                    List<Thing> thingList = current.GetThingList(map);
                    TerrainDef terrainDefHere = terrainGrid.TerrainAt(current) ;
                    if (isBuiltRoad(terrainDefHere))
                    {
                        map.roofGrid.SetRoof(current, null) ; // remove any roof
                        if (map.fogGrid.IsFogged(current))
                        {
                            map.fogGrid.Unfog(current); // no fog on road
                        }

                        if (thingList.Count > 0)
                        {
                            //RoadsOfTheRim.DebugLog("Placed " + thingList.Count + " things on top of " + terrainDefHere.label);
                            MoveThings(map, current);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RoadsOfTheRim.DebugLog("Exception during map generation.", e);
            }
        }

        public static bool isBuiltRoad(TerrainDef def)
        {
            return RoadsOfTheRim.builtRoadTerrains.Contains(def) ;
        }

        /*
        Moves all things in a cell to the closest cell that is empty and not a built road
         */
        public static void MoveThings(Map map , IntVec3 cell)
        {
            List<Thing> thingList = cell.GetThingList(map);
            TerrainGrid terrainGrid = map.terrainGrid;
            //thingList.RemoveAll(item => item !=null);
            foreach (Thing thingToMove in thingList) // Go through all things on that cell
            {
                //RoadsOfTheRim.DebugLog("Trying to move " + thingToMove.Label);
                List<IntVec3> cellChecked = new List<IntVec3>() ;
                cellChecked.Add(cell) ;
                bool goodCellFound = false ;
                while (!goodCellFound) // Keep doing this as long as I haven't found a good cell (empty, and not a road)
                {
                    List<IntVec3> newCells = cellChecked ;
                    expandNeighbouringCells(ref newCells , map) ;
                    foreach (IntVec3 c in newCells)
                    {
                        TerrainDef terrainDefHere = terrainGrid.TerrainAt(c) ;
                        List<Thing> thingList2 = c.GetThingList(map);
                        if ( !isBuiltRoad(terrainDefHere) && thingList2.Count==0)
                        {
                            //RoadsOfTheRim.DebugLog("Moved "+thingToMove.Label);
                            thingToMove.SetPositionDirect(c) ;
                            goodCellFound = true ;
                            break ;
                        }
                    }
                    if (newCells.Count <= cellChecked.Count ) // break out of the loop if we couldn't find any new cells
                    {
                        break ;
                    }
                    cellChecked = newCells ;
                }
            }
        }

        public static void expandNeighbouringCells(ref List<IntVec3> cells , Map map)
        {
            List<IntVec3> expandedCells = new List<IntVec3>() ;
            foreach (IntVec3 c in cells)
            {
                if (!expandedCells.Contains(c) && !cells.Contains(c)) // Add the current cell
                {
                    expandedCells.Add(c) ;
                }
                foreach (IntVec3 c2 in GenAdjFast.AdjacentCells8Way(c)) // Add all the current cell's neighbours
                {
                    if (!expandedCells.Contains(c2) && !cells.Contains(c2) && c.InBounds(map))
                    {
                        expandedCells.Add(c2) ;
                    }
                }
            }
            cells = expandedCells ;
        }
    }
}