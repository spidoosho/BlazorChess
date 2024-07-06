namespace BlazorChess.Utility
{
    public class BlazorMove((int X, int Y) startPos, (int X, int Y) endPos, BlazorMove.BlazorMoveType? lastMoveType = null, char? promotionCharPiece = null)
    {
        public enum BlazorMoveType { Normal, EnPassant, Castling, Promotion, None };

        public BlazorMoveType? lastMoveType = lastMoveType;

        public (int X, int Y) startPos = startPos;

        public (int X, int Y) endPos = endPos;

        public char? promotionCharPiece = promotionCharPiece;
    }
}
