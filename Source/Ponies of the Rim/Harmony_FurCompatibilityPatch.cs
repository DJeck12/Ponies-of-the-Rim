using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace PoniesOfTheRim
{
    public static class FurCompatibilityPatch
    {
        public const string PonyFurFallbackPath = "Transparent/Transparent";

        private static readonly string[] PonyBodyTypeDefNames = { "Pony", "PonyChild", "PonyBaby" };

        public static void EnsurePonyFurPaths()
        {
            List<BodyTypeDef> ponyBodyTypes = new List<BodyTypeDef>();
            foreach (string defName in PonyBodyTypeDefNames)
            {
                BodyTypeDef bt = DefDatabase<BodyTypeDef>.GetNamedSilentFail(defName);
                if (bt != null)
                    ponyBodyTypes.Add(bt);
            }
            if (ponyBodyTypes.Count == 0)
            {
                Log.Warning("[PoniesOfTheRim] Типы тел пони не найдены — патч совместимости шерсти пропущен.");
                return;
            }

            Dictionary<string, List<string>> byMod = new Dictionary<string, List<string>>();

            foreach (FurDef fur in DefDatabase<FurDef>.AllDefsListForReading)
            {
                if (fur.bodyTypeGraphicPaths == null)
                    fur.bodyTypeGraphicPaths = new List<BodyTypeGraphicData>();

                bool addedAny = false;
                foreach (BodyTypeDef bt in ponyBodyTypes)
                {
                    if (fur.bodyTypeGraphicPaths.Any(d => d?.bodyType == bt))
                        continue;

                    fur.bodyTypeGraphicPaths.Add(new BodyTypeGraphicData
                    {
                        bodyType = bt,
                        texturePath = PonyFurFallbackPath
                    });
                    addedAny = true;
                }

                if (addedAny)
                {
                    string modName = fur.modContentPack?.Name ?? "Unknown";
                    if (!byMod.TryGetValue(modName, out List<string> list))
                    {
                        list = new List<string>();
                        byMod[modName] = list;
                    }
                    list.Add(fur.defName);
                }
            }

            if (byMod.Count == 0)
            {
                Log.Message("[PoniesOfTheRim] Совместимость шерсти: все FurDef уже имеют текстуры для тел пони.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[PoniesOfTheRim] Совместимость шерсти: заглушка добавлена в bodyTypeGraphicPaths для FurDef:");
            foreach (KeyValuePair<string, List<string>> kv in byMod.OrderBy(k => k.Key))
            {
                sb.AppendLine();
                sb.Append($"  - {kv.Key}: {string.Join(", ", kv.Value)}");
            }
            Log.Message(sb.ToString());
        }
    }
}