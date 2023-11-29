using ExitGames.Concurrency.Fibers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

namespace KayKitMultiplayerServer.Utility.BackgroundThreads
{
    public class BackgroundThread<K, C> : IBackgroundThread　where C : class
    {
        private bool isRunning = false;
        private bool isStop = false;

        private long _firstStartDelayMilliseconds;

        private IFiber _fiber;
        private Stopwatch timer;

        private Dictionary<K, C> _tTable;
        private Action<C> _updateAction;
        private long _regularInMs;


        public BackgroundThread(IFiber fiber, Dictionary<K, C> tTable, long regularInMs, Action<C> updateAction) // Include all IoC objects this thread needs i.e. IRegion, IStats, etc...
        {
            _fiber = fiber;
            _tTable = tTable;
            _regularInMs = regularInMs;
            _updateAction = updateAction;
            _firstStartDelayMilliseconds = 100L;

            Setup();
            Run();
        }

        public void Setup()
        {
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

        public void Update()
        {
            if (isStop)
                return;

            // Check to see if there are any players - We need a list of players. If we have no players, sleep for a second and try again, keeps from chewing up the CPU
            if (_tTable.Count <= 0)
            {
                timer.Restart();
                return;
            }

            if (timer.Elapsed < TimeSpan.FromMilliseconds(100)) // run every 1000ms - 1s
            {
                return;
            }

            _fiber.Schedule(() => Update(timer.Elapsed), 0);

            // Restart the timer so that, just in case it takes longer than 100ms, it'll start over as soon as the process finishes.
            timer.Restart();
        }

        public void Update(TimeSpan elapsed)
        {
            // Do the update here.
            Parallel.ForEach(_tTable.Values, SendUpdate);
        }

        public void SendUpdate(C t)
        {
            if (t != null)
            {
                _updateAction.Invoke(t);
            }
        }
    }
}