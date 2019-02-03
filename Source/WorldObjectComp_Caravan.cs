using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Text;
using System ;

namespace RoadsOfTheRim
{
    public enum CaravanState : byte
    {
        Moving,
        NightResting,
        AllOwnersHaveMentalBreak,
        AllOwnersDowned,
        ImmobilizedByMass,
        ReadyToWork
    }

    public class WorldObjectComp_Caravan : WorldObjectComp
    {
        public bool currentlyWorkingOnSite = false;

        public bool workOnWakeUp = false;

        private RoadConstructionSite site ;

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
            return site ;
        }

        public bool setSiteFromTile()
        {
            try
            {
                site = (RoadConstructionSite)Find.WorldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), (GetCaravan().Tile));
                return true ;
            }
            catch (Exception e)
            {
                Log.Error("[RotR] Exception : " + e);
                return false ;
            }
        }

        public void unsetSite()
        {
            site = null ;
        }

        public CaravanState CaravanCurrentState()
        {
            Caravan caravan = GetCaravan() ;
            if (caravan.pather.MovingNow)
            {
                return CaravanState.Moving ;
            }
            if (caravan.ImmobilizedByMass)
            {
                return CaravanState.ImmobilizedByMass ;
            }
            if (caravan.AllOwnersDowned)
            {
                return CaravanState.AllOwnersDowned ;
            }
            if (caravan.AllOwnersHaveMentalBreak)
            {
                return CaravanState.AllOwnersHaveMentalBreak ;
            }
            if (caravan.ImmobilizedByMass)
            {
                return CaravanState.ImmobilizedByMass ;
            }
            if (caravan.NightResting)
            {
                return CaravanState.NightResting ;
            }
            return CaravanState.ReadyToWork ;
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
                // Caravan is working AND there's a site here AND caravan can work AND the site is indeed the same the caravan was working on
                if (this.currentlyWorkingOnSite & isThereAConstructionSiteHere() & (CaravanCurrentState() == CaravanState.ReadyToWork) && (GetCaravan().Tile==getSite().Tile))
                {
                    base.CompTick();
                    site.TryToSkipBetterRoads(caravan) ; // No need to work if there's a better road here
                    if (RoadsOfTheRim.doSomeWork(caravan, getSite(), out bool noMoreResources))
                    {
                        stopWorking() ;
                        unsetSite() ;
                    }
                }

                // Site tile and Caravan tile mismatch 
                if (getSite()!=null && (GetCaravan().Tile!=getSite().Tile))
                {
                    stopWorking() ;
                    unsetSite() ;
                }

                // Stop working as soon as the caravan moves, or rests, or is downed
                if (this.currentlyWorkingOnSite & (CaravanCurrentState() != CaravanState.ReadyToWork))
                {
                    stopWorking();
                    string stoppedReason = "";
                    if (CaravanCurrentState() == CaravanState.AllOwnersDowned)
                    {
                        stoppedReason = "Everyone is down";
                    }
                    if (CaravanCurrentState() == CaravanState.AllOwnersHaveMentalBreak)
                    {
                        stoppedReason = "Everyone is having a mental break";
                    }
                    if (CaravanCurrentState() == CaravanState.ImmobilizedByMass)
                    {
                        stoppedReason = "Too heavy to move";
                    }
                    // If the caravan is resting, stop working but remember to restart working on wake up
                    if (CaravanCurrentState() == CaravanState.NightResting)
                    {
                        this.workOnWakeUp = true;
                        stoppedReason = " resting at night. Work will resume in the morning.";
                    }
                    if (stoppedReason != "")
                    {
                        Messages.Message("Caravan stopped working on site : " + stoppedReason, MessageTypeDefOf.RejectInput);
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
            if (CaravanCurrentState() == CaravanState.ReadyToWork)
            {
                Caravan caravan = GetCaravan();
                caravan.pather.StopDead();
                setSiteFromTile() ;
                this.currentlyWorkingOnSite = true ;
            }
            else
            {
                Log.Warning("[RotR] : Caravan was given the order to start working but can't work.");
            }
        }
        
        //Stop working on a Construction Site. No need to check anything, just stop
        public void stopWorking()
        {
            this.currentlyWorkingOnSite = false ;
            // TO DO : A quick message (with a reason) would be nice
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
            return CalculateConstruction(GetCaravan().PawnsListForReading);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.currentlyWorkingOnSite, "RoadsOfTheRim_Caravan_currentlyWorkingOnSite" , false , true);
            Scribe_Values.Look<bool>(ref this.workOnWakeUp, "RoadsOfTheRim_Caravan_workOnWakeUp", false, true);
            Scribe_References.Look<RoadConstructionSite>(ref this.site, "RoadsOfTheRim_Caravan_RoadConstructionSite");
        }
    }
}
