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
        public int debugCost_GetRoadMovementDifficultyMultiplier;
        public int debugCost_CalculatedMovementDifficultyAt;

        public WorldComponent_RoadBuildingState(World world) : base(world)
        {
            currentlyTargeting = null ;
            debugCost_GetRoadMovementDifficultyMultiplier = 0;
            debugCost_CalculatedMovementDifficultyAt = 0;
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
