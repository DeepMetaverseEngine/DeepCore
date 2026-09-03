using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Message
{


    [MessageType(BattleConstants.ZoneVarTemplate)]
    public class ZoneVarTemplate : IExternalizable
    {
        public Type TemplateType;
        public int TemplateID;
        public void ReadExternal(IInputStream input)
        {
            this.TemplateType = input.GetValueType();
            this.TemplateID = input.GetS32();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutValueType(this.TemplateType);
            output.PutS32(this.TemplateID);
        }
    }

    [MessageType(BattleConstants.ZoneVarObject)]
    public class ZoneVarObject : IExternalizable
    {
        public uint ObjID;
        public void ReadExternal(IInputStream input)
        {
            this.ObjID = input.GetU32();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU32(this.ObjID);
        }
    }

    [MessageType(BattleConstants.ZoneVarObjectBuff)]
    public class ZoneVarObjectBuff : IExternalizable
    {
        public uint ObjID;
        public int BuffID;
        public void ReadExternal(IInputStream input)
        {
            this.ObjID = input.GetU32();
            this.BuffID = input.GetS32();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU32(this.ObjID);
            output.PutS32(this.BuffID);
        }
    }
    [MessageType(BattleConstants.ZoneVarObjectSkill)]
    public class ZoneVarObjectSkill : IExternalizable
    {
        public uint ObjID;
        public int SkillID;
        public void ReadExternal(IInputStream input)
        {
            this.ObjID = input.GetU32();
            this.SkillID = input.GetS32();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU32(this.ObjID);
            output.PutS32(this.SkillID);
        }
    }
    [MessageType(BattleConstants.ZoneVarObjectAura)]
    public class ZoneVarObjectAura : IExternalizable
    {
        public uint ObjID;
        public int AuraID;
        public void ReadExternal(IInputStream input)
        {
            this.ObjID = input.GetU32();
            this.AuraID = input.GetS32();
        }
        public void WriteExternal(IOutputStream output)
        {
            output.PutU32(this.ObjID);
            output.PutS32(this.AuraID);
        }
    }
}

