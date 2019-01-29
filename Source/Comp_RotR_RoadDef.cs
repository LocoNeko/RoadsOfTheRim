using Verse;
using RimWorld;

namespace RoadsOfTheRim
{

    public class CompProperties_RotR_RoadDef : CompProperties
	{
		public CompProperties_RotR_RoadDef()
		{
            // Check CompProperties_Facility & its XML to see how I can store a list here
			compClass = typeof(Comp_RotR_RoadDef);
		}
    }

    public class Comp_RotR_RoadDef : ThingComp
    {

        public bool built = false ; // Whether or not this road was built or generated
        public CompProperties_RotR_RoadDef properties
		{
			get
			{
				return (CompProperties_RotR_RoadDef)props;
			}
        }
        
    }
}