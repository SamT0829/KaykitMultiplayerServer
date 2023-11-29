using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using ExitGames.Concurrency.Fibers;
using KayKitMultiplayerServer.System.GameSystem.Info;
using KayKitMultiplayerServer.Utility;
using KayKitMultiplayerServer.Utility.BackgroundThreads;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace KayKitMultiplayerServer.System.GameSystem.Games
{
    public class DragonBoard : OnlineGameBase
    {
        private enum GamePlayer
        {
            None,
            PlayerA,
            PlayerB,
        }
        private enum BoardGameMessage
        {
            DraggingPieceMessage,
            DraggingCardMessage,
        }
        private enum DraggingPieceMessage
        {
            PieceID,
            PieceOnDrag,
            PiecePositionX,
            PiecePositionY,
            PieceTileX,
            PieceTileY,
        }
        private enum DraggingCardMessage
        {
            CardID,
            CardPositionX,
            CardPositionY,
            CardTileX,
            CardTileY,
        }
        private enum DragonBoardGameStaticData
        {
            GameTiles,
            GamePlayer,
        }
        private enum DragonBoardGameDynamicData
        {
            GamePiece,
            GameTimer,
        }
        public enum DragonBoardGameState
        {
            None,
            Prepare,
            PlayerA,
            PlayerB,
            Ending,
        }
        private enum TileType
        {

            None,
            Land,
            SmallWall,
            Wall,
            Water,
            Fire,
        }
        private class TileBase
        {
            public int X;
            public int Y;
            public TileType TileType { get; private set; }
            public PieceBase TilePiece { get; set; }

            public void InitTile(int x, int y, TileType tileType)
            {
                X = x;
                Y = y;
                TileType = tileType;
                TilePiece = null;
            }

            public void SetTileLocation(int x, int y)
            {
                X = x;
                Y = y;
            }

            public void SetTileType(TileType tileType)
            {
                TileType = tileType;
            }

            public void SetTilePiece(PieceBase tilePiece)
            {
                TilePiece = tilePiece;
            }
        }
        private class CardBase
        {
            public int CardId;
            public string CardName;
            public int Damage;
            public int Health;

            public CardBase(int cardId)
            {
                CardId = cardId;
            }

            public void SetCardData(string name, int damage, int health)
            {
                CardName = name;
                Damage = damage;
                Health = health;
            }
        }
        private class PieceBase
        {
            public int PieceId;
            public GamePlayer GamePlayer;
            public int X;
            public int Y;


            public bool onDrag = false;
            public double PositionX;
            public double PositionY;

            public void InitPiece(int pieceId, GamePlayer gamePlayer, double positionX, double positionY, int x, int y)
            {
                PieceId = pieceId;
                GamePlayer = gamePlayer;
                PositionX = positionX;
                PositionY = positionY;
                X = x;
                Y = y;
            }
            public void OnDragging(bool onDrag, double positionX, double positionY, int tileX, int tileY)
            {
                this.onDrag = onDrag;
                PositionX = positionX;
                PositionY = positionY;
                X = tileX;
                Y = tileY;
            }
        }

        private class Player
        {
            public int Health;
            public bool MyTurn;
            public GamePlayer GamePlayer;
            public List<CardBase> CardsHand = new();
            public List<CardBase> DeckCard = new();

            public Player(GamePlayer gamePlayer)
            {
                Health = 30;
                GamePlayer = gamePlayer;
                MyTurn = false;
            }

            // Game
            public void InitGameCard()
            {
                CardBase card = new CardBase(1);
                CardBase card2 = new CardBase(2);

                card.SetCardData("Card", 2, 2);
                card2.SetCardData("Card2", 10, 5);
                DeckCard.Add(card);
                DeckCard.Add(card2);
            }

            public void DrawCard()
            {
                if(DeckCard.Count <= 0)
                    return;

                var card = DeckCard[new Random().Next(0, DeckCard.Count)];
                DeckCard.Remove(card);
                CardsHand.Add(card);
            }
        }

        private int TILE_COUNT_X = 8;
        private int TILE_COUNT_Y = 8;
        private int TILE_SIZE = 60;

        private int _playerCount = 0;
        private GamePlayer playerCount
        {
            get
            {
                _playerCount++;
                var count = _playerCount;
                return (GamePlayer)count;
            }
        }

        private TileBase[,] _gameTiles;

        private Dictionary<long, Player> _accountIdGamePlayerTable = new();
        private Dictionary<Player, List<PieceBase>> _playerGamePieceTable = new();

        // Timer
        private TimeSpan PrepareTimer = TimeSpan.FromSeconds(3);
        private TimeSpan GameTimer = TimeSpan.FromSeconds(30);
        private TimeSpan FinishTimer = TimeSpan.FromSeconds(5);

        private RoutineBackgroundThread<DragonBoardGameState> routine;
        private DragonBoardGameState gameState = DragonBoardGameState.Prepare;
            
        public DragonBoard(IFiber fiber)
        {
            routine = new RoutineBackgroundThread<DragonBoardGameState>(fiber, DragonBoardGameState.Prepare, 10L);
            routine.AddState(DragonBoardGameState.Prepare, Convert.ToInt64(PrepareTimer.TotalMilliseconds), PrepareBegin, PrepareUpdate, PrepareFinish);
            routine.AddState(DragonBoardGameState.PlayerA, Convert.ToInt64(GameTimer.TotalMilliseconds), PlayerABegin, PlayerAUpdate, PlayerAFinish);
            routine.AddState(DragonBoardGameState.PlayerB, Convert.ToInt64(GameTimer.TotalMilliseconds), PlayerBBegin, PlayerAUpdate, PlayerBFinish);
            routine.AddState(DragonBoardGameState.Ending, Convert.ToInt64(FinishTimer.TotalMilliseconds), () => routine.Stop(), null, null);

            routine.AddTrasitionState(DragonBoardGameState.Prepare, DragonBoardGameState.PlayerA);
            routine.AddTrasitionState(DragonBoardGameState.PlayerA, DragonBoardGameState.PlayerB);
            routine.AddTrasitionState(DragonBoardGameState.PlayerB, DragonBoardGameState.PlayerA);
        }
        protected override bool OnInitGame()
        {
            InitGameBoard();

            return true;
        }
        protected override void OnStartGame()
        {
            routine.Run();
            DebugLog.Log(gameState.ToString());
        }
        protected override void OnPlayerJoinGame(long accountId)
        {
            if (!_accountIdGamePlayerTable.ContainsKey(accountId))
            {
                GamePlayer gamePlayer = playerCount;
                Player player = new Player(gamePlayer);
                player.InitGameCard();
                _accountIdGamePlayerTable.Add(accountId, player);
                _playerGamePieceTable.Add(player, new());
            }

            if (_accountIdGamePlayerTable.Values.FirstOrDefault(player => player.GamePlayer == GamePlayer.PlayerA) != null 
                && _accountIdGamePlayerTable.Values.FirstOrDefault(player => player.GamePlayer == GamePlayer.PlayerB) != null)
            {
                OnStartGame();
            }
        }
        public override ErrorCode PlayerSyncGame(long accountId, Dictionary<int, object> playerMessage, Dictionary<int, object> gameMessage,
            out GameStaticInfo gameStaticInfo, out GameDynamicInfo gameDynamicInfo, out GameResultInfo gameResultInfo)
        {
            gameResultInfo = null;

            // Sync Game player Info
            if (gameMessage != null)
            {
                if (GetGamePlayer(accountId, out Player player))
                {
                    if (player.GamePlayer.ToString() == gameState.ToString())
                    {
                        if (gameMessage.RetrieveMessageItem(BoardGameMessage.DraggingPieceMessage, out Dictionary<int, object> draggingPieceMessage, false))
                            RetrieveDraggingPieceMessage(player, draggingPieceMessage);

                        if (gameMessage.RetrieveMessageItem(BoardGameMessage.DraggingCardMessage, out Dictionary<int, object> draggingCardMessage, false))
                            RetrieveDraggingCardMessage(player, draggingCardMessage);
                    }
                }
            }

            SetGameStaticData(out gameStaticInfo);
            SetGameDynamicData(out gameDynamicInfo);

            return ErrorCode.Success;
        }
        private bool GetGamePlayer(long accountId, out Player player)
        {
            return _accountIdGamePlayerTable.TryGetValue(accountId, out player);
        }
        private void RetrieveDraggingPieceMessage(Player player, Dictionary<int, object> draggingPieceMessage)
        {
            if (!draggingPieceMessage.RetrieveMessageItem(DraggingPieceMessage.PieceID, out int pieceId)) return;
            if (!draggingPieceMessage.RetrieveMessageItem(DraggingPieceMessage.PieceOnDrag, out bool pieceOnDrag)) return;
            if (!draggingPieceMessage.RetrieveMessageItem(DraggingPieceMessage.PiecePositionX, out double piecePositionX)) return;
            if (!draggingPieceMessage.RetrieveMessageItem(DraggingPieceMessage.PiecePositionY, out double piecePositionY)) return;
            if (!draggingPieceMessage.RetrieveMessageItem(DraggingPieceMessage.PieceTileX, out int pieceTileX)) return;
            if (!draggingPieceMessage.RetrieveMessageItem(DraggingPieceMessage.PieceTileY, out int pieceTileY)) return;

            if (!player.MyTurn)
                return;

            if (_playerGamePieceTable.TryGetValue(player, out List<PieceBase> gamePiece))
            {
                PieceBase piece = gamePiece.FirstOrDefault(p => p.PieceId == pieceId);
                if (piece == null)
                    return;

                if (player.MyTurn)
                {
                    if (!pieceOnDrag)
                    {
                        _gameTiles[piece.X, piece.Y].TilePiece = null;
                        _gameTiles[pieceTileX, pieceTileY].TilePiece = piece;
                    }

                    piece.OnDragging(pieceOnDrag, piecePositionX, piecePositionY, pieceTileX, pieceTileY);
                }
            }
        }
        private void RetrieveDraggingCardMessage(Player player, Dictionary<int, object> draggingCardMessage)
        {
            if (!draggingCardMessage.RetrieveMessageItem(DraggingCardMessage.CardID, out int cardId)) return;
            if (!draggingCardMessage.RetrieveMessageItem(DraggingCardMessage.CardPositionX, out double x)) return;
            if (!draggingCardMessage.RetrieveMessageItem(DraggingCardMessage.CardPositionY, out double y)) return;
            if (!draggingCardMessage.RetrieveMessageItem(DraggingCardMessage.CardTileX, out int cardTileX)) return;
            if (!draggingCardMessage.RetrieveMessageItem(DraggingCardMessage.CardTileY, out int cardTileY)) return;

            if (!player.MyTurn)
                return;

            if (_playerGamePieceTable.TryGetValue(player, out List<PieceBase> gamePiece))
            {
                CardBase card = player.CardsHand.Find(card => card.CardId == cardId);
                PieceBase piece = new PieceBase();
                piece.InitPiece(card.CardId, player.GamePlayer, x, y, cardTileX, cardTileY);
                PieceBase checkPiece = gamePiece.FirstOrDefault(p => p.PieceId == cardId);
                if (checkPiece != null)
                    return;

                _gameTiles[cardTileX, cardTileY].TilePiece = piece;
                gamePiece.Add(piece);
                player.CardsHand.Remove(card);
            }
        }
        public override ErrorCode PlayerNetworkInput(long accountId, PlayerNetworkInput playerNetworkInput)
        {
            return ErrorCode.Success;
        }
        public override ErrorCode RemoveGame()
        {
            return ErrorCode.Success;
        }

        // Game Static Data
        protected override void SetGameStaticData(out GameStaticInfo gameStaticInfo)
        {
            gameStaticInfo = _gameStaticInfo;
            SerializeTileData();

            _gameStaticInfo.AddStaticData(DragonBoardGameStaticData.GamePlayer, JsonConvert.SerializeObject(_accountIdGamePlayerTable));
        }

        // Game Dynamic Data
        protected override void SetGameDynamicData(out GameDynamicInfo gameDynamicInfo)
        {
            gameDynamicInfo = _gameDynamicInfo;
            _gameDynamicInfo.ServerNowTime = DateTime.Now.Ticks;

            var gamePieceMessage = _playerGamePieceTable.ToDictionary(x => (int)x.Key.GamePlayer, x => x.Value);
            _gameDynamicInfo.AddDynamicData(DragonBoardGameDynamicData.GamePiece, JsonConvert.SerializeObject(gamePieceMessage));

            var gameTimerMessage = new List<object>();
            gameTimerMessage.Add(gameState);
            gameTimerMessage.Add(GameTimer.TotalMilliseconds);
            _gameDynamicInfo.AddDynamicData(DragonBoardGameDynamicData.GameTimer, JsonConvert.SerializeObject(gameTimerMessage));
        }
        public override ErrorCode OnPlayerLeaveGame(long accountId)
        {
            return ErrorCode.Success;
        }
        private void InitGameBoard()
        {
            if (_gameTiles == null)
            {
                _gameTiles = new TileBase[TILE_COUNT_X, TILE_COUNT_Y];
                for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    _gameTiles[x, y] = new TileBase();
                    _gameTiles[x, y].InitTile(x, y, TileType.Land);
                }
            }
            else
            {
                for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                    ResetTileSetting(_gameTiles[x, y]);
            }
        }
     
        private void ResetTileSetting(TileBase tile)
        {
            tile.SetTileType(TileType.Land);
        }
        private void SerializeTileData()
        {
            var gameTilesData = new List<object>();
            for (int y = 0; y < TILE_COUNT_Y; y++)
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                gameTilesData.Add(JsonConvert.SerializeObject(_gameTiles[x, y]));
            }

            _gameStaticInfo.AddStaticData(DragonBoardGameStaticData.GameTiles, gameTilesData);
        }

        // Game Rotine State
        private void PrepareBegin()
        {
            gameState = DragonBoardGameState.Prepare;
        }
        private bool PrepareUpdate(long timeelapsed)
        {
            PrepareTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if (PrepareTimer.TotalMilliseconds < 0)
                PrepareTimer = TimeSpan.Zero;

            return true;
        }
        private void PrepareFinish()
        {
            DebugLog.Log("PrepareFinish");
        }
        private void PlayerABegin()
        {
            gameState = DragonBoardGameState.PlayerA;
            GameTimer = TimeSpan.FromSeconds(30);
            var playerA = _accountIdGamePlayerTable.Values.First(player => player.GamePlayer == GamePlayer.PlayerA);
            playerA.MyTurn = true;
            playerA.DrawCard();
        }
        private bool PlayerAUpdate(long timeelapsed)
        {
            GameTimer -= TimeSpan.FromMilliseconds(timeelapsed);
            if (GameTimer.TotalMilliseconds < 0)
                GameTimer = TimeSpan.Zero;


            return true;
        }
        private void PlayerAFinish()
        {
            DebugLog.Log("PlayerAFinish");
            var playerA = _accountIdGamePlayerTable.Values.First(player => player.GamePlayer == GamePlayer.PlayerA);
            playerA.MyTurn = false;

            if (_playerGamePieceTable.TryGetValue(playerA, out List<PieceBase> pieces))
            {
                pieces.ForEach(piece =>
                {
                    if (piece.onDrag)
                    {
                        var position = CalculatePosition(piece.PositionX, piece.PositionY);
                        _gameTiles[piece.X, piece.Y].TilePiece = null;
                        _gameTiles[position.x, position.y].TilePiece = piece;
                        piece.OnDragging(false, piece.PositionX, piece.PositionY, position.x, position.y);
                    }
                });
            }
        }
        private void PlayerBBegin()
        {
            gameState = DragonBoardGameState.PlayerB;
            GameTimer = TimeSpan.FromSeconds(30);
            var playerB = _accountIdGamePlayerTable.Values.First(player => player.GamePlayer == GamePlayer.PlayerB);
            playerB.MyTurn = true;
            playerB.DrawCard();
        }

        private void PlayerBFinish()
        {
            DebugLog.Log("PlayerBFinish");
            var playerB = _accountIdGamePlayerTable.Values.First(player => player.GamePlayer == GamePlayer.PlayerB);
            playerB.MyTurn = false;

            if (_playerGamePieceTable.TryGetValue(playerB, out List<PieceBase> pieces))
            {
                pieces.ForEach(piece =>
                {
                    if (piece.onDrag)
                    {
                        var position = CalculatePosition(piece.PositionX, piece.PositionY);
                        _gameTiles[piece.X, piece.Y].TilePiece = null;
                        _gameTiles[position.x, position.y].TilePiece = piece;
                        piece.OnDragging(false, piece.PositionX, piece.PositionY, position.x, position.y);
                    }
                });
            }
        }

        // Game
        public Vector2Int CalculatePosition(double x, double y){
            var intX = (int)(x / this.TILE_SIZE);
            if (intX > this.TILE_COUNT_X - 1)
                intX = TILE_COUNT_X - 1;

            var intY = (int)(y / this.TILE_SIZE);
            if (intY > this.TILE_COUNT_X - 1)
                intY = TILE_COUNT_X - 1;

            Vector2Int vector2Int = new Vector2Int(intX, intY);
            return vector2Int;
        }


    }
}