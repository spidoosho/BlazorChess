using BlazorChessMiddleware.DependencyInjectionsInterfaces;

namespace LobbyCodeGenerator
{
    public class MyLobbyCodeGenerator : ILobbyCodeGenetor
    {
        private int index = 0;
        private readonly object indexLock = new();

        /// <summary>
        /// Returns an lobby code index and then increments it. If index reaches MAX_LOBBY_CODE, resets to zero.
        /// </summary>
        /// <returns>Lobby code as a string</returns>
        public string GetLobbyCode()
        {
            lock (indexLock)
            {
                if (index > ILobbyCodeGenetor.MAX_LOBBY_CODE)
                {
                    index = 0;
                }

                return (++index).ToString("00000");
            }
        }
    }
}
