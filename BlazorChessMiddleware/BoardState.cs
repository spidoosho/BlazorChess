using System.Text;

namespace BlazorChessMiddleware
{
    public class BoardState
    {
        public class Folds
        {
            /// <summary>
            /// Chessboard piece configuration appeared once
            /// </summary>
            public HashSet<string> OneFold { get; set; }
            /// <summary>
            /// Chessboard piece configuration appeared twice
            /// </summary>
            public HashSet<string> TwoFold { get; set; }
            /// <summary>
            /// True if a chessboard piece configuration appeared three times
            /// </summary>
            public bool IsThreeFold { get; set; }

            /// <summary>
            /// Initialize three fold tracking
            /// </summary>
            /// <param name="firstMove">True if it is the first move</param>
            /// <param name="startBoard">Start chessboard. Has to be defined if firstMove is true</param>
            public Folds(bool firstMove, char[,]? startBoard)
            {
                OneFold = [];

                if (firstMove)
                {
                    ArgumentNullException.ThrowIfNull(startBoard);
                    OneFold.Add(SerializeBoad(startBoard));
                }
                TwoFold = [];
                IsThreeFold = false;
            }

            /// <summary>
            /// Serializes chessboard into the string
            /// </summary>
            /// <param name="board">Chessboard to serialize</param>
            /// <returns>Serialized string of the chessboard</returns>
            public static string SerializeBoad(char[,] board)
            {
                var sb = new StringBuilder();

                for (int i = 0; i < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH; i++)
                {
                    for (int j = 0; j < MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH; j++)
                    {
                        sb.Append(board[i, j]);
                    }
                }

                return sb.ToString();
            }
        }

        public char[,] Board { get; set; }
        public Dictionary<(int X, int Y), List<(int X, int Y)>> possibleMoves;
        public MiddlewareConstants.PIECE_COLOR ColorOnMove { get; set; }
        public Move LastMove;
        public KingsTracker Kings { get; set; }
        public int FiftyMoveCounter { get; set; }
        public Folds ThreeFoldCounter { get; set; }
        public NotMovedRooksTracker NotMovedRooks { get; set; }
        public MiddlewareConstants.GameStateEnum State { get; set; }
        public List<(int X, int Y)> AttackingPieces { get; set; }
        public BoardState(char[,] startBoard, (List<(int X, int Y)> White, List<(int X, int Y)> Black) rooksStartPos, ((int X, int Y) White, (int X, int Y) Black) kingsPos)
        {
            Board = new char[MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH, MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH];
            Array.Copy(startBoard, Board, startBoard.Length);
            Kings = new((kingsPos.White, kingsPos.Black));
            ThreeFoldCounter = new Folds(true, startBoard);
            FiftyMoveCounter = 0;
            NotMovedRooks = new(rooksStartPos.White, rooksStartPos.Black);
            AttackingPieces = [];
            possibleMoves = [];
            State = MiddlewareConstants.GameStateEnum.Normal;
            ColorOnMove = MiddlewareConstants.PIECE_COLOR.White;
        }
    }

    public struct Move((int X, int Y) startPos, (int X, int Y) endPos, Move.MoveType? lastMoveType = null, char? promotionCharPiece = null)
    {
        public enum MoveType { Normal, EnPassant, Castling, Promotion, None };

        public MoveType? moveType = lastMoveType;

        public (int X, int Y) startPos = startPos;

        public (int X, int Y) endPos = endPos;

        public char? promotionCharPiece = promotionCharPiece;
    }

    public struct NotMovedRooksTracker
    {
        public List<(int X, int Y)> White { get; set; }
        public List<(int X, int Y)> Black { get; set; }

        public NotMovedRooksTracker(List<(int X, int Y)> whiteRooksStartPos, List<(int X, int Y)> blackRooksStartPos)
        {
            White = whiteRooksStartPos;
            Black = blackRooksStartPos;
        }
    }

    public class KingsTracker 
    {
        public KingPos White { get; set; }
        public KingPos Black { get; set; }

        public KingsTracker(((int X, int Y) White, (int X, int Y) Black) kingsPos)
        {
            White = new KingPos(MiddlewareConstants.PIECE_COLOR.White, kingsPos.White);
            Black = new KingPos(MiddlewareConstants.PIECE_COLOR.Black, kingsPos.Black);
        }

        public class KingPos
        {
            public KingPos(MiddlewareConstants.PIECE_COLOR color, (int X, int Y) kingPos)
            {
                switch (color)
                {
                    case MiddlewareConstants.PIECE_COLOR.White:
                        HasMoved = false;
                        Position = kingPos;
                        break;
                    case MiddlewareConstants.PIECE_COLOR.Black:
                        HasMoved = false;
                        Position = kingPos;
                        break;
                    default:
                        throw new ArgumentException("Invalid color. Color must be black or white.");
                }
            }
            public bool HasMoved { get; set; }
            public (int X, int Y) Position { get; set; }
        }
    }
}
