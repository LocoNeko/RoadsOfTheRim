using System;
using Verse;

namespace RoadsOfTheRim
{
    class ThingCompProperties_RotR_Vehicles : CompProperties
    {
        public ThingCompProperties_RotR_Vehicles()
        {
            this.compClass = typeof(ThingComp_RotR_Vehicles);
        }

        public ThingCompProperties_RotR_Vehicles(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }

        public int Seats = 0;
        public bool OffRoad = false;
        public int Capacity = 0;
        public float Speed = 0f;
        public float Fuel = 1000f; // TO DO : Debug this back to 0 once happy
    }

    class ThingComp_RotR_Vehicles : ThingComp
    {

        public ThingCompProperties_RotR_Vehicles properties
        {
            get
            {
                return (ThingCompProperties_RotR_Vehicles)props;
            }
        }

        public int Seats => properties.Seats;
        public bool OffRoad => properties.OffRoad;
        public int Capacity => properties.Capacity;
        public float Speed => properties.Speed;
        public float Fuel => properties.Fuel;

        /*
        public override void CompTick()
        {
            RoadsOfTheRim.DebugLog("Tick on a vehicle ("+parent.Label+") : "+Seats+" seats, speed "+Speed+", "+(OffRoad?"":"not")+" offroad. Fuel : "+Fuel );
        }
        */

    }


}
