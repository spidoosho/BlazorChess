namespace BlazorChess.Utility
{
    internal static class BlazorConstants
    {
        public const int CHESSBOARD_DIMENSION_LENGTH = 8;

        public const string OPPONENT = "Opponent";

        public const string READY = "Ready";
        public const string NOT_READY = "Not ready";

        public const char EMPTY_TILE = '\0';
        public const char MOVE_CHAR = 'o';

        public static readonly char[] WHITE_PROMOTION_PIECES = ['r', 'n', 'b', 'q'];
        public static readonly char[] BLACK_PROMOTION_PIECES = ['R', 'N', 'B', 'Q'];

        public static char BLACK_PAWN = 'P';
        public static char WHITE_PAWN = 'p';

        public enum Status { None, InLobby, InGame };
    }
}
