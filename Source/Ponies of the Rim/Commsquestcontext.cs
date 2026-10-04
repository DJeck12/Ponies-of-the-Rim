using System;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace PoniesOfTheRim
{
    public static class CommsQuestContext
    {
        [ThreadStatic]
        public static Faction TargetFaction;
    }

    public static class QuestFactionPatches
    {
        public static void IsGoodFaction_Postfix(ref bool __result, Faction faction)
        {
            Faction target = CommsQuestContext.TargetFaction;
            if (target != null)
            {
                __result = faction == target;
            }
        }

        public static bool GetNearbySettlement_RunInt_Prefix(QuestNode_GetNearbySettlement __instance)
        {
            Faction target = CommsQuestContext.TargetFaction;
            if (target == null)
            {
                return true;
            }
            Settlement settlement = FindBestSettlement(target);
            if (settlement == null)
            {
                PonyLog.WarnOnce(
                    "QuestFactionPatches.NoSettlement|" + target.Name,
                    "Квесты: для фракции '" + target.Name + "' не найдено поселение — используется ванильная логика.");
                return true;
            }

            Slate slate = QuestGen.slate;
            StoreSettlement(__instance, slate, settlement);

            string storeCanCaravanAs = __instance.storeCanCaravanAs.GetValue(slate);
            if (!storeCanCaravanAs.NullOrEmpty())
            {
                Map map;
                slate.TryGet<Map>("map", out map);
                bool canCaravan = map != null
                    && settlement.Tile.Valid && map.Tile.Valid
                    && settlement.Tile.Layer == map.Tile.Layer
                    && settlement.Tile.LayerDef.SurfaceTiles;
                slate.Set(storeCanCaravanAs, canCaravan);
            }
            return false;
        }

        public static bool GetNearbySettlement_TestRunInt_Prefix(ref bool __result, QuestNode_GetNearbySettlement __instance, Slate slate)
        {
            Faction target = CommsQuestContext.TargetFaction;
            if (target == null)
            {
                return true;
            }
            Settlement settlement = FindBestSettlement(target);
            if (settlement == null)
            {
                __result = false;
                return false;
            }
            StoreSettlement(__instance, slate, settlement);
            __result = true;
            return false;
        }

        private static void StoreSettlement(QuestNode_GetNearbySettlement node, Slate slate, Settlement settlement)
        {
            slate.Set(node.storeAs.GetValue(slate) ?? "settlement", settlement);

            string storeFactionAs = node.storeFactionAs.GetValue(slate);
            if (!storeFactionAs.NullOrEmpty())
            {
                slate.Set(storeFactionAs, settlement.Faction);
            }

            string storeFactionLeaderAs = node.storeFactionLeaderAs.GetValue(slate);
            if (!storeFactionLeaderAs.NullOrEmpty() && settlement.Faction.leader != null)
            {
                slate.Set(storeFactionLeaderAs, settlement.Faction.leader);
            }
        }

        private static Settlement FindBestSettlement(Faction faction)
        {
            Map playerMap = Find.CurrentMap;
            if (playerMap == null)
            {
                return null;
            }
            PlanetTile playerTile = playerMap.Tile;
            return Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && !s.Faction.IsPlayer)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(playerTile, s.Tile))
                .FirstOrDefault();
        }
    }
}