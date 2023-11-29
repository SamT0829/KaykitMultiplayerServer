using System;
using System.Collections.Generic;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.DBRelated;

namespace KayKitMultiplayerServer.System.Common.Subsystems
{
    public class LogSystem : ServerSubsystemBase
    {
        public LogSystem(IFiber systemFiber) : base(systemFiber)
        {
        }

        public void LogError(long accountId, GameType gameType, ErrorCode err)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();

            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LogPlayerErrorLog_AccountId, accountId)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LogPlayerErrorLog_GameType, (int)gameType)) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LogPlayerErrorLog_ErrorLog, err.ToString())) return;
            if (!DBManager.AddDbParam(parameters, DbStoreProcedureInput.LogPlayerErrorLog_LogTime, DateTime.Now)) return;

            DBManager.Instance.ExecuteNonQuery(DBCatagory.GameLog, DbStoreProcedureInput.LogPlayerErrorLog,
                parameters, _systemFiber, null);
        }
    }
}