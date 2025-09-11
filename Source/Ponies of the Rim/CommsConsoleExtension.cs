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
        public int minGoodwill = 0;
        public bool isGoodwillCost = false;
        public float delayDaysMin = 0f;
        public float delayDaysMax = 0f;
    }

    public class QuestSettings
    {
        public string questButtonText;
        public string questResponseText;
        public string questScriptDef;
        public float cooldownDays = 0f;
        public int minGoodwill = 0;
        public bool isGoodwillCost = false;
        public float delayDaysMin = 0f;
        public float delayDaysMax = 0f;
    }
}