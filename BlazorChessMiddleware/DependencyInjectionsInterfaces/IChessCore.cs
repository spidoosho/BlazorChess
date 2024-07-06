namespace BlazorChessMiddleware.DependencyInjectionsInterfaces
{
    public interface IChessCore
    {
        /// <summary>
        /// Processes move into the state
        /// </summary>
        /// <param name="state">Board state before the move</param>
        /// <param name="lastMove">Last played move</param>
        /// <returns>Updated Board state</returns>
        public BoardState ProcessMove(BoardState state, Move lastMove);

        /// <summary>
        /// Gets possible moves based on current Board state
        /// </summary>
        /// <param name="state">Current Board state</param>
        /// <returns>Dictionary of keys of pieces positions and values of their possible moves</returns>
        public Dictionary<(int X, int Y), List<(int X, int Y)>> GetPossibleMoves(BoardState state);

        /// <summary>
        /// Defines a board for the game
        /// </summary>
        /// <returns>Chessboard to play on</returns>
        public char[,] GetStartBoard();

        /// <summary>
        /// Gets rooks start position on the chessboard
        /// </summary>
        /// <returns>touple of colors of rooks position</returns>
        public (List<(int X, int Y)> White, List<(int X, int Y)> Black) GetRooksStartPositions();

        /// <summary>
        /// Gets kings start position on the chessboard
        /// </summary>
        /// <returns>touple of colors of king position</returns>
        public ((int X, int Y) White, (int X, int Y) Black) GetKingsStartPositions();
    }
}
