using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.DebugLogRelated;
using KayKitMultiplayerServer.Network.ConfigReader;
using KayKitMultiplayerServer.System;
using KayKitMultiplayerServer.System.Common;
using KayKitMultiplayerServer.TableRelated;
using Photon.SocketServer;
using System.Collections.Generic;
using System;
using KayKitMultiplayerServer.Network.Client;
using KayKitMultiplayerServer.Network;
using KayKitMultiplayerServer.Network.Server;
using Photon.SocketServer.Rpc;

namespace KayKitMultiplayerServer
{
    public class PhotonApplication : ApplicationBase
    {
        private const string _configPath = @"\bin\Config\";
        private const string _configAllPath = @"\bin\Config\All\";
        private const string _tablePath = @"\bin\Table\";

        private const string _serverConfigFileName = "ServerConfig.txt";
        private const string _dbListFileName = "DbConfig.txt";
        private const string _serverListFileName = "ServerListConfig.txt";
        private const string _gameroomListFileName = "GameroomListConfig.txt";
        private const string _chatChannelListFileName = "ChatChannelListConfig.txt";
        private const string _miscellaneousConfigName = "MiscellaneousConfig.txt";
        private const string _netMsgWhiteListConfigName = "NetWhiltListConfig.txt";


        private static int ConnectionIdStartingNumber = 100;
        private int _webConnectionSerialNumber = -1;

        private object _lockConnectionNumberObj = new object();
        private int ConnectionSerialNumber
        {
            get
            {
                lock (_lockConnectionNumberObj)
                {
                    int retv = ConnectionIdStartingNumber;
                    ++ConnectionIdStartingNumber;
                    return retv;
                }
            }
        }
        private int WebConnectionSerialNumber
        {
            get
            {
                lock (_lockConnectionNumberObj)
                {
                    int retv = _webConnectionSerialNumber;
                    --_webConnectionSerialNumber;
                    return retv;
                }
            }
        }

        public string TableAbsolutePath { get; private set; }

        private IFiber _fiber = new PoolFiber();
        public ServerInfo ServerInfo;
        public NetworkHandler NetHandle { get; private set; }
        public static new PhotonApplication Instance
        {
            get => (PhotonApplication)ApplicationBase.Instance;
        }

        protected override PeerBase CreatePeer(InitRequest initRequest)
        {
            if (initRequest.LocalPort == ServerInfo.ServerInnerPort)
            {
                // for S2S connections
                return new PhotonInboundPeer(initRequest, NetHandle);
            }
            if (initRequest.LocalPort == ServerInfo.ClientOuterPort)
            {
                return new PhotonPeer(initRequest, ConnectionSerialNumber, NetHandle);
            }
            if (initRequest.LocalPort == ServerInfo.ClientWebPort)
            {
                return new PhotonPeer(initRequest, WebConnectionSerialNumber, NetHandle);
            }

            return new PhotonPeer(initRequest, ConnectionSerialNumber, NetHandle);
        }

        protected override void Setup()
        {
            ServerLog serverLog = new ServerLog();
            DebugLog.Initialize(serverLog);

            NetHandle = new NetworkHandler();

            SystemManager.Initialize();

            string configAllAbsolutePath = ApplicationPath + _configAllPath;
            string configAbsolutePath = ApplicationPath + @"\" + ApplicationName + _configPath;
            TableAbsolutePath = ApplicationPath + _tablePath;
            DebugLog.Log("Initialize = " + TableAbsolutePath + @"PrivateKeys\");

            //read from config
            TableManager.Instance.CreateTable<DBConfigReader>(configAllAbsolutePath + _dbListFileName);
            TableManager.Instance.CreateTable<ServerListConfigReader>(configAllAbsolutePath + _serverListFileName);
            //TableManager.Instance.CreateTable<MiscellaneousConfigReader>(configAllAbsolutePath + _miscellaneousConfigName);

            TableManager.Instance.CreateTable<ServerConfigReader>(configAbsolutePath + _serverConfigFileName);

            // Initialize Server Config
            var serverName = TableManager.Instance.GetTable<ServerConfigReader>().ServerName;
            var severType = TableManager.Instance.GetTable<ServerConfigReader>().SeverType;
            var serverId = TableManager.Instance.GetTable<ServerListConfigReader>().GetServerID(serverName);
            int.TryParse(TableManager.Instance.GetTable<ServerListConfigReader>().GetInnerPort(serverId), out int serverInnerPort);
            int.TryParse(TableManager.Instance.GetTable<ServerListConfigReader>().GetOuterPort(serverId), out int clientOuterPort);
            int.TryParse(TableManager.Instance.GetTable<ServerListConfigReader>().GetWebPort(serverId), out int clientWebPort);
            ServerInfo = new ServerInfo(serverId, serverName, severType, serverInnerPort, clientOuterPort, clientWebPort);

            // initialize tables
            TableManager.Instance.CreateFirmTablesFromDictionary(TableManager.Instance.GetTable<ServerConfigReader>().TableTable, TableAbsolutePath);

            // initialize DB
            CreateDbConnections();

            // initialize subsystems 
            CreateSubsystems();

            DebugLog.LogWarning("server startup end INITILIZE");
        }

        protected override void TearDown()
        {
            _fiber.Schedule(() =>
            {
                SystemManager.Instance.ShutDown();
                DBManager.Instance.ShutDown();
            }, 0L);

            _fiber.Dispose();

            //DebugLog.LogWarning("Server Shut Down.......");
        }

        private void CreateDbConnections()
        {
            Dictionary<string, DBCatagory> dbListTable = TableManager.Instance.GetTable<ServerConfigReader>().ConnectDBTable;
            Dictionary<string, DBCatagory>.Enumerator enumer = dbListTable.GetEnumerator();
            while (enumer.MoveNext())
            {
                DebugLog.LogWarning("Connect to DB : " + enumer.Current.Key);

                DBManager.Instance.CreatDbConnection(enumer.Current.Value,
                    TableManager.Instance.GetTable<DBConfigReader>().GetIpAddress(enumer.Current.Key),
                    TableManager.Instance.GetTable<DBConfigReader>().GetPort(enumer.Current.Key),
                    TableManager.Instance.GetTable<DBConfigReader>().GetHostName(enumer.Current.Key),
                    TableManager.Instance.GetTable<DBConfigReader>().GetPassword(enumer.Current.Key),
                    TableManager.Instance.GetTable<DBConfigReader>().GetDbName(enumer.Current.Key),
                    null);
            }
            enumer.Dispose();
        }

        private void CreateSubsystems()
        {
            _fiber.Start();
            object[] param = { _fiber };
            List<string> subsystemList = TableManager.Instance.GetTable<ServerConfigReader>().SubsystemList;


            for (int i = 0; i < subsystemList.Count; i++)
            {
                Type t = Type.GetType(subsystemList[i]);
                DebugLog.LogWarning("Attach subsystem : " + subsystemList[i]);


                SubsystemBase subsystem = Activator.CreateInstance(t, param) as SubsystemBase;                  //反射
                SystemManager.Instance.AttachSystem(subsystem);
            }

            SystemManager.Instance.MainSystem.OnAllSubSystemPrepared();
        }
    }
}