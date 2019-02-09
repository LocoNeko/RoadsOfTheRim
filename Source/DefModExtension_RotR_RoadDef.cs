using Verse;
using RimWorld;
using RimWorld.Planet ;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System ;

namespace RoadsOfTheRim
{
    public class RotR_cost
    {
        public string name ;
        public int count ;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured RotR_cost: " + xmlRoot.OuterXml, false);
                return;
            }
            name = xmlRoot.Name;
            count = (int)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(int));
        }
    }

    public class DefModExtension_RotR_RoadDef : DefModExtension
    {
        public bool built = false ; // Whether or not this road is built or generated
                                    // Base roads (DirtPath, DirtRoad, StoneRoad, AncientAsphaltRoad, AncientAsphaltHighway) will have this set to false, 
                                    // Built roads (DirtPath+, DirtRoad+, StoneRoad+, AsphaltRoad+, GlitterRoad) will have this set to true
                                    // Built roads will prevent rocks from being generated on top of them on maps

        public float biomeModifier = 0f ;

        public float hillinessModifier = 0f ;

        public float winterModifier = 0f ;

        public bool canBuildOnImpassable = false ;

        public bool canBuildOnWater = false ;

        public float minConstruction = 0f;

        public float percentageOfminConstruction = 0f;

        public TechLevel techlevelToBuild = TechLevel.Neolithic;

        public static string[] allResources = new string[] { "Wood", "Stone", "Steel", "Chemfuel" }; // TO DO : Add all needed resources here later (plasteel, components, etc)

        public static string[] allResourcesAndWork = new string[] { "Work", "Wood", "Stone", "Steel", "Chemfuel" };

        public List<RotR_cost> costs = new List<RotR_cost>() ;

        public string GetCosts()
        {
            StringBuilder s = new StringBuilder() ;
            s.Append("The road is "+(built ? "" : "not")+" built. Costs : ") ;
            foreach (RotR_cost c in costs)
            {
                s.Append(c.count + " " + c.name + ", ");
            }
            return s.ToString() ;
        }

        public int GetCost(string name)
        {
            RotR_cost aCost = costs.Find(c => c.name == name) ;
            return (aCost==null) ? 0 : aCost.count ;
        }

    }
}