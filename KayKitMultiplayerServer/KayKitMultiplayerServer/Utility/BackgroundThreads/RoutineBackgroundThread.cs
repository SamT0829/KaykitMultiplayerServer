using System;
using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using System.Diagnostics;

namespace KayKitMultiplayerServer.Utility.BackgroundThreads
{
    public class RoutineBackgroundThread<E> : IBackgroundThread where E : Enum
    {
        public delegate void RoutineAction();
        public delegate bool RunnigRoutineAction(long timeElapsed);

        protected class RoutineState
        {
            /// <summary>
            /// 時間消耗
            /// </summary>
            public long TimeConsumption { get; private set; }
            private RoutineAction _beginAction;
            private RunnigRoutineAction _updateAction;
            private RoutineAction _endAction;

            public RoutineState(long timeConsumption, RoutineAction beginAction,
                RunnigRoutineAction updateAction, RoutineAction endAction)
            {
                TimeConsumption = timeConsumption;
                _beginAction = beginAction;
                _updateAction = updateAction;
                _endAction = endAction;
            }

            public void ExecuteBegin()
            {
                _beginAction?.Invoke();
            }

            public bool ExecuteUpdate(long timeElapsed)
            {
                if (_updateAction == null)
                {
                    return true;
                }
                return _updateAction(timeElapsed);
            }

            public void ExecuteEnd()
            {
                _endAction?.Invoke();
            }
        }

        private bool isRunning = false;
        private bool isStop = false;

        private long _firstStartDelayMilliseconds;

        private IFiber _fiber;
        private Stopwatch timer;

        private Dictionary<E, RoutineState> _routineStateTable = new Dictionary<E, RoutineState>();
        private Dictionary<E, E> _nextStateTable = new Dictionary<E, E>();

        private const int s_stateNone = 0;
        private E _stateNone = (E)Enum.ToObject(typeof(E), s_stateNone);
        private E _currentState = (E)Enum.ToObject(typeof(E), s_stateNone);
        private E _startingState = (E)Enum.ToObject(typeof(E), s_stateNone);
        private E _nextState = (E)Enum.ToObject(typeof(E), s_stateNone);
        public long StateConsumptionOffsetTime { get; set; } = 0L;

        private long _stateBeginTime;
        private long _lastStartingTime;
        private long _regularInMs;

        public RoutineBackgroundThread(IFiber fiber, E startingState, long regularInMs)
        {
            _fiber = fiber;
            _firstStartDelayMilliseconds = 100L;
            _startingState = startingState;
            _regularInMs = regularInMs;

            Setup();
        }

        public void Setup()
        {

        }

        public void AddState(E stateName, long msConsumption, RoutineAction begin, RunnigRoutineAction update, RoutineAction end)
        {
            RoutineState theState = new RoutineState(msConsumption, begin, update, end);
            _routineStateTable[stateName] = theState;
        }

        public void AddTrasitionState(E begin, E end)
        {
            _nextStateTable[begin] = end;
        }

        public void Run()
        {
            if (!isRunning)
            {
                timer = new Stopwatch();
                timer.Start();

                isRunning = true;
                _fiber.ScheduleOnInterval(Update, _firstStartDelayMilliseconds, _regularInMs);
            }
        }

        public void Start()
        {
            isStop = false;
            timer.Start();
        }

        public void Stop()
        {
            isStop = true;
            timer.Stop();
        }

        public void Update()
        {
            if (isStop)
                return;

            if (_currentState.GetHashCode() == _stateNone.GetHashCode())
            {
                _nextState = _startingState;
            }

            RoutineState currentState;

            if (_nextState.GetHashCode() != _stateNone.GetHashCode())
            {
                if (_currentState.GetHashCode() != _stateNone.GetHashCode())
                {
                    if (_routineStateTable.TryGetValue(_currentState, out currentState) && currentState != null)
                    {
                        currentState.ExecuteEnd();
                        StateConsumptionOffsetTime = 0L;
                    }
                }

                //DebugLog.Log("[Table] " + _tableId + ": " + _currentState + " -> " + _nextState);
                _currentState = _nextState;
                _nextState = _stateNone;
                if (!_routineStateTable.TryGetValue(_currentState, out currentState))
                {
                    // error
                    DebugLog.LogError("[1]Unable to find table game state = " + _currentState.ToString());
                }

                currentState.ExecuteBegin();
                _lastStartingTime = timer.ElapsedMilliseconds;
                _stateBeginTime = _lastStartingTime;
                return;
            }

            if (_currentState.GetHashCode() == _stateNone.GetHashCode())
            {
                DebugLog.LogError("current table game state is none ");
                // error
            }
            if (!_routineStateTable.TryGetValue(_currentState, out currentState))
            {
                DebugLog.LogError("[2]Unable to find table game state = " + _currentState.ToString());
                // error
            }

            long currentStartTime = timer.ElapsedMilliseconds;
            bool transferEnable = currentState.ExecuteUpdate(currentStartTime - _lastStartingTime);
            _lastStartingTime = currentStartTime;
            if (currentStartTime - _stateBeginTime >= currentState.TimeConsumption + StateConsumptionOffsetTime && transferEnable)
            {
                _nextStateTable.TryGetValue(_currentState, out _nextState);

                // Restart the timer so that, just in case it takes longer than 100ms, it'll start over as soon as the process finishes.
                timer.Restart();
            }
        }

        public void NextState()
        {
            if (_nextState.GetHashCode() == _stateNone.GetHashCode())
            {
                _nextStateTable.TryGetValue(_currentState, out _nextState);
            }
        }
    }
}