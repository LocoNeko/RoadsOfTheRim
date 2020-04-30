using System ;
using System.Text;
using System.Collections.Generic ;
using System.Linq;
using RimWorld ;
using RimWorld.Planet;
using Verse ;
using UnityEngine;

namespace RoadsOfTheRim
{
    public class RoadConstructionLeg : WorldObject
    {
        private RoadConstructionSite site ;

        private RoadConstructionLeg previous ;

        private RoadConstructionLeg next;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<RoadConstructionSite>(ref site, "site");
            Scribe_References.Look<RoadConstructionLeg>(ref previous, "previous");
            Scribe_References.Look<RoadConstructionLeg>(ref next, "next");
        }

        public override Material Material
        {
            get
            {
                if (next==null)
                {
                    // This alternate Material : goal flag
                    return RotR_StaticConstructorOnStartup.ConstructionLegLast_Material;
                }
                return base.Material ;
            }

        }

        public RoadConstructionLeg Previous
        {
            get
            {
                return previous ;
            }
            set
            {
                previous = value ;
            }
        }

        public RoadConstructionLeg Next
        {
            get
            {
                return next;
            }
            set
            {
                next = value;
            }
        }

        public RoadConstructionSite GetSite()
        {
            return site;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            if (this.Next is null)
            {
                stringBuilder.Append("Goal");
            }
            else
            {
                stringBuilder.Append("RoadsOfTheRim_siteInspectString".Translate(this.GetSite().roadDef.label, string.Format("{0:0.0}", this.GetSite().roadDef.movementCostMultiplier)));

                // Show total cost modifiers : TODO - MOve that to a static function in WorldObjectComp_ConstructionSite which uses it for the site alreday
                float totalCostModifier = 0f;
                stringBuilder.Append(this.GetSite().CostModifersDescription(ref totalCostModifier));

                // Show costs
                WorldObjectComp_ConstructionSite SiteComp = this.GetSite().GetComponent<WorldObjectComp_ConstructionSite>();
                foreach (string resourceName in DefModExtension_RotR_RoadDef.allResourcesAndWork)
                {
                    if (SiteComp.GetCost(resourceName) > 0)
                    {
                        // The cost modifier doesn't affect some advanced resources, as defined in static DefModExtension_RotR_RoadDef.allResourcesWithoutModifiers
                        // TO DO : COuld this be combined with WorldObjectComp_ConstructionSite.setCosts() ? shares a lot in common except rebates. Can we really calcualte rebate on a leg ?
                        float costModifierForThisResource = ((DefModExtension_RotR_RoadDef.allResourcesWithoutModifiers.Contains(resourceName)) ? 1 : totalCostModifier);
                        stringBuilder.AppendLine();
                        stringBuilder.Append((int)SiteComp.GetCost(resourceName) * costModifierForThisResource + " " + resourceName);
                    }
                }
            }

            return stringBuilder.ToString();
        }


