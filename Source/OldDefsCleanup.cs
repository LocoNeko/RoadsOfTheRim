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
            RoadsOfTheRim.DebugLog("AN ISR2G just ticked at " + this.parent.Position.ToString());
        }

    }
}