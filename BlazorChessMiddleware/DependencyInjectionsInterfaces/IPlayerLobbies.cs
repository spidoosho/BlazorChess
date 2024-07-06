using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorChessMiddleware.DependencyInjectionsInterfaces
{
    public enum JoinLobbyStatus { AlreadyInLobby, LobbyDoesNotExist, LobbyIsFull, LobbyJoined }

    public enum CreateLobbyStatus { AlreadyInLobby, LobbyAlreadyExists, LobbyCreated }

    public enum LobbyRemovePlayerStatus { PlayerNotInLobby, PlayerRemoved, LobbyOwnerMoved }

    public enum PlayersReadyStatus { PlayerNotInLobby, PlayerReady, BothPlayerReady }

    public interface IPlayerLobbies
    {
        /// <summary>
        /// Adds player to the lobby tracker
        /// </summary>
        /// <param name="lobbyId">Id of the lobby</param>
        /// <param name="connectionId">Player to add to the lobby</param>
        /// <returns>JoinLobbyStatus after adding the player to the lobby</returns>
        public JoinLobbyStatus JoinLobby(string lobbyId, string connectionId);

        /// <summary>
        /// Gets player lobby from lobbies tracker
        /// </summary>
        /// <param name="connectionId">Connection id of the player</param>
        /// <returns>Lobby id or null if player is not in lobby</returns>
        public string? GetPlayerLobby(string connectionId);

        /// <summary>
        /// Creates a new lobby with a player
        /// </summary>
        /// <param name="connectionId">Connection id of the player</param>
        /// <param name="lobbyId">Lobby id for the new lobby</param>
        /// <returns>CreateLobbyStatus after the creation</returns>
        public CreateLobbyStatus CreateLobby(string connectionId, string lobbyId);

        /// <summary>
        /// Updates trackers when a player disconnects
        /// </summary>
        /// <param name="connectionId">Connection id of the disconnected player</param>
        /// <returns>LobbyRemovePlayerStatus and the lobby id, lobby id is null if lobby wasnt found</returns>
        public (LobbyRemovePlayerStatus Status, string? lobbyId) RemoveDisconnectedPlayer(string connectionId);

        /// <summary>
        /// Updates player status in lobbies tracker
        /// </summary>
        /// <param name="connectionId">player connection id</param>
        /// <returns>PlayerReadyStatus after the update </returns>
        public PlayersReadyStatus PlayerReady(string connectionId);

        /// <summary>
        /// Remove id from lobby tracker and move it to game tracker
        /// </summary>
        /// <param name="lobbyId">lobby of the id</param>
        /// <returns>Players' ids</returns>
        public (string playerOne, string playerTwo)? GameReady(string lobbyId);
    }
}
