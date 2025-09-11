using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{
    public class FactionIncidentCooldown(World world) : WorldComponent(world)
    {
        public Dictionary<ValueTuple<Faction, string>, float> lastTimes = [];
        public List<DelayedQuest> delayedQuests = [];

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref lastTimes, "lastTimes", LookMode.Deep, LookMode.Value);
            lastTimes ??= [];

            Scribe_Collections.Look(ref delayedQuests, "delayedQuests", LookMode.Deep);
            delayedQuests ??= [];
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            for (int i = delayedQuests.Count - 1; i >= 0; i--)
            {
                DelayedQuest delayed = delayedQuests[i];
                if (Find.TickManager.TicksGame >= delayed.fireTick)
                {
                    QuestScriptDef def = DefDatabase<QuestScriptDef>.GetNamed(delayed.questDefName);
                    if (def != null)
                    {
                        Slate slate = new();
                        slate.Set("asker", delayed.faction);
                        Quest quest = QuestGen.Generate(def, slate);
                        Find.QuestManager.Add(quest);
                    }
                    delayedQuests.RemoveAt(i);
                }
            }
        }
    }

    public class DelayedQuest : IExposable
    {
        public string questDefName;
        public Faction faction;
        public int fireTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref questDefName, "questDefName");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref fireTick, "fireTick");
        }
    }
}