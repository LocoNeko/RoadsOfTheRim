using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Text;

namespace RoadsOfTheRim
{
    public class WorldObjectComp_Caravan : WorldObjectComp
    {
        public bool currentlyWorkingOnSite = false;
        public bool workOnWakeUp = false;

        public Caravan GetCaravan()
        {
            return (Caravan)this.parent;
        }

        public bool isThereAConstructionSiteHere()
        {
            return Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), GetCaravan().Tile);
        }

        public RoadConstructionSite getSite()
        {
            try
            {
                return (RoadConstructionSite)Find.WorldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), ((Caravan)this.parent).Tile);
            }
            catch
            {
                return null;
            }
        }

        public bool CaravanCanWork()
        {
            Caravan caravan = GetCaravan() ;
            return (!caravan.CantMove & !caravan.pather.MovingNow) ;
        }

        public override void CompTick()
        {
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                Caravan caravan = GetCaravan();
                // Wake up the caravan if it was nightresting
                if (this.workOnWakeUp && !caravan.NightResting)
                {
                    this.workOnWakeUp = false;
                    this.currentlyWorkingOnSite = true;
                }

                // Do some work & stop working if finished
                if (this.currentlyWorkingOnSite & isThereAConstructionSiteHere() & CaravanCanWork())
                {
                    RoadConstructionSite TheSite = getSite();
                    if (TheSite.GetComponent<CompRoadsOfTheRimConstructionSite>().doSomeWork(caravan))
                    {
                        stopWorking() ;
                    }
                    base.CompTick();
                }

                // Stop working as soon as the caravan moves, or rests, or is downed
                if (this.currentlyWorkingOnSite & !CaravanCanWork())
                {
                    stopWorking();
                    // If the caravan is resting, stop working but remember to restart working on wake up
                    if (caravan.NightResting)
                    {
                        this.workOnWakeUp = true;
                    }
                }

                if (!isThereAConstructionSiteHere())
                {
                    stopWorking();
                }
            }
        }

        //Start working on a Construction Site.
        public void startWorking()
        {
            if (CaravanCanWork())
            {
                Caravan caravan = GetCaravan();
                caravan.pather.StopDead();
                this.currentlyWorkingOnSite = true ;
            }
            else
            {
                Log.Message("[Roads of the Rim] : Caravan was given the order to start working but can't work.");
            }
        }
        
        //Stop working on a Construction Site. No need to check anything, just stop
        public void stopWorking()
        {
            this.currentlyWorkingOnSite = false ;
        }

        /*
        * Amount of work :
        * - Construction speed (0.5 + 0.15 per level) times the construct success chance (0.75 to 1.13 - lvl 8 is 1)
        * - Pack animals help as well (see below)
        */
        public static float CalculateConstruction(List<Pawn> pawns)
        {
            float totalConstruction = 0f;
            float animalConstruction = 0f;
            StringBuilder str = new StringBuilder();
            foreach (Pawn pawn in pawns)
            {
                if (pawn.IsColonist)
                {
                    totalConstruction += pawn.GetStatValue(StatDefOf.ConstructionSpeed) * pawn.GetStatValue(StatDefOf.ConstructSuccessChance);
                    str.Append(pawn.Name+" (Hmn) : "+ pawn.GetStatValue(StatDefOf.ConstructionSpeed)+"*"+ pawn.GetStatValue(StatDefOf.ConstructSuccessChance)+", ");
                }
                else if (pawn.RaceProps.packAnimal)
                {
                    animalConstruction += pawn.GetStatValue(StatDefOf.ConstructionSpeed) * pawn.GetStatValue(StatDefOf.ConstructSuccessChance);
                    str.Append(pawn.Name + " (Ani) : " + pawn.GetStatValue(StatDefOf.ConstructionSpeed) + "*" + pawn.GetStatValue(StatDefOf.ConstructSuccessChance) + ", ");
                }
            }
            // Pack animals can only add as much work as humans (i.e. : at best, pack animals double the amount of work)
            if (animalConstruction > totalConstruction)
            {
                animalConstruction = totalConstruction;
            }
            totalConstruction += animalConstruction;
            str.Append(" Total = "+totalConstruction);
            // Log.Message("[RofR] DEBUG : Calculate construction - "+str);
            // TO DO : the pawns should learn construction a little when actual construction is done
            return totalConstruction;
        }

        public float amountOfWork()
        {
            Caravan caravan = (Caravan)this.parent;
            return CalculateConstruction(caravan.PawnsListForReading);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.currentlyWorkingOnSite, "RoadsOfTheRim_Caravan_currentlyWorkingOnSite" , false , true);
            Scribe_Values.Look<bool>(ref this.workOnWakeUp, "RoadsOfTheRim_Caravan_workOnWakeUp", false, true);
        }
    }
}
