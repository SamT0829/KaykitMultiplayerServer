using System;
using System.Collections.Generic;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;
using KayKitMultiplayerServer.System.Common;
using KayKitMultiplayerServer.System.Common.Subsystems;
using KayKitMultiplayerServer.System.MainSystems;

namespace KayKitMultiplayerServer.System.AccountSystem.Subsystems
{
    public class LoginSystem : ServerSubsystemBase
    {
        private Dictionary<long, DateTime> _protectingLoginTable = new Dictionary<long, DateTime>();

        public LoginSystem(IFiber systemFiber) : base(systemFiber)
        {
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.AccountRegisterRequest, OnAccountRegisterRequest);
            RegisterObservedMessage(MessageType.ClientHandlerMessage, ClientHandlerMessage.AccountLoginRequest, OnAccountLoginRequest);
        }
        public void AccomplishLogin(long accountId)
        {
            _protectingLoginTable.Remove(accountId);
        }
        private void OnAccountRegisterRequest(int connectionId, Dictionary<int, object> msg)
        {
        }

        private void OnAccountLoginRequest(int connectionId, Dictionary<int, object> msg)
        {
            GameType gameType;
            string gameId = string.Empty;
            string password;
            DateTime loginTime = DateTime.Now;

            if (!RetrieveMessageItem(msg, AccountLoginRequest.GameType, out gameType)) return;
            if (!RetrieveMessageItem(msg, AccountLoginRequest.GameId, out gameId)) return;
            if (!RetrieveMessageItem(msg, AccountLoginRequest.Password, out password)) return;

            string gameTypeName = Enum.ToObject(typeof(GameType), gameType).ToString();
            // account login 
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountLogin_GameType, gameTypeName)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountLogin_GameId, gameId)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountLogin_Password, password)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.AccountLogin_LoginTime, loginTime)) return;

            DBManager.Instance.ExecuteReader(DBCatagory.Account, DbStoreProcedureInput.AccountLogin,
                parameters, _systemFiber, (reader) => OnDbCallback_AccountLogin(connectionId, reader, gameType)
            );
        }

        private void OnDbCallback_AccountLogin(int connectionId, List<Dictionary<string, object>> reader, GameType gameType)
        {
            ErrorCode err = ErrorCode.Success;
            long accountId = 0L;
            int outSuccess = 0;
            int outAccountNotExist = 0;
            int outOauthExpired = 0;
            bool isSuccess = false;
            bool isAccountNotExist = false;
            bool isOauthExpired = false;

            if (reader.Count <= 0)
            {
                DebugLog.Log("[A account SP] reader count=0");
                err = ErrorCode.AccountUnableRetrieveData;
            }
            else
            {
                Dictionary<string, object> needReader = reader[0];
                //DebugLog.Log("[A account SP] reader[0]=[" + string.Join(";", reader[0].Select(x => x.Key + "=" + x.Value)) + "]");
                if (!DBManager.GetDbParam(needReader, DbStoreProcedureOutput.AccountLogin_AccountId, out accountId)) { };
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.AccountLogin_Success, out outSuccess))
                { isSuccess = outSuccess == 1 ? true : false; };
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.AccountLogin_AccountNotExist, out outAccountNotExist))
                { isAccountNotExist = outAccountNotExist == 1 ? true : false; };
                if (DBManager.GetDbParam(needReader, DbStoreProcedureOutput.AccountLogin_OauthExpired, out outOauthExpired))
                { isOauthExpired = outOauthExpired == 1 ? true : false; };

                if (isAccountNotExist)
                {
                    err = ErrorCode.AccountNotExist;
                }
                else if (isOauthExpired)
                {
                    err = ErrorCode.AccountOauthExpired;
                }
                else if (_protectingLoginTable.TryGetValue(accountId, out DateTime theTime))
                {
                    if (DateTime.Now < theTime)
                    {
                        err = ErrorCode.AccountWaitingOthers;
                    }
                    else
                    {
                        _protectingLoginTable.Remove(accountId);
                    }
                }
            }

            if (err == ErrorCode.Success)
            {
                _protectingLoginTable.Add(accountId, DateTime.Now.AddSeconds(10));
                SystemManager.Instance.GetSubsystem<AccountMainSystem>().SendClientLoginRequestToProxyServer(connectionId, accountId, gameType);
            }
            else
            {
                Dictionary<int, object> outMessage = new Dictionary<int, object>();
                AddMessageItem(outMessage, (int)AccountLoginRespond.ErrorCode, (int)err);
                AddMessageItem(outMessage, (int)AccountLoginRespond.SessionId, 0);
                AddMessageItem(outMessage, (int)AccountLoginRespond.LobbyServerIP, string.Empty);
                AddMessageItem(outMessage, (int)AccountLoginRespond.LobbyServerPort, 0);
                PhotonApplication.Instance.NetHandle.Send(connectionId, ClientHandlerMessage.AccountLoginRespond, outMessage);

                //login error log
                LogSystem system = SystemManager.Instance.GetSubsystem<LogSystem>();
                system.LogError(accountId, gameType, err);
            }
        }
    }
}