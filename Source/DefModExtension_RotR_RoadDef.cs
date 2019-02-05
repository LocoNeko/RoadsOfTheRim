using Verse;
using RimWorld;
using RimWorld.Planet ;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System ;

namespace RoadsOfTheRim
{

    public class DefModExtension_RotR_RoadDef : DefModExtension
    {
        public bool built = false ; // Whether or not this road is built or generated
                                    // Base roads (DirtPath, DirtRoad, StoneRoad, AncientAsphaltRoad, AncientAsphaltHighway) will have this set to false, 
                                    // Built roads (DirtPath+, DirtRoad+, StoneRoad+, AsphaltRoad+, GlitterRoad) will have this set to true
                                    // Built roads will prevent rocks from being generated on top of them on maps

        // In order to avoid this mess below, how can I turned this into a <li> in XML ?

        public float biomeModifier = 0f ;

        public float hillinessModifier = 0f ;

        public float winterModifier = 0f ;

        public bool canBuildOnImpassable = false ;

        public bool canBuildOnWater = false ;

        public float minConstruction = 0f ;

        public float percentageOfminConstruction = 0f ;

        public TechLevel techlevelToBuild = TechLevel.Neolithic ;

        public static string[] allResources = new string[] {"Wood" , "Stone" , "Steel" , "Chemfuel"} ; // TO DO : Add all needed resources here later (plasteel, components, etc)

        public static string[] allResourcesAndWork = new string[] {"Work" , "Wood" , "Stone" , "Steel" , "Chemfuel"} ;
        
        public Dictionary<string , int> costs = new Dictionary<string, int>() ;

        /*
        Returns a dictionary that shows how much of each resources (and work) is needed to build this road
         */
        public string GetCosts()
        {
            StringBuilder s = new StringBuilder() ;
            s.Append("The road is "+(built ? "" : "not")+" built. Costs : ") ;
            foreach(KeyValuePair<string, int> item in costs)
            {
                s.Append(item.Value+" "+item.Key+",  ") ; // i.e. "300 Wood, "
            }
            return s.ToString() ;
        }

        public int GetCost(string name)
        {
            int value = 0 ;
            if (!costs.TryGetValue(name , out value))
            {
                return 0 ; // TO DO : Throwing an excepion would be bettah ?
            }
            return value ;
        }
    }
}