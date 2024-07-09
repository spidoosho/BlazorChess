using BlazorChessMiddleware;
using BlazorChessMiddleware.DependencyInjectionsInterfaces;

namespace ChessCore
{
    public class ClassicalChessCore : IChessCore
    {
        /// <summary>
        /// Defines a board 
        /// </summary>
        /// <returns>Chessboard to play on</returns>
        public char[,] GetStartBoard()
        {
            return ChessCoreConstants.DEFAULT_BOARD;
        }

        /// <summary>
        /// Gets rooks start position on the chessboard
        /// </summary>
        /// <returns>touple of colors of rooks position</returns>
        public (List<(int X, int Y)> White, List<(int X, int Y)> Black) GetRooksStartPositions()
        {
            return (ChessCoreConstants.WHITE_ROOKS_START_POS, ChessCoreConstants.BLACK_ROOKS_START_POS);
        }

        /// <summary>
        /// Gets kings start position on the chessboard
        /// </summary>
        /// <returns>touple of colors of king position</returns>
        public ((int X, int Y) White, (int X, int Y) Black) GetKingsStartPositions()
        {
            return (ChessCoreConstants.WHITE_KING_START_POS, ChessCoreConstants.BLACK_KING_START_POS);
        }

        /// <summary>
        /// Public non static method for the interface contract
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="lastMove">Last played move</param>
        /// <returns>Updated Board state</returns>
        public BoardState ProcessMove(BoardState state, Move lastMove)
        {
            return StaticProcessMove(state, lastMove);
        }

        /// <summary>
        /// Public non static method for the interface contract
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <returns>Dictionary of keys of pieces positions and values of their possible moves</returns>
        public Dictionary<(int X, int Y), List<(int X, int Y)>> GetPossibleMoves(BoardState state)
        {
            return StaticGetPossibleMoves(state);
        }

        /// <summary>
        /// Processes move into the state
        /// </summary>
        /// <param name="state">Board state before the move</param>
        /// <param name="lastMove">Last played move</param>
        /// <returns>Updated Board state</returns>
        private static BoardState StaticProcessMove(BoardState state, Move lastMove)
        {
            state.LastMove = UpdateMoveType(state.Board, state.LastMove, lastMove);
            state.FiftyMoveCounter = UpdateFiftyMoveRule(state.Board, lastMove, state.FiftyMoveCounter);
            state.Board = UpdateBoard(state.Board, state.LastMove);
            state.ThreeFoldCounter = UpdateThreeFoldRule(state.Board, state.FiftyMoveCounter, state.ThreeFoldCounter);
            state.NotMovedRooks = UpdateRooksTracker(state.Board, state.LastMove, state.NotMovedRooks);
            state.Kings = UpdateKingsTracker(state.Board, state.LastMove, state.Kings);

            // rotate colors on move
            state.ColorOnMove = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? MiddlewareConstants.PIECE_COLOR.Black : MiddlewareConstants.PIECE_COLOR.White;

            state.AttackingPieces = GetCheckingPositions(state.Board,
                state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? state.Kings.White.Position : state.Kings.Black.Position,
                state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? MiddlewareConstants.PIECE_COLOR.Black : MiddlewareConstants.PIECE_COLOR.White);


            if (state.FiftyMoveCounter >= 50 || state.ThreeFoldCounter.IsThreeFold || IsInsufficientMaterial(state.Board))
            {
                // force draws
                state.State = MiddlewareConstants.GameStateEnum.Draw;
                return state;
            }

            if (state.AttackingPieces.Count > 0)
            {
                state.State = MiddlewareConstants.GameStateEnum.Check;
            }
            else
            {
                state.State = MiddlewareConstants.GameStateEnum.Normal;
            }

            state.possibleMoves = StaticGetPossibleMoves(state);

            if (state.possibleMoves.Count == 0)
            {
                if (state.State == MiddlewareConstants.GameStateEnum.Check)
                {
                    // checkmate when player has no moves and is checked
                    state.State = MiddlewareConstants.GameStateEnum.Checkmate;
                }
                else
                {
                    state.State = MiddlewareConstants.GameStateEnum.Draw;
                }
            }

            return state;
        }

        /// <summary>
        /// Gets possible moves based on current Board state
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <returns>Dictionary of keys of pieces positions and values of their possible moves</returns>
        private static Dictionary<(int X, int Y), List<(int X, int Y)>> StaticGetPossibleMoves(BoardState state)
        {
            var result = new Dictionary<(int X, int Y), List<(int X, int Y)>>();

            var pieces = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR : ChessCoreConstants.BLACK_PIECES_CHAR_ARR;
            var defendingKingPosition = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? state.Kings.White.Position : state.Kings.Black.Position;
            var defendingKingChar = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White
                ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX] : ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX];


