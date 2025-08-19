using UnityEngine;

namespace LifeQuest.Utilities
{
    public enum LogLevel { Trace = 0, Debug = 1, Info = 2, Warn = 3, Error = 4, Off = 5 }

    public static class Logger
    {
        public static LogLevel MinLevel = LogLevel.Info;
        public static string Prefix = "[LifeQuest]";

        public static void I(string tag, string msg, Object ctx = null) => Log(LogLevel.Info, tag, msg, ctx);
        public static void W(string tag, string msg, Object ctx = null) => Log(LogLevel.Warn, tag, msg, ctx);
        public static void E(string tag, string msg, Object ctx = null) => Log(LogLevel.Error, tag, msg, ctx);

        static void Log(LogLevel level, string tag, string msg, Object ctx)
        {
            if (level < MinLevel || level == LogLevel.Off) return;
            string line = $"{Prefix}[{tag}] {msg}";
            switch (level)
            {
                case LogLevel.Warn: Debug.LogWarning(line, ctx); break;
                case LogLevel.Error: Debug.LogError(line, ctx); break;
                default: Debug.Log(line, ctx); break;
            }
        }
    }
}
