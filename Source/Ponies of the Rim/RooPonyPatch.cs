using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;


namespace PoniesOfTheRim
{
[StaticConstructorOnStartup]
    public static class MinotaurCompatibilityPatch
    {
        private static readonly string[] targetModPackageIds = new string[]
        {
            "tug.Minotaur",
            "tug.Minotaur.Expanded",
            "V.Rooboid.Faun",
            "tug.Satyr",
            "tug.SatyrFaun.Expanded"
        };

        static MinotaurCompatibilityPatch()
        {
            // Проверяем, загружен ли хотя бы один из целевых модов
            if (!IsAnyTargetModActive())
            {
                Log.Message("[Pony Compat] No target mods detected, skipping Minotaur compatibility patch");
                return;
            }

            var harmony = new Harmony("poniesoftherim.minotaur.compat");
            
            // Патчим PawnRenderNode_Fur.GraphicFor с повышенным приоритетом
            var original = AccessTools.Method(typeof(PawnRenderNode_Fur), "GraphicFor");
            if (original != null)
            {
                var prefix = new HarmonyMethod(typeof(MinotaurCompatibilityPatch), nameof(DisableFurForPonyMinotaur));
                prefix.priority = Priority.HigherThanNormal;
                
                harmony.Patch(original, prefix: prefix);
                
                string activeMods = string.Join(", ", GetActiveTargetMods());
                Log.Message($"[Pony Compat] Successfully patched for compatibility with: {activeMods}");
            }
        }

        // Проверяет, активен ли хотя бы один из целевых модов
        private static bool IsAnyTargetModActive()
        {
            return targetModPackageIds.Any(packageId => 
                ModsConfig.IsActive(packageId));
        }

        // Возвращает список активных целевых модов
        private static string[] GetActiveTargetMods()
        {
            return targetModPackageIds
                .Where(packageId => ModsConfig.IsActive(packageId))
                .ToArray();
        }

        // Отключаем FurDef для пони с геном RBM_UnguligradeLegs
        public static bool DisableFurForPonyMinotaur(Pawn pawn, ref Graphic __result)
        {
            if (pawn?.story == null || !pawn.IsPony())
                return true;

            // Проверяем наличие гена RBM_UnguligradeLegs
            GeneDef unguligradeGene = DefDatabase<GeneDef>.GetNamed("RBM_UnguligradeLegs", false);
            if (unguligradeGene == null)
                return true;

            if (pawn.genes?.HasActiveGene(unguligradeGene) != true)
                return true;

            // У пони есть ген RBM_UnguligradeLegs - ОТКЛЮЧАЕМ FurDef
            // Временно обнуляем furDef, чтобы моды Roo ничего не делали
            FurDef originalFurDef = pawn.story.furDef;
            pawn.story.furDef = null;
            
            // Возвращаем null чтобы использовалась стандартная графика тела
            __result = null;
            
            // Восстанавливаем furDef после возврата
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (pawn?.story != null)
                {
                    pawn.story.furDef = originalFurDef;
                }
            });
            
            // Блокируем все остальные патчи (включая моды Roo)
            return false;
        }
    }
}
