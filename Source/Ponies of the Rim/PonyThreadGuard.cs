using System.Threading;
using Verse;

namespace PoniesOfTheRim
{
    [StaticConstructorOnStartup]
    internal static class PonyThreadGuard
    {
        private static readonly int MainThreadId = Thread.CurrentThread.ManagedThreadId;
        public static void ReportIfOffMain(string site, ref int reportedFlag)
        {
            if (Volatile.Read(ref reportedFlag) != 0)
            {
                return;
            }
            if (!Prefs.DevMode)
            {
                return;
            }
            int id = Thread.CurrentThread.ManagedThreadId;
            if (id == MainThreadId)
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

            Log.Warning($"[PoniesOfTheRim] {site}: выполнено в потоке #{id}, главный поток #{MainThreadId}. " +
                        "Всё разделяемое изменяемое состояние на этом пути обязано быть потокобезопасным.");
        }
    }
}