            for (int i = 0; i < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH; i++)
            {
                for (int j = 0; j < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH; j++)
                {
                    if (!pieces.Contains(state.Board[j, i]))
                    {
                        // skip tiles that does not contain player's piece
                        continue;
                    }

                    var moves = GetMoves(state, (j, i));
                    if (moves.Count == 0)
                    {
                        // no need for moves filtering
                        continue;
                    }

                    if (state.State == MiddlewareConstants.GameStateEnum.Check)
                    {
                        // if player is in check, filter only moves that removes checks
                        var filteredList = new List<(int X, int Y)>();

                        foreach (var move in moves)
                        {
                            var mockupDefendingKingPosition = defendingKingPosition;
                            if (state.Board[j, i] == defendingKingChar)
                            {
                                // if it is a king move create mockup king position
                                mockupDefendingKingPosition = move;
                            }

                            if (!IsStillCheck(state.Board, mockupDefendingKingPosition, new Move((j, i), move) { moveType = Move.MoveType.Normal }))
                            {
                                // move removed check
                                filteredList.Add(move);
                            }
                        }

                        if (filteredList.Count > 0)
                        {
                            // add filtered moves
                            result[(j, i)] = filteredList;
                        }
                    }
                    else
                    {
                        // add move
                        result[(j, i)] = moves;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Checks for draw by insufficient material
        /// </summary>
        /// <param name="board">Chessboard to check</param>
        /// <returns>If board is in draw by insufficient material</returns>
        private static bool IsInsufficientMaterial(char[,] board)
        {
            (int X, int Y)? blackBishopPos = null;
            (int X, int Y)? whiteBishopPos = null;

            int whitePiecesCounter = 0;
            int blackpiecesCounter = 0;

            // counts pieces on the board
            for (int j = 0; j < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH; j++)
            {
                for (int i = 0; i < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH; i++)
                {
                    if (board[i, j] == ChessCoreConstants.EMPTY_SQUARE)
                    {
                        // skip empty tile
                        continue;
                    }

                    if (board[i, j] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.BISHOP_INDEX] && !whiteBishopPos.HasValue)
                    {
                        // first white bishop found
                        whiteBishopPos = (i, j);
                        whitePiecesCounter++;
                    }
                    else if (board[i, j] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.BISHOP_INDEX] && !blackBishopPos.HasValue)
                    {
                        // first black bishop found
                        blackBishopPos = (i, j);
                        blackpiecesCounter++;
                    }
                    else if (board[i, j] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX] ||
                             board[i, j] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KNIGHT_INDEX])
                    {
                        // black king and knights counter
                        blackpiecesCounter++;
                    }
                    else if (board[i, j] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX] ||
                             board[i, j] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KNIGHT_INDEX])
                    {
                        // white king and knights counter
                        whitePiecesCounter++;
                    }
                    else
                    {
                        // if the piece is not king/bishop/knight or there are two bishops/knights then it is not drawable
                        return false;
                    }

