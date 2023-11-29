using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.GameSystem.Games;
using NotImplementedException = System.NotImplementedException;

namespace KayKitMultiplayerServer.System.GameSystem.Common
{
    public interface IGameBuilder
    {
        IGameLogic buildGameLogic(IFiber fiber);
        OnlineGameBase buildOnlineGame(IFiber fiber);
    }
    public class KayKitBrawlBuilder : IGameBuilder
    {
        public IGameLogic buildGameLogic(IFiber fiber)
        {
            return new KayKitBrawl(fiber);
        }

        public OnlineGameBase buildOnlineGame(IFiber fiber)
        {
            return null;
        }
    }

    public class KayKitTeamBrawlBuilder : IGameBuilder
    {
        public IGameLogic buildGameLogic(IFiber fiber)
        {
            return new KayKitTeamBrawl(fiber);
        }
        public OnlineGameBase buildOnlineGame(IFiber fiber)
        {
            return null;
        }
    }

    public class KayKitCoinBrawlBuilder : IGameBuilder
    {
        public IGameLogic buildGameLogic(IFiber fiber)
        {
            return new KayKitCoinBrawl(fiber);
        }
        public OnlineGameBase buildOnlineGame(IFiber fiber)
        {
            return null;
        }
    }

    public class DragonBoardGameBuilder : IGameBuilder
    {
        public IGameLogic buildGameLogic(IFiber fiber)
        {
            return null;
        }

        public OnlineGameBase buildOnlineGame(IFiber fiber)
        {
            return new DragonBoard(fiber);
        }
    }
}