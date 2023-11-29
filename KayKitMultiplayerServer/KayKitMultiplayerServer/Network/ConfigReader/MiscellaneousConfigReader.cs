using KayKitMultiplayerServer.TableRelated;
using KayKitMultiplayerServer.Utility;
using System.Collections.Generic;
using System;

namespace KayKitMultiplayerServer.Network.ConfigReader
{
    public class MiscellaneousConfigReader : TableBase
    {
        private const string _SAMRSAKey = "SAMRSAKey";
        private const string _miscellaneousName = "MiscellaneousName";
        private const string _miscellaneousValue1 = "MiscellaneousValue1";
        private const string _miscellaneousValue2 = "MiscellaneousValue2";

        private Dictionary<string, Action<string, string>> _configNameParseFunctionTable =
            new Dictionary<string, Action<string, string>>();

        public string SAMRSAPublicKey { private set; get; }
        public string SAMRSAPrivateKey { private set; get; }

        public MiscellaneousConfigReader() : base()
        {
            _configNameParseFunctionTable[_SAMRSAKey] = OnSAMRSAKey;

        }
        protected override void OnRowParsed(List<object> rowContent)
        {
            ValueTypeWrapper<string> miscellaneousName = rowContent[GetColumnNameIndex(_miscellaneousName)] as ValueTypeWrapper<string>;
            ValueTypeWrapper<string> miscellaneousValue1 = rowContent[GetColumnNameIndex(_miscellaneousValue1)] as ValueTypeWrapper<string>;
            ValueTypeWrapper<string> miscellaneousValue2 = rowContent[GetColumnNameIndex(_miscellaneousValue2)] as ValueTypeWrapper<string>;

            if (miscellaneousName == null || miscellaneousValue1 == null)
            {
                return;
            }

            Action<string, string> action;
            if (_configNameParseFunctionTable.TryGetValue(miscellaneousName.Value, out action) && action != null)
            {
                action(miscellaneousValue1, miscellaneousValue2);
            }
        }

        protected override void OnTableParsed()
        {
        }

        private void OnSAMRSAKey(string miscValue1, string miscValue2)
        {
            SAMRSAPublicKey = miscValue1;
            SAMRSAPrivateKey = miscValue2;
        }
    }
}