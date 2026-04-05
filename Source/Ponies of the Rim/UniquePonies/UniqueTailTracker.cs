using System.Collections.Generic;
using Verse;

namespace PoniesOfTheRim.UniquePonies
{
    public class UniqueTailInitTracker : GameComponent
    {
        private HashSet<int> _initializedPawnIds = new();

        public UniqueTailInitTracker(Game game) { }

        public bool IsInitialized(int pawnId)
            => _initializedPawnIds.Contains(pawnId);

        public void MarkInitialized(int pawnId)
            => _initializedPawnIds.Add(pawnId);

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref _initializedPawnIds, "uniqueTailInitialized", LookMode.Value);
            _initializedPawnIds ??= new HashSet<int>();
        }

        public void Reset(Pawn pawn)
            => _initializedPawnIds.Remove(pawn.thingIDNumber);

        public static UniqueTailInitTracker Current
            => Verse.Current.Game?.GetComponent<UniqueTailInitTracker>();
    }
}