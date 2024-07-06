using System.Collections.Concurrent;
using BlazorChessMiddleware;
using BlazorChessMiddleware.DependencyInjectionsInterfaces;
using Newtonsoft.Json;

namespace GameManager
{
    public class MyGameManager : IGameManager
    {
        private readonly ConcurrentDictionary<string, BoardState> games = [];

        /// <summary>
        /// Adds a lobby id to games tracker
        /// </summary>
        /// <param name="lobbyId">lobby id</param>
        /// <returns>User state after the game start</returns>
        public UserState CreateGame(string lobbyId, IChessCore chessCore)
        {
            var board = chessCore.GetStartBoard();
            var state = new BoardState(board, chessCore.GetRooksStartPositions(), chessCore.GetKingsStartPositions());
            state.possibleMoves = chessCore.GetPossibleMoves(state);

            games.TryAdd(lobbyId, state);

            return UserState.GetUserState(state);
        }

        /// <summary>
        /// Processes move into the User state
        /// </summary>
        /// <param name="lobbyId">Unique game lobby id</param>
        /// <param name="serializedMove">Serialized move sent to Chesshub</param>
        /// <param name="chessCore">Injected Dependency Chesscore</param>
        /// <returns>User state after move processed</returns>
        /// <exception cref="ArgumentException"></exception>
        public UserState ProcessMove(string lobbyId, string serializedMove, IChessCore chessCore)
        {
            if (!games.TryGetValue(lobbyId, out BoardState? game))
            {
                throw new ArgumentException("Lobby not found");
            }

            var newMove = ParseMove(serializedMove);
            if (!newMove.HasValue)
            {
                throw new ArgumentException("Could not parsed serialized marmove");
            }

            var updatedState = chessCore.ProcessMove(game, newMove.Value);

            return UserState.GetUserState(updatedState);
        }

        /// <summary>
        /// Removes player from games tracker
        /// </summary>
        /// <param name="gameId">Unique game id</param>
        /// <returns></returns>
        public GameRemovePlayerStatus RemovePlayer(string gameId)
        {
            if (!games.TryGetValue(gameId, out _))
            {
                return GameRemovePlayerStatus.PlayerNotInGame;
            }


            if (games.TryRemove(gameId, out _))
            {
                return GameRemovePlayerStatus.PlayerRemoved;
            }

            return GameRemovePlayerStatus.PlayerNotInGame;
        }

        /// <summary>
        /// Tries to parse serialized move
        /// </summary>
        /// <param name="serializedMove">Move to deserialize</param>
        /// <returns>Move or null if the move couldn't be deserialized</returns>
        private static Move? ParseMove(string serializedMove)
        {
            try
            {
                return JsonConvert.DeserializeObject<Move>(serializedMove);
            }
            catch
            {
                return null;
            }
        }
    }
}
