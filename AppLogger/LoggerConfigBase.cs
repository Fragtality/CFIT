using Serilog;

namespace CFIT.AppLogger
{
    public abstract class LoggerConfigBase : ILoggerConfig
    {
        public string LogDirectory { get { return "log"; } }
        public abstract string LogFile { get; }
        public RollingInterval LogInterval { get { return RollingInterval.Day; } }
        public int SizeLimit { get { return 1024 * 1024; } }
        public int LogCount { get { return 3; } }
        public string LogTemplate { get { return "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message} {NewLine}"; } }
        public LogLevel LogLevel { get { return LogLevel.Verbose; } }
    }
}
