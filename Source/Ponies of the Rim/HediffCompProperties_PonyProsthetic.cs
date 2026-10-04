using System.Threading;
using Verse;

namespace PoniesOfTheRim
{
    public class HediffCompProperties_PonyProsthetic : HediffCompProperties
    {
        public string prefix;

        public string inBrackets;

        public string descriptionExtra;

        public string tipStringExtra;

        [NoTranslate]
        public string prefixKey = "Pony_ProstheticPrefix";

        [NoTranslate]
        public string inBracketsKey = "Pony_ProstheticInBrackets";

        [NoTranslate]
        public string descriptionExtraKey;

        [NoTranslate]
        public string tipStringExtraKey = "Pony_ProstheticTipExtra";

        [Unsaved(false)]
        private bool _resolved;

        [Unsaved(false)]
        private string _prefixResolved;

        [Unsaved(false)]
        private string _inBracketsResolved;

        [Unsaved(false)]
        private string _descriptionExtraResolved;

        [Unsaved(false)]
        private string _tipStringExtraResolved;

        public HediffCompProperties_PonyProsthetic()
        {
            compClass = typeof(HediffComp_PonyProsthetic);
        }

        public string ResolvedPrefix
        {
            get
            {
                EnsureResolved();
                return _prefixResolved;
            }
        }

        public string ResolvedInBrackets
        {
            get
            {
                EnsureResolved();
                return _inBracketsResolved;
            }
        }

        public string ResolvedDescriptionExtra
        {
            get
            {
                EnsureResolved();
                return _descriptionExtraResolved;
            }
        }

        public string ResolvedTipStringExtra
        {
            get
            {
                EnsureResolved();
                return _tipStringExtraResolved;
            }
        }

        private void EnsureResolved()
        {
            LoadedLanguage language = LanguageDatabase.activeLanguage;
            if (Volatile.Read(ref _resolved) || language == null)
            {
                return;
            }
            _prefixResolved = Resolve(prefixKey, prefix);
            _inBracketsResolved = Resolve(inBracketsKey, inBrackets);
            _descriptionExtraResolved = Resolve(descriptionExtraKey, descriptionExtra);
            _tipStringExtraResolved = Resolve(tipStringExtraKey, tipStringExtra);
            Volatile.Write(ref _resolved, true);
            PonyLog.TraceOnce("PonyProsthetic.Resolved", "Протезы: подписи для языка '" + language.folderName + "' — префикс: " + Show(_prefixResolved) + ", в скобках: " + Show(_inBracketsResolved) + ", подсказка: " + Show(_tipStringExtraResolved) + ".");
        }

        private static string Resolve(string key, string literal)
        {
            if (!key.NullOrEmpty())
            {
                if (key.CanTranslate() || LanguageDatabase.defaultLanguage?.HaveTextForKey(key) == true)
                {
                    string text = key.Translate();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return null;
                    }
                    return text;
                }
                PonyLog.WarnOnce("PonyProsthetic.MissingKey|" + key, "Протезы: ключ перевода '" + key + "' не найден ни в текущем языке, ни в английском — " + (literal.NullOrEmpty() ? "эта часть подписи протеза не выводится." : "выводится строка из XML."));
            }
            if (literal.NullOrEmpty())
            {
                return null;
            }
            return literal;
        }

        private static string Show(string text)
        {
            if (text == null)
            {
                return "нет";
            }
            return "«" + text + "»";
        }
    }
}