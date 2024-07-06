using BlazorChessMiddleware.DependencyInjectionsInterfaces;

namespace PlayerLobbies
{
    public class MyPlayerLobbies : IPlayerLobbies
    {
        public struct Lobby(string lobbyId)
        {
            public string lobbyId = lobbyId;

            public (string? id, bool isReady) playerOne;

            public (string? id, bool isReady) playerTwo;
        }

        /// <summary>
        /// Dictionary of keys of players' connection id and values of lobby id
        /// </summary>
        readonly Dictionary<string, string> _playerInLobbies = [];

        /// <summary>
        /// Dictionary of keys of lobbyId and values of Lobby struct
        /// </summary>
        readonly Dictionary<string, Lobby> _lobbies = [];

        /// <summary>
        /// Lock for updating _playerInLobbies and _lobbies
        /// </summary>
        readonly object lobbyLock = new();

        /// <summary>
        /// Updates player status in lobbies tracker
        /// </summary>
        /// <param name="connectionId">player connection id</param>
        /// <returns>PlayerReadyStatus after the update </returns>
        /// <exception cref="InvalidOperationException"></exception>
        public PlayersReadyStatus PlayerReady(string connectionId)
        {
            lock (lobbyLock)
            {
                if (!_playerInLobbies.TryGetValue(connectionId, out string? value))
                {
                    // player is not in lobby
                    return PlayersReadyStatus.PlayerNotInLobby;
                }

                Lobby lobby = _lobbies[value];

                if (lobby.playerOne.id == null || lobby.playerTwo.id == null)
                {
                    throw new InvalidOperationException("Player in lobby is null");
                }

                if (lobby.playerOne.id.Equals(connectionId))
                {
                    lobby.playerOne.isReady = true;
                }
                else if (lobby.playerTwo.id.Equals(connectionId))
                {
                    lobby.playerTwo.isReady = true;
                }
                else
                {
                    throw new InvalidOperationException("Player in not in lobby, but should be.");
                }

                _lobbies[value] = lobby;

                if (_lobbies[value].playerOne.isReady && _lobbies[value].playerTwo.isReady)
                {
                    // both players are ready for the game
                    return PlayersReadyStatus.BothPlayerReady;
                }

                return PlayersReadyStatus.PlayerReady;
            }
        }

        /// <summary>
        /// Creates a new lobby with a player
        /// </summary>
        /// <param name="connectionId">Connection id of the player</param>
        /// <param name="lobbyId">Lobby id for the new lobby</param>
        /// <returns>CreateLobbyStatus after the creation</returns>
        public CreateLobbyStatus CreateLobby(string connectionId, string lobbyId)
        {
            lock (lobbyLock)
            {
                if (_playerInLobbies.ContainsKey(connectionId))
                {
                    // player is already in different lobby
                    return CreateLobbyStatus.AlreadyInLobby;
                }

                if (_lobbies.ContainsKey(lobbyId))
                {
                    // lobby already exists
                    return CreateLobbyStatus.LobbyAlreadyExists;
                }

                _lobbies[lobbyId] = new Lobby
                {
                    playerOne = (connectionId, false),
                    playerTwo = (null, false),
                };

                _playerInLobbies[connectionId] = lobbyId;

                return CreateLobbyStatus.LobbyCreated;
            }
        }

        /// <summary>
        /// Gets player lobby from lobbies tracker
        /// </summary>
        /// <param name="connectionId">Connection id of the player</param>
        /// <returns>Lobby id or null if player is not in lobby</returns>
        public string? GetPlayerLobby(string connectionId)
        {
            lock (lobbyLock)
            {
                if (!_playerInLobbies.TryGetValue(connectionId, out string? result))
                {
                    return null;
                }

                return result;
            }
        }

        /// <summary>
        /// Adds player to the lobby tracker
        /// </summary>
        /// <param name="lobbyId">Id of the lobby</param>
        /// <param name="connectionId">Player to add to the lobby</param>
        /// <returns>JoinLobbyStatus after adding the player to the lobby</returns>
        public JoinLobbyStatus JoinLobby(string lobbyId, string connectionId)
        {
            lock (lobbyLock)
            {
                if (_playerInLobbies.ContainsKey(connectionId))
                {
                    // player is already in lobby
                    return JoinLobbyStatus.AlreadyInLobby;
                }

                if (!_lobbies.TryGetValue(lobbyId, out Lobby value))
                {
                    // lobby is not in lobby tracker
                    return JoinLobbyStatus.LobbyDoesNotExist;
                }

                if (value.playerTwo.id != null)
                {
                    // lobby is already filled up
                    return JoinLobbyStatus.LobbyIsFull;
                }

                var lobby = value;
                lobby.playerTwo = (connectionId, false);
                _lobbies[lobbyId] = lobby;

                _playerInLobbies[connectionId] = lobbyId;

                return JoinLobbyStatus.LobbyJoined;
            }
        }

        /// <summary>
        /// Updates trackers when a player disconnects
        /// </summary>
        /// <param name="connectionId">Connection id of the disconnected player</param>
        /// <returns>LobbyRemovePlayerStatus and the lobby id, lobby id is null if lobby wasnt found</returns>
        public (LobbyRemovePlayerStatus, string?) RemoveDisconnectedPlayer(string connectionId)
        {
            lock (_lobbies)
            {
                if (!_playerInLobbies.TryGetValue(connectionId, out var lobbyId))
                {
                    // player is not in the lobby
                    return (LobbyRemovePlayerStatus.PlayerNotInLobby, null);
                }

                // removes disconnected user
                _playerInLobbies.Remove(connectionId);

                if (!_lobbies.TryGetValue(lobbyId, out var lobby))
                {
                    // lobby was not found
                    return (LobbyRemovePlayerStatus.PlayerRemoved, null);
                }

                if (lobby.playerOne.id != null && lobby.playerOne.id == connectionId &&
                    lobby.playerTwo.id != null)
                {
                    // disconnected user is lobby creator
                    // second player moved to lobby creator
                    lobby.playerOne = new(lobby.playerTwo.id, false);
                    lobby.playerTwo = new(null, false);
                    return (LobbyRemovePlayerStatus.LobbyOwnerMoved, lobbyId);
                }

                return (LobbyRemovePlayerStatus.PlayerRemoved, lobbyId);
            }
        }

        /// <summary>
        /// Remove id from lobby tracker and move it to game tracker
        /// </summary>
        /// <param name="lobbyId">lobby of the id</param>
        /// <returns>Players' ids</returns>
        public (string playerOne, string playerTwo)? GameReady(string lobbyId)
        {
            lock (_lobbies)
            {
                if (!_lobbies.TryGetValue(lobbyId, out var lobby))
                {
                    return null;
                }

                if (lobby.playerOne.id != null && lobby.playerOne.isReady &&
                    lobby.playerTwo.id != null && lobby.playerTwo.isReady)
                {
                    _playerInLobbies.Remove(lobby.playerOne.id);
                    _playerInLobbies.Remove(lobby.playerTwo.id);
                    _lobbies.Remove(lobbyId);
                    return (lobby.playerOne.id, lobby.playerTwo.id);
                }
            }
            return null;
        }
    }
}
