# Dependency Injections

GamesManager, PlayerLobbies and LobbyCodeGenerator could be unified into one dependency injection, but are separated in this project for better readability.

## ChessCore

ChessCore processes players' moves. It provides logic of the pieces and their interactions. Having ChessCore separate and on the server prevents user from cheating and also enables different logic implementations.

## GamesManager

GamesManager keeps tracks of ongoing games. Games are distinguished by their lobby ids. GamesManager stores game data in BoardState. It sends user move to ChessCore to be processed. GamesManager also handles when a player disconnects.

## PlayerLobbies

PlayerLobbies manages players' connection ids in lobbies. It handles lobby creation and player requests to join lobbies. PlayerLobbies also keep tracks if the players are ready the start the game or not.

## LobbyCodeGenerator

LobbyCodeGenerator has the easiest job. It just has to return a unique five digit lobby code that has not been used in a while. In a current implementation generator assumes small traffic and just returns an increment of the code from 00001 to 90000. After reaching the last code, it resets the code back to 00001.
