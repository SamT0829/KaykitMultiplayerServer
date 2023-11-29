using KayKitMultiplayerServer.System.Common;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;

namespace KayKitMultiplayerServer.System
{
    public class SystemManager
    {
        private Dictionary<Type, SubsystemBase> _typeToSubsystemTable = new Dictionary<Type, SubsystemBase>();
        private Dictionary<MessageType , Dictionary<int, List<Type>>> _msgToSubsystemTable = new Dictionary<MessageType, Dictionary<int, List<Type>>>();
        private ReaderWriterLock _lock = new ReaderWriterLock();

        public Common.MainSystem MainSystem { get; private set; }

        protected static SystemManager _instance;

        public static SystemManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SystemManager();
                }
                return _instance;
            }
        }

        public static void Initialize()
        {
            if (_instance == null)
            {
                _instance = new SystemManager();
            }
        }

        public void RegisterMainSystem(Common.MainSystem mainSystem)
        {
            if (MainSystem == null)
            {
                MainSystem = mainSystem;
            }
        }

        public T GetSubsystem<T>() where T : SubsystemBase
        {
            _lock.AcquireReaderLock(1000);
            SubsystemBase subsystem;
            if (_typeToSubsystemTable.TryGetValue(typeof(T), out subsystem)
                && subsystem != null && subsystem is SubsystemBase)
            {
                _lock.ReleaseReaderLock();
                return (T)subsystem;
            }
            else
            {
                _lock.ReleaseReaderLock();
            }
            return default(T);

        }

        public void ShutDown()
        {
            //_lock.AcquireReaderLock(1000);

            var enumerator = _typeToSubsystemTable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                enumerator.Current.Value.ShutDown();
            }
            enumerator.Dispose();
            //_lock.ReleaseReaderLock();
        }

        public void AttachSystem(SubsystemBase system)
        {
            _lock.AcquireWriterLock(1000);
            _typeToSubsystemTable.Add(system.GetType(), system);
            //system.OnAttached();
            _lock.ReleaseWriterLock();
        }

        public void DispatchMessage(MessageType msgType, int messageId, int senderId, Dictionary<int, object> message)
        {
            _lock.AcquireReaderLock(1000);
            Dictionary<int, List<Type>> subsystemMessage;
            List<Type> systemList;
            if (_msgToSubsystemTable.TryGetValue(msgType, out subsystemMessage) && subsystemMessage != null)
            {
                if (subsystemMessage.TryGetValue(messageId, out systemList) && systemList != null)
                systemList.ForEach(system =>
                {
                    SubsystemBase subsystem;
                    if (_typeToSubsystemTable.TryGetValue(system, out subsystem) && subsystem != null)
                    {
                        subsystem.OnReceiveMessage(msgType, messageId, senderId, message);
                    }
                });
            }
            _lock.ReleaseReaderLock();
        }

        // only for SubsystemBase
        internal void AddMsgTypeListener<E>(MessageType msgType, E messageHandle, Type systemType)
        {
            _lock.AcquireWriterLock(1000);
            Dictionary<int, List<Type>> subsystemMessage;
            List<Type> systemTypeList;

            if (!_msgToSubsystemTable.TryGetValue(msgType, out subsystemMessage))
            {
                subsystemMessage = new Dictionary<int, List<Type>>();
                _msgToSubsystemTable.Add(msgType, subsystemMessage);
            }

            if (!subsystemMessage.TryGetValue(messageHandle.GetHashCode(), out systemTypeList))
            {
                systemTypeList = new List<Type>();
                subsystemMessage.Add(messageHandle.GetHashCode(), systemTypeList);
            }

            var system = systemTypeList.FirstOrDefault(s => s == systemType);
            if(system == null)
                systemTypeList.Add(systemType);

            _lock.ReleaseWriterLock();
        }
        // only for SubsystemBase
        internal void RemoveMsgTypeListener<E>(MessageType msgType, E messageHandle, Type systemType)
        {
            _lock.AcquireWriterLock(1000);
            Dictionary<int, List<Type>> subsystemMessage;
            List<Type> systemTypeList;

            if (_msgToSubsystemTable.TryGetValue(msgType, out subsystemMessage) && subsystemMessage != null)
            {
                if (subsystemMessage.TryGetValue(messageHandle.GetHashCode(), out systemTypeList) && systemTypeList != null)
                {
                    systemTypeList.Remove(systemType);

                    if (systemTypeList.Count == 0)
                        subsystemMessage.Remove(messageHandle.GetHashCode());
                }

                if (subsystemMessage.Count == 0)
                    _msgToSubsystemTable.Remove(msgType);
            }
            _lock.ReleaseWriterLock();
        }
    }
}