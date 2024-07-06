namespace BlazorChessMiddleware.DependencyInjectionsInterfaces
{
    public enum GameRemovePlayerStatus { PlayerNotInGame, PlayerRemoved }
    public interface IGameManager
    {
        /// <summary>
        /// Adds a lobby id to games tracker    
        /// </summary>
        /// <param name="lobbyId">lobby id</param>
        /// <returns>User state after the game start</returns>
        public UserState CreateGame(string lobbyId, IChessCore chessCore);

        /// <summary>
        /// Processes move into the User state
        /// </summary>
        /// <param name="lobbyId">Unique game lobby id</param>
        /// <param name="serializedMove">Serialized move sent to Chesshub</param>
        /// <param name="chessCore">Injected Dependency Chesscore</param>
        /// <returns>User state after move processed</returns>
        public UserState ProcessMove(string lobbyId, string move, IChessCore chessCore);

        /// <summary>
        /// Removes player from games tracker
        /// </summary>
        /// <param name="gameId">Unique game id</param>
        /// <returns></returns>
        public GameRemovePlayerStatus RemovePlayer(string gameId);
    }
}
