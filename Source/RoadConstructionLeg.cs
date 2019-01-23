using System ;
using System.Collections.Generic ;
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
                    // TO DO : This alternate structure should be a goal flag. Would love to NOT hardcode the path and somehow put it in the XML for RoadConstructionLeg, but can I ?
                    return MaterialPool.MatFrom(this.def.texture , ShaderDatabase.WorldOverlayTransparentLit , Color.blue , WorldMaterials.DynamicObjectRenderQueue ) ;
                }
                return base.Material ;
            }

        }

        // Here, test if we picked a tile that's already part of the chain.
        // Yes -> delete this leg and all legs after it
        // No -> create a new Leg
        public static RoadConstructionLeg ActionOnTile(RoadConstructionSite site , int tile)
        {
            // The RoadConstructionSite given is somehow wrong
            if (site.def != DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true))
            {
                return null ;
            }
            try
            {
                return new RoadConstructionLeg(site, tile) ;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString()) ;
                return null ;
            }
        }

        public override void Draw()
        {
            WorldGrid worldGrid = Find.WorldGrid;
            Vector3 fromPos = worldGrid.GetTileCenter(Tile);
            if (next!=null)
            {
                // Draw a line to the next leg
                Vector3 toPos = worldGrid.GetTileCenter(next.Tile);
                GenDraw.DrawWorldLineBetween(fromPos, toPos) ;
            }
            // Note : I override the material (see above) to display a goal flag if the leg is the last one, and a circle if it's not, so it looks like this :
            // Site---Leg---Leg---Leg---Leg---Goal
            base.Draw() ;
        }

        public void SetNext(RoadConstructionLeg nextLeg)
        {
            try
            {
                next = nextLeg ;
            }
            catch (Exception e)
            {
                Log.Error("[RotR] Exception : "+e.ToString());
            }
        }

        public void remove()
        {

        }

        private RoadConstructionLeg(RoadConstructionSite site , int tile)
        {
            List<int> neighbouringTiles = new List<int>();
            Find.WorldGrid.GetTileNeighbors(tile, neighbouringTiles);
            // This is not a neighbour : do nothing
            if (!neighbouringTiles.Contains(site.LastLeg.Tile))
            {
                return ;
            }
            this.site = site;
            // This is not the first Leg
            if (site.LastLeg.def == DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionLeg", true))
            {
                RoadConstructionLeg l = site.LastLeg as RoadConstructionLeg ;
                l.SetNext(this) ;
                previous = l ;
                // Then, change the material of the previous leg to a dot, and the last leg to a goal flag
            }
            else
            {
                previous = null ;
            }
            SetNext(null) ;
            site.LastLeg = this ;
        }
    }
}