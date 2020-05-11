using System;
using Verse;

namespace RoadsOfTheRim
{
    class ThingCompProperties_RotR_Vehicles : CompProperties
    {
        public ThingCompProperties_RotR_Vehicles()
        {
            this.compClass = typeof(ThingCompProperties_RotR_Vehicles);
        }

        public ThingCompProperties_RotR_Vehicles(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }

        public int Fuel = 0;
        public int Seats = 0;
        public bool OffRoad = false;
        public float Speed = 0;
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

        public int Fuel => properties.Fuel;
        public int Seats => properties.Seats;
        public bool OffRoad => properties.OffRoad;
        public float Speed => properties.Speed;

        public override void CompTick()
        {
            RoadsOfTheRim.DebugLog("Tick on a vehicle : "+parent.Label+" "+Seats+" seats, speed "+Speed+", "+(OffRoad?"":"not")+" offroad. Fuel : "+Fuel );
        }

    }


}
