namespace KayKitMultiplayerServer.Utility.BackgroundThreads
{
    public interface IBackgroundThread
    {
        void Setup();
        void Run();
        void Update();
        void Start();
        void Stop();
    }
}