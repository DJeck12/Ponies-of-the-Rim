using System.Threading;
using Verse;

namespace PoniesOfTheRim
{
    internal static class PonyThreadGuard
    {
        public static void ReportIfOffMain(string site, ref int reportedFlag)
        {
            if (Volatile.Read(ref reportedFlag) != 0 || !Prefs.DevMode || UnityData.IsInMainThread)
            {
                return;
            }
            if (LongEventHandler.AnyEventNowOrWaiting || Scribe.mode != LoadSaveMode.Inactive)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref reportedFlag, 1, 0) != 0)
            {
                return;
            }

            PonyLog.Warn($"Threads: {site} ran off the main thread (thread {Thread.CurrentThread.ManagedThreadId})");
        }
    }
}