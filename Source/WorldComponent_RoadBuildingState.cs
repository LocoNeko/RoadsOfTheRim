using RimWorld.Planet;

namespace RoadsOfTheRim
{
    public class WorldComponent_RoadBuildingState : WorldComponent
    {
        private RoadConstructionSite currentlyTargeting;
        private Caravan caravan;
        private bool pickingSiteTile;

        public WorldComponent_RoadBuildingState(World world) : base(world)
        {
            currentlyTargeting = null ;
            pickingSiteTile = false ;
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

        public bool PickingSiteTile
        {
            get
            {
                return this.pickingSiteTile;
            }
            set
            {
                this.pickingSiteTile = value;
            }
        }

    }
}
