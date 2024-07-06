# Developer documentation

BlazorChess is a [ASP.NET](https://dotnet.microsoft.com/en-us/apps/aspnet) web app using [Blazor](https://learn.microsoft.com/cs-cz/aspnet/core/blazor/) and [SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr)

In this document you will get an overview of the project. In other documents you can read more about specific parts of the project.

## Modularity

Project is divided into 3 parts - BlazorView which handles user input and output; Dependency Injections (ChessCore, GameManager, LobbyCodeGenerator, PlayerLobbies) which defines specific backend implementations handling game logic and player management; ChessHub which connects BlazorView input and Dependency Injections outputs using BlazorChessMiddleWare. For example for April's fools day can ChessCore be replaced with different module, let's say AprilFoolsChessCore, which replaces all pawns for knights and BlazorChess would still perfectly work.

Each dependency injection can be easily replaced in [BlazorChess Program.cs](https://github.com/spiduso/BlazorChess/blob/main/BlazorChess/Program.cs). Implementations must follow the interface contracts defined in [BlazorChessMiddleware.DependencyInjectionsInterfaces](https://github.com/spiduso/BlazorChess/tree/main/BlazorChessMiddleware/DependencyInjectionsInterfaces).

The less fun modules can be replaced too. BlazorView can be replaced if dependency injections are carefully added and ChessHub correctly connected. It is recommended to replace BlazorView with ChessHub since they are intertwined by definition.

## ASP.NET Blazor

Blazor is a front-end web framework allowing client and server-side rendering and interactivity. Using ASP.NET Blazor also enables easy hosting and deploying on Azure servers.

## SignalR

In this project I use SignalR for communication between users and a server. SignalR assigns ConnectionId for each user connections. That means each website tab has its own ConnectionId, but when user is moved to another link on the page or refreshes the page, the ConnectionId changes. The disadvantage is that user must stay on the same page for entirety of the game and should not refresh the page or the game will be lost. However this approach allows two players to play on one device, since each player can play on different tabs. SignalR also provides Groups. It is a collection of connections associated with a name. Which is perfect for game handling, since ChessHub will assign one group for each game. Connections need to be manually added to Groups, but it handles removal of connections when disconnected and removal of empty Groups on its own.
