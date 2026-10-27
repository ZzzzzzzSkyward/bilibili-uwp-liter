using NLog;
using NLog.Config;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Storage;

namespace BiliLite.Helpers
{
    public enum LogType
    {
        INFO,
        DEBUG,
        ERROR,
        FATAL
    }

    public class LogHelper
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;
        public static LoggingConfiguration config;
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public static string SessionId { get; private set; }

        public static void Initialize()
        {
            lock (SyncRoot)
            {
                if (initialized)
                    return;

                SessionId = Guid.NewGuid().ToString("N");
                config = new LoggingConfiguration();
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                var logfile = new NLog.Targets.FileTarget()
                {
                    Name = "logfile",
                    CreateDirs = true,
                    FileName = storageFolder.Path + @"\log\" + DateTime.Now.ToString("yyyyMMdd") + ".log",
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|session=${gdc:item=sessionId}|${message}|${exception:format=ToString}"
                };
                config.AddRule(LogLevel.Info, LogLevel.Info, logfile);
                config.AddRule(LogLevel.Debug, LogLevel.Debug, logfile);
                config.AddRule(LogLevel.Error, LogLevel.Error, logfile);
                config.AddRule(LogLevel.Fatal, LogLevel.Fatal, logfile);
                LogManager.Configuration = config;
                GlobalDiagnosticsContext.Set("sessionId", SessionId);
                initialized = true;
            }

            Log(LogSessionMetadata(), LogType.INFO);
        }

        public static void Log(string message, LogType type, Exception ex = null)
        {
            if (!initialized)
                Initialize();

            if (ex != null)
                message += " | HResult=0x" + ex.HResult.ToString("X8");

            Debug.WriteLine("[" + type.ToString() + "]" + message);
            switch (type)
            {
                case LogType.INFO:
                    logger.Info(message);
                    break;
                case LogType.DEBUG:
                    logger.Debug(message);
                    break;
                case LogType.ERROR:
                    logger.Error(ex, message);
                    break;
                case LogType.FATAL:
                    logger.Fatal(ex, message);
                    break;
                default:
                    break;
            }
        }

        public static void Flush()
        {
            LogManager.Flush(TimeSpan.FromSeconds(2));
        }

        private static string LogSessionMetadata()
        {
            var packageVersion = Package.Current.Id.Version;
            return "Log session started; sessionId=" + SessionId
                + "; appVersion=" + packageVersion.Major + "." + packageVersion.Minor + "." + packageVersion.Build + "." + packageVersion.Revision
                + "; osVersion=" + Environment.OSVersion.Version
                + "; architecture=" + RuntimeInformation.ProcessArchitecture;
        }

        public static bool IsNetworkError(Exception ex)
        {
            if (ex.HResult == -2147012867 || ex.HResult == -2147012889)
            {
                return true;
            }
            {
                return false;
            }
        }
    }
}
