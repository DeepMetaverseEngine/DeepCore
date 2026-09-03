using CatJson;
using DeepCore;
using System;
using X.Core.Battle.Data;

namespace X.SceneServer.CatJsonCustomFormatter
{
    public class PlayerPetSaveDataFormatter : BaseJsonFormatter<PlayerPetSaveData>
    {
        public override PlayerPetSaveData ParseJson(JsonParser parser, Type type, Type realType)
        {
            parser.Lexer.GetNextTokenByType(TokenType.LeftBrace);
            var PetSkillCDInfo = parser.ParseJson<HashMap<ulong, HashMap<int, DateTime>>>();
            parser.Lexer.GetNextTokenByType(TokenType.RightBrace);

            PlayerPetSaveData data = new PlayerPetSaveData();
            data.PetSkillCDInfo = PetSkillCDInfo;
            return data;
        }

        public override void ToJson(JsonParser parser, PlayerPetSaveData value, Type type, Type realType, int depth)
        {
            parser.Append('{');
            parser.Append(value.PetSkillCDInfo.ToJson());
            parser.Append('}');
        }
    }
}
