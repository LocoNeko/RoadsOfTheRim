using System;
using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace RoadsOfTheRim
{
    public static class CaravanVehiclesUtility
    {
        public static int NumberOfSeats(Caravan c)
        {
            int result = 0;
            WorldObjectComp_Caravan CaravanComp = c.GetComponent<WorldObjectComp_Caravan>();
            if (CaravanComp != null)
            {
                List<Thing> ListOfVehicles = CaravanComp.GetListOfVehicles();
                foreach (Thing vehicle in ListOfVehicles)
                {
                    result += ThingCompUtility.TryGetComp<ThingComp_RotR_Vehicles>(vehicle).Seats;
                }
            }
            return result;
        }
    }
}
