using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public static class AbilityToggleEnforcer
    {
        private static Dictionary<string, List<string>> _grantToRacesCache;

        public static void Register(Harmony harmony)
        {
            BuildGrantToRacesCache();

            harmony.Patch(
                AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
                postfix: new HarmonyMethod(
                    typeof(AbilityToggleEnforcer),
                    nameof(SpawnSetup_Postfix))
            );

            harmony.Patch(
                AccessTools.Method(typeof(Game), nameof(Game.FinalizeInit)),
                postfix: new HarmonyMethod(
                    typeof(AbilityToggleEnforcer),
                    nameof(FinalizeInit_Postfix))
            );
        }

        private static void BuildGrantToRacesCache()
        {
            _grantToRacesCache = new Dictionary<string, List<string>>();
            foreach (AbilityDef def in DefDatabase<AbilityDef>.AllDefs)
            {
                AbilityToggleExtension ext = def.GetModExtension<AbilityToggleExtension>();
                if (ext == null || ext.grantToRaces.NullOrEmpty())
                    continue;

                _grantToRacesCache[def.defName] = ext.grantToRaces;
            }
        }

        private static void SpawnSetup_Postfix(Pawn __instance, bool respawningAfterLoad)
        {
            if (respawningAfterLoad)
                return;

            if (__instance.abilities == null)
                return;

            EnforcePawn(__instance);
        }

        private static void FinalizeInit_Postfix()
        {
            PoniesOfTheRimSettingsData settings = PoniesOfTheRimSettings.settings;
            if (settings == null)
                return;

            List<AbilityDef> disabled = new List<AbilityDef>();
            List<AbilityDef> enabled  = new List<AbilityDef>();

            foreach (KeyValuePair<string, bool> kv in settings.abilityToggles)
            {
                AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(kv.Key);
                if (def == null)
                    continue;

                if (kv.Value) enabled.Add(def);
                else          disabled.Add(def);
            }

            if (disabled.Count == 0 && enabled.Count == 0)
                return;

            int affected = 0;

            foreach (Pawn pawn in AllRelevantPawns())
            {
                if (pawn.abilities == null)
                    continue;

                foreach (AbilityDef def in disabled)
                    pawn.abilities.RemoveAbility(def);

                foreach (AbilityDef def in enabled)
                {
                    if (ShouldGrantToPawn(def, pawn))
                        pawn.abilities.GainAbility(def);
                }

                affected++;
            }

            if (affected > 0)
                Log.Message($"[PoniesOfTheRim] AbilityToggleEnforcer: обработано {affected} пешек после загрузки.");
        }

        public static void EnforcePawn(Pawn pawn)
        {
            if (pawn?.abilities == null)
                return;

            PoniesOfTheRimSettingsData settings = PoniesOfTheRimSettings.settings;
            if (settings == null)
                return;

            foreach (KeyValuePair<string, bool> kv in settings.abilityToggles)
            {
                AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(kv.Key);
                if (def == null)
                    continue;

                if (!kv.Value)
                {
                    pawn.abilities.RemoveAbility(def);
                }
                else if (ShouldGrantToPawn(def, pawn))
                {
                    pawn.abilities.GainAbility(def);
                }
            }
        }

        private static bool ShouldGrantToPawn(AbilityDef def, Pawn pawn)
        {
            if (_grantToRacesCache == null)
                return false;

            if (!_grantToRacesCache.TryGetValue(def.defName, out List<string> races))
                return false;

            return races.Contains(pawn.def.defName);
        }

        private static IEnumerable<Pawn> AllRelevantPawns()
        {
            foreach (Map map in Find.Maps)
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                    yield return pawn;

            foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
                yield return pawn;
        }
    }
}