using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace PoniesOfTheRim
{


    [StaticConstructorOnStartup]
    public static class CommsConsolePatch
    {
        static CommsConsolePatch()
        {
            new Harmony("Rimworld.Pony.PoniesOfTheRim").Patch(AccessTools.Method(typeof(FactionDialogMaker), "FactionDialogFor"), null, new HarmonyMethod(typeof(CommsConsolePatch).GetMethod("CommsConsolePostfix")));

        }
    
    [HarmonyPostfix]
        public static void CommsConsolePostfix(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            DiaNode localResult = __result;
            CommsConsoleExtension conExt = faction.def.GetModExtension<CommsConsoleExtension>();
            if (conExt == null) return;

            var textField = AccessTools.Field(typeof(DiaOption), "text");
            string disconnectText = "(Disconnect)".Translate();
            int disconnectIndex = __result.options.FindIndex(o => (string)textField.GetValue(o) == disconnectText);

            foreach (var textSet in conExt.texts ?? [])
            {
                if (!string.IsNullOrEmpty(textSet.answerText) && !string.IsNullOrEmpty(textSet.questionText))
                {
                    DiaNode textsNode = new(textSet.answerText.Translate()); 
                    DiaOption backOption = new("OK".Translate())
                    {
                        linkLateBind = () => localResult 
                    };
                    textsNode.options.Add(backOption);

                    DiaOption historyOption = new(textSet.questionText.Translate())
                    {
                        link = textsNode, 
                        resolveTree = false 
                    };

                    string optionText = (string)textField.GetValue(historyOption);

                    if (!__result.options.Any(o => (string)textField.GetValue(o) == optionText))
                    {
                        if (disconnectIndex > 0) 
                        {
                            __result.options.Insert(disconnectIndex, historyOption);
                            disconnectIndex++;
                        }
                        else
                        {
                            __result.options.Add(historyOption); 
                        }
                    }
                }
            }

            FactionIncidentCooldown cooldownComp = Find.World.GetComponent<FactionIncidentCooldown>();
            if (cooldownComp == null)
            {
                cooldownComp = new FactionIncidentCooldown(Find.World);
                Find.World.components.Add(cooldownComp);
            }

            foreach (var incident in conExt.incidents ?? [])
            {
                if (string.IsNullOrEmpty(incident.incidentButtonText) || string.IsNullOrEmpty(incident.incidentDef)) continue;

                IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamed(incident.incidentDef, false);
                if (incidentDef == null) continue;

                var cooldownKey = new ValueTuple<Faction, string>(faction, incident.incidentDef);

                bool canTrigger = true;
                string disableReason = null;

                if (incident.minGoodwill > 0)
                {
                    if (faction.PlayerGoodwill < incident.minGoodwill)
                    {
                        canTrigger = false;
                        disableReason = incident.isGoodwillCost 
                            ? $"У Вас недостаточно отношений с этой фракцией, требуется: {incident.minGoodwill} (текущее: {faction.PlayerGoodwill}).".Translate()
                            : $"Требуется минимум {incident.minGoodwill} отношений (текущее: {faction.PlayerGoodwill}).".Translate();
                    }
                }

                if (canTrigger && incident.cooldownDays > 0)
                {
                    if (cooldownComp.lastTimes.TryGetValue(cooldownKey, out float lastTime))
                    {
                        float ticksToNext = lastTime + (incident.cooldownDays * 60000f) - Find.TickManager.TicksGame;
                        if (ticksToNext > 0)
                        {
                            canTrigger = false;
                            disableReason = $"Следующая возможность через: {(ticksToNext / 60000f).ToString("F1")} дней.".Translate();
                        }
                    }
                }

                DiaOption incidentOption = new(incident.incidentButtonText.Translate());

                if (!canTrigger)
                {
                    incidentOption.disabled = true;
                    incidentOption.disabledReason = disableReason;
                }
                else
                {
                    DiaNode incidentNode = new(incident.incidentResponseText.Translate());
                    DiaOption okOption = new("OK".Translate())
                    {
                        linkLateBind = () => localResult
                    };
                    incidentNode.options.Add(okOption);

                    incidentOption.action = () =>
                    {
                        IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, Find.CurrentMap);
                        parms.faction = faction;
                        parms.forced = true;

                        int delayTicks = 0;
                        if (incident.delayDaysMin > 0 || incident.delayDaysMax > 0)
                        {
                            delayTicks = Rand.RangeInclusive((int)(incident.delayDaysMin * 60000f), (int)(incident.delayDaysMax * 60000f));
                        }

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

                        if (incident.isGoodwillCost && incident.minGoodwill > 0)
                        {
                            faction.TryAffectGoodwillWith(Faction.OfPlayer, -incident.minGoodwill);
                        }

                        incidentOption.disabled = true;
                        string newDisableReason = null;
                        if (incident.minGoodwill > 0 && faction.PlayerGoodwill < incident.minGoodwill)
                        {
                            newDisableReason = incident.isGoodwillCost 
                                ? $"У Вас недостаточно отношений с этой фракцией, требуется: {incident.minGoodwill} (текущее: {faction.PlayerGoodwill}).".Translate()
                                : $"Требуется минимум {incident.minGoodwill} отношений (текущее: {faction.PlayerGoodwill}).".Translate();
                        }
                        else if (incident.cooldownDays > 0)
                        {
                            float ticksToNext = cooldownComp.lastTimes[cooldownKey] + (incident.cooldownDays * 60000f) - Find.TickManager.TicksGame;
                            if (ticksToNext > 0)
                            {
                                newDisableReason = $"Следующая возможность через: {(ticksToNext / 60000f).ToString("F1")} дней.".Translate();
                            }
                        }
                        incidentOption.disabledReason = newDisableReason ?? "Запрос выполнен. Повторите позже.".Translate();
                    };

                    incidentOption.link = incidentNode;
                    incidentOption.resolveTree = false;
                }

                string optionText = (string)textField.GetValue(incidentOption);
                if (!__result.options.Any(o => (string)textField.GetValue(o) == optionText))
                {
                    if (disconnectIndex > 0)
                    {
                        __result.options.Insert(disconnectIndex, incidentOption);
                        disconnectIndex++;
                    }
                    else
                    {
                        __result.options.Add(incidentOption);
                    }
                }
            }

            foreach (var questSet in conExt.quests ?? [])
            {
                if (string.IsNullOrEmpty(questSet.questButtonText) || string.IsNullOrEmpty(questSet.questScriptDef)) continue;

                QuestScriptDef questScriptDef = DefDatabase<QuestScriptDef>.GetNamed(questSet.questScriptDef, false);
                if (questScriptDef == null) continue;

                var cooldownKey = new ValueTuple<Faction, string>(faction, questSet.questScriptDef);

                bool canTrigger = true;
                string disableReason = null;

                if (questSet.minGoodwill > 0)
                {
                    if (faction.PlayerGoodwill < questSet.minGoodwill)
                    {
                        canTrigger = false;
                        disableReason = questSet.isGoodwillCost 
                            ? $"У Вас недостаточно отношений с этой фракцией, требуется: {questSet.minGoodwill} (текущее: {faction.PlayerGoodwill}).".Translate()
                            : $"Требуется минимум {questSet.minGoodwill} отношений (текущее: {faction.PlayerGoodwill}).".Translate();
                    }
                }

                if (canTrigger && questSet.cooldownDays > 0)
                {
                    if (cooldownComp.lastTimes.TryGetValue(cooldownKey, out float lastTime))
                    {
                        float ticksToNext = lastTime + (questSet.cooldownDays * 60000f) - Find.TickManager.TicksGame;
                        if (ticksToNext > 0)
                        {
                            canTrigger = false;
                            disableReason = $"Следующая возможность через: {(ticksToNext / 60000f).ToString("F1")} дней.".Translate();
                        }
                    }
                }

                DiaOption questOption = new(questSet.questButtonText.Translate());

                if (!canTrigger)
                {
                    questOption.disabled = true;
                    questOption.disabledReason = disableReason;
                }
                else
                {
                    DiaNode questNode = new DiaNode(questSet.questResponseText.Translate());
                    DiaOption okOption = new("OK".Translate())
                    {
                        linkLateBind = () => localResult
                    };
                    questNode.options.Add(okOption);

                    questOption.action = () =>
                    {
                        int delayTicks = 0;
                        if (questSet.delayDaysMin > 0 || questSet.delayDaysMax > 0)
                        {
                            delayTicks = Rand.RangeInclusive((int)(questSet.delayDaysMin * 60000f), (int)(questSet.delayDaysMax * 60000f));
                        }

                        if (delayTicks > 0)
                        {
                            DelayedQuest delayed = new()
                            {
                                questDefName = questSet.questScriptDef,
                                faction = faction,
                                fireTick = Find.TickManager.TicksGame + delayTicks
                            };
                            cooldownComp.delayedQuests.Add(delayed);
                        }
                        else
                        {
                            Slate slate = new();
                            slate.Set("asker", faction);
                            Quest quest = QuestGen.Generate(questScriptDef, slate);
                            Find.QuestManager.Add(quest);
                        }

                        if (questSet.cooldownDays > 0)
                        {
                            cooldownComp.lastTimes[cooldownKey] = Find.TickManager.TicksGame;
                        }

                        if (questSet.isGoodwillCost && questSet.minGoodwill > 0)
                        {
                            faction.TryAffectGoodwillWith(Faction.OfPlayer, -questSet.minGoodwill);
                        }

                        questOption.disabled = true;
                        string newDisableReason = null;
                        if (questSet.minGoodwill > 0 && faction.PlayerGoodwill < questSet.minGoodwill)
                        {
                            newDisableReason = questSet.isGoodwillCost 
                                ? $"У Вас недостаточно отношений с этой фракцией, требуется: {questSet.minGoodwill} (текущее: {faction.PlayerGoodwill}).".Translate()
                                : $"Требуется минимум {questSet.minGoodwill} отношений (текущее: {faction.PlayerGoodwill}).".Translate();
                        }
                        else if (questSet.cooldownDays > 0)
                        {
                            float ticksToNext = cooldownComp.lastTimes[cooldownKey] + (questSet.cooldownDays * 60000f) - Find.TickManager.TicksGame;
                            if (ticksToNext > 0)
                            {
                                newDisableReason = $"Следующая возможность через: {(ticksToNext / 60000f).ToString("F1")} дней.".Translate();
                            }
                        }
                        questOption.disabledReason = newDisableReason ?? "Запрос выполнен. Повторите позже.".Translate();
                    };

                    questOption.link = questNode;
                    questOption.resolveTree = false;
                }

                string optionText = (string)textField.GetValue(questOption);
                if (!__result.options.Any(o => (string)textField.GetValue(o) == optionText))
                {
                    if (disconnectIndex > 0)
                    {
                        __result.options.Insert(disconnectIndex, questOption);
                        disconnectIndex++;
                    }
                    else
                    {
                        __result.options.Add(questOption);
                    }
                }
            }
        }
    }
}