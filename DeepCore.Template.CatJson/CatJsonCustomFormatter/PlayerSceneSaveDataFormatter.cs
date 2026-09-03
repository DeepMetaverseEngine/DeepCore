using CatJson;
using DeepCore;
using DeepCore.Geometry;
using System;
using X.Core.Battle.Data;

namespace X.SceneServer.CatJsonCustomFormatter
{
    public class PlayerSceneSaveDataFormatter : BaseJsonFormatter<PlayerSceneSaveData>
    {
        public override PlayerSceneSaveData ParseJson(JsonParser parser, Type type, Type realType)
        {
            parser.Lexer.GetNextTokenByType(TokenType.LeftBrace);
            float direction = parser.Lexer.GetNextTokenByType(TokenType.Number).AsFloat();
            parser.Lexer.GetNextTokenByType(TokenType.Comma);
            int lastWorldSceneId = parser.Lexer.GetNextTokenByType(TokenType.Number).AsInt();
            parser.Lexer.GetNextTokenByType(TokenType.Comma);
            var lastWorldScenePos = parser.ParseJson<Vector3>();
            parser.Lexer.GetNextTokenByType(TokenType.Comma);
            var playerSkillCDInfo = parser.ParseJson<HashMap<int, DateTime>>();
            parser.Lexer.GetNextTokenByType(TokenType.Comma);
            var petSaveData = parser.ParseJson<PlayerPetSaveData>();
            parser.Lexer.GetNextTokenByType(TokenType.RightBrace);

            var ret = new PlayerSceneSaveData();
            ret.Direction = direction;
            ret.LastWorldSceneId = lastWorldSceneId;
            ret.LastWorldScenePos = lastWorldScenePos;
            ret.PlayerSkillCDInfo = playerSkillCDInfo;
            ret.PetSaveData = petSaveData;

            return ret;
        }

        public override void ToJson(JsonParser parser, PlayerSceneSaveData value, Type type, Type realType, int depth)
        {
            parser.Append('{');
            parser.Append(value.Direction.ToString());
            parser.Append(", ");
            parser.Append(value.LastWorldSceneId.ToString());
            parser.Append(", ");
            parser.Append(value.LastWorldScenePos.ToJson(parser));
            parser.Append(", ");
            parser.Append(value.PlayerSkillCDInfo.ToJson(parser));
            parser.Append(", ");
            parser.Append(value.PetSaveData.ToJson(parser));
            parser.Append('}');
        }
    }
}
