using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Verse;

namespace PoniesOfTheRim
{
    internal static class PonyLog
    {
        public const string Prefix = "[PoniesOfTheRim] ";
        private const int MaxBufferedOffThread = 64;

        private enum Level
        {
            Message,
            Warning,
            Error
        }

        private readonly struct PendingEntry
        {
            public readonly Level Level;
            public readonly string Text;

            public PendingEntry(Level level, string text)
            {
                Level = level;
                Text = text;
            }
        }

        private static readonly ConcurrentDictionary<string, byte> OnceKeys =
            new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        private static readonly ConcurrentQueue<PendingEntry> Pending =
            new ConcurrentQueue<PendingEntry>();

        private static int _pendingCount;
        private static int _droppedCount;
        private static bool _forceVerbose;
        private static bool _pumpInstalled;

        public static bool Verbose
        {
            get { return _forceVerbose || Prefs.DevMode; }
            set { _forceVerbose = value; }
        }

        public static void Trace(string message)
        {
            if (Verbose)
            {
                Emit(Level.Message, message);
            }
        }

        public static void Info(string message)
        {
            Emit(Level.Message, message);
        }

        public static void Warn(string message)
        {
            Emit(Level.Warning, message);
        }

        public static void Error(string message)
        {
            Emit(Level.Error, message);
        }

        public static void TraceOnce(string id, string message)
        {
            if (Verbose && ClaimOnce(id))
            {
                Emit(Level.Message, message);
            }
        }

        public static void WarnOnce(string id, string message)
        {
            if (ClaimOnce(id))
            {
                Emit(Level.Warning, message);
            }
        }

        public static void ErrorOnce(string id, string message)
        {
            if (ClaimOnce(id))
            {
                Emit(Level.Error, message);
            }
        }

        public static void WarnCaught(
            string message,
            Exception ex,
            [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int callerLine = 0,
            [CallerMemberName] string callerMember = "")
        {
            if (ClaimOnce(CaughtKey(callerFile, callerLine)))
            {
                Emit(Level.Warning, DescribeCaught(message, ex, callerFile, callerLine, callerMember));
            }
        }

        public static void ErrorCaught(
            string message,
            Exception ex,
            [CallerFilePath] string callerFile = "",
            [CallerLineNumber] int callerLine = 0,
            [CallerMemberName] string callerMember = "")
        {
            if (ClaimOnce(CaughtKey(callerFile, callerLine)))
            {
                Emit(Level.Error, DescribeCaught(message, ex, callerFile, callerLine, callerMember));
            }
        }

        public static void ResetOnce()
        {
            OnceKeys.Clear();
        }

        public static void Flush()
        {
            if (Volatile.Read(ref _pendingCount) == 0 && Volatile.Read(ref _droppedCount) == 0)
            {
                return;
            }
            if (UnityData.IsInMainThread)
            {
                FlushPending();
            }
        }

        public static void InstallMainThreadPump()
        {
            if (_pumpInstalled || !UnityData.IsInMainThread)
            {
                return;
            }
            _pumpInstalled = true;
            UnityEngine.Application.onBeforeRender += Flush;
            FlushPending();
        }

        private static bool ClaimOnce(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return true;
            }
            return OnceKeys.TryAdd(id, 0);
        }

        private static string CaughtKey(string file, int line)
        {
            return "caught:" + file + ":" + line;
        }

        private static string DescribeCaught(string message, Exception ex, string file, int line, string member)
        {
            string header = string.IsNullOrEmpty(message) ? "Непредвиденная ошибка." : message;

            string where = FileNameOnly(file) + ":" + line;
            if (!string.IsNullOrEmpty(member))
            {
                where += ", " + member;
            }

            string body = (ex == null) ? "(исключение не передано)" : ex.ToString();

            return header +
                   "\nМесто: " + where +
                   "\n" + body +
                   "\nПовторы этой ошибки до конца сессии скрыты.";
        }

        private static string FileNameOnly(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "<неизвестный файл>";
            }
            int slash = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        private static void Emit(Level level, string message)
        {
            string text = Prefix + (message ?? "<null>");

            if (!UnityData.IsInMainThread)
            {
                Buffer(level, text);
                return;
            }

            FlushPending();
            Write(level, text);
        }

        private static void Buffer(Level level, string text)
        {
            if (Interlocked.Increment(ref _pendingCount) > MaxBufferedOffThread)
            {
                Interlocked.Decrement(ref _pendingCount);
                Interlocked.Increment(ref _droppedCount);
                return;
            }
            Pending.Enqueue(new PendingEntry(level, text));
        }

        private static void FlushPending()
        {
            PendingEntry entry;
            while (Pending.TryDequeue(out entry))
            {
                Interlocked.Decrement(ref _pendingCount);
                Write(entry.Level, entry.Text);
            }

            int dropped = Interlocked.Exchange(ref _droppedCount, 0);
            if (dropped > 0)
            {
                Write(Level.Warning, Prefix + "Пропущено сообщений из фоновых потоков: " + dropped +
                                     " (переполнение буфера).");
            }
        }

        private static void Write(Level level, string text)
        {
            switch (level)
            {
                case Level.Warning:
                    Log.Warning(text);
                    break;
                case Level.Error:
                    Log.Error(text);
                    break;
                default:
                    Log.Message(text);
                    break;
            }
        }
    }
}