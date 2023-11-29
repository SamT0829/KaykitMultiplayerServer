using KayKitMultiplayerServer.DebugLogRelated;

public static class DebugLog
{
    private static IDebugLog _log;

    public static LogDelegate Log { get; private set; }
    public static LogFormatDelegate LogFormat { get; private set; }
    public static LogDelegate LogDebug { get; private set; }
    public static LogFormatDelegate LogDebugFormat { get; private set; }
    public static LogDelegate LogError { get; private set; }
    public static LogFormatDelegate LogErrorFormat { get; private set; }
    public static LogDelegate LogWarning { get; private set; }
    public static LogFormatDelegate LogWarningFormat { get; private set; }

    public static void Initialize(IDebugLog log)
    {
        _log = log;
        Log = _log.Log;
        LogDebug = _log.LogDebug;
        LogWarning = _log.LogWarning;
        LogError = _log.LogError;
        LogFormat = _log.LogFormat;
        LogDebugFormat = _log.LogDebugFormat;
        LogWarningFormat = _log.LogWarningFormat;
        LogErrorFormat = _log.LogErrorFormat;
    }
}
