using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlazorChessMiddleware
{
    public class UserState
    {
        [JsonConverter(typeof(BoardConverter))]
        public required char[,] Board { get; set; }

        [JsonConverter(typeof(DictionaryMovesConverter))]
        public Dictionary<(int X, int Y), List<(int X, int Y)>> possibleMoves = [];

        public MiddlewareConstants.GameStateEnum GameState { get; set; }

        public required List<(int X, int Y)> AttackingPieces { get; set; }

        public (int X, int Y)? CheckedKingPos { get; set; }

        public MiddlewareConstants.PIECE_COLOR ColorOnMove { get; set; }

        /// <summary>
        /// Retrieves User state from Board state for Blazorchess view 
        /// </summary>
        /// <param name="state">Board state</param>
        /// <returns>User state to be serialized and sent to the view</returns>
        public static UserState GetUserState(BoardState state)
        {
            var result = new UserState
            {
                possibleMoves = new Dictionary<(int X, int Y), List<(int X, int Y)>>(state.possibleMoves),

                Board = new char[8, 8],

                ColorOnMove = state.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? MiddlewareConstants.PIECE_COLOR.White : MiddlewareConstants.PIECE_COLOR.Black,

                AttackingPieces = new List<(int X, int Y)>(state.AttackingPieces),

                GameState = state.State
            };

            if (result.GameState == MiddlewareConstants.GameStateEnum.Check || result.GameState == MiddlewareConstants.GameStateEnum.Checkmate)
            {
                result.CheckedKingPos = result.ColorOnMove == MiddlewareConstants.PIECE_COLOR.White ? state.Kings.White.Position : state.Kings.Black.Position;
            }

            Array.Copy(state.Board, result.Board, state.Board.Length);

            return result;
        }
    }

    public class BoardConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(char[,]);
        }

        /// <summary>
        /// Converts serialized string of chessboard into char[,] 
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns>Array of arrays of characters</returns>
        /// <exception cref="JsonSerializationException"></exception>
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            char[,] board = new char[MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH, MiddlewareConstants.CHESSBOARD_DIMENSION_LENGTH];
            if (reader.TokenType != JsonToken.String || reader.Value == null || ((string)reader.Value).Length != board.Length)
            {
                throw new JsonSerializationException($"Type must be string of length {board.Length}!");
            }

            int x = 0;
            int y = 0;

            foreach (char c in (string)reader.Value)
            {
                if (x == 8)
                {
                    x = 0;
                    y++;
                }

                board[x++, y] = c;
            }

            return board;
        }

        /// <summary>
        /// Converts char[,] to serialized string of a chessboard
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        /// <exception cref="JsonSerializationException"></exception>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                throw new JsonSerializationException("Json object is null!");
            }

            char[,] board = (char[,])value;
            var stringBuilder = new StringBuilder();

            if (board.GetLength(0) != 8 || board.GetLength(1) != 8)
            {
                throw new JsonSerializationException("Board is not size of 8x8");
            }

            for (int y = 0; y < board.GetLength(0); y++)
            {
                for (int x = 0; x < board.GetLength(1); x++)
                {
                    stringBuilder.Append(board[x, y]);
                }
            }

            (new JValue(stringBuilder.ToString())).WriteTo(writer);
        }
    }

    public class DictionaryMovesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<(int X, int Y), List<(int X, int Y)>>);
        }

        /// <summary>
        /// Converts serialized string of chessboard into Dictionary of positions and moves
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        /// <exception cref="JsonSerializationException"></exception>
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var result = new Dictionary<(int X, int Y), List<(int X, int Y)>>();
            (int X, int Y) startPos;
            List<(int X, int Y)> moves;
            try
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                    {
                        JObject jo = JObject.Load(reader);

                        if (jo == null || jo.First == null || jo.Last == null)
                        {
                            throw new Exception();
                        }

                        JProperty start = (JProperty)jo.First;

                        if (start.Value.First == null || start.Value.Last == null)
                        {
                            throw new Exception();
                        }

                        startPos.X = start.Value.First.Value<int>();
                        startPos.Y = start.Value.Last.Value<int>();

                        JProperty end = (JProperty)jo.Last;
                        moves = [];

                        foreach (JArray move in end.Value)
                        {
                            if (move.First == null || move.Last == null)
                            {
                                throw new Exception();
                            }
                            moves.Add((move.First.Value<int>(), move.Last.Value<int>()));
                        }

                        result.Add(startPos, moves);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Json object not recognized: {ex}");
            }
        }

        /// <summary>
        /// Converts dictionary of positions and moves to a serialized string 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        /// <exception cref="JsonSerializationException"></exception>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null || value is not Dictionary<(int X, int Y), List<(int X, int Y)>>)
            {
                throw new JsonSerializationException("Json object is null or not correct type!");
            }

            Dictionary<(int X, int Y), List<(int X, int Y)>> possibleMoves = (Dictionary<(int X, int Y), List<(int X, int Y)>>)value;

            writer.WriteToken(JsonToken.StartArray);
            foreach (var position in possibleMoves.Keys)
            {
                writer.WriteToken(JsonToken.StartObject);

                writer.WritePropertyName("startPos");

                writer.WriteToken(JsonToken.StartArray);
                writer.WriteValue(position.X);
                writer.WriteValue(position.Y);
                writer.WriteToken(JsonToken.EndArray);

                writer.WritePropertyName("moves");
                writer.WriteToken(JsonToken.StartArray);
                foreach (var (X, Y) in possibleMoves[position])
                {
                    writer.WriteToken(JsonToken.StartArray);
                    writer.WriteValue(X);
                    writer.WriteValue(Y);
                    writer.WriteToken(JsonToken.EndArray);
                }
                writer.WriteToken(JsonToken.EndArray);
                writer.WriteToken(JsonToken.EndObject);
            }
            writer.WriteToken(JsonToken.EndArray);
        }
    }
}
