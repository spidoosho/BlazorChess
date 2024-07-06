namespace BlazorChessMiddleware.DependencyInjectionsInterfaces
{
    public interface ILobbyCodeGenetor
    {
        /// <summary>
        /// Max lobby code
        /// </summary>
        public const int MAX_LOBBY_CODE = 90_000;

        /// <summary>
        /// Returns an unique lobby code index.
        /// </summary>
        /// <returns>Lobby code as a string</returns>
        public string GetLobbyCode();
    }
}
