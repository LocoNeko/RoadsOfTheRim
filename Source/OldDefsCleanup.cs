using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{

    public class OldDefsCleanupComp : ThingComp
    {
        public override void CompTick()
        {
            Thing oldISR2G = this.parent ;
            int level = 0;
            if (oldISR2G.def.defName == "RotR_ISR2G")
            {
                level = 1;
            }
            else if (oldISR2G.def.defName == "RotR_AISR2G")
            {
                level = 2;
            }
            if (level > 0)
            {
                RoadsOfTheRim.DebugLog("Destroying a ISR2G level " + level + " at position "  + oldISR2G.Position.ToString());
                oldISR2G.Destroy();
            }
        }
    }
}