        // Here, test if we picked a tile that's already part of the chain for this construction site (different construction sites can cross each other's paths)
        // Yes -> 
        //      Was it the construction site itself ?
        //      Yes -> We are done creating the site
        //      No ->  delete this leg and all legs after it
        // No -> create a new Leg
        public static bool ActionOnTile(RoadConstructionSite site , int tile)
        {
            if (site.def != DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true))
            {
                Log.Error("[RotR] - The RoadConstructionSite given is somehow wrong");
                return true ;
            }
            try
            {
                foreach (WorldObject o in Find.WorldObjects.ObjectsAt(tile))
                {
                    // Action on the construction site = we're done
                    if ( (o.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true)) && (RoadConstructionSite)o == site)
                    {
                        return true; 
                    }
                    // Action on a leg that's part of this chain = we should delete all legs after that & keep targetting
                    if ((o.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionLeg", true)) && ((RoadConstructionLeg)o).site == site)
                    {
                        RoadConstructionLeg.Remove((RoadConstructionLeg)o);
                        Target(site);
                        return false;
                    }
                }

                // Check whether we clicked on a neighbour
                List<int> neighbouringTiles = new List<int>();
                Find.WorldGrid.GetTileNeighbors(tile, neighbouringTiles);
                // This is not a neighbour : do nothing
                if (!neighbouringTiles.Contains(site.LastLeg.Tile))
                {
                    Target(site);
                    return false;
                }

                // There can be no ConstructionLeg on a biome that doesn't allow roads
                if (!DefModExtension_RotR_RoadDef.BiomeAllowed(tile , site.roadDef , out BiomeDef biomeHere))
                {
                    Messages.Message("RoadsOfTheRim_BiomePreventsConstruction".Translate(site.roadDef.label , biomeHere.label) , MessageTypeDefOf.RejectInput);
                    Target(site);
                    return false ;
                }
                else if (!DefModExtension_RotR_RoadDef.ImpassableAllowed(tile , site.roadDef))
                {
                    Messages.Message("RoadsOfTheRim_BiomePreventsConstruction".Translate(site.roadDef.label, " impassable mountains"), MessageTypeDefOf.RejectInput);
                    Target(site);
                    return false;
                }

                RoadConstructionLeg newLeg = (RoadConstructionLeg)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionLeg", true));
                newLeg.Tile = tile;
                newLeg.site = site;
                // This is not the first Leg
                if (site.LastLeg.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionLeg", true))
                {
                    RoadConstructionLeg l = site.LastLeg as RoadConstructionLeg;
                    l.SetNext(newLeg);
                    newLeg.previous = l;
                }
                else
                {
                    newLeg.previous = null;
                }
                newLeg.SetNext(null);
                Find.WorldObjects.Add(newLeg);
                site.LastLeg = newLeg ;
                Target(site);
                return false;
            }
            catch (Exception e)
            {
                Log.Error("[RotR] Exception : " + e);
                return true;
            }
        }

        public override void Draw()
        {
            base.Draw();
            WorldGrid worldGrid = Find.WorldGrid;
            Vector3 fromPos = worldGrid.GetTileCenter(Tile);
            Vector3 toPos = ((previous != null) ? worldGrid.GetTileCenter(previous.Tile) : worldGrid.GetTileCenter(site.Tile));
            float d = 0.05f;
            fromPos += fromPos.normalized * d;
            toPos += toPos.normalized * d;
            GenDraw.DrawWorldLineBetween(fromPos, toPos);
            // Note : I override the material (see above) to display a goal flag if the leg is the last one, and a circle if it's not, so it looks like this :
            // Site---Leg---Leg---Leg---Leg---Goal
        }

        public void SetNext(RoadConstructionLeg nextLeg)
        {
            try
            {
                next = nextLeg ;
            }
            catch (Exception e)
            {
                Log.Error("[RotR] Exception : "+e);
            }
        }

        public static void Target(RoadConstructionSite site)
        {
            // Log.Warning("[RotR] - Target(site)");
            Find.WorldTargeter.BeginTargeting(delegate (GlobalTargetInfo target)
            {
                return RoadConstructionLeg.ActionOnTile(site, target.Tile);
            },
            true, RotR_StaticConstructorOnStartup.ConstructionLeg_MouseAttachment , false, null ,
            delegate (GlobalTargetInfo target)
            {
                return "RoadsOfTheRim_BuildToHere".Translate();
            });
        }

        /*
         * Remove all legs up to and including the one passed in argument      
         */      
        public static void Remove(RoadConstructionLeg leg)
        {
            RoadConstructionSite site = leg.site;
            RoadConstructionLeg CurrentLeg = (RoadConstructionLeg)site.LastLeg;
            while (CurrentLeg != leg.previous)
            {
                if (CurrentLeg.previous!=null)
                {
                    RoadConstructionLeg PreviousLeg = CurrentLeg.previous;
                    PreviousLeg.SetNext(null);
                    site.LastLeg = PreviousLeg;
                    Find.WorldObjects.Remove(CurrentLeg);
                    CurrentLeg = PreviousLeg;
                }
                else
                {
                    Find.WorldObjects.Remove(CurrentLeg);
                    site.LastLeg = site;
                    break;
                }
            }
        }
    }
}