using System.Text;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{
    public class RoadConstructionSite : WorldObject
    {
        public RoadBuildableDef roadToBuild;

        public Tile toTile;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            RoadsOfTheRimSettings settings = LoadedModManager.GetMod<RoadsOfTheRim>().GetSettings<RoadsOfTheRimSettings>();
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
                g.disabledReason = null;
            }
            yield break;
        }

        /*
         * Do I need anything in the constructor ?
         */
        public RoadConstructionSite()
        {
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            if (stringBuilder.Length != 0)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.Append("Building: " + roadToBuild.label + " (movement difficulty: " + roadToBuild.movementCostMultiplier + ")");
            stringBuilder.AppendLine();
            stringBuilder.Append("Work left: " + roadToBuild.workNeeded);
            return stringBuilder.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<RoadBuildableDef>(ref this.roadToBuild, "roadToBuild", (RoadBuildableDef)null, false);
        }

        // Tile : caravan.Tile
        // IncidentWorker_QuestPeaceTalks : shows me a good way to create a worldObject
    }

    /*
    public class CompProperties_RoadsOfTheRimConstructionSite : WorldObjectCompProperties
    {
        public string siteType = "dirt path from comp";

        public CompProperties_RoadsOfTheRimConstructionSite()
        {
            compClass = typeof(CompRoadsOfTheRimConstructionSite);
        }
    }

    public class CompRoadsOfTheRimConstructionSite : WorldObjectComp
    {
        public CompProperties_RoadsOfTheRimConstructionSite properties
        {
            get
            {
                return (CompProperties_RoadsOfTheRimConstructionSite)props;
            }
        }
    }
    */
}