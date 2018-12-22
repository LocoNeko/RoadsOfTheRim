using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RoadsOfTheRim
{
    public class CaravanJobDriver_WorkOnSite : CaravanJobDriver
    {
        // Check whether the caravan is indeed on a construction site
        // Check whether the caravan is resting
        // Calculate the caravan's work per tick based on : pawns' construction + pack animals (but limited to doubling pawns work)
        // Use caravan material to deduct them from the construction site CompRoadsOfTheRimConstructionSite (done_*** : done_work , done_wood, done_steel...)
        // Do the tick action itself that consumes resources (TBC) and checks whether the work is finised, to notify the RoadConstructionSite
        // FIRST STEP  : just to the checks for construction site existence, and build instantaneously. This can be done later : caravan resting, resources, and work
    }
}