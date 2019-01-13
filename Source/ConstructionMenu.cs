using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RoadsOfTheRim
{
    /*
    Nice looking cartridge-style option picker (I can start by looking at DingoDjango/DeepOreIdentifier this tells me quite a lot about how to draw on the UI)
    Layout : 1 vertical cartridge per type of buidalble road (no need to show anything if empty)
    Each cartridge has :
    - A square image at the top, representing the type of road
    - The name of the road below
    - A list of costs, one per line :
    > Work
    > Wood
    > Stone
    > Steel
    > Chemfuel
    Each cost could be represented by the icon of the resource (need to think of a work icon)
    For resources with a cost of 0, don't display them

    Upon hover, the cartridge should be highligthed
    Upon clicking outside, the cartridge should disappear
    Upon clicking on it, we cna finally call FinaliseConstructionSite(caravan.Tile, toTile_int, thisRoadBuildableDef);

    Check, among others :
    * Widgets many methods
    */

    public class ConstructionMenu : Window
    {
        private readonly int fromTile;
        private readonly int toTile;
        private readonly RoadDef bestExistingRoad;
        private readonly List<RoadBuildableDef> buildableRoads;
        public override Vector2 InitialSize => new Vector2(676, 544);

        public ConstructionMenu(int fromTile, int toTile , RoadDef bestExistingRoad)
        {
            this.fromTile = fromTile;
            this.toTile = toTile;
            this.bestExistingRoad = bestExistingRoad ;
            buildableRoads = new List<RoadBuildableDef>() ;
            foreach (RoadBuildableDef thisRoadBuildableDef in DefDatabase<RoadBuildableDef>.AllDefs)
            {
                if (bestExistingRoad == null || RoadsOfTheRim.isRoadBetter(DefDatabase<RoadDef>.GetNamed(thisRoadBuildableDef.roadDef, true), bestExistingRoad))
                {
                    buildableRoads.Add(thisRoadBuildableDef);
                }
            }
        }

        public int CountBuildableRoads()
        {
            return (buildableRoads!=null ? buildableRoads.Count : 0);
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (Event.current.isKey)
            {
                Close();
            }

            //Resources icons
            for (int i = 0; i < 5; i++)
            {
                // Icon
                Rect ResourceRect = new Rect(0, 202f + i * 40f, 32f, 32f);
                ThingDef theDef;
                switch (i)
                {
                    case 0:
                        theDef = ThingDefOf.Spark;
                        break;
                    case 1:
                        theDef = ThingDefOf.WoodLog;
                        break;
                    case 2:
                        theDef = ThingDefOf.BlocksGranite;
                        break;
                    case 3:
                        theDef = ThingDefOf.Steel;
                        break;
                    default:
                        theDef = ThingDefOf.Chemfuel;
                        break;
                }
                if (i==0)
                {
                    Widgets.ButtonImage(ResourceRect, ContentFinder<Texture2D>.Get("UI/Commands/AddConstructionSite"));
                }
                else
                {
                    Widgets.ThingIcon(ResourceRect, theDef);
                }
            }

            // Sections : one per type of buildable road
            int nbOfSections = 0;
            Vector2 groupSize = new Vector2(144 , 512);
            foreach (RoadBuildableDef aDef in buildableRoads)
            {
                GUI.BeginGroup(new Rect(new Vector2(64 + 144 * nbOfSections, 32f), groupSize));

                // Buildable Road icon
                Texture2D theButton = ContentFinder<Texture2D>.Get("World/WorldObjects/"+aDef.defName, true);
                Rect ButtonRect = new Rect(8, 8, 128, 128);
                if (Widgets.ButtonImage(ButtonRect, theButton))
                {
                    if (Event.current.button == 0)
                    {
                        SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, null);
                        RoadsOfTheRim.FinaliseConstructionSite(fromTile, toTile, aDef);
                        Close();
                    }
                }

                // Buildable Road label
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Rect NameRect = new Rect(0, 144, 144f , 32f);
                Widgets.Label(NameRect, aDef.label);

                // Resources amounts
                Text.Font = GameFont.Small;
                for (int i=0; i<5; i++)
                {
                    int amount;
                    switch (i)
                    {
                        case 0:
                            amount = aDef.work;
                            break;
                        case 1:
                            amount = aDef.wood;
                            break;
                        case 2:
                            amount = aDef.stone;
                            break;
                        case 3:
                            amount = aDef.steel;
                            break;
                        default:
                            amount = aDef.chemfuel;
                            break;
                    }
                    Rect ResourceAmountRect = new Rect(0, 176f + i * 40f, 144f, 32f);
                    Widgets.Label(ResourceAmountRect, amount.ToString());
                }
                /*Textures :
                 * Things/Item/Resource/WoodLog
                 * Things/Item/Resource/StoneBlocks                
                 * Things/Item/Resource/Steel
                 * Things/Item/Resource/Chemfuel
                 */
                GUI.EndGroup();
                nbOfSections++;
            }
            Text.Anchor = TextAnchor.UpperLeft;
            // I must do action.Invoke on an action likned to the button : How ?
        }

    }
}
