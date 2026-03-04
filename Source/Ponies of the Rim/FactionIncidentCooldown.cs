using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace PoniesOfTheRim
{
    public class FactionIncidentCooldown : WorldComponent
    {
        
                public Dictionary<CooldownKey, float> lastTimes = new Dictionary<CooldownKey, float>();
                public List<DelayedQuest> delayedQuests = new List<DelayedQuest>();
                private List<Faction> cd_factions = new List<Faction>();
        private List<string> cd_defNames = new List<string>();
        private List<float> cd_ticks = new List<float>();

        public FactionIncidentCooldown(World world) : base(world) { }

        public override void ExposeData()
        {
                        if (Scribe.mode == LoadSaveMode.Saving)
            {
                cd_factions = new List<Faction>();
                cd_defNames = new List<string>();
                cd_ticks = new List<float>();

                foreach (var kvp in lastTimes)
                {
                                        if (kvp.Key.faction == null) continue;

                    cd_factions.Add(kvp.Key.faction);
                    cd_defNames.Add(kvp.Key.defName);
                    cd_ticks.Add(kvp.Value);
                }
            }

            Scribe_Collections.Look(ref cd_factions, "cd_factions", LookMode.Reference);
            Scribe_Collections.Look(ref cd_defNames, "cd_defNames", LookMode.Value);
            Scribe_Collections.Look(ref cd_ticks, "cd_ticks", LookMode.Value);
            Scribe_Collections.Look(ref delayedQuests, "delayedQuests", LookMode.Deep);

                        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                lastTimes = new Dictionary<CooldownKey, float>();

                if (cd_factions != null && cd_defNames != null && cd_ticks != null)
                {
                    int count = Math.Min(cd_factions.Count, Math.Min(cd_defNames.Count, cd_ticks.Count));
                    for (int i = 0; i < count; i++)
                    {
                        if (cd_factions[i] != null && cd_defNames[i] != null)
                        {
                            var key = new CooldownKey(cd_factions[i], cd_defNames[i]);
                            lastTimes[key] = cd_ticks[i];
                        }
                    }
                }

                                cd_factions = null;
                cd_defNames = null;
                cd_ticks = null;
            }

            if (delayedQuests == null)
            {
                delayedQuests = new List<DelayedQuest>();
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            for (int i = delayedQuests.Count - 1; i >= 0; i--)
            {
                DelayedQuest delayed = delayedQuests[i];
                if (Find.TickManager.TicksGame >= delayed.fireTick)
                {
                    QuestScriptDef questScript = DefDatabase<QuestScriptDef>.GetNamed(delayed.questDefName, false);
                    if (questScript != null && delayed.faction != null)
                    {
                                                CommsConsolePatch.GenerateQuestWithFaction(questScript, delayed.faction);
                    }
                    else if (questScript != null)
                    {
                                                Slate slate = new Slate();
                        Quest quest = QuestGen.Generate(questScript, slate);
                        Find.QuestManager.Add(quest);
                        QuestUtility.SendLetterQuestAvailable(quest);
                    }
                    else
                    {
                        Log.Warning($"[PoniesOfTheRim] Delayed quest '{delayed.questDefName}' not found in DefDatabase.");
                    }
                    delayedQuests.RemoveAt(i);
                }
            }
        }
    }

    public struct CooldownKey : IEquatable<CooldownKey>
    {
        public Faction faction;
        public string defName;

        public CooldownKey(Faction faction, string defName)
        {
            this.faction = faction;
            this.defName = defName;
        }

        public bool Equals(CooldownKey other)
        {
            return faction == other.faction && defName == other.defName;
        }

        public override bool Equals(object obj)
        {
            return obj is CooldownKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((faction?.GetHashCode() ?? 0) * 397) ^ (defName?.GetHashCode() ?? 0);
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
            Scribe_Values.Look(ref fireTick, "fireTick", 0);
        }
    }
}