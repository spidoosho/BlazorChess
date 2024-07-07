# BlazorView and ChessHub

Since BlazorView and ChessHub is highly connected, I will describe them both in this document.

## BlazorView

Displays server data like chessboard with pieces to the user and sends user data to the server using hubConnection.

BlazorView handles methods that are called by or called to ChessHub (for example JoinLobby, CreateLobby, ProcessMove, OfferDraw, etc.).

BlazorView show chessboard using CSS and pieces using ASCII characters that depicts pieces. It uses Bootstrap modals for handling essential user decisions like choosing a promotion piece, accepting/declining opponents draw offer and game ends (checkmate, draw and resign).

BlazorView on its own does not provide any game logic, everything, including pieces positions and their possible moves, are given from ChessHub.

## ChessHub

ChessHub implements Hub class for communication with BlazorView. It logically connects dependency injections and processes their outputs and sends it to the BlazorView.

## BlazorChessMiddleware

ChessHub's best friend is BlazorChessMiddleware. Dependency injections must provide methods implementations for the interface contracts. Also it defines UserState which can be serialized to be sent to BlazorView and BoardState which defines ChessCore output.
