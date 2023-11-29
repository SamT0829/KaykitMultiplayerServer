using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using System;

namespace KayKitMultiplayerServer.System.Common
{
    public class ServerSubsystemBase : SubsystemBase
    {
        protected IFiber _systemFiber;

        private IFiber _selfCreatedFiber;

        protected ServerSubsystemBase(IFiber systemFiber)
            : base()
        {
            if (systemFiber == null)
            {
                _selfCreatedFiber = new PoolFiber();
                _selfCreatedFiber.Start();
                _systemFiber = _selfCreatedFiber;
            }
            else
            {
                _systemFiber = systemFiber;
            }
        }

        public override void ShutDown()
        {
            if (_selfCreatedFiber != null)
            {
                _selfCreatedFiber.Dispose();
            }
        }
       

        public override void OnReceiveMessage(MessageType msgType, int messageId, int senderID, Dictionary<int, object> msg)
        {
            _systemFiber.Schedule(() => { base.OnReceiveMessage(msgType, messageId, senderID, msg); }, 0L);
        }

        protected override void RegisterObservedMessage<E>(MessageType msgType, E messageHandle, Action<int, Dictionary<int, object>> listener)
        {
            _systemFiber.Schedule(() => { base.RegisterObservedMessage(msgType, messageHandle, listener); }, 0L);
        }

        protected override void UnregisterObservedMessage<E>(MessageType msgType, E messageHandle)
        {
            _systemFiber.Schedule(() => { base.UnregisterObservedMessage(msgType, messageHandle); }, 0L);
        }
    }
}