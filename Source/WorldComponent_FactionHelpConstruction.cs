using System.Collections.Generic ;
using RimWorld ;
using RimWorld.Planet ;
using Verse ;

namespace RoadsOfTheRim
{
    public class WorldComponent_FactionRoadConstructionHelp : WorldComponent
    {
        public const int helpCooldownTicks = 5 * GenDate.TicksPerDay; // A faction can only help on a construction site 5 days after it's finished helping on another one
        private Dictionary<Faction, int> canHelpAgainAtTick = new Dictionary<Faction, int>();
        public Dictionary<Faction, bool> currentlyHelping = new Dictionary<Faction, bool>();
        public WorldComponent_FactionRoadConstructionHelp(World world) : base(world)
        {
        }

        public void setHelpAgainTick(Faction faction, int tick)
        {
            canHelpAgainAtTick[faction] = tick;
        }

        public int? getHelpAgainTick(Faction faction)
        {
            int result;
            if (canHelpAgainAtTick.TryGetValue(faction, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public bool getCurrentlyHelping(Faction faction)
        {
            bool result;
            if (currentlyHelping.TryGetValue(faction, out result))
            {
                return result;
            }
            return false;
        }

        public bool inCooldown(Faction faction)
        {
            int? helpAgainTick = getHelpAgainTick(faction);
            if ((helpAgainTick == null) || (Find.TickManager.TicksGame >= getHelpAgainTick(faction)))
            {
                return false;
            }
            return true;
        }

        public float daysBeforeFactionCanHelp(Faction faction)
        {
            int? tick;
            try
            {
                tick = getHelpAgainTick(faction);
                if (tick == null)
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
            return (float)(getHelpAgainTick(faction) - Find.TickManager.TicksGame) / GenDate.TicksPerDay;
        }

    }
}