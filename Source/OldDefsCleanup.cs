using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RoadsOfTheRim
{
    // I had to take into account the old defs of ISR2G that used to be buildings, and replace them with new ISR2G defs that are craftable items
    // So I use this comp and add it to the old Defs
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
                string newThingDefName = (level == 1 ? "RotR_ISR2GNew" : "RotR_AISR2GNew");
                Thing newThing = ThingMaker.MakeThing(ThingDef.Named(newThingDefName));
                IntVec3 position = oldISR2G.Position;
                Map map = oldISR2G.MapHeld;
                RoadsOfTheRim.DebugLog("Replacing a ISR2G level " + level + " at position " + position.ToString());
                oldISR2G.Destroy();
                GenPlace.TryPlaceThing(newThing, position, map, ThingPlaceMode.Near);
            }
        }
    }
}