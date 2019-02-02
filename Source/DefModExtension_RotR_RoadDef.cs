using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RoadsOfTheRim
{
    public class RotR_costList : IExposable
    {
        public string name;

        public int count;

        public RotR_costList()
        {

        }

        public RotR_costList(string name, int count)
        {
            if (count < 0)
            {
                Log.Warning("NO");
                count = 0;
            }
            this.name = name ;
            this.count = count ;
        }


        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref count, "count", 1, false);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured RotR_costList: " + xmlRoot.OuterXml, false);
            }
            else
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "name", xmlRoot.Name);
                count = (int)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(int));
            }
        }

    }

    public class DefModExtension_RotR_RoadDef : DefModExtension
    {
        public bool built = false ; // Whether or not this road is built or generated
                                    // Base roads (DirtPath, DirtRoad, StoneRoad, AncientAsphaltRoad, AncientAsphaltHighway) will have this set to false, 
                                    // Built roads (DirtPath+, DirtRoad+, StoneRoad+, AsphaltRoad+, GlitterRoad) will have this set to true

        public int work = 0 ;

        public List<RotR_costList> costList;

        public string Description()
        {
            StringBuilder s = new StringBuilder();
            s.Append(" Built :" + built+" Costs : "+work+" work & ");
            if (costList!=null)
            {
                foreach (RotR_costList t in costList)
                {
                    s.Append(t.count+"x"+t.name+", ");
                }
            }
            else
            {
                s.Append(" no other resource.");
            }
            return s.ToString();
        }
    }
}