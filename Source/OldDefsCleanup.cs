using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{
    public class RotR_ISR2G : ThingWithComps
    {
        public override void PostMake()
        {
            base.PostMake();
            RoadsOfTheRim.DebugLog("Just made an ISR2G in " + this.Position.ToString());
        }
    }

    public class RotR_AISR2G : ThingWithComps
    {
        public override void PostMake()
        {
            base.PostMake();
            RoadsOfTheRim.DebugLog("Just made an AISR2G in " + this.Position.ToString());
        }
    }
}