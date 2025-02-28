using Serilog;

namespace CFIT.AppLogger
{
    public interface ILoggerConfig
    {
        string LogDirectory { get; }
        string LogFile { get; }
        RollingInterval LogInterval { get; }
        int SizeLimit { get; }
        int LogCount { get; }
        string LogTemplate { get; }
        LogLevel LogLevel { get; }
    }
}
