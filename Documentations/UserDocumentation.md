# User Documentation for BlazorChess

In this documentation you will learn how to play chess in BlazorChess!

## Before the game

Open two tabs of the website for you and your opponent.

### Create a lobby

Click on a button called "Create a lobby". Server should reply you with a lobby code. Send the lobby code to your opponent! Wait until your opponent joins the lobby

### Join a lobby

Enter a lobby code in the input field and click on a button called "Join". Now you have joined your opponent in the lobby!

### In the lobby

When both players join the lobby, you will be assigned a piece color. When you will be ready for a game, click on button called "Ready". When both of you clicked on ready, the game will start.

## In the game

During your move you will be able to click and see your possible moves for your pieces. When you will play your move, wait for your opponent move.

### Checks and checkmate

When your or opponent's king will be in danger, BlazorChess will highlight attacking pieces and a checked king. If the king is checkmated, the game will automatically end.

### Draws

Game will automatically detect every possible draw and ends the game.

Other than draws listed below, there is one more quality of life draw called Dead position. This draw is not implemented, but will result in a draw sooner or later, because if chessboard is in a dead position, one of the draws below will occur.

#### Stalemate

You or your opponent will be cornered and have no possible move.

#### Threefold repetition

The game is repetitive and a chessboard piece configuration will appear three times during the game.

#### Fifty move rule

The game does not advance and in 50 moves no pawn moves or captures occurred.

#### Mutual agreement

You or your opponent offers a draw by clicking on the button called "Offer a draw" and the second player agrees to the draw.

### Forfeit

One of the players can click on a "Forfeit" button and the game will immediately end. Player automatically forfeits if the player disconnects.

## After the game

If the game ends, the button "Go back to lobby" will appear for you to create or join another game!
