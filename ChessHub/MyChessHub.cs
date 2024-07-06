using BlazorChessMiddleware;
using BlazorChessMiddleware.DependencyInjectionsInterfaces;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace ChessHub
{
    public class MyChessHub : Hub
    {
        /// <summary>
        /// Processes JoinLobby request from BlazorView
        /// </summary>
        /// <param name="lobbyId">Lobby id to join</param>
        /// <param name="_playerLobbies">Lobbies singleton</param>
        /// <exception cref="InvalidOperationException">Join request could not be resolved</exception>
        public async Task JoinLobby(string lobbyId, IPlayerLobbies _playerLobbies)
        {
            switch (_playerLobbies.JoinLobby(lobbyId, Context.ConnectionId))
            {
                case JoinLobbyStatus.AlreadyInLobby:
                    await Clients.Caller.SendAsync("ReceiveMessage", "You are already in lobby.");
                    break;

                case JoinLobbyStatus.LobbyDoesNotExist:
                    await Clients.Caller.SendAsync("ReceiveMessage", "Lobby does not exist.");
                    break;

                case JoinLobbyStatus.LobbyIsFull:
                    await Clients.Caller.SendAsync("ReceiveMessage", "Lobby is full.");
                    break;

                case JoinLobbyStatus.LobbyJoined:
                    // prioritize first player to get white pieces
                    await Clients.Group(lobbyId).SendAsync("LobbyReady", MiddlewareConstants.PIECE_COLOR.White.ToString(), lobbyId);
                    await Clients.Caller.SendAsync("LobbyReady", MiddlewareConstants.PIECE_COLOR.Black.ToString(), lobbyId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
                    break;

                default:
                    throw new InvalidOperationException("Join case not implemented");
            }
        }

        /// <summary>
        /// Processes CreateLobby request from BlazorView
        /// </summary>
        /// <param name="_playerLobbies">Lobbies singleton</param>
        /// <param name="_lobbyCodeGenerator">Code singleton</param>
        /// <exception cref="InvalidOperationException">Create request could not be resolved</exception>
        public async Task CreateLobby(IPlayerLobbies _playerLobbies, ILobbyCodeGenetor _lobbyCodeGenerator)
        {
            // checks if player is already in lobby
            if (_playerLobbies.GetPlayerLobby(Context.ConnectionId) != null)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", $"You are already in lobby.");
                return;
            }

            // requests unique lobby code
            var lobbyId = _lobbyCodeGenerator.GetLobbyCode();


            if (_playerLobbies.CreateLobby(Context.ConnectionId, lobbyId) == CreateLobbyStatus.LobbyCreated)
            {
                _playerLobbies.CreateLobby(Context.ConnectionId, lobbyId);
                await Groups.AddToGroupAsync(Context.ConnectionId, lobbyId);
                await Clients.Caller.SendAsync("ReceiveMessage", $"Your lobby code is {lobbyId}");
            }
            else
            {
                throw new InvalidOperationException("Could not create lobby.");
            }
        }

        /// <summary>
        /// Processes ReadyUp request from BlazorView
        /// </summary>
        /// <param name="_playerLobbies">Lobbies singleton</param>
        /// <param name="_gameManager">Games singleton</param>
        /// <exception cref="InvalidOperationException">Ready up request could not be resolved</exception>
        public async Task ReadyUp(IPlayerLobbies _playerLobbies, IGameManager _gameManager, IChessCore _chessCore)
        {
            // checks if lobby exists
            var lobby = _playerLobbies.GetPlayerLobby(Context.ConnectionId);

            if (lobby == null)
            {
                Console.WriteLine("Lobby not found");
                return;
            }

            switch (_playerLobbies.PlayerReady(Context.ConnectionId))
            {
                case PlayersReadyStatus.PlayerReady:
                    await Clients.Caller.SendAsync("PlayerReady");
                    await Clients.GroupExcept(lobby, Context.ConnectionId).SendAsync("OpponentReady");
                    break;
                case PlayersReadyStatus.BothPlayerReady:
                    var playersId = _playerLobbies.GameReady(lobby) ?? throw new InvalidOperationException("Both players have to be ready in this situation");

                    // starts the game
                    UserState userState = _gameManager.CreateGame(lobby, _chessCore);

                    // lobby creator starts with white
                    await Clients.Client(playersId.playerOne).SendAsync("MakeMove", JsonConvert.SerializeObject(userState));
                    // second player waits with black
                    await Clients.Client(playersId.playerTwo).SendAsync("UpdateBoard", JsonConvert.SerializeObject(userState));
                    break;
                default:
                    throw new InvalidOperationException("Unexpected Player Ready Operation");
            }
        }


        /// <summary>
        /// Processes BlazorView request when player disconnects
        /// </summary>
        /// <param name="gameId">Game Id if player was in one, otherwise null</param>
        /// <param name="_playerLobbies">Lobbies singleton</param>
        /// <param name="_gameManager">Games singleton</param>
        /// <returns></returns>
        public async Task PlayerDisconnected(string? gameId, IPlayerLobbies _playerLobbies, IGameManager _gameManager)
        {
            if (gameId == null)
            {
                // player was not in game
                var (status, lobbyId) = _playerLobbies.RemoveDisconnectedPlayer(Context.ConnectionId);

                // player was not in lobby
                if (lobbyId == null)
                {
                    return;
                }

                if (status == LobbyRemovePlayerStatus.LobbyOwnerMoved)
                {
                    // a second player is in a lobby, then assign him as a lobby owner
                    await Clients.GroupExcept(lobbyId, Context.ConnectionId).SendAsync("LobbyOwnerMoved", gameId);
                }
                else
                {
                    // a second player is in a lobby and already is a lobby owner
                    await Clients.GroupExcept(lobbyId, Context.ConnectionId).SendAsync("ReceiveMessage", "Opponent left.");
                }
            }
            else
            {
                // aborts game
                var status = _gameManager.RemovePlayer(gameId);

                if (status == GameRemovePlayerStatus.PlayerRemoved)
                {
                    // sends message to second player
                    await Clients.GroupExcept(gameId, Context.ConnectionId).SendAsync("GameEnded", "Opponent left.");
                }
            }
        }

        /// <summary>
        /// Processes OfferDraw request from BlazorView
        /// </summary>
        /// <param name="gameId">game Id</param>
        /// <returns></returns>
        public async Task OfferDraw(string gameId)
        {
            // notifies other player about the draw offer
            await Clients.GroupExcept(gameId, Context.ConnectionId).SendAsync("DrawOffered");
        }

        /// <summary>
        /// Processes ResolveDraw request from BlazorView
        /// </summary>
        /// <param name="gameId">game Id</param>
        /// <param name="drawAccepted">bool if player accepted the draw</param>
        public async Task ResolveDraw(string gameId, bool drawAccepted)
        {
            if (drawAccepted)
            {
                // ends game for both players
                await Clients.Group(gameId).SendAsync("GameEnded", "Draw accepted");
            }
            else
            {
                // notifies draw offerer that the game continues
                await Clients.GroupExcept(gameId, Context.ConnectionId).SendAsync("ReceiveMessage", "Draw denied");
            }
        }

        /// <summary>
        /// Processes PlayerForfeited request from BlazorView
        /// </summary>
        /// <param name="gameId">game Id</param>
        public async Task PlayerForfeited(string gameId)
        {
            // ends the game
            await Clients.Caller.SendAsync("GameEnded", "You forfeited.");
            // notifies the other player that the game ended
            await Clients.GroupExcept(gameId, Context.ConnectionId).SendAsync("GameEnded", "Opponent forfeited.");
        }

        /// <summary>
        /// Processes ProcessMove request from BlazorView
        /// </summary>
        /// <param name="gameId">Game Id in which move was made</param>
        /// <param name="move">Move to process</param>
        /// <param name="_gameManager">Games singleton</param>
        /// <returns></returns>
        public async Task ProcessMove(string gameId, string move, IGameManager _gameManager, IChessCore _chessCore)
        {
            BlazorChessMiddleware.UserState userState = _gameManager.ProcessMove(gameId, move, _chessCore);

            await Clients.Caller.SendAsync("UpdateBoard", JsonConvert.SerializeObject(userState));
            await Clients.GroupExcept(gameId, Context.ConnectionId).SendAsync("MakeMove", JsonConvert.SerializeObject(userState));

            switch (userState.GameState)
            {
                case MiddlewareConstants.GameStateEnum.Checkmate:
                    await Clients.Groups(gameId).SendAsync("GameEnded", "Checkmate");
                    break;
                case MiddlewareConstants.GameStateEnum.Check:
                    await Clients.Groups(gameId).SendAsync("ReceiveMessage", "Check");
                    break;
                case MiddlewareConstants.GameStateEnum.Draw:
                    await Clients.Groups(gameId).SendAsync("GameEnded", "Draw");
                    break;
                case MiddlewareConstants.GameStateEnum.Normal:
                    // does not require an action
                    break;
                default:
                    throw new InvalidOperationException("Game state enum is not implemented.");
            }
        }
    }
}
