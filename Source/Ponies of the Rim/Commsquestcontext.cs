using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
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
        private static bool patchApplied = false;
        private static FieldInfo fi_GetFaction_storeAs;
        private static FieldInfo fi_Settlement_storeAs;
        private static FieldInfo fi_Settlement_storeFactionLeaderAs;
        private static FieldInfo fi_Settlement_storeCanCaravanAs;

        public static void Apply(Harmony harmony)
        {
            if (patchApplied) return;

            try
            {
                MethodInfo isGoodFaction = AccessTools.Method(
                typeof(QuestNode_GetFaction), "IsGoodFaction");

                if (isGoodFaction != null)
                {
                    harmony.Patch(
                        isGoodFaction,
                        postfix: new HarmonyMethod(typeof(QuestFactionPatches),
                            nameof(IsGoodFaction_Postfix))
                    );
                    PonyLog.Trace("Квесты: ✓ QuestNode_GetFaction.IsGoodFaction.");
                }
                else
                {
                    PonyLog.Warn("Квесты: не найден QuestNode_GetFaction.IsGoodFaction — патч пропущен.");
                }

                Type settlementNodeType = typeof(QuestNode_GetNearbySettlement);
                MethodInfo runInt = AccessTools.Method(settlementNodeType, "RunInt");

                if (runInt != null)
                {
                    harmony.Patch(
                        runInt,
                        prefix: new HarmonyMethod(typeof(QuestFactionPatches),
                            nameof(GetNearbySettlement_RunInt_Prefix))
                    );
                    PonyLog.Trace("Квесты: ✓ QuestNode_GetNearbySettlement.RunInt.");
                }
                else
                {
                    PonyLog.Warn("Квесты: не найден QuestNode_GetNearbySettlement.RunInt — патч пропущен.");
                }

                MethodInfo testRunInt = AccessTools.Method(settlementNodeType, "TestRunInt");
                if (testRunInt != null)
                {
                    harmony.Patch(
                        testRunInt,
                        prefix: new HarmonyMethod(typeof(QuestFactionPatches),
                            nameof(GetNearbySettlement_TestRunInt_Prefix))
                    );
                    PonyLog.Trace("Квесты: ✓ QuestNode_GetNearbySettlement.TestRunInt.");
                }
                else
                {
                    PonyLog.Warn("Квесты: не найден QuestNode_GetNearbySettlement.TestRunInt — патч пропущен.");
                }

                fi_GetFaction_storeAs = AccessTools.Field(typeof(QuestNode_GetFaction), "storeAs");
                fi_Settlement_storeAs = AccessTools.Field(settlementNodeType, "storeAs");
                fi_Settlement_storeFactionLeaderAs = AccessTools.Field(settlementNodeType, "storeFactionLeaderAs");
                fi_Settlement_storeCanCaravanAs = AccessTools.Field(settlementNodeType, "storeCanCaravanAs");

                patchApplied = true;
            }
            catch (Exception ex)
            {
                PonyLog.Error($"Квесты: не удалось применить патчи квестовых фракций:\n{ex}");
            }
        }


        public static void IsGoodFaction_Postfix(ref bool __result, Faction faction, Slate slate)
        {
            if (CommsQuestContext.TargetFaction == null) return;

            __result = (faction == CommsQuestContext.TargetFaction);
        }


        public static bool GetNearbySettlement_RunInt_Prefix(QuestNode __instance)
        {
            if (CommsQuestContext.TargetFaction == null)
                return true;

            Faction target = CommsQuestContext.TargetFaction;

            Settlement settlement = FindBestSettlement(target);
            if (settlement == null)
            {
                PonyLog.WarnOnce(
                    "QuestFactionPatches.NoSettlement|" + target.Name,
                    "Квесты: для фракции '" + target.Name + "' не найдено поселение — используется ванильная логика.");
                return true;
            }

            Slate slate = QuestGen.slate;
            string storeAs = ReadSlateRef(fi_Settlement_storeAs, __instance, slate) ?? "settlement";
            string storeLeaderAs = ReadSlateRef(fi_Settlement_storeFactionLeaderAs, __instance, slate);
            string storeCanCaravanAs = ReadSlateRef(fi_Settlement_storeCanCaravanAs, __instance, slate);
            slate.Set(storeAs, settlement);

            if (!string.IsNullOrEmpty(storeLeaderAs) && target.leader != null)
            {
                slate.Set(storeLeaderAs, target.leader);
            }

            if (!string.IsNullOrEmpty(storeCanCaravanAs))
            {
                Map map = null;
                slate.TryGet<Map>("map", out map);
                bool canCaravan = map != null && map.Tile >= 0;
                slate.Set(storeCanCaravanAs, canCaravan);
            }

            return false;
        }

        public static bool GetNearbySettlement_TestRunInt_Prefix(ref bool __result, QuestNode __instance, Slate slate)
        {
            if (CommsQuestContext.TargetFaction == null)
                return true;

            Faction target = CommsQuestContext.TargetFaction;

            Settlement settlement = FindBestSettlement(target);
            if (settlement == null)
            {
                __result = false;
                return false;
            }

            string storeAs = ReadSlateRef(fi_Settlement_storeAs, __instance, slate) ?? "settlement";
            string storeLeaderAs = ReadSlateRef(fi_Settlement_storeFactionLeaderAs, __instance, slate);
            string storeCanCaravanAs = ReadSlateRef(fi_Settlement_storeCanCaravanAs, __instance, slate);

            slate.Set(storeAs, settlement);

            if (!string.IsNullOrEmpty(storeLeaderAs) && target.leader != null)
                slate.Set(storeLeaderAs, target.leader);

            if (!string.IsNullOrEmpty(storeCanCaravanAs))
            {
                Map map = null;
                slate.TryGet<Map>("map", out map);
                slate.Set(storeCanCaravanAs, map != null && map.Tile >= 0);
            }

            __result = true;
            return false;
        }


        private static Settlement FindBestSettlement(Faction faction)
        {
            Map playerMap = Find.CurrentMap;
            if (playerMap == null) return null;

            int playerTile = playerMap.Tile;

            return Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && !s.Faction.IsPlayer)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(playerTile, s.Tile))
                .FirstOrDefault();
        }

        private static string ReadSlateRef(FieldInfo fieldInfo, object instance, Slate slate)
        {
            if (fieldInfo == null || instance == null) return null;

            try
            {
                object slateRef = fieldInfo.GetValue(instance);
                if (slateRef == null) return null;

                Type type = slateRef.GetType();
                MethodInfo getValueMethod = type.GetMethod("GetValue", new Type[] { typeof(Slate) });
                if (getValueMethod != null)
                {
                    return getValueMethod.Invoke(slateRef, new object[] { slate }) as string;
                }
            }
            catch { }

            return null;
        }
    }
}