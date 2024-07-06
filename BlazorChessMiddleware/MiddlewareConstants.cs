namespace BlazorChessMiddleware
{
    public static class MiddlewareConstants
    {
        public enum PIECE_COLOR { White, Black };
        public enum GameStateEnum { Checkmate, Check, Normal, Draw }
        public const int CHESSBOARD_DIMENSION_LENGTH = 8;
    }
}