                    if (whitePiecesCounter > 2 || blackpiecesCounter > 2)
                    {
                        // if there are three or more pieces of any color then it is not drawable
                        return false;
                    }
                }
            }

            if (blackpiecesCounter + whitePiecesCounter < 4)
            {
                // two kings and one other piece is a draw
                return true;
            }

            // if conditions caught all piece counts except four pieces
            if (whiteBishopPos.HasValue && blackBishopPos.HasValue &&
                Math.Abs(whiteBishopPos.Value.X + whiteBishopPos.Value.Y) % 2 == Math.Abs(blackBishopPos.Value.X + blackBishopPos.Value.Y) % 2)
            {
                // two kings and two bishops on the same color tile is a draw
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates fifty move rule counter
        /// </summary>
        /// <param name="boardBeforeMove">Chessboard before move</param>
        /// <param name="move">Current move</param>
        /// <param name="currentCounter">Current fifty move rule counter</param>
        /// <returns>Fifty move rule counter after the move</returns>
        private static int UpdateFiftyMoveRule(char[,] boardBeforeMove, Move move, int currentCounter)
        {
            if (boardBeforeMove[move.startPos.X, move.startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] ||
                boardBeforeMove[move.startPos.X, move.startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX])
            {
                // pawn move resets the counter
                return 0;
            }

            if (boardBeforeMove[move.endPos.X, move.endPos.Y] != ChessCoreConstants.EMPTY_SQUARE)
            {
                // capture resets the counter
                return 0;
            }

            // increment counter on other moves
            return currentCounter + 1;
        }

        /// <summary>
        /// Updates three fold tracker
        /// </summary>
        /// <param name="boardAfterMove">Chessboard after the move</param>
        /// <param name="currentFiftyMoveCounter">Current three fold tracker</param>
        /// <param name="currentFolds"></param>
        /// <returns>Updated three fold tracker</returns>
        private static BoardState.Folds UpdateThreeFoldRule(char[,] boardAfterMove, int currentFiftyMoveCounter, BoardState.Folds currentFolds)
        {
            var serializedBoard = BoardState.Folds.SerializeBoad(boardAfterMove);
            if (currentFiftyMoveCounter == 0)
            {
                // new chessboard configuration thanks to fifty move counter
                currentFolds = new BoardState.Folds(false, null);
                currentFolds.OneFold.Add(serializedBoard);

                return currentFolds;
            }

            if (currentFolds.OneFold.Remove(serializedBoard))
            {
                // board was removed from onefold
                currentFolds.TwoFold.Add(serializedBoard);
            }
            else if (currentFolds.TwoFold.Remove(serializedBoard))
            {
                // board was removed from twofold => three fold draw 
                currentFolds = new BoardState.Folds(false, null)
                {
                    IsThreeFold = true
                };
            }
            else
            {
                // new chessboard configuration
                currentFolds.OneFold.Add(serializedBoard);
            }

            return currentFolds;
        }

        /// <summary>
        /// Updates kings movement tracker
        /// </summary>
        /// <param name="board">Current chessboard</param>
        /// <param name="lastMove">Last played move</param>
        /// <param name="currentTracker">Current kings movement tracker</param>
        /// <returns>Updated kings movement tracker</returns>
        private static KingsTracker UpdateKingsTracker(char[,] board, Move lastMove, KingsTracker currentTracker)
        {
            // finds a player's on move king
            var friendlyKingChar = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(board[lastMove.endPos.X, lastMove.endPos.Y])
                     ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX] : ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX];

            if (board[lastMove.endPos.X, lastMove.endPos.Y] == friendlyKingChar)
            {
                // last move is a king move
                if (friendlyKingChar == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX])
                {
                    // update white king 
                    currentTracker.White.Position = lastMove.endPos;
                    currentTracker.White.HasMoved = true;
                }
                else
                {
                    // update black king
                    currentTracker.Black.Position = lastMove.endPos;
                    currentTracker.Black.HasMoved = true;
                }
            }

            return currentTracker;
        }

        /// <summary>
        /// Updates rooks on start positions tracker
        /// </summary>
        /// <param name="board">Current chessboard</param>
        /// <param name="lastMove">Last played move</param>
        /// <param name="currentTracker">Current rooks on start positions tracker</param>
        /// <returns>Updated rooks on start positions tracker</returns>
        private static NotMovedRooksTracker UpdateRooksTracker(char[,] board, Move lastMove, NotMovedRooksTracker currentTracker)
        {
            // finds a player's on move rook
            var friendlyRookChar = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(board[lastMove.endPos.X, lastMove.endPos.Y])
                ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.ROOK_INDEX] : ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.ROOK_INDEX];

            (int X, int Y)? rookPos = null;

            if (lastMove.moveType == Move.MoveType.Castling)
            {
                // finds in which direction rook moved during castling
                var rookMoveDirection = (lastMove.endPos.X - lastMove.startPos.X) / Math.Abs(lastMove.endPos.X - lastMove.startPos.X);

                // finds start rook position
                int x = lastMove.endPos.X + rookMoveDirection;
                while (x != 0 && x != MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH - 1)
                {
                    x += rookMoveDirection;
                }
                rookPos = (x, lastMove.endPos.Y);
            }
            else if (board[lastMove.endPos.X, lastMove.endPos.Y] == friendlyRookChar)
            {
                // regular rook move
                rookPos = (lastMove.startPos.X, lastMove.startPos.Y);
            }

            if (!rookPos.HasValue)
            {
                // last move was not a rook move
                return currentTracker;
            }

            if (friendlyRookChar == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.ROOK_INDEX])
            {
                // white rook move => removes rook from the tracker
                currentTracker.White.Remove(rookPos.Value);
            }
            else
            {
                // black rook move => removes rook from the tracker
                currentTracker.Black.Remove(rookPos.Value);
            }

            return currentTracker;
        }

        /// <summary>
        /// Adds move type to the move
        /// </summary>
        /// <param name="board">Current chessboard</param>
        /// <param name="lastMove">Last move before current move</param>
        /// <param name="move">Move without move type</param>
        /// <returns>Move with move type</returns>
        private static Move UpdateMoveType(char[,] board, Move lastMove, Move move)
        {
            var friendlyPawnChar = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(board[move.startPos.X, move.startPos.Y])
                ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] : ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX];

            var opponentPawnChar = friendlyPawnChar == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX]
                ? ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] : ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX];

            var friendlyKingChar = friendlyPawnChar == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX]
                ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX] : ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX];

            if (board[move.startPos.X, move.startPos.Y] == friendlyKingChar && Math.Abs(move.startPos.X - move.endPos.X) > 1)
            {
                // king moved two or more tiles => only possibility is castling
                move.moveType = Move.MoveType.Castling;
                return move;
            }
            else if (board[move.startPos.X, move.startPos.Y] == friendlyPawnChar)
            {
                var nextStep = move.endPos.Y + (move.endPos.Y - move.startPos.Y);

                if (board[lastMove.endPos.X, lastMove.endPos.Y] == opponentPawnChar && Math.Abs(lastMove.startPos.Y - lastMove.endPos.Y) == 2
                    && Math.Abs(move.startPos.X - move.endPos.X) == 1 && Math.Abs(move.startPos.Y - move.endPos.Y) == 1
                    && board[move.endPos.X, move.startPos.Y] == opponentPawnChar)
                {
                    // pawn moved diagonaly and last move was two tiles opponent pawn move next to the friendly pawn
                    move.moveType = Move.MoveType.EnPassant;
                    return move;
                }
                else if (nextStep < 0 || nextStep >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                {
                    // pawn moved to the edge of the board => promotion
                    move.moveType = Move.MoveType.Promotion;
                    return move;
                }
            }

            // no other special move type
            move.moveType = Move.MoveType.Normal;
            return move;
        }

        /// <summary>
        /// Updates board based on last move
        /// </summary>
        /// <param name="oldBoard">Chessboard before move update</param>
        /// <param name="newMove">Move to update to chessboard</param>
        /// <returns>Updated board</returns>
        /// <exception cref="ArgumentException"></exception>
        private static char[,] UpdateBoard(char[,] oldBoard, Move newMove)
        {
            var board = oldBoard;

            switch (newMove.moveType)
            {
                case Move.MoveType.Normal:
                    // updates start position and end position
                    board[newMove.endPos.X, newMove.endPos.Y] = board[newMove.startPos.X, newMove.startPos.Y];
                    board[newMove.startPos.X, newMove.startPos.Y] = ChessCoreConstants.EMPTY_SQUARE;
                    break;
                case Move.MoveType.EnPassant:
                    board[newMove.endPos.X, newMove.endPos.Y] = board[newMove.startPos.X, newMove.startPos.Y];
                    board[newMove.startPos.X, newMove.startPos.Y] = ChessCoreConstants.EMPTY_SQUARE;

                    // also updates captured opponent pawn tile
                    board[newMove.endPos.X, newMove.startPos.Y] = ChessCoreConstants.EMPTY_SQUARE;
                    break;
                case Move.MoveType.Castling:
                    // updates a king start and end position
                    board[newMove.endPos.X, newMove.endPos.Y] = board[newMove.startPos.X, newMove.startPos.Y];
                    board[newMove.startPos.X, newMove.startPos.Y] = ChessCoreConstants.EMPTY_SQUARE;

                    // finds and updates rook start and end position
                    var rookMoveDirection = (newMove.endPos.X - newMove.startPos.X) / Math.Abs(newMove.endPos.X - newMove.startPos.X);

                    // find default rook position
                    int x = newMove.endPos.X + rookMoveDirection;
                    while (x != 0 && x != MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH - 1)
                    {
                        x += rookMoveDirection;
                    }
                    board[newMove.startPos.X + rookMoveDirection, newMove.endPos.Y] = board[x, newMove.endPos.Y];
                    board[x, newMove.endPos.Y] = ChessCoreConstants.EMPTY_SQUARE;

                    break;
                case Move.MoveType.Promotion:
                    if (!newMove.promotionCharPiece.HasValue)
                        throw new ArgumentException("PromotionCharPiece is null when promoting.");

                    // end position replaced with the chosen promotion piece
                    board[newMove.endPos.X, newMove.endPos.Y] = newMove.promotionCharPiece.Value;
                    board[newMove.startPos.X, newMove.startPos.Y] = ChessCoreConstants.EMPTY_SQUARE;
                    break;
                default:
                    throw new ArgumentException($"Unexpected MoveType: {newMove.moveType}");
            }

            return board;
        }

        /// <summary>
        /// Get moves for piece on the startPos
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="startPos">Position of a piece to get moves</param>
        /// <returns>Moves for the piece on the startPos</returns>
        /// <exception cref="ArgumentException"></exception>
        private static List<(int X, int Y)> GetMoves(BoardState state, (int X, int Y) startPos)
        {
            var result = new List<(int X, int Y)>();
            if (startPos.X < 0 || startPos.X >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || startPos.Y < 0 || startPos.Y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH ||
                state.Board[startPos.X, startPos.Y] == ChessCoreConstants.EMPTY_SQUARE)
            {
                // if startPos is out of the chessboard or no piece is on the startPos then returns no moves
                return result;
            }

            // used if statements instead of switch statement because switch cases cannot be static readonly variables
            if (state.Board[startPos.X, startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] ||
                state.Board[startPos.X, startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX])
            {
                result = GetPawnMoves(state, (startPos.X, startPos.Y));
            }
            else if (state.Board[startPos.X, startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.ROOK_INDEX] ||
                     state.Board[startPos.X, startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.ROOK_INDEX])
            {
                result = GetRookMoves(state, (startPos.X, startPos.Y));
            }
            else if (state.Board[startPos.X, startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KNIGHT_INDEX] ||
                     state.Board[startPos.X, startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KNIGHT_INDEX])
            {
                result = GetKnightMoves(state, (startPos.X, startPos.Y));
            }
            else if (state.Board[startPos.X, startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.BISHOP_INDEX] ||
                     state.Board[startPos.X, startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.BISHOP_INDEX])
            {
                result = GetBishopMoves(state, (startPos.X, startPos.Y));
            }
            else if (state.Board[startPos.X, startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX] ||
                     state.Board[startPos.X, startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.KING_INDEX])
            {
                result = GetKingMoves(state, (startPos.X, startPos.Y));
            }
            else if (state.Board[startPos.X, startPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.QUEEN_INDEX] ||
                     state.Board[startPos.X, startPos.Y] == ChessCoreConstants.BLACK_PIECES_CHAR_ARR[ChessCoreConstants.QUEEN_INDEX])
            {
                result = GetQueenMoves(state, (startPos.X, startPos.Y));
            }
            else
            {
                throw new ArgumentException("Character on startPos is not valid.");
            }

            return result;
        }

        /// <summary>
        /// Gets non self-checking pawn moves
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="pawnPos">Pawn position to get moves</param>
        /// <returns>Moves for non self-checking pawn in pawnPos</returns>
        private static List<(int X, int Y)> GetPawnMoves(BoardState state, (int X, int Y) pawnPos)
        {
            var result = new List<(int X, int Y)>();
            // y-axis direction is dependent on pawn color
            var yDirection = state.Board[pawnPos.X, pawnPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] ? 1 : -1;
            // y-axis start pawn column is dependent on pawn color
            var startCol = state.Board[pawnPos.X, pawnPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] ? ChessCoreConstants.WHITE_PAWN_START_COL : ChessCoreConstants.BLACK_PAWN_START_COL;

            var opponentCharPieces = state.Board[pawnPos.X, pawnPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.PAWN_INDEX] ?
                ChessCoreConstants.BLACK_PIECES_CHAR_ARR : ChessCoreConstants.WHITE_PIECES_CHAR_ARR;
            var opponentPawn = opponentCharPieces[0];

            var diagonalCaptureDirections = new int[] { 1, -1 };

            int x;
            int y;

            foreach (var xDirection in diagonalCaptureDirections)
            {
                // reuses self-check calculation because capture/en passant in same xDirection will have the same self-check result
                bool? isMoveSelfCheck = null;

                x = pawnPos.X + xDirection;
                y = pawnPos.Y + yDirection;

                // en passant
                if (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH &&
                    state.Board[x, pawnPos.Y] == opponentPawn &&
                    Math.Abs(state.LastMove.startPos.Y - state.LastMove.endPos.Y) == 2 && x == state.LastMove.endPos.X)
                {
                    isMoveSelfCheck = DoesMoveCreateSelfCheck(state, new Move((pawnPos.X, pawnPos.Y), (x, y)));
                    if (!isMoveSelfCheck.Value)
                    {
                        // adds to result because move is not self-checking
                        result.Add((x, y));
                    }
                }

                // basic capture
                x = pawnPos.X + xDirection;
                y = pawnPos.Y + yDirection;
                if (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH
                    && opponentCharPieces.Contains(state.Board[x, y]))
                {
                    if (isMoveSelfCheck.HasValue)
                    {
                        // self-check was already calculated in en-passant
                        if (!isMoveSelfCheck.Value)
                        {
                            // adds to result because move is not self-checking
                            result.Add((x, y));
                        }
                    }
                    else
                    {
                        if (!DoesMoveCreateSelfCheck(state, new Move((pawnPos.X, pawnPos.Y), (x, y))))
                        {
                            // adds to result because move is not self-checking
                            result.Add((x, y));
                        }
                    }
                }
            }

            // one tile vertical step
            x = pawnPos.X;
            y = pawnPos.Y + yDirection;
            if (y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && state.Board[x, y] == ChessCoreConstants.EMPTY_SQUARE &&
                !DoesMoveCreateSelfCheck(state, new Move((pawnPos.X, pawnPos.Y), (x, y))))
            {
                result.Add((x, y));

                // two tile vertical steps
                x = pawnPos.X;
                y = pawnPos.Y + 2 * yDirection;
                if (pawnPos.Y == startCol && state.Board[x, y] == ChessCoreConstants.EMPTY_SQUARE)
                {
                    // if one tile step does not self-check, then two step will not self check too
                    result.Add((x, y));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets non self-checking rook moves
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="rookPos">Rook position to get moves</param>
        /// <returns>Moves for non self-checking rook in rookPos</returns>
        private static List<(int X, int Y)> GetRookMoves(BoardState state, (int X, int Y) rookPos)
        {
            var result = new List<(int X, int Y)>();
            // only rook direction moves are horizontal and vertical
            var increments = new (int X, int Y)[] { (0, 1), (1, 0), (-1, 0), (0, -1) };
            var oppositeCharPieces = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(state.Board[rookPos.X, rookPos.Y]) ?
                ChessCoreConstants.BLACK_PIECES_CHAR_ARR : ChessCoreConstants.WHITE_PIECES_CHAR_ARR;

            foreach (var (X, Y) in increments)
            {
                // checks each rook direction move increment

                if (X == 0 && rookPos.Y + Y >= 0 && rookPos.Y + Y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                {
                    // vertical move
                    int y = rookPos.Y + Y;
                    if (oppositeCharPieces.Contains(state.Board[rookPos.X, y]))
                    {
                        // tile contains opponent piece
                        if (!DoesMoveCreateSelfCheck(state, new Move((rookPos.X, rookPos.Y), (rookPos.X, y))))
                        {
                            // adds capture move
                            result.Add((rookPos.X, y));
                        }
                    }
                    else if (state.Board[rookPos.X, y] == ChessCoreConstants.EMPTY_SQUARE)
                    {
                        // tile is empty
                        if (DoesMoveCreateSelfCheck(state, new Move((rookPos.X, rookPos.Y), (rookPos.X, y))))
                        {
                            continue;
                        }

                        result.Add((rookPos.X, y));
                        y += Y;
                        while (y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && state.Board[rookPos.X, y] == ChessCoreConstants.EMPTY_SQUARE)
                        {
                            // adds moves until the next increment is not empty
                            result.Add((rookPos.X, y));
                            y += Y;
                        }

                        if (y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && oppositeCharPieces.Contains(state.Board[rookPos.X, y]))
                        {
                            // adds move because the non-empty tile is opponent piece
                            result.Add((rookPos.X, y));
                        }
                    }
                }
                else if (Y == 0 && rookPos.X + X >= 0 && rookPos.X + X < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                {
                    // horizontal move

                    int x = rookPos.X + X;
                    if (oppositeCharPieces.Contains(state.Board[x, rookPos.Y]))
                    {
                        // tile contains opponent piece
                        if (!DoesMoveCreateSelfCheck(state, new Move((rookPos.X, rookPos.Y), (x, rookPos.Y))))
                        {
                            // adds capture move
                            result.Add((x, rookPos.Y));
                        }
                    }
                    else if (state.Board[x, rookPos.Y] == ChessCoreConstants.EMPTY_SQUARE)
                    {
                        // tile is empty
                        if (DoesMoveCreateSelfCheck(state, new Move((rookPos.X, rookPos.Y), (x, rookPos.Y))))
                        {
                            continue;
                        }

                        result.Add((x, rookPos.Y));
                        x += X;
                        while (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && state.Board[x, rookPos.Y] == ChessCoreConstants.EMPTY_SQUARE)
                        {
                            // adds moves until the next increment is not empty
                            result.Add((x, rookPos.Y));
                            x += X;
                        }

                        if (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && oppositeCharPieces.Contains(state.Board[x, rookPos.Y]))
                        {
                            // adds move because the non-empty tile is opponent piece
                            result.Add((x, rookPos.Y));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets non self-checking knight moves
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="knightPos">Knight position to get knight</param>
        /// <returns>Moves for non self-checking knight in knightPos</returns>
        private static List<(int X, int Y)> GetKnightMoves(BoardState state, (int X, int Y) knightPos)
        {
            var result = new List<(int X, int Y)>();
            // only knight possible shifts in L shape
            int[] shifts = [1, 2, -1, -2];
            var friendlyCharPieces = state.Board[knightPos.X, knightPos.Y] == ChessCoreConstants.WHITE_PIECES_CHAR_ARR[ChessCoreConstants.KNIGHT_INDEX] ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR : ChessCoreConstants.BLACK_PIECES_CHAR_ARR;
            bool isNotSelfCheckMove = false; // self-check needs to be check once for each knight move thanks to the unusual L shape move

            foreach (int x in shifts)
            {
                foreach (int y in shifts)
                {
                    (int X, int Y) possibleMove = (knightPos.X + x, knightPos.Y + y);
                    if (Math.Abs(x) == Math.Abs(y) || possibleMove.X >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || possibleMove.X < 0 ||
                        possibleMove.Y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || possibleMove.Y < 0 ||
                        friendlyCharPieces.Contains(state.Board[possibleMove.X, possibleMove.Y]))
                    {
                        // skips non L-shape moves, outside of the chessboard moves or moves to the tile with friendly piece
                        continue;
                    }

                    if (isNotSelfCheckMove || !DoesMoveCreateSelfCheck(state, new Move((knightPos.X, knightPos.Y), (possibleMove.X, possibleMove.Y))))
                    {
                        // adds non self-check move
                        result.Add((possibleMove.X, possibleMove.Y));
                        isNotSelfCheckMove = true;
                    }
                    else
                    {
                        // returns empty list
                        return result;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Gets non self-checking bishop moves
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="bishopPos">Bishop position to get bishop</param>
        /// <returns>Moves for non self-checking bishop in bishopPos</returns>
        private static List<(int X, int Y)> GetBishopMoves(BoardState state, (int X, int Y) bishopPos)
        {
            var result = new List<(int X, int Y)>();
            var opponentCharPieces = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(state.Board[bishopPos.X, bishopPos.Y]) ? ChessCoreConstants.BLACK_PIECES_CHAR_ARR : ChessCoreConstants.WHITE_PIECES_CHAR_ARR;

            // bishop can move only in diagonal
            int[] directions = [1, -1];

            foreach (int xDirection in directions)
            {
                foreach (int yDirections in directions)
                {
                    var x = bishopPos.X + xDirection;
                    var y = bishopPos.Y + yDirections;

                    if (x < 0 || x >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || y < 0 || y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                    {
                        // skips outside of the chessboard move
                        continue;
                    }

                    if (opponentCharPieces.Contains(state.Board[x, y]))
                    {
                        // potentional capture move

                        if (!DoesMoveCreateSelfCheck(state, new Move((bishopPos.X, bishopPos.Y), (x, y))))
                        {
                            // adds non self-check capture move
                            result.Add((x, y));
                        }
                    }
                    else if (state.Board[x, y] == ChessCoreConstants.EMPTY_SQUARE)
                    {
                        // potentional move on empty tile

                        if (DoesMoveCreateSelfCheck(state, new Move((bishopPos.X, bishopPos.Y), (x, y))))
                        {
                            // skips direction because it move self-checks
                            continue;
                        }

                        result.Add((x, y));

                        x += xDirection;
                        y += yDirections;
                        while (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && state.Board[x, y] == ChessCoreConstants.EMPTY_SQUARE)
                        {
                            // checks whole diagonal while it is empty tile
                            result.Add((x, y));
                            x += xDirection;
                            y += yDirections;
                        }

                        if (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && opponentCharPieces.Contains(state.Board[x, y]))
                        {
                            // non-empty tile is an opponent piece
                            result.Add((x, y));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets non self-checking king moves
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="kingPos">King position to get king</param>
        /// <returns>Moves for non self-checking king in kingpPos</returns>
        private static List<(int X, int Y)> GetKingMoves(BoardState state, (int X, int Y) kingPos)
        {
            var result = new List<(int X, int Y)>();
            var friendlyCharPieces = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(state.Board[kingPos.X, kingPos.Y]) ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR : ChessCoreConstants.BLACK_PIECES_CHAR_ARR;

            // king can move in every direction
            int[] directions = [1, -1, 0];

            foreach (int xDirection in directions)
            {
                foreach (int yDirections in directions)
                {
                    var x = kingPos.X + xDirection;
                    var y = kingPos.Y + yDirections;

                    if ((xDirection == 0 && yDirections == 0) || x < 0 || x >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || y < 0 || y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH ||
                        friendlyCharPieces.Contains(state.Board[x, y]))
                    {
                        // skips moves outside of chessboard or move on a tile with friendly piece
                        continue;
                    }

                    // creates mockup board to check for safe moves
                    var mockupBoard = new char[MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH, MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH];
                    Array.Copy(state.Board, mockupBoard, state.Board.Length);
                    mockupBoard[x, y] = mockupBoard[kingPos.X, kingPos.Y];
                    mockupBoard[kingPos.X, kingPos.Y] = ChessCoreConstants.EMPTY_SQUARE;

                    if (GetCheckingPositions(mockupBoard, (x, y), state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? MiddlewareConstants.PIECE_COLOR.Black : MiddlewareConstants.PIECE_COLOR.White).Count == 0)
                    {
                        // adds safe move
                        result.Add((x, y));
                    }
                }
            }

            // castling
            var friendlyKing = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? state.Kings.White : state.Kings.Black;
            var friendlyNotMovedRooks = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? state.NotMovedRooks.White : state.NotMovedRooks.Black;

            if (!friendlyKing.HasMoved && friendlyNotMovedRooks.Count > 0)
            {
                // checks for castling move with unmoved king and rooks

                foreach (var rookPos in friendlyNotMovedRooks)
                {
                    bool isPathClear = true;
                    int step = (rookPos.X - kingPos.X) / Math.Abs(rookPos.X - kingPos.X);
                    for (int x = kingPos.X + step; x != rookPos.X && isPathClear; x += step)
                    {
                        if (state.Board[x, kingPos.Y] != ChessCoreConstants.EMPTY_SQUARE)
                        {
                            // there is a piece between the rook and the king
                            isPathClear = false;
                        }
                    }

                    if (!isPathClear)
                    {
                        continue;
                    }

                    for (int x = kingPos.X + step; x != rookPos.X && isPathClear; x += step)
                    {
                        // creates mockup board for tiles in which direction the king has to move for safe castling
                        var mockupBoard = new char[MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH, MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH];
                        Array.Copy(state.Board, mockupBoard, state.Board.Length);
                        mockupBoard[x, kingPos.Y] = mockupBoard[kingPos.X, kingPos.Y];
                        mockupBoard[kingPos.X, kingPos.Y] = ChessCoreConstants.EMPTY_SQUARE;

                        if (GetCheckingPositions(mockupBoard, (x, kingPos.Y), state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? MiddlewareConstants.PIECE_COLOR.Black : MiddlewareConstants.PIECE_COLOR.White).Count != 0)
                        {
                            // opponent piece is attacking tile needed for castling
                            isPathClear = false;
                        }
                    }

                    if (isPathClear)
                    {
                        // castling is a safe move
                        result.Add((kingPos.X + 2 * step, kingPos.Y));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets non self-checking queen moves
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="queenPos">Queen position to get queen</param>
        /// <returns>Moves for non self-checking queen in queenpPos</returns>
        private static List<(int X, int Y)> GetQueenMoves(BoardState state, (int X, int y) queenPos)
        {
            var result = new List<(int X, int Y)>();

            // queen moves are combination of rook and bishop moves
            result.AddRange(GetRookMoves(state, queenPos));
            result.AddRange(GetBishopMoves(state, queenPos));

            return result;
        }

        /// <summary>
        /// Simplifies direction for comparing with other directieons
        /// </summary>
        /// <param name="direction">Direction for simplification</param>
        /// <returns>Simplified direction</returns>
        private static (int X, int Y) SimplifyDirection((int X, int Y) direction)
        {
            if (direction.X == direction.Y || direction.X == 0 || direction.Y == 0)
            {
                // diagonal, vertical or horizontal directions can be simplified to (1/-1, 0), (0, 1/-1) or (1/-1, 1/-1) direction
                direction.X = direction.X == 0 ? 0 : direction.X / (Math.Abs(direction.X)); // 0 or -1 or 1
                direction.Y = direction.Y == 0 ? 0 : direction.Y / (Math.Abs(direction.Y));
            }
            else if (direction.X < direction.Y)
            {
                if (direction.Y % direction.X == 0)
                {
                    // Y is divisible by X
                    direction.Y /= Math.Abs(direction.X);
                    direction.X /= Math.Abs(direction.X);
                }
            }
            else if (direction.X > direction.Y)
            {
                if (direction.X % direction.Y == 0)
                {
                    // X is divisible by Y
                    direction.X /= Math.Abs(direction.Y);
                    direction.Y /= Math.Abs(direction.Y);
                }
            }

            return direction;
        }

        /// <summary>
        /// Controls if given move in given state creates a self-check.
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <param name="move">Last played move</param>
        /// <returns></returns>
        private static bool DoesMoveCreateSelfCheck(BoardState state, Move move)
        {
            // Does not check for knight or pawn self-checks, since these pieces cannot create a self-check from non-check after opponent move

            (int X, int Y) friendlyKingPos = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(state.Board[move.startPos.X, move.startPos.Y]) ?
                state.Kings.White.Position : state.Kings.Black.Position;

            var opponentCharPieces = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(state.Board[move.startPos.X, move.startPos.Y]) ?
                ChessCoreConstants.BLACK_PIECES_CHAR_ARR : ChessCoreConstants.WHITE_PIECES_CHAR_ARR;
            var opponentQueenChar = opponentCharPieces[ChessCoreConstants.QUEEN_INDEX];
            var opponentRookChar = opponentCharPieces[ChessCoreConstants.ROOK_INDEX];
            var opponentBishopChar = opponentCharPieces[ChessCoreConstants.BISHOP_INDEX];

            (int X, int Y) moveDirection = SimplifyDirection((move.endPos.X - move.startPos.X, move.endPos.Y - move.startPos.Y));
            (int X, int Y) kingDirection = SimplifyDirection((friendlyKingPos.X - move.startPos.X, friendlyKingPos.Y - move.startPos.Y));

            if (moveDirection.X == kingDirection.X * (-1) && moveDirection.Y == kingDirection.Y * (-1))
            {
                // Direction of the move is equal to direction of king to the moved piece => does not create self check move
                return false;
            }

            int x = move.startPos.X + kingDirection.X;
            int y = move.startPos.Y + kingDirection.Y;
            while (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && (x, y) != friendlyKingPos)
            {
                if (state.Board[x, y] != ChessCoreConstants.EMPTY_SQUARE)
                {
                    // there is a piece in between king and move start position => cannot be self-check
                    return false;
                }
                x += kingDirection.X;
                y += kingDirection.Y;
            }

            // direction of move is not equal to direction of king to piece -> check for checks in the king to piece direction
            var possibleAttackingPieces = new List<char>();
            switch (kingDirection)
            {
                case (0, 1):
                case (1, 0):
                case (0, -1):
                case (-1, 0):
                    // possible horizontal or vertical check
                    possibleAttackingPieces.Add(opponentQueenChar); // queen 
                    possibleAttackingPieces.Add(opponentRookChar); // rook
                    break;
                case (1, 1):
                case (1, -1):
                case (-1, 1):
                case (-1, -1):
                    // possible diagonal check
                    possibleAttackingPieces.Add(opponentBishopChar);  // bishop
                    possibleAttackingPieces.Add(opponentQueenChar); // queen 
                    break;

            }

            x = move.startPos.X + kingDirection.X * (-1);
            y = move.startPos.Y + kingDirection.Y * (-1);
            while (x >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y >= 0 && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
            {
                if (possibleAttackingPieces.Contains(state.Board[x, y]))
                {
                    // there is an attacking piece in the direction
                    return true;
                }
                if (state.Board[x, y] != ChessCoreConstants.EMPTY_SQUARE)
                {
                    // there is a non attacking piece in the direction
                    return false;
                }

                x += kingDirection.X * (-1);
                y += kingDirection.Y * (-1);
            }

            // there is no piece in the direction
            return false;
        }

        /// <summary>
        /// Get every king-checking piece positions
        /// </summary>
        /// <param name="board">Current chessboard</param>
        /// <param name="defendingKingPos">Defending king positions</param>
        /// <param name="attackingColor">Attacking player color</param>
        /// <returns>List of all king-checking piece positions</returns>
        private static List<(int X, int Y)> GetCheckingPositions(char[,] board, (int X, int Y) defendingKingPos, MiddlewareConstants.PIECE_COLOR attackingColor)
        {
            var checkingPos = new List<(int X, int Y)>();

            var attackingCharPieces = attackingColor == MiddlewareConstants.PIECE_COLOR.White ? ChessCoreConstants.WHITE_PIECES_CHAR_ARR : ChessCoreConstants.BLACK_PIECES_CHAR_ARR;
            char attackingPawnChar = attackingCharPieces[ChessCoreConstants.PAWN_INDEX];
            char attackingRookChar = attackingCharPieces[ChessCoreConstants.ROOK_INDEX];
            char attackingKnightChar = attackingCharPieces[ChessCoreConstants.KNIGHT_INDEX];
            char attackingBishopChar = attackingCharPieces[ChessCoreConstants.BISHOP_INDEX];
            char attackingQueenChar = attackingCharPieces[ChessCoreConstants.QUEEN_INDEX];

            // pawn checks
            // attacking direction is based on the piece color
            var attackingPawnDir = attackingColor == MiddlewareConstants.PIECE_COLOR.White ? -1 : 1;
            foreach (var pawnDirection in new int[] { -1, 1 })
            {
                // pawn attacks only in diagonal in the attacking direction
                int x = defendingKingPos.X + pawnDirection;
                int y = defendingKingPos.Y + attackingPawnDir;

                if (x >= 0 && y >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && board[x, y] == attackingPawnChar)
                {
                    // king is in the pawn attacking tile
                    checkingPos.Add((x, y));
                }
            }

            // knight checks
            var knightDirections = new int[] { -1, 1, -2, 2 };
            foreach (var xDir in knightDirections)
            {
                foreach (var yDir in knightDirections)
                {
                    var x = xDir + defendingKingPos.X;
                    var y = yDir + defendingKingPos.Y;

                    if (Math.Abs(xDir) == Math.Abs(yDir) || x < 0 || y < 0 || x >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                    {
                        // skips a non L-shaped moves and out of chessboard moves 
                        continue;
                    }
                    if (board[defendingKingPos.X + xDir, defendingKingPos.Y + yDir] == attackingKnightChar)
                    {
                        // king is in the knight attacking tile
                        checkingPos.Add((x, y));
                    }
                }
            }

            // rook, bishop, queen attacks in diagonal, horizontal or vertical
            var directions = new int[] { -1, 1, 0 };

            foreach (var dirX in directions)
            {
                foreach (var dirY in directions)
                {
                    if (dirX == 0 && dirY == 0)
                    {
                        // skips (0,0) in place direction
                        continue;
                    }

                    int x = defendingKingPos.X + dirX;
                    int y = defendingKingPos.Y + dirY;

                    if (x < 0 || y < 0 || x >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                    {
                        // skips out of chessboard moves
                        continue;
                    }

                    while (x >= 0 && y >= 0 && x < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && y < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH && board[x, y] == ChessCoreConstants.EMPTY_SQUARE)
                    {
                        // checks for a piece in the direction
                        x += dirX;
                        y += dirY;
                    }

                    if (x < 0 || y < 0 || x >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH || y >= MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH)
                    {
                        // skips out of chessboard moves
                        continue;
                    }

                    // piece found

                    if (dirX == 0 || dirY == 0)
                    {
                        // horizontal or vertical direction
                        if (board[x, y] == attackingRookChar || board[x, y] == attackingQueenChar)
                        {
                            // adds rook or queen to the checking pieces
                            checkingPos.Add((x, y));
                        }
                    }
                    else
                    {
                        // diagonal direction
                        if (board[x, y] == attackingBishopChar || board[x, y] == attackingQueenChar)
                        {
                            // adds bishop or queen to the checking pieces
                            checkingPos.Add((x, y));
                        }
                    }
                }
            }

            return checkingPos;
        }

        /// <summary>
        /// Controls if move removes a check
        /// </summary>
        /// <param name="board">Board before move</param>
        /// <param name="defendingKingPos">Defending king position</param>
        /// <param name="newMove">Possible move</param>
        /// <returns>True if move removes check</returns>
        private static bool IsStillCheck(char[,] board, (int X, int Y) defendingKingPos, Move newMove)
        {
            // gets attackingColor based on defendingKingPos color
            var attackingColor = ChessCoreConstants.WHITE_PIECES_CHAR_ARR.Contains(board[defendingKingPos.X, defendingKingPos.Y]) ? MiddlewareConstants.PIECE_COLOR.Black : MiddlewareConstants.PIECE_COLOR.White;

            // mockup board
            var mockupBoard = new char[MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH, MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH];
            Array.Copy(board, mockupBoard, board.Length);

            // updates mockup board with new move
            var updatedMockupBoard = UpdateBoard(mockupBoard, newMove);

            var updatedAttackingPieces = GetCheckingPositions(updatedMockupBoard, defendingKingPos, attackingColor);
            return updatedAttackingPieces.Count > 0;
        }
    }
}
