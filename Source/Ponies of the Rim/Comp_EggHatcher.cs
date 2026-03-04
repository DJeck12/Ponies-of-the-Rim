using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace PoniesOfTheRim
{
    public class Comp_EggHatcher : ThingComp
    {
        public float gestateProgress;

        private static List<string> tmpLastNames = new List<string>(3);

        public Pawn hatchee;

        public Pawn mother;

        public Pawn father;

        public GeneSet geneSet;

        public XenotypeDef xenotype;

        public CompTemperatureRuinable tempComp;

        public bool hatched;

        public CompProperties_EggHatcher Props => (CompProperties_EggHatcher)props;

        public bool TemperatureDamaged
        {
            get
            {
                if (tempComp != null)
                {
                    return tempComp.Ruined;
                }
                return false;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (tempComp == null)
            {
                tempComp = parent.TryGetComp<CompTemperatureRuinable>();
            }
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);

            if (xenotype == null)
            {
                xenotype = mother?.genes?.Xenotype ?? XenotypeDefOf.Baseliner;
            }

            if (!TemperatureDamaged)
            {
                float num = (float)delta / (Props.daysToHatch * 60000f);
                gestateProgress += num;
                if (gestateProgress > 1f)
                {
                    Hatch();
                }
            }

            if (hatched)
            {
                parent.Destroy();
            }
        }

        public void GenerateChild()
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                mother?.kindDef ?? PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: true,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: false,
                1f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowPregnant: false,
                allowFood: true,
                allowAddictions: true,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                0f, 0f, null, 1f, null, null, null, null, null, null, null, null,
                RandomLastName(mother, null, father),
                null, null, null,
                forceNoIdeo: true,
                forceNoBackstory: false,
                forbidAnyTitle: false,
                forceDead: false,
                null,
                forcedXenotype: xenotype,
                forcedEndogenes: (geneSet != null)
                    ? geneSet.GenesListForReading
                    : PregnancyUtility.GetInheritedGenes(father, mother),
                forcedCustomXenotype: null,
                allowedXenotypes: null,
                forceBaselinerChance: 0f,
                developmentalStages: DevelopmentalStage.Newborn
            );

            hatchee = PawnGenerator.GeneratePawn(request);

            if (mother != null && father != null
                && GeneUtility.SameHeritableXenotype(mother, father)
                && mother.genes.UniqueXenotype)
            {
                hatchee.genes.xenotypeName = mother.genes.xenotypeName;
                hatchee.genes.iconDef = mother.genes.iconDef;
            }

            if (TryGetInheritedXenotype(mother, father, out var xenotypeDirect))
            {
                hatchee.genes?.SetXenotypeDirect(xenotypeDirect);
            }
            else if (ShouldBeHybrid(mother, father))
            {
                hatchee.genes.hybrid = true;
                hatchee.genes.xenotypeName = "Hybrid".Translate();
            }
        }

        public void Hatch()
        {
            if (hatchee == null)
            {
                GenerateChild();
            }

            hatchee.ageTracker.AgeBiologicalTicks = 0L;
            hatchee.ageTracker.BirthAbsTicks = Find.TickManager.TicksAbs;
            hatchee.SetFaction(Faction.OfPlayer);

            if (!PawnUtility.TrySpawnHatchedOrBornPawn(hatchee, parent))
            {
                return;
            }

            if (mother != null)
            {
                if (hatchee.playerSettings != null && mother.playerSettings != null)
                {
                    hatchee.playerSettings.AreaRestrictionInPawnCurrentMap =
                        mother.playerSettings.AreaRestrictionInPawnCurrentMap;
                }

                if (mother.Spawned)
                {
                    mother.GetLord()?.AddPawn(hatchee);
                }
            }

            if (hatchee.RaceProps.IsFlesh)
            {
                if (mother != null)
                {
                    hatchee.relations.AddDirectRelation(PawnRelationDefOf.Parent, mother);
                }
                if (father != null)
                {
                    hatchee.relations.AddDirectRelation(PawnRelationDefOf.Parent, father);
                }
            }

            SendLetter();
            hatched = true;
        }

        public void SendLetter()
        {
            string label;
            string desc;

            if (mother != null)
            {
                label = "Pony_EggHatchedLabel".Translate(mother.NameShortColored);
                desc = "Pony_EggHatchedDesc".Translate(mother.NameShortColored);
            }
            else
            {
                label = "Pony_EggHatchedLabel".Translate(hatchee.NameShortColored);
                desc = "Pony_EggHatchedDesc".Translate(hatchee.NameShortColored);
            }

            ChoiceLetter_BabyBirth choiceLetter =
                (ChoiceLetter_BabyBirth)LetterMaker.MakeLetter(label, desc, LetterDefOf.BabyBirth, (TargetInfo)hatchee);
            choiceLetter.Start();
            Find.LetterStack.ReceiveLetter(choiceLetter);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (!hatched && hatchee != null)
            {
                hatchee.Discard();
                hatchee = null;
            }
        }

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string baseStr = base.CompInspectStringExtra();
            if (!string.IsNullOrEmpty(baseStr))
            {
                stringBuilder.Append(baseStr);
            }
            if (mother != null)
            {
                stringBuilder.AppendLine($"Mother: {mother}");
            }
            stringBuilder.AppendLine("Progress: " + gestateProgress.ToStringPercent());
            stringBuilder.Append("Time Left: " +
                ((int)((1f - gestateProgress) * Props.daysToHatch * 60000f)).ToStringTicksToPeriod());
            return stringBuilder.ToString();
        }

        public void RegenerateChild()
        {
            if (hatchee != null)
            {
                hatchee.Discard();
                hatchee = null;
            }
            GenerateChild();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref gestateProgress, "gestateProgress", 0f);
            Scribe_Deep.Look(ref hatchee, "hatchee");
            Scribe_References.Look(ref mother, "mother");
            Scribe_References.Look(ref father, "father");
            Scribe_Deep.Look(ref geneSet, "geneSet");
            Scribe_Defs.Look(ref xenotype, "xenotype");
            Scribe_Values.Look(ref hatched, "hatched", defaultValue: false);
        }

        public static string RandomLastName(Pawn geneticMother, Pawn birthingMother, Pawn father)
        {
            tmpLastNames.Clear();
            if (geneticMother != null)
            {
                tmpLastNames.Add(PawnNamingUtility.GetLastName(geneticMother));
            }
            if (father != null)
            {
                tmpLastNames.Add(PawnNamingUtility.GetLastName(father));
            }
            if (birthingMother != null && birthingMother != geneticMother && birthingMother != father)
            {
                tmpLastNames.Add(PawnNamingUtility.GetLastName(birthingMother));
            }
            if (tmpLastNames.Count == 0)
            {
                return null;
            }
            return tmpLastNames.RandomElement();
        }

        public static bool TryGetInheritedXenotype(Pawn mother, Pawn father, out XenotypeDef xenotype)
        {
            bool hasMotherGenes = mother?.genes != null;
            bool hasFatherGenes = father?.genes != null;

            if (hasMotherGenes && hasFatherGenes
                && mother.genes.Xenotype.inheritable
                && father.genes.Xenotype.inheritable
                && mother.genes.Xenotype == father.genes.Xenotype)
            {
                xenotype = mother.genes.Xenotype;
                return true;
            }
            if (hasMotherGenes && !hasFatherGenes && mother.genes.Xenotype.inheritable)
            {
                xenotype = mother.genes.Xenotype;
                return true;
            }
            if (hasFatherGenes && !hasMotherGenes && father.genes.Xenotype.inheritable)
            {
                xenotype = father.genes.Xenotype;
                return true;
            }

            xenotype = null;
            return false;
        }

        public static bool ShouldBeHybrid(Pawn mother, Pawn father)
        {
            bool hasMotherGenes = mother?.genes != null;
            bool hasFatherGenes = father?.genes != null;

            if (hasMotherGenes && hasFatherGenes)
            {
                if (mother.genes.hybrid && father.genes.hybrid)
                {
                    return true;
                }
                if (mother.genes.Xenotype.inheritable && father.genes.Xenotype.inheritable)
                {
                    return true;
                }
                bool motherInheritable = mother.genes.Xenotype.inheritable || mother.genes.hybrid;
                bool fatherInheritable = father.genes.Xenotype.inheritable || father.genes.hybrid;
                if (motherInheritable || fatherInheritable)
                {
                    return true;
                }
            }

            if ((hasMotherGenes && !hasFatherGenes && mother.genes.hybrid)
                || (hasFatherGenes && !hasMotherGenes && father.genes.hybrid))
            {
                return true;
            }

            return false;
        }
    }
}