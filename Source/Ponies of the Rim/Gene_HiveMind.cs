using RimWorld;
using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim
{
public class Gene_HiveMind : Gene
{
    private const float BONUS_SMALL_HIVE = 0.05f; // +5% при <11 носителях
    private const float BONUS_LARGE_HIVE = 0.03f;  // +3% при >=11 носителях
    private const int LARGE_HIVE_THRESHOLD = 11;
    private const int CHECK_INTERVAL = 250;

    private int cachedHiveMemberCount = 0;
    private int lastCheckTick = -1;

    public int CachedHiveMemberCount => cachedHiveMemberCount;

    public float LearningBonus
    {
        get
        {
            if (cachedHiveMemberCount <= 0) return 0f;

            // totalWithGene = сородичи + сам носитель
            int totalWithGene = cachedHiveMemberCount + 1;
            float bonusPerMember = totalWithGene < LARGE_HIVE_THRESHOLD
                ? BONUS_SMALL_HIVE
                : BONUS_LARGE_HIVE;

            return cachedHiveMemberCount * bonusPerMember;
        }
    }

    public override void Tick()
    {
        base.Tick();

        if (Find.TickManager.TicksGame - lastCheckTick < CHECK_INTERVAL)
            return;

        lastCheckTick = Find.TickManager.TicksGame;

        // Безфракционные не получают бонуса
        if (pawn.Map == null || pawn.Faction == null)
        {
            cachedHiveMemberCount = 0;
            return;
        }

        Faction myFaction = pawn.Faction;
        int count = 0;

        foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (p != pawn
                && !p.Dead
                && !p.Downed
                && p.Faction == myFaction
                && p.genes?.HasActiveGene(def) == true)
            {
                count++;
            }
        }

        cachedHiveMemberCount = count;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cachedHiveMemberCount, "cachedHiveMemberCount", 0);
        Scribe_Values.Look(ref lastCheckTick, "lastCheckTick", -1);
    }
}

    public class MapComponent_HiveMind : MapComponent
    {
        private const int CHECK_INTERVAL = 250;
        private int lastCheckTick = -1;

        private Dictionary<Faction, HashSet<Pawn>> hiveMembersCache
            = new Dictionary<Faction, HashSet<Pawn>>();

        public MapComponent_HiveMind(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Не тикаем если Biotech активен
            if (ModsConfig.BiotechActive)
                return;

            if (Find.TickManager.TicksGame - lastCheckTick < CHECK_INTERVAL)
                return;

            lastCheckTick = Find.TickManager.TicksGame;
            RebuildCache();
        }

        private void RebuildCache()
        {
            hiveMembersCache.Clear();

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p.Dead || p.Downed || p.Faction == null)
                    continue;

                if (!p.IsChangeling() && !p.IsChangedling())
                    continue;

                if (!hiveMembersCache.TryGetValue(p.Faction, out var list))
                {
                    list = new HashSet<Pawn>();
                    hiveMembersCache[p.Faction] = list;
                }

                list.Add(p);
            }
        }

        public int GetHiveMemberCount(Pawn pawn)
    {
        if (pawn.Faction == null)
            return 0;

        if (!hiveMembersCache.TryGetValue(pawn.Faction, out var set))
            return 0;

        return set.Contains(pawn) ? set.Count - 1 : 0;
    }
    }
}
