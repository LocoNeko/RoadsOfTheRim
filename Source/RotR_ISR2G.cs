using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;


namespace RoadsOfTheRim
{
    public class RotR_ISR2G : Thing
    {
        public override void ExposeData()
        {
            base.ExposeData();
            RoadsOfTheRim.DebugLog("Exposing data of ISR2G :"+this.Label+" at position "+this.Position.ToString());
        }

    }
}


