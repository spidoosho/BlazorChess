namespace ChessCore
{
    public class ChessCoreConstants
    {
        public static readonly char[] WHITE_PIECES_CHAR_ARR = ['p', 'r', 'n', 'b', 'k', 'q'];
        public static readonly char[] BLACK_PIECES_CHAR_ARR = ['P', 'R', 'N', 'B', 'K', 'Q'];

        public static readonly int PAWN_INDEX = 0;
        public static readonly int ROOK_INDEX = 1;
        public static readonly int KNIGHT_INDEX = 2;
        public static readonly int BISHOP_INDEX = 3;
        public static readonly int KING_INDEX = 4;
        public static readonly int QUEEN_INDEX = 5;

        public static readonly char EMPTY_SQUARE = '\0';

        public static readonly List<(int X, int Y)> WHITE_ROOKS_START_POS = [(0, 0), (7, 0)];
        public static readonly List<(int X, int Y)> BLACK_ROOKS_START_POS = [(0, 7), (7, 7)];

        public static readonly (int X, int Y) WHITE_KING_START_POS = (4, 0);
        public static readonly (int X, int Y) BLACK_KING_START_POS = (4, 7);

        public static readonly char[,] DEFAULT_BOARD = new char[,] {{ 'r', 'p', '\0', '\0', '\0', '\0', 'P', 'R' },
                                                                    { 'n', 'p', '\0', '\0', '\0', '\0', 'P', 'N' },
                                                                    { 'b', 'p', '\0', '\0', '\0', '\0', 'P', 'B' },
                                                                    { 'q', 'p', '\0', '\0', '\0', '\0', 'P', 'Q' },
                                                                    { 'k', 'p', '\0', '\0', '\0', '\0', 'P', 'K' },
                                                                    { 'b', 'p', '\0', '\0', '\0', '\0', 'P', 'B' },
                                                                    { 'n', 'p', '\0', '\0', '\0', '\0', 'P', 'N' },
                                                                    { 'r', 'p', '\0', '\0', '\0', '\0', 'P', 'R' }};
    }
}
