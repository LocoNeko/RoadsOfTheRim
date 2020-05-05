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
        public override void Tick()
        {
            base.Tick();
            RoadsOfTheRim.DebugLog("AN ISR2G just ticked at " + this.Position.ToString());
        }
    }

    public class RotR_AISR2G : ThingWithComps
    {
        public override void Tick()
        {
            base.Tick();
            RoadsOfTheRim.DebugLog("AN ISR2G just ticked at " + this.Position.ToString());
        }
    }
}