using System.Collections.Generic ;
using RimWorld ;
using Verse;

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
            RoadsOfTheRim.DebugLog("Cleaning up roads if I can");
            TerrainGrid terrainGrid = map.terrainGrid;
            foreach (IntVec3 current in map.AllCells)
			{
                List<Thing> thingList = current.GetThingList(map);
                TerrainDef terrainDefHere = terrainGrid.TerrainAt(current) ;
                //if (terrainDefHere.defName == "AsphaltRecent" && thingList.Count>0)
                if (isBuiltRoad(terrainDefHere) && thingList.Count>0)
                {
                    //RoadsOfTheRim.DebugLog("Placed " + thingList.ToStringSafe() + " on top of a recently built Asphalt Road") ;
                    RoadsOfTheRim.DebugLog("Placed " + thingList.ToStringSafe() + " on top of a Broken Asphalt Road") ;
                }
            }
        }

        public static bool isBuiltRoad(TerrainDef def)
        {
            return (def.defName == "BrokenAsphalt") ;
        }

        /*
        Moves all things in a cell to the closest cell that is empty and not a built road
         */
        public static void MoveThings(Map map , IntVec3 cell)
        {
            List<Thing> thingList = cell.GetThingList(map);
            TerrainGrid terrainGrid = map.terrainGrid;
            foreach (Thing thingToMove in thingList) // Go through all things on that cell
            {
                List<IntVec3> cellChecked = new List<IntVec3>() ;
                cellChecked.Add(cell) ;
                bool goodCellFound = false ;
                while (!goodCellFound) // Keep doing this as long as I haven't found a good cell (empty, and not a road)
                {
                    List<IntVec3> newCells = cellChecked ;
                    expandNeighbouringCells(ref newCells) ;
                    foreach (IntVec3 c in newCells)
                    {
                        TerrainDef terrainDefHere = terrainGrid.TerrainAt(c) ;
                        List<Thing> thingList2 = c.GetThingList(map);
                        if ( !isBuiltRoad(terrainDefHere) && thingList2.Count==0)
                        {
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

        public static void expandNeighbouringCells(ref List<IntVec3> cells)
        {
            List<IntVec3> expandedCells = new List<IntVec3>() ;
            foreach (IntVec3 c in cells)
            {
                if (!expandedCells.Contains(c) && !cells.Contains(c)) // Add the current cell
                {
                    expandedCells.Add(c) ;
                }
                foreach (IntVec3 c2 in GenAdjFast.AdjacentCells8Way(c)) // Add all the current cell's enighbours
                {
                    if (!expandedCells.Contains(c2) && !cells.Contains(c2))
                    {
                        expandedCells.Add(c2) ;
                    }
                }
            }
            cells = expandedCells ;
        }
    }
}