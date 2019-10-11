﻿using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

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

    public static class PawnBuildingUtility
    {
        public static bool HealthyColonist(Pawn p)
        {
            return (p.IsFreeColonist && p.health.State == PawnHealthState.Mobile);
        }

        public static bool HealthyPackAnimal(Pawn p)
        {
            return (p.RaceProps.packAnimal && p.health.State == PawnHealthState.Mobile);
        }

        public static float ConstructionValue(Pawn p)
        {
            return (p.GetStatValue(StatDefOf.ConstructionSpeed) * p.GetStatValue(StatDefOf.ConstructSuccessChance));
        }

        public static int ConstructionLevel(Pawn p)
        {
            return (p.skills.GetSkill(SkillDefOf.Construction).levelInt);
        }

        public static string ShowConstructionValue(Pawn p)
        {
            if (PawnBuildingUtility.HealthyColonist(p))
            {
                return String.Format("{0:0.##}", PawnBuildingUtility.ConstructionValue(p));
            }
            if (PawnBuildingUtility.HealthyPackAnimal(p))
            {
                return String.Format("+{0:0.##}", PawnBuildingUtility.ConstructionValue(p));
            }
            return "-";
        }

        public static string ShowSkill(Pawn p)
        {
            if (PawnBuildingUtility.HealthyColonist(p))
            {
                return String.Format("{0:0}", PawnBuildingUtility.ConstructionLevel(p));
            }
            return "-";
        }

        public static string ShowBestRoad(Pawn p)
        {
            RoadDef BestRoadDef = null;
            if (PawnBuildingUtility.HealthyColonist(p))
            { 
                foreach (RoadDef thisDef in DefDatabase<RoadDef>.AllDefs)
                {
                    if (thisDef.HasModExtension<DefModExtension_RotR_RoadDef>() && thisDef.GetModExtension<DefModExtension_RotR_RoadDef>().built) // Only add RoadDefs that are buildable, based on DefModExtension_RotR_RoadDef.built
                    {
                        DefModExtension_RotR_RoadDef RoadDefMod = thisDef.GetModExtension<DefModExtension_RotR_RoadDef>();
                        if (PawnBuildingUtility.ConstructionLevel(p) >= RoadDefMod.minConstruction)
                        {
                            if ((BestRoadDef == null) || (thisDef.movementCostMultiplier < BestRoadDef.movementCostMultiplier))
                            {
                                BestRoadDef = thisDef;
                            }
                        }
                    }
                }
                if (BestRoadDef != null)
                {
                    return BestRoadDef.label;
                }
            }
            return "-";
        }
    }

        public class WorldObjectComp_Caravan : WorldObjectComp
    {
        public bool currentlyWorkingOnSite = false;

        // workOnWakeUp must be more than just working when waking up, it must tell the caravan to work as long as the site is not finished
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
                RoadsOfTheRim.DebugLog("" , e) ;
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
            /* Remove as this should not prevent the caravan from working (Issue #38)
            if (caravan.ImmobilizedByMass)
            {
                return CaravanState.ImmobilizedByMass ;
            }
            */
            if (caravan.AllOwnersDowned)
            {
                return CaravanState.AllOwnersDowned ;
            }
            if (caravan.AllOwnersHaveMentalBreak)
            {
                return CaravanState.AllOwnersHaveMentalBreak ;
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
                    // More general use of workOnWakeUp : set it to true if the caravan was working on a site but stopped working for any reason listed in CaravanState
                    this.workOnWakeUp = true;
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
                    if (CaravanCurrentState() == CaravanState.NightResting)
                    {
                        stoppedReason = " resting at night. Work will resume in the morning.";
                    }
                    if (stoppedReason != "")
                    {
                        Messages.Message("Caravan stopped working on site : " + stoppedReason, MessageTypeDefOf.RejectInput);
                    }
                    // This should not happen ?
                    else
                    {
                        this.workOnWakeUp = false;
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
        public float amountOfWork(bool verbose = false)
        {
            List<Pawn> pawns = GetCaravan().PawnsListForReading ;
            DefModExtension_RotR_RoadDef roadDefModExtension = null ;
            try 
            {
                roadDefModExtension = site.roadDef.GetModExtension<DefModExtension_RotR_RoadDef>() ;
            }
            catch { /* Either there's no site, no roaddef, or no modextension. In any case, not much to do here */}
            //site.roadDef.GetModExtension<DefModExtension_RotR_RoadDef>().minConstruction ;
            float totalConstruction = 0f;
            float totalConstructionAboveMinLevel = 0f;
            float animalConstruction = 0f;
            foreach (Pawn pawn in pawns)
            {
                /*
                if (pawn.IsFreeColonist && pawn.health.State == PawnHealthState.Mobile)
                {
                    totalConstruction += pawn.GetStatValue(StatDefOf.ConstructionSpeed) * pawn.GetStatValue(StatDefOf.ConstructSuccessChance);

                    if (roadDefModExtension!=null && pawn.skills.GetSkill(SkillDefOf.Construction).levelInt >= roadDefModExtension.minConstruction)
                    {
                        totalConstructionAboveMinLevel += pawn.GetStatValue(StatDefOf.ConstructionSpeed) * pawn.GetStatValue(StatDefOf.ConstructSuccessChance);
                    }
                }
                else if (pawn.RaceProps.packAnimal  && pawn.health.State == PawnHealthState.Mobile)
                {
                    animalConstruction += pawn.GetStatValue(StatDefOf.ConstructionSpeed) * pawn.GetStatValue(StatDefOf.ConstructSuccessChance);
                }
                */
                float PawnConstructionValue = PawnBuildingUtility.ConstructionValue(pawn);

                if (PawnBuildingUtility.HealthyColonist(pawn))
                {
                    totalConstruction += PawnConstructionValue ;

                    if (roadDefModExtension != null && PawnBuildingUtility.ConstructionLevel(pawn) >= roadDefModExtension.minConstruction)
                    {
                        totalConstructionAboveMinLevel += PawnConstructionValue;
                    }
                }

                else if (PawnBuildingUtility.HealthyPackAnimal(pawn))
                {
                    animalConstruction += PawnConstructionValue;
                }
            }

            // Check minimum construction level requirements if needed
            float ratioActuallyWorked = 1f ;
            if (roadDefModExtension!=null)
            {
                float ratioOfConstructionAboveMinLevel = totalConstructionAboveMinLevel / totalConstruction ;
                if (ratioOfConstructionAboveMinLevel < roadDefModExtension.percentageOfminConstruction)
                {
                    ratioActuallyWorked = (ratioOfConstructionAboveMinLevel/roadDefModExtension.percentageOfminConstruction) ; // Reduce total construction by the shortage of skill, expressed as a ratio 
                    totalConstruction *= ratioActuallyWorked ;
                    if (verbose)
                    {
                        Messages.Message("RoadsOfTheRim_InsufficientConstructionMinLevel".Translate(totalConstruction, roadDefModExtension.percentageOfminConstruction.ToString("P0"), roadDefModExtension.minConstruction), MessageTypeDefOf.NegativeEvent);
                    }
                }
            }

            // Pack animals can only add as much work as humans (i.e. : at best, pack animals double the amount of work)
            if (animalConstruction > totalConstruction)
            {
                animalConstruction = totalConstruction;
            }
            totalConstruction += animalConstruction;
            return totalConstruction;
        }

        public void teachPawns(float ratio) // The pawns learn a little construction
        {
            ratio = Math.Max(Math.Min(1,ratio), 0);
            List<Pawn> pawns = GetCaravan().PawnsListForReading;
            RoadsOfTheRim.DebugLog("Teaching Construction to pawns");
            foreach (Pawn pawn in pawns)
            {
                if (pawn.IsFreeColonist && pawn.health.State == PawnHealthState.Mobile && !pawn.RaceProps.packAnimal)
                {
                    pawn.skills.Learn(SkillDefOf.Construction, ratio, false);
                    RoadsOfTheRim.DebugLog(pawn.Name+" learned " + ratio + " Xp = "+pawn.skills.GetSkill(SkillDefOf.Construction).XpTotalEarned);
                }
            }

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
