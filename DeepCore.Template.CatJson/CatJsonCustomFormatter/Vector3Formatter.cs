using CatJson;
using DeepCore.Geometry;
using System;

namespace X.SceneServer.CatJsonCustomFormatter
{
    public class Vector3Formatter : BaseJsonFormatter<Vector3>
    {
        public override Vector3 ParseJson(JsonParser parser, Type type, Type realType)
        {
            parser.Lexer.GetNextTokenByType(TokenType.LeftBrace);
            float x = parser.Lexer.GetNextTokenByType(TokenType.Number).AsFloat();
            parser.Lexer.GetNextTokenByType(TokenType.Comma);
            float y = parser.Lexer.GetNextTokenByType(TokenType.Number).AsFloat();
            parser.Lexer.GetNextTokenByType(TokenType.Comma);
            float z = parser.Lexer.GetNextTokenByType(TokenType.Number).AsFloat();
            parser.Lexer.GetNextTokenByType(TokenType.RightBrace);
            return new Vector3(x, y, z);
        }

        public override void ToJson(JsonParser parser, Vector3 value, Type type, Type realType, int depth)
        {
            parser.Append('{');
            parser.Append(value.X.ToString());
            parser.Append(", ");
            parser.Append(value.Y.ToString());
            parser.Append(", ");
            parser.Append(value.Z.ToString());
            parser.Append('}');
        }
    }
}
