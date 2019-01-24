using RimWorld.Planet;

namespace RoadsOfTheRim
{
    public class WorldComponent_RoadBuildingState : WorldComponent
    {
        private RoadConstructionSite currentlyTargeting;

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
    }
}
