using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;

namespace CFIT.AppLogger
{
    public enum LogLevel
    {
        Critical = 5,
        Error = 4,
        Warning = 3,
        Information = 2,
        Debug = 1,
        Verbose = 0,
    }

    public static class Logger
    {
        public static ConcurrentQueue<string> Messages { get; private set; } = new ConcurrentQueue<string>();
        public static LogLevel MinimumLevel { get; private set; } = LogLevel.Verbose;
        public static LogLevel MinimumLevelMessages { get; set; } = LogLevel.Information;
        public static LogLevel HighestLevel { get; private set; } = LogLevel.Verbose;
        public static LogLevel DiscardLevel { get; private set; } = LogLevel.Information;
        public static int MaximumSize { get; set; } = 1024 * 1024 * 10;
        public static string FileName { get; set; } = "session.log";
        public static bool SessionKeepFile { get; set; } = false;
        public static bool SessionKeepOldLog { get; set; } = false;
        public static bool SessionKeepRemoveOld { get; set; } = false;

        public static void CreateAppLoggerRotated(ILoggerConfig config)
        {
            FileName = Path.Combine(config.LogDirectory, config.LogFile);
            LoggerConfiguration loggerConfiguration;
            if (config.SizeLimit > 0)
                loggerConfiguration = new LoggerConfiguration().WriteTo.File(FileName, rollingInterval: config.LogInterval, retainedFileCountLimit: config.LogCount, fileSizeLimitBytes: config.SizeLimit,
                                                    outputTemplate: config.LogTemplate);
            else
                loggerConfiguration = new LoggerConfiguration().WriteTo.File(FileName, rollingInterval: config.LogInterval, retainedFileCountLimit: config.LogCount,
                                                    outputTemplate: config.LogTemplate);
            SetLogLevel(loggerConfiguration, config.LogLevel);
            MinimumLevel = config.LogLevel;
            Serilog.Log.Logger = loggerConfiguration.CreateLogger();
        }

        public static void CreateAppLoggerSimple(string filePath = "log/startup.log", LogLevel minimumLevel = LogLevel.Debug, string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message} {NewLine}")
        {
            FileName = filePath;
            File.Create(FileName).Close();

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                        .WriteTo.File(FileName, outputTemplate: template, fileSizeLimitBytes: MaximumSize);
            SetLogLevel(loggerConfiguration, minimumLevel);
            MinimumLevel = minimumLevel;
            Serilog.Log.Logger = loggerConfiguration.CreateLogger();
        }

        public static void CreateAppLoggerSession(string filePath, LogLevel minimumLevel = LogLevel.Debug, string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message} {NewLine}")
        {
            FileName = filePath;
            if (File.Exists(FileName))
            {
                if ((new FileInfo(FileName)).Length != 0 && SessionKeepOldLog)
                {
                    if (File.Exists(FileName + ".old"))
                        File.Delete(FileName + ".old");
                    File.Move(FileName, FileName + ".old");
                }
                else
                    File.Delete(FileName);
            }

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                        .WriteTo.File(FileName, outputTemplate: template, fileSizeLimitBytes: MaximumSize);

            SetLogLevel(loggerConfiguration, minimumLevel);
            MinimumLevel = minimumLevel;
            Serilog.Log.Logger = loggerConfiguration.CreateLogger();
        }

        public static void CloseAndFlush()
        {
            Serilog.Log.CloseAndFlush();
        }

        public static void DestroyLoggerSession()
        {
            CloseAndFlush();

            if (!SessionKeepFile && File.Exists(FileName) && HighestLevel <= DiscardLevel)
            {
                File.Delete(FileName);
                if (File.Exists(FileName + ".old"))
                    File.Delete(FileName + ".old");
            }

            if (SessionKeepRemoveOld && File.Exists(FileName + ".old"))
                File.Delete(FileName + ".old");
        }

        public static Serilog.Core.Logger GetLogger(string filePath, LogLevel minimumLevel = LogLevel.Debug, string template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} - {Message} {NewLine}")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return null;

                File.Create(filePath).Close();

                LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                            .WriteTo.File(filePath, outputTemplate: template);
                SetLogLevel(loggerConfiguration, minimumLevel);
                return loggerConfiguration.CreateLogger();
            }
            catch (Exception ex)
            {
                LogException(ex);
                return null;
            }
        }

        public static void SetLogLevel(LoggerConfiguration loggerConfiguration, LogLevel logLevel)
        {
            if (logLevel == LogLevel.Warning)
                loggerConfiguration.MinimumLevel.Warning();
            else if (logLevel == LogLevel.Debug)
                loggerConfiguration.MinimumLevel.Debug();
            else if (logLevel == LogLevel.Verbose)
                loggerConfiguration.MinimumLevel.Verbose();
            else
                loggerConfiguration.MinimumLevel.Information();
        }

        private static void WriteLog(LogLevel level, string message, string classFile = "", string classMethod = "")
        {
            if (level > HighestLevel)
                HighestLevel = level;

            if (level < MinimumLevel)
                return;

            string context = GetContext(classFile, classMethod);
            message = message.Replace("\n", "").Replace("\r", "");
            
            switch (level)
            {
                case LogLevel.Critical:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Fatal(message);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Error(message);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Warning(message);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Information(message);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Debug(message);
                    break;
                case LogLevel.Verbose:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Verbose(message);
                    break;
                default:
                    Serilog.Log.Logger.ForContext("SourceContext", context).Debug(message);
                    break;
            }
            if (level >= MinimumLevelMessages)
                Messages.Enqueue(message);
        }

        public static void Log(LogLevel level, string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(level, message, classFile, classMethod);
        }

        public static void Error(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Error, message, classFile, classMethod);
        }

        public static void Warning(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Warning, message, classFile, classMethod);
        }

        public static void Information(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Information, message, classFile, classMethod);
        }

        public static void Debug(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Debug, message, classFile, classMethod);
        }

        public static void Verbose(string message, [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLog(LogLevel.Verbose, message, classFile, classMethod);
        }

        private static void WriteLogException(Exception ex, string message = "", string classFile = "", string classMethod = "")
        {
            string context = GetContext(classFile, classMethod);
            message = message.Replace("\n", "").Replace("\r", "");
            if (!string.IsNullOrEmpty(message))
                message = $"{message}: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}";
            else
                message = $"Exception catched: '{ex.GetType()}' - '{ex.Message}' - '{ex.TargetSite}' - {ex.StackTrace}";

            Serilog.Log.Logger.ForContext("SourceContext", context).Error(message);
            Messages.Enqueue(message);
        }

        public static void LogException(Exception ex, string message = "", [CallerFilePath] string classFile = "", [CallerMemberName] string classMethod = "")
        {
            WriteLogException(ex, message, classFile, classMethod);
        }

        private static string GetContext(string classFile, string classMethod)
        {
            string context = Path.GetFileNameWithoutExtension(classFile) + ":" + classMethod;
            if (context.Length > 32)
                context = context.Substring(0, 32);

            return string.Format(" {0,-32} ", context);
        }
    }
}
