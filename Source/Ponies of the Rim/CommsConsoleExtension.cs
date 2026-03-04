using Verse;
using System.Collections.Generic;

namespace PoniesOfTheRim
{
    public class CommsConsoleExtension : DefModExtension
    {
        public List<TextSettings> texts;
        public List<IncidentSettings> incidents;
        public List<QuestSettings> quests;
    }

    public class TextSettings
    {
        public string questionText;
        public string answerText;
    }

    public class IncidentSettings
    {
        public string incidentButtonText;
        public string incidentResponseText;
        public string incidentDef;
        public float cooldownDays = 0f;
                                public int minGoodwill = int.MinValue;
                public int goodwillCost = 0;
        public float delayDaysMin = 0f;
        public float delayDaysMax = 0f;
    }

    public class QuestSettings
    {
        public string questButtonText;
        public string questResponseText;

                public string questScriptDef;

                public List<string> questScriptDefs;

        public float cooldownDays = 0f;
                        public int minGoodwill = int.MinValue;
                public int goodwillCost = 0;
        public float delayDaysMin = 0f;
        public float delayDaysMax = 0f;
        public List<string> AllQuestDefs
        {
            get
            {
                var result = new List<string>();
                if (!string.IsNullOrEmpty(questScriptDef))
                    result.Add(questScriptDef);
                if (questScriptDefs != null)
                {
                    foreach (var def in questScriptDefs)
                    {
                        if (!string.IsNullOrEmpty(def) && !result.Contains(def))
                            result.Add(def);
                    }
                }
                return result;
            }
        }

        public bool HasAnyQuest => !string.IsNullOrEmpty(questScriptDef) || (questScriptDefs != null && questScriptDefs.Count > 0);
    }
}