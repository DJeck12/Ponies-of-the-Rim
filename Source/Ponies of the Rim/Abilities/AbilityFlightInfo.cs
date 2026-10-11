using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PoniesOfTheRim.Abilities
{
    public class Ability_FlightInfo : Ability
    {
        public Ability_FlightInfo(Pawn pawn)
            : base(pawn)
        {
        }

        public Ability_FlightInfo(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }

        public override IEnumerable<Command> GetGizmos()
        {
            yield break;
        }
    }
}