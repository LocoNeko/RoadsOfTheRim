using System;
using Verse;

namespace RoadsOfTheRim
{
    class ThingCompProperties_RotR_Vehicles : CompProperties
    {
        public int Fuel = 0;
        public ThingCompProperties_RotR_Vehicles()
        {
            this.compClass = typeof(ThingCompProperties_RotR_Vehicles);
        }

        public ThingCompProperties_RotR_Vehicles(Type compClass) : base(compClass)
        {
            this.compClass = compClass;
        }
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

        public override void CompTick()
        {
            RoadsOfTheRim.DebugLog("Tick on a vehicle");
        }

    }


}
