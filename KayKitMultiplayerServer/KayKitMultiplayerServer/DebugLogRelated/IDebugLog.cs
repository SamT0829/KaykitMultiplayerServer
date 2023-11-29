namespace KayKitMultiplayerServer.DebugLogRelated
{
    public delegate void LogDelegate(object message);
    public delegate void LogFormatDelegate(string format, params object[] args);

    public interface IDebugLog
    {
        LogDelegate Log { get; }
        LogFormatDelegate LogFormat { get; }
        LogDelegate LogDebug { get; }
        LogFormatDelegate LogDebugFormat { get; }
        LogDelegate LogWarning { get; }
        LogFormatDelegate LogWarningFormat { get; }
        LogDelegate LogError { get; }
        LogFormatDelegate LogErrorFormat { get; }
    }
}