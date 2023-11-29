using ExitGames.Logging.Log4Net;
using log4net.Config;
using Photon.SocketServer;
using System.IO;
using ExitGames.Logging;

namespace KayKitMultiplayerServer.DebugLogRelated
{
    public class ServerLog : IDebugLog
    {
        public LogDelegate Log { get; private set; }
        public LogDelegate LogDebug { get; private set; }
        public LogDelegate LogError { get; private set; }
        public LogFormatDelegate LogFormat { get; private set; }
        public LogFormatDelegate LogDebugFormat { get; private set; }
        public LogFormatDelegate LogErrorFormat { get; private set; }
        public LogDelegate LogWarning { get; private set; }
        public LogFormatDelegate LogWarningFormat { get; private set; }

        public ServerLog()
        {
            log4net.GlobalContext.Properties["Photon:ApplicationLogPath"] =
                ApplicationBase.Instance.ApplicationPath + @"\log\" + ApplicationBase.Instance.ApplicationName + ".log";//Path.Combine(ApplicationBase.Instance.ApplicationPath, "log");
            FileInfo file = new FileInfo(Path.Combine(ApplicationBase.Instance.BinaryPath, "log4net.config"));

            if (file.Exists)
            {
                LogManager.SetLoggerFactory(Log4NetLoggerFactory.Instance);
                XmlConfigurator.ConfigureAndWatch(file);
            }

            Log = LogManager.GetCurrentClassLogger().Info;
            LogDebug = LogManager.GetCurrentClassLogger().Debug;
            LogWarning = LogManager.GetCurrentClassLogger().Warn;
            LogError = LogManager.GetCurrentClassLogger().Error;
            LogFormat = LogManager.GetCurrentClassLogger().InfoFormat;
            LogDebugFormat = LogManager.GetCurrentClassLogger().DebugFormat;
            LogWarningFormat = LogManager.GetCurrentClassLogger().WarnFormat;
            LogErrorFormat = LogManager.GetCurrentClassLogger().ErrorFormat;

            //取得或設定資源管理員目前用以在執行階段查詢特定文化特性資源所用的文化特性。
            //System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");       // 強制使用者使用特定的多國語系   zh-TW 為繁體中文
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
        }
    }
}