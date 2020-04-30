using RimWorld.Planet;
using RimWorld;
using Verse;
using System.Linq;

namespace RoadsOfTheRim
{
    public class WorldComponent_RoadBuildingState : WorldComponent
    {
        private RoadConstructionSite currentlyTargeting;
        private Caravan caravan;

        public WorldComponent_RoadBuildingState(World world) : base(world)
        {
            currentlyTargeting = null ;
        }

        public RoadConstructionSite CurrentlyTargeting
        {
            get
            {
                return this.currentlyTargeting;
            }
            set
            {
                this.currentlyTargeting = value;
            }
        }

        public Caravan Caravan
        {
            get
            {
                return this.caravan;
            }
            set
            {
                this.caravan = value;
            }
        }

    }
}
