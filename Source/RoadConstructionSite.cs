using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{

    public class RoadConstructionSite : WorldObject
    {
        public RoadBuildableDef roadToBuild;

        public static int maxTicksToNeighbour = 2 * GenDate.TicksPerDay ; // 2 days

        public static int maxNeighbourDistance = 100 ; // search 100 tiles away

        public static int MaxSettlementsInDescription = 3;

        private static readonly Color ColorTransparent = new Color(0.0f, 0.0f, 0.0f, 0f);

        private static readonly Color ColorFilled = new Color(0.9f, 0.85f, 0.2f, 1f);

        private static readonly Color ColorUnfilled = new Color(0.3f, 0.3f, 0.3f, 1f);

        private Material ProgressBarMaterial;

        public List<Settlement> listOfSettlements ;

        public string NeighbouringSettlementsDescription ;

        public WorldObject LastLeg ;

        /*
        Factions help
        - Faction that helps 
        - Tick at which help starts
        - Total amount of work that will be provided (helping factions are always considered having enough resources to help)
        - Amount of work that will be done per tick
         */

        public Faction helpFromFaction ; // Which faction is helping

        public int helpFromTick ; // From when will the faction help

        public float helpAmount; // How much will the faction help

        public float helpWorkPerTick; // How much will the faction help per tick

        public static void DeleteSite(RoadConstructionSite site)
        {
            IEnumerable<WorldObject> constructionLegs = Find.WorldObjects.AllWorldObjects.Cast<WorldObject>().Where(
                leg => leg.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionLeg", true) &&
                ((RoadConstructionLeg)leg).GetSite() == site
            ).ToArray();
            foreach(RoadConstructionLeg l in constructionLegs)
            {
                Find.WorldObjects.Remove(l);
            }
            Find.WorldObjects.Remove(site);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
                g.disabledReason = null;
            }
            // Ability to remove the construction site without needing to go there with a Caravan.
            yield return (Gizmo)RoadsOfTheRim.RemoveConstructionSite(Tile);
            yield break;
        }

        public void initListOfSettlements()
        {
            if (listOfSettlements == null)
            {
                listOfSettlements = neighbouringSettlements();
            }
        }

        public void populateDescription()
        {
            initListOfSettlements() ;
            List<string> s = new List<string>();
            if ((listOfSettlements != null) && (listOfSettlements.Count > 0))
            {
                foreach (Settlement settlement in listOfSettlements.Take(MaxSettlementsInDescription))
                {
                    float nbDays = (float)CaravanArrivalTimeEstimator.EstimatedTicksToArrive(Tile, settlement.Tile, null)/GenDate.TicksPerDay;
                    s.Add("RoadsOfTheRim_siteDescription".Translate(settlement.Name, string.Format("{0:0.00}",nbDays)));
                }
            }
            NeighbouringSettlementsDescription = String.Join(", ", s.ToArray());
            if (listOfSettlements.Count > MaxSettlementsInDescription)
            {
                NeighbouringSettlementsDescription += "RoadsOfTheRim_siteDescriptionExtra".Translate(listOfSettlements.Count - MaxSettlementsInDescription);
            }
        }

        public string fullName()
        {
            // The first time we ask for the site's full name, let's make sure everything is properly populated : neighbouringSettlements , NeighbouringSettlementsDescription
            if (listOfSettlements == null)
            {
                populateDescription();
            }
            StringBuilder result = new StringBuilder();
            result.Append("RoadsOfTheRim_siteFullName".Translate(roadToBuild.label));
            if (NeighbouringSettlementsDescription.Length>0)
            {
                result.Append("RoadsOfTheRim_siteFullNameNeighbours".Translate(NeighbouringSettlementsDescription));
            }
            return result.ToString();
        }

        public List<Settlement> neighbouringSettlements()
        {
            if (this.Tile!=-1)
            {
                List<Settlement> result = new List<Settlement>();
                List<int> tileSearched = new List<int>();
                searchForSettlements(Tile, ref result);
                return result;
            }
            return null;
        }

        public void searchForSettlements(int startTile, ref List<Settlement> settlementsSearched)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            WorldGrid worldGrid = Find.WorldGrid;
            foreach (Settlement s in Find.WorldObjects.Settlements)
            {
                if ( (worldGrid.ApproxDistanceInTiles(startTile, s.Tile) <= maxNeighbourDistance) && (CaravanArrivalTimeEstimator.EstimatedTicksToArrive(startTile, s.Tile, null) <= maxTicksToNeighbour) )
                {
                    settlementsSearched.Add(s);
                }
            }
            timer.Stop();
            RoadsOfTheRim.DebugLog("Time spent searching for settlements : "+timer.ElapsedMilliseconds+"ms");
        }

        public Settlement closestSettlementOfFaction(Faction faction)
        {
            initListOfSettlements();
            int travelTicks = maxTicksToNeighbour;
            Settlement closestSettlement = null;
            if (listOfSettlements != null)
            {
                foreach (Settlement settlement in listOfSettlements)
                {
                    if (settlement.Faction == faction)
                    {
                        int travelTicksFromHere = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(settlement.Tile, Tile, null);
                        if (travelTicksFromHere < travelTicks)
                        {
                            closestSettlement = settlement;
                            travelTicks = travelTicksFromHere;
                        }
                    }
                }
            }
            return closestSettlement ;
        }

        public override void PostAdd()
        {
            LastLeg = this;
            populateDescription();
        }

        /*
         * Returns the next leg in the chain, or null if all is left is the construction site (which should never happen, since it should get destroyed when the last leg is built)       
         */
        public RoadConstructionLeg GetNextLeg()
        {
            if (LastLeg!=this)
            {
                RoadConstructionLeg CurrentLeg = (RoadConstructionLeg)LastLeg;
                while (CurrentLeg.Previous != null)
                {
                    CurrentLeg = CurrentLeg.Previous;
                }
                return (RoadConstructionLeg)CurrentLeg;
            }
            return null;
        }

        public void MoveWorkersToNextLeg(int fromTile)
        {
            RoadConstructionLeg nextLeg = GetNextLeg() ;
            if (nextLeg!=null)
            {
                List<Caravan> CaravansWorkingHere = new List<Caravan>() ;
                Find.WorldObjects.GetPlayerControlledCaravansAt(fromTile , CaravansWorkingHere) ;
                foreach (Caravan c in CaravansWorkingHere) // Move to the nextLeg all caravans that are currently set to work on this site
                {
                    if (c.GetComponent<WorldObjectComp_Caravan>().getSite() == this)
                    {
                        c.pather.StartPath(Tile, new CaravanArrivalAction_StartWorkingOnRoad());
                    }
                }
                if (helpFromFaction != null) // Delay when the help starts on the next leg by as many ticks as it would take a caravan to travel from the site to the next leg
                {
                    int delay = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(fromTile, Tile, null) ;
                    if (helpFromTick > Find.TickManager.TicksGame)
                    {
                        helpFromTick += delay ;
                    }
                    else
                    {
                        helpFromTick = Find.TickManager.TicksGame + delay ;
                    }
                }
            }
        }

        public void TryToSkipBetterRoads(Caravan caravan = null)
        {
            RoadConstructionLeg nextLeg = GetNextLeg() ;
            if (nextLeg!=null) // nextLeg == null should never happen
            {
                RoadDef bestExistingRoad = RoadsOfTheRim.BestExistingRoad(Tile , nextLeg.Tile) ;
                // We've found an existing road that is better than the one we intend to build : skip this leg and move to the next
                RoadDef newRoadDef = DefDatabase<RoadDef>.GetNamed(roadToBuild.getRoadDef());
                if (!RoadsOfTheRim.isRoadBetter(newRoadDef , bestExistingRoad))
                {
                    int currentTile = Tile;
                    Tile = nextLeg.Tile; // The construction site moves to the next leg
                    RoadConstructionLeg nextNextLeg = nextLeg.Next;
                    if (nextNextLeg!=null)
                    {
                        nextNextLeg.Previous = null ; // The nextNext leg is now the next
                        GetComponent<WorldObjectComp_ConstructionSite>().setCosts();
                        MoveWorkersToNextLeg(currentTile) ;
                    }
                    else // Finish construction
                    {
                        GetComponent<WorldObjectComp_ConstructionSite>().EndConstruction(caravan) ;
                    }
                    Find.World.worldObjects.Remove(nextLeg);
                }
            }
        }

        /*
        public void setDestination(int destination)
        {
            toTile = destination ;
        }
        */

        public bool resourcesAlreadyConsumed()
        {
            return this.GetComponent<WorldObjectComp_ConstructionSite>().resourcesAlreadyConsumed() ;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("RoadsOfTheRim_siteInspectString".Translate(roadToBuild.label, string.Format("{0:0.0}",roadToBuild.movementCostMultiplier)));
            stringBuilder.AppendLine();
            stringBuilder.Append(this.GetComponent<WorldObjectComp_ConstructionSite>().progressDescription()) ;
            return stringBuilder.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look<RoadBuildableDef>(ref this.roadToBuild, "roadToBuild");
            Scribe_References.Look<Faction>(ref helpFromFaction, "helpFromFaction");
            Scribe_Values.Look<int>(ref helpFromTick, "helpFromTick");
            Scribe_Values.Look<float>(ref helpAmount, "helpAmount");
            Scribe_Values.Look<float>(ref helpWorkPerTick, "helpWorkPerTick");
            Scribe_References.Look<WorldObject>(ref LastLeg, "LastLeg");
        }

        public void UpdateProgressBarMaterial()
        {
            float percentageDone = GetComponent<WorldObjectComp_ConstructionSite>().percentageDone();
            ProgressBarMaterial = new Material(ShaderDatabase.MetaOverlay);
            Texture2D texture = new Texture2D(100, 100);
            ProgressBarMaterial.mainTexture = texture;
            for (int y = 0; y < 100; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    if (x >= 80)
                    {
                        if (y < (int)(100 * percentageDone))
                        {
                            texture.SetPixel(x, y, ColorFilled);
                        }
                        else
                        {
                            texture.SetPixel(x, y, ColorUnfilled);
                        }
                    }
                    else
                    {
                        texture.SetPixel(x, y, ColorTransparent);
                    }
                }
            }
            texture.Apply();
        }

        /*
        Check WorldObject Draw method to find why the construction site icon is rotated strangely when expanded
         */
        public override void Draw()
        {
            if (RoadsOfTheRim.RoadBuildingState.CurrentlyTargeting!=this || this.roadToBuild==null) // Do not draw the site if it's not yet finalised or if we don't know the type of road to build yet
            {
                base.Draw();
                WorldGrid worldGrid = Find.WorldGrid;
                Vector3 fromPos = worldGrid.GetTileCenter(this.Tile);
                float percentageDone = GetComponent<WorldObjectComp_ConstructionSite>().percentageDone();
                if (!ProgressBarMaterial)
                {
                    UpdateProgressBarMaterial();
                }
                WorldRendererUtility.DrawQuadTangentialToPlanet(fromPos, Find.WorldGrid.averageTileSize * .8f, 0.15f, ProgressBarMaterial);
            }
        }

        public void initiateFactionHelp(Faction faction , int tick , float amount , float amountPerTick)
        {
            helpFromFaction = faction ;
            helpFromTick = tick ;
            helpAmount = amount ;
            helpWorkPerTick = amountPerTick ;
            Find.LetterStack.ReceiveLetter(
                "RoadsOfTheRim_FactionStartsHelping".Translate(),
                "RoadsOfTheRim_FactionStartsHelpingText".Translate(helpFromFaction.Name, fullName() , string.Format("{0:0.00}", (float)(tick - Find.TickManager.TicksGame) / (float)GenDate.TicksPerDay)),
                LetterDefOf.PositiveEvent,
                new GlobalTargetInfo(this)
            );

        }

        public float factionHelp()
        {
            float amountOfHelp = 0;
            if ( (helpFromFaction!=null) && (Find.TickManager.TicksGame>helpFromTick) )
            {
                if (helpFromFaction.PlayerRelationKind == FactionRelationKind.Ally)
                {
                    // amountOfHelp is capped at the total amount of help provided (which is site.helpAmount)
                    amountOfHelp = helpWorkPerTick ;
                    if (helpAmount < helpWorkPerTick)
                    {
                        amountOfHelp = helpAmount;
                        //Log.Message(String.Format("[RotR] - faction {0} helps with {1:0.00} work", helpFromFaction.Name, amountOfHelp));
                        EndFactionHelp() ;
                    }
                    helpAmount -= amountOfHelp;
                }
                // Cancel help if the faction is not an ally any more
                else
                {
                    Find.LetterStack.ReceiveLetter(
                        "RoadsOfTheRim_FactionStopsHelping".Translate(),
                        "RoadsOfTheRim_FactionStopsHelpingText".Translate(helpFromFaction.Name , roadToBuild.label),
                        LetterDefOf.NegativeEvent,
                        new GlobalTargetInfo(this)
                    );
                    EndFactionHelp() ;
                }
            }
            return amountOfHelp;
        }

        public void EndFactionHelp()
        {
            RoadsOfTheRim.factionsHelp.helpFinished(helpFromFaction) ;
            helpFromFaction = null ;
            helpAmount = 0 ;
            helpFromTick = -1 ;
            helpWorkPerTick = 0 ;
        }

        // IncidentWorker_QuestPeaceTalks : shows me a good way to create a worldObject
    }

    /*
    A construction site comp :
    - Keeps track of all costs
    - Keeps track of how much work is left to do
    - Applies the effects of work done by a caravan
    - Creates the road once work is done
     */

}
