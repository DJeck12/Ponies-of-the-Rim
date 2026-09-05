using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PoniesOfTheRim
{
    public static class CommsConsolePatch
    {
        private static readonly System.Reflection.FieldInfo diaOptionTextField = AccessTools.Field(typeof(DiaOption), "text");
        public static void CommsConsolePostfix(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            DiaNode localResult = __result;
            CommsConsoleExtension conExt = faction.def.GetModExtension<CommsConsoleExtension>();
            if (conExt == null) return;

            string disconnectText = "(" + "Disconnect".Translate() + ")";
            int disconnectIndex = __result.options.FindIndex(o => GetOptionText(o) == disconnectText);

                        bool canNegotiate;
            if (StatDefOf.NegotiationAbility.Worker.IsDisabledFor(negotiator))
            {
                canNegotiate = false;
            }
            else
            {
                canNegotiate = negotiator.GetStatValue(StatDefOf.NegotiationAbility) > 0f;
            }

            AddTextOptions(__result, localResult, conExt, negotiator, faction, canNegotiate, ref disconnectIndex);
            AddIncidentOptions(__result, localResult, conExt, negotiator, faction, canNegotiate, ref disconnectIndex);
            AddQuestOptions(__result, localResult, conExt, negotiator, faction, canNegotiate, ref disconnectIndex);
        }

                                private static void AddTextOptions(DiaNode result, DiaNode localResult, CommsConsoleExtension conExt, Pawn negotiator, Faction faction, bool canNegotiate, ref int disconnectIndex)
        {
            if (conExt.texts == null) return;

            foreach (var textSet in conExt.texts)
            {
                if (string.IsNullOrEmpty(textSet.answerText) || string.IsNullOrEmpty(textSet.questionText))
                    continue;

                DiaOption historyOption = new DiaOption(TranslateWithContext(textSet.questionText, negotiator, faction));

                if (!canNegotiate)
                {
                    historyOption.disabled = true;
                    historyOption.disabledReason = "PonyComms_CannotSpeak".Translate();
                }
                else
                {
                    DiaNode textsNode = new DiaNode(TranslateWithContext(textSet.answerText, negotiator, faction));
                    DiaOption backOption = new DiaOption("OK".Translate())
                    {
                        linkLateBind = () => localResult
                    };
                    textsNode.options.Add(backOption);

                    historyOption.link = textsNode;
                    historyOption.resolveTree = false;
                }

                InsertOptionSafely(result, historyOption, ref disconnectIndex);
            }
        }

        private static void AddIncidentOptions(DiaNode result, DiaNode localResult, CommsConsoleExtension conExt, Pawn negotiator, Faction faction, bool canNegotiate, ref int disconnectIndex)
        {
            if (conExt.incidents == null) return;
            FactionIncidentCooldown cooldownComp = Find.World.GetComponent<FactionIncidentCooldown>();
            if (cooldownComp == null)
            {
                Log.ErrorOnce(
                    "[PoniesOfTheRim] FactionIncidentCooldown WorldComponent not found! " +
                    "This should be auto-registered by RimWorld. Check that the assembly is loaded correctly.",
                    "PoniesOfTheRim_NoCooldownComp".GetHashCode()
                );
                return;
            }

            foreach (var incident in conExt.incidents)
            {
                if (string.IsNullOrEmpty(incident.incidentButtonText) || string.IsNullOrEmpty(incident.incidentDef))
                    continue;

                IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(incident.incidentDef, false);
                if (incidentDef == null)
                {
                    Log.WarningOnce(
                        $"[PoniesOfTheRim] IncidentDef '{incident.incidentDef}' not found.",
                        incident.incidentDef.GetHashCode()
                    );
                    continue;
                }

                var cooldownKey = new CooldownKey(faction, incident.incidentDef);

                                bool canTrigger = true;
                string disableReason = null;

                CheckCanNegotiate(canNegotiate, ref canTrigger, ref disableReason);
                CheckGoodwillCondition(faction, incident.minGoodwill, incident.goodwillCost, ref canTrigger, ref disableReason);
                CheckCooldownCondition(cooldownComp, cooldownKey, incident.cooldownDays, ref canTrigger, ref disableReason);

                DiaOption incidentOption = new DiaOption(FormatButtonText(incident.incidentButtonText, incident.goodwillCost, negotiator, faction));

                if (!canTrigger)
                {
                    incidentOption.disabled = true;
                    incidentOption.disabledReason = disableReason;
                }
                else
                {
                    DiaNode incidentNode = new DiaNode(TranslateWithContext(incident.incidentResponseText, negotiator, faction));
                    DiaOption okOption = new DiaOption("OK".Translate())
                    {
                        linkLateBind = () => localResult
                    };
                    incidentNode.options.Add(okOption);

                                        var capturedIncident = incident;
                    var capturedDef = incidentDef;
                    var capturedKey = cooldownKey;

                    incidentOption.action = () =>
                    {
                        ExecuteIncident(capturedDef, capturedIncident, faction, cooldownComp, capturedKey);
                        DisableAfterUse(incidentOption, faction, capturedIncident.minGoodwill, capturedIncident.goodwillCost, cooldownComp, capturedKey, capturedIncident.cooldownDays);
                    };

                    incidentOption.link = incidentNode;
                    incidentOption.resolveTree = false;
                }

                InsertOptionSafely(result, incidentOption, ref disconnectIndex);
            }
        }

        private static void AddQuestOptions(DiaNode result, DiaNode localResult, CommsConsoleExtension conExt, Pawn negotiator, Faction faction, bool canNegotiate, ref int disconnectIndex)
        {
            if (conExt.quests == null) return;

            FactionIncidentCooldown cooldownComp = Find.World.GetComponent<FactionIncidentCooldown>();
            if (cooldownComp == null) return;

            foreach (var questSet in conExt.quests)
            {
                if (string.IsNullOrEmpty(questSet.questButtonText) || !questSet.HasAnyQuest)
                    continue;

                                List<QuestScriptDef> validQuests = new List<QuestScriptDef>();
                foreach (string defName in questSet.AllQuestDefs)
                {
                    QuestScriptDef qDef = DefDatabase<QuestScriptDef>.GetNamed(defName, false);
                    if (qDef != null)
                    {
                        validQuests.Add(qDef);
                    }
                    else
                    {
                        Log.WarningOnce(
                            $"[PoniesOfTheRim] QuestScriptDef '{defName}' not found.",
                            defName.GetHashCode()
                        );
                    }
                }

                if (validQuests.Count == 0) continue;

                                var cooldownKey = new CooldownKey(faction, "questpool_" + questSet.questButtonText);

                bool canTrigger = true;
                string disableReason = null;

                CheckCanNegotiate(canNegotiate, ref canTrigger, ref disableReason);
                CheckGoodwillCondition(faction, questSet.minGoodwill, questSet.goodwillCost, ref canTrigger, ref disableReason);
                CheckCooldownCondition(cooldownComp, cooldownKey, questSet.cooldownDays, ref canTrigger, ref disableReason);

                DiaOption questOption = new DiaOption(FormatButtonText(questSet.questButtonText, questSet.goodwillCost, negotiator, faction));

                if (!canTrigger)
                {
                    questOption.disabled = true;
                    questOption.disabledReason = disableReason;
                }
                else
                {
                    DiaNode questNode = new DiaNode(TranslateWithContext(questSet.questResponseText, negotiator, faction));
                    DiaOption okOption = new DiaOption("OK".Translate())
                    {
                        linkLateBind = () => localResult
                    };
                    questNode.options.Add(okOption);

                    var capturedQuestSet = questSet;
                    var capturedValidQuests = validQuests;
                    var capturedKey = cooldownKey;

                    questOption.action = () =>
                    {
                        ExecuteQuest(capturedValidQuests, capturedQuestSet, faction, cooldownComp, capturedKey);
                        DisableAfterUse(questOption, faction, capturedQuestSet.minGoodwill, capturedQuestSet.goodwillCost, cooldownComp, capturedKey, capturedQuestSet.cooldownDays);
                    };

                    questOption.link = questNode;
                    questOption.resolveTree = false;
                }

                InsertOptionSafely(result, questOption, ref disconnectIndex);
            }
        }

        private static void ExecuteIncident(IncidentDef incidentDef, IncidentSettings incident, Faction faction, FactionIncidentCooldown cooldownComp, CooldownKey cooldownKey)
        {
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, Find.CurrentMap);
            parms.faction = faction;
            parms.forced = true;

            int delayTicks = CalculateDelayTicks(incident.delayDaysMin, incident.delayDaysMax);

            if (delayTicks > 0)
            {
                FiringIncident firingInc = new FiringIncident(incidentDef, null, parms);
                QueuedIncident qi = new QueuedIncident(firingInc, Find.TickManager.TicksGame + delayTicks);
                Find.Storyteller.incidentQueue.Add(qi);
            }
            else
            {
                incidentDef.Worker.TryExecute(parms);
            }

            if (incident.cooldownDays > 0)
            {
                cooldownComp.lastTimes[cooldownKey] = Find.TickManager.TicksGame;
            }

            if (incident.goodwillCost > 0)
            {
                faction.TryAffectGoodwillWith(Faction.OfPlayer, -incident.goodwillCost);
            }
        }

        private static void ExecuteQuest(List<QuestScriptDef> validQuests, QuestSettings questSet, Faction faction, FactionIncidentCooldown cooldownComp, CooldownKey cooldownKey)
        {
                        QuestScriptDef chosenQuest = validQuests.RandomElement();

            int delayTicks = CalculateDelayTicks(questSet.delayDaysMin, questSet.delayDaysMax);

            if (delayTicks > 0)
            {
                                DelayedQuest delayed = new DelayedQuest
                {
                    questDefName = chosenQuest.defName,
                    faction = faction,
                    fireTick = Find.TickManager.TicksGame + delayTicks
                };
                cooldownComp.delayedQuests.Add(delayed);
            }
            else
            {
                GenerateQuestWithFaction(chosenQuest, faction);
            }

            if (questSet.cooldownDays > 0)
            {
                cooldownComp.lastTimes[cooldownKey] = Find.TickManager.TicksGame;
            }

            if (questSet.goodwillCost > 0)
            {
                faction.TryAffectGoodwillWith(Faction.OfPlayer, -questSet.goodwillCost);
            }
        }

        public static void GenerateQuestWithFaction(QuestScriptDef questScriptDef, Faction faction)
        {
            try
            {
                CommsQuestContext.TargetFaction = faction;

                Slate slate = new Slate();

                                slate.Set("faction", faction);
                slate.Set("commsFaction", faction);

                if (faction.leader != null)
                {
                    slate.Set("asker", faction.leader);
                }

                                Settlement settlement = FindBestSettlementForFaction(faction);
                if (settlement != null)
                {
                    slate.Set("settlement", settlement);
                }

                Quest quest = QuestGen.Generate(questScriptDef, slate);
                Find.QuestManager.Add(quest);
                QuestUtility.SendLetterQuestAvailable(quest);
            }
            catch (Exception ex)
            {
                Log.Error($"[PoniesOfTheRim] Failed to generate quest '{questScriptDef.defName}' for faction '{faction?.Name}': {ex}");
            }
            finally
            {
                CommsQuestContext.TargetFaction = null;
            }
        }

        private static Settlement FindBestSettlementForFaction(Faction faction)
        {
            Map playerMap = Find.CurrentMap;
            if (playerMap == null) return null;

            int playerTile = playerMap.Tile;

            return Find.WorldObjects.Settlements
                .Where(s => s.Faction == faction && !s.Faction.IsPlayer)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(playerTile, s.Tile))
                .FirstOrDefault();
        }

        private static string GetOptionText(DiaOption option)
        {
            return (string)diaOptionTextField.GetValue(option);
        }

        private static string FormatButtonText(string key, int goodwillCost, Pawn negotiator, Faction faction)
        {
            TaggedString text = TranslateWithContext(key, negotiator, faction);
            if (goodwillCost > 0)
            {
                text += " (" + "PonyComms_GoodwillCostLabel".Translate(goodwillCost) + ")";
            }
            return text;
        }

                                                                                        
        private static TaggedString TranslateWithContext(string key, Pawn negotiator, Faction faction)
        {
            if (string.IsNullOrEmpty(key)) return "";

            Pawn leader = faction?.leader;
            string leaderName = leader?.LabelShort ?? faction?.Name ?? "???";

            return key.Translate(
                leaderName,
                negotiator.Named("NEGOTIATOR"),
                leader != null ? leader.Named("LEADER") : (NamedArgument)leaderName,
                faction.Named("FACTION")
            );
        }

        private static void CheckCanNegotiate(bool canNegotiate, ref bool canTrigger, ref string disableReason)
        {
            if (!canTrigger) return;

            if (!canNegotiate)
            {
                canTrigger = false;
                disableReason = "PonyComms_CannotSpeak".Translate();
            }
        }

        private static void CheckGoodwillCondition(Faction faction, int minGoodwill, int goodwillCost, ref bool canTrigger, ref string disableReason)
        {
            if (!canTrigger) return;

                        if (minGoodwill != int.MinValue && faction.PlayerGoodwill < minGoodwill)
            {
                canTrigger = false;
                disableReason = "PonyComms_MinGoodwillRequired".Translate(minGoodwill, faction.PlayerGoodwill);
            }
        }

        private static void CheckCooldownCondition(FactionIncidentCooldown cooldownComp, CooldownKey cooldownKey, float cooldownDays, ref bool canTrigger, ref string disableReason)
        {
            if (!canTrigger || cooldownDays <= 0) return;

            if (cooldownComp.lastTimes.TryGetValue(cooldownKey, out float lastTime))
            {
                float ticksToNext = lastTime + (cooldownDays * 60000f) - Find.TickManager.TicksGame;
                if (ticksToNext > 0)
                {
                    canTrigger = false;
                    disableReason = "PonyComms_CooldownRemaining".Translate((ticksToNext / 60000f).ToString("F1"));
                }
            }
        }

        private static void DisableAfterUse(DiaOption option, Faction faction, int minGoodwill, int goodwillCost, FactionIncidentCooldown cooldownComp, CooldownKey cooldownKey, float cooldownDays)
        {
            option.disabled = true;

            string newReason = null;
            if (minGoodwill != int.MinValue && faction.PlayerGoodwill < minGoodwill)
            {
                newReason = "PonyComms_MinGoodwillRequired".Translate(minGoodwill, faction.PlayerGoodwill);
            }
            else if (cooldownDays > 0 && cooldownComp.lastTimes.TryGetValue(cooldownKey, out float lastTime))
            {
                float ticksToNext = lastTime + (cooldownDays * 60000f) - Find.TickManager.TicksGame;
                if (ticksToNext > 0)
                {
                    newReason = "PonyComms_CooldownRemaining".Translate((ticksToNext / 60000f).ToString("F1"));
                }
            }

            option.disabledReason = newReason ?? (string)"PonyComms_RequestDone".Translate();
        }

        private static int CalculateDelayTicks(float delayDaysMin, float delayDaysMax)
        {
            if (delayDaysMin <= 0 && delayDaysMax <= 0) return 0;
            return Rand.RangeInclusive((int)(delayDaysMin * 60000f), (int)(delayDaysMax * 60000f));
        }

        private static void InsertOptionSafely(DiaNode result, DiaOption newOption, ref int disconnectIndex)
        {
                        string newText = GetOptionText(newOption);
            if (result.options.Any(o => GetOptionText(o) == newText))
                return;

            if (disconnectIndex >= 0)
            {
                result.options.Insert(disconnectIndex, newOption);
                disconnectIndex++;
            }
            else
            {
                result.options.Add(newOption);
            }
        }
    }
}