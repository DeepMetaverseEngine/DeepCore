using DeepCore;
using DeepCore.Geometry;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Xml;
using DeepMetaGame.Data.Misc;
using DeepMetaGame.Data.Template;
using System.Xml.Linq;

namespace DeepMetaGame.Data.Message
{
    /// <summary>
    /// 添加单位事件
    /// </summary>
    [MessageType(BattleConstants.AddUnitEvent)]
    public class AddUnitEvent : ZoneNotify
    {
        public SyncUnitInfo Sync;
        protected override void OnDisposing()
        {
            Sync = null;
        }
        public uint unit_id
        {
            get
            {
                if (Sync != null)
                {
                    return Sync.ObjectID;
                }
                return 0;
            }
        }

        public AddUnitEvent() { }
        public AddUnitEvent Init(SyncUnitInfo sync,object sender)
        {
            this.sender = sender;
            this.Sync = sync;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            output.PutExt(Sync);
            output.PutUTF(ErrorMessage);
        }

        override public void ReadExternal(IInputStream input)
        {
            Sync = input.GetExt<SyncUnitInfo>();
            ErrorMessage = input.GetUTF();
        }
    }

    /// <summary>
    /// 添加法术/飞行道具效果事件
    /// </summary>
    [MessageType(BattleConstants.AddSpellEvent)]
    public class AddSpellEvent : ZoneNotify
    {
        [XmlSerializable]
        public bool IsLauncherSender
        {
            get { return mask.Get(0); }
            set { mask.Set(0, value); }
        }
        [XmlSerializable]
        public bool IsTargetPos
        {
            get { return mask.Get(1); }
            set { mask.Set(1, value); }
        }
        [XmlSerializable]
        public bool IsTargetObject
        {
            get { return mask.Get(2); }
            set { mask.Set(2, value); }
        }
        [XmlSerializable]
        public bool IsNormal
        {
            get { return mask.Get(3); }
            set { mask.Set(4, value); }
        }
        [XmlSerializable]
        public bool IsSyncPos
        {
            get { return mask.Get(4); }
            set { mask.Set(4, value); }
        }
        [XmlSerializable]
        public bool HasTemplate
        {
            get { return mask.Get(5); }
            set { mask.Set(5, value); }
        }
        [XmlSerializable]
        public bool IsSpellMagnitude
        {
            get { return mask.Get(6); }
            set { mask.Set(6, value); }
        }

        [XmlSerializable]
        public int spell_template_id
        {
            get { return launch_data != null ? launch_data.SpellID : 0; }
        }
        [XmlSerializable]
        public LaunchSpell LaunchData
        {
            get { return launch_data; }
        }

        private BitSet8 mask = new BitSet8();
        private LaunchSpell launch_data;
        private uint launch_data_sn = 0;
        public uint spell_id;
        public uint launcher_unit_id;
        public uint sender_unit_id;
        public uint target_obj_id;
        public Vector3? target_pos;
        public Vector3 spell_pos;
        public Vector3? normal;
        public float direction;
        public bool senderChain;
        public float startSpeed;
        public SpellTemplate template;
        protected override void OnDisposing()
        {
            this.mask = new BitSet8();
            this.launch_data = default;
            this.launch_data_sn = 0;
            this.spell_id = default;
            this.launcher_unit_id = default;
            this.sender_unit_id = default;
            this.target_obj_id = default;
            this.target_pos = default;
            this.spell_pos = default;
            this.normal = default;
            this.direction = default;
            this.senderChain = default;
            this.startSpeed = default;
            this.template = default;
        }
        public AddSpellEvent() { }

        public AddSpellEvent Init(LaunchSpell launch_data, object sender)
        {
            this.launch_data = launch_data;
            this.sender = sender;
            return this;
        }
        /*
        public AddSpellEvent(IZoneSpell spell)
        {
            this.launch_data = spell.LaunchData;
            this.spell_id = spell.ObjectID;
            this.sender_unit_id = spell.SenderID;
            this.launcher_unit_id = spell.LauncherID;
            this.target_obj_id = spell.TargetID;
            this.target_pos = spell.TargetPos;
            this.spell_pos = spell.Position;
            this.direction = spell.Direction;

            //mask//
            this.IsLauncherSender = (launcher_unit_id == sender_unit_id);
            this.IsTargetPos = (spell.TemplateData.MType == SpellTemplate.MotionType.Cannon) && (target_pos != null);
            this.IsTargetObject = (target_obj_id != 0);
            this.IsSyncPos = spell.TemplateData.IsLaunchSpellEventSyncPos;
        }
        */

        public override void BeforeWrite(TemplateManager templates)
        {
            base.BeforeWrite(templates);
            if (launch_data != null)
            {
                launch_data_sn = launch_data.SerialNumber;
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            base.EndRead(templates);
            if (launch_data_sn != 0)
            {
                launch_data = templates.GetSnData<LaunchSpell>(launch_data_sn);
            }
        }

        override public void WriteExternal(IOutputStream output)
        {
            this.HasTemplate = (template != null);

            output.PutU8(mask.Mask);
            output.PutVU32(spell_id);
            output.PutVU32(launch_data_sn);
            if (IsLauncherSender)
            {
                output.PutVU32(launcher_unit_id);
            }
            else
            {
                output.PutVU32(sender_unit_id);
                output.PutVU32(launcher_unit_id);
            }
            if (IsTargetObject)
            {
                output.PutVU32(target_obj_id);
            }
            if (IsTargetPos)
            {
                var p = target_pos.Value;
                output.WritePos(in p);
            }
            if (IsNormal)
            {
                var p = normal.Value;
                output.WritePos(in p);
            }
            if (IsSyncPos)
            {
                output.WritePosAndDirection(in spell_pos, direction);
            }
            else
            {
                output.WriteDirection(direction);
            }
            output.PutBool(senderChain);
            if (HasTemplate)
            {
                output.PutSer(template);
            }
            output.PutF32(startSpeed);
        }

        override public void ReadExternal(IInputStream input)
        {
            mask.Mask = input.GetU8();
            spell_id = input.GetVU32();
            launch_data_sn = input.GetVU32();
            if (IsLauncherSender)
            {
                launcher_unit_id = sender_unit_id = input.GetVU32();
            }
            else
            {
                sender_unit_id = input.GetVU32();
                launcher_unit_id = input.GetVU32();
            }
            if (IsTargetObject)
            {
                target_obj_id = input.GetVU32();
            }
            if (IsTargetPos)
            {
                target_pos = input.ReadPos3D();
            }
            if (IsNormal)
            {
                normal = input.ReadPos3D();
            }
            if (IsSyncPos)
            {
                input.ReadPosAndDirection(out spell_pos, out direction);
            }
            else
            {
                input.ReadDirection(out direction);
            }
            senderChain = input.GetBool();
            if (HasTemplate)
            {
                template = input.GetObj<SpellTemplate>();
            }
            this.startSpeed = input.GetF32();
        }
    }

    [MessageType(BattleConstants.AddEffectEvent)]
    public class AddEffectEvent : ZoneNotify, PositionMessage
    {
        public uint senderID;
        public Vector3 pos;
        public float direction;
        private uint effect_sn = 0;
        public LaunchEffect effect;
        protected override void OnDisposing()
        {
            senderID = 0;
            pos = default;
            direction = 0;
            effect = null;
            effect_sn = 0;
        }
        public AddEffectEvent() { }
        public AddEffectEvent Init(uint senderID, Vector3 pos, float dir, LaunchEffect effect)
        {
            this.senderID = senderID;
            this.pos = pos;
            this.direction = dir;
            this.effect = effect;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutVU32(senderID);
            output.WritePosAndDirection(in pos, direction);
            //             output.PutF32(x);
            //             output.PutF32(y);
            //             output.PutF32(direction);
            output.PutVU32(effect_sn);
        }
        override public void ReadExternal(IInputStream input)
        {
            senderID = input.GetVU32();
            input.ReadPosAndDirection(out pos, out direction);
            //             this.x = input.GetF32();
            //             this.y = input.GetF32();
            //             this.direction = input.GetF32();
            effect_sn = input.GetVU32();
        }
        public override void BeforeWrite(TemplateManager templates)
        {
            if (effect != null)
            {
                effect_sn = effect.SerialNumber;
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            effect = templates.GetSnData<LaunchEffect>(effect_sn);
        }
        public DeepCore.Geometry.Vector3 Position => pos;
    }

    /// <summary>
    /// 添加道具事件
    /// </summary>
    [MessageType(BattleConstants.AddItemEvent)]
    public class AddItemEvent : ZoneNotify
    {
        public SyncItemInfo Sync;
        protected override void OnDisposing()
        {
            Sync = null;
        }
        //public uint creater_id; // target object id, only for filter
        public uint unit_id
        {
            get { if (Sync != null) { return Sync.ObjectID; } return 0; }
        }

        public AddItemEvent() { }
        public AddItemEvent Init(SyncItemInfo sync, object sender)
        {
            this.Sync = sync;
            this.sender = sender;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutExt(Sync);
            output.PutUTF(ErrorMessage);
        }
        override public void ReadExternal(IInputStream input)
        {
            Sync = input.GetExt<SyncItemInfo>();
            ErrorMessage = input.GetUTF();
        }
    }

    /// <summary>
    /// 某个对象从场景中移除
    /// </summary>
    [MessageType(BattleConstants.RemoveObjectEvent)]
    public class RemoveObjectEvent : ZoneNotify
    {
        public uint object_id;
        protected override void OnDisposing()
        {
            object_id = 0;
        }
        public RemoveObjectEvent() { }
        public RemoveObjectEvent Init(uint oid)
        {
            object_id = oid;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutVU32(object_id);
        }
        override public void ReadExternal(IInputStream input)
        {
            object_id = input.GetVU32();
        }
    }

    /// <summary>
    /// 服务端通知客户端执行一段脚本
    /// </summary>
    [MessageType(BattleConstants.DoScriptEvent)]
    public class DoScriptEvent : ZoneNotify
    {
        public string ScriptFileName;
        protected override void OnDisposing()
        {
            ScriptFileName = null;
        }
        public DoScriptEvent() { }
        public DoScriptEvent Init(string filename)
        {
            ScriptFileName = filename;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(ScriptFileName);

        }
        override public void ReadExternal(IInputStream input)
        {
            ScriptFileName = input.GetUTF();
        }
    }

    /// <summary>
    /// 脚本系统指令
    /// </summary>
    [MessageType(BattleConstants.ScriptCommandEvent)]
    public class ScriptCommandEvent : ZoneNotify
    {
        public string message;
        protected override void OnDisposing()
        {
            message = null;
        }
        public ScriptCommandEvent() { }
        public ScriptCommandEvent Init(string msg)
        {
            message = msg;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(message);

        }
        override public void ReadExternal(IInputStream input)
        {
            message = input.GetUTF();
        }
    }

    /// <summary>
    /// 游戏结束指令
    /// </summary>
    [MessageType(BattleConstants.GameOverEvent)]
    public class GameOverEvent : ZoneNotify
    {
        public string message;
        public byte WinForce;
        public IExternalizable ExtandData;

        protected override void OnDisposing()
        {
            message = null;
            WinForce = 0;
            ExtandData = null;
        }
        public GameOverEvent() { }
        public GameOverEvent Init(byte force, string msg)
        {
            WinForce = force;
            message = msg;
            return this;

        }
        public GameOverEvent Init(byte force, string msg, IExternalizable ext)
        {
            WinForce = force;
            message = msg;
            ExtandData = ext;
            return this;
        }


        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(message);
            output.PutU8(WinForce);
            output.PutExt(ExtandData);
        }
        override public void ReadExternal(IInputStream input)
        {
            message = input.GetUTF();
            WinForce = input.GetU8();
            ExtandData = input.GetExtAny();
        }
    }

    //     [MessageType(BattleConstants.DecorationChangedEvent)]
    //     public class DecorationChangedEvent : ZoneNotify
    //     {
    //         public string Name;
    //         public bool Enable;
    // 
    //         public DecorationChangedEvent() { }
    //         public DecorationChangedEvent(string name, bool enable)
    //         {
    //             Name = name;
    //             Enable = enable;
    //         }
    // 
    //         override public void WriteExternal(IOutputStream output)
    //         {
    //             output.PutUTF(Name);
    //             output.PutBool(Enable);
    //         }
    // 
    //         override public void ReadExternal(IInputStream input)
    //         {
    //             Name = input.GetUTF();
    //             Enable = input.GetBool();
    //         }
    //     }

    [MessageType(BattleConstants.SyncEnvironmentVarEvent)]
    public class SyncEnvironmentVarEvent : ZoneNotify
    {
        public ClientStruct.ZoneEnvironmentVar Var;
        protected override void OnDisposing()
        {
            Var = null;
        }
        public SyncEnvironmentVarEvent() { }
        public SyncEnvironmentVarEvent Init(ClientStruct.ZoneEnvironmentVar var)
        {
            Var = var;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            output.EncodeExternalizable(Var);
        }

        override public void ReadExternal(IInputStream input)
        {
            Var = input.DecodeExternalizable(new ClientStruct.ZoneEnvironmentVar());
        }
    }

    [MessageType(BattleConstants.ChangeBGMEvent)]
    public class ChangeBGMEvent : ZoneNotify
    {
        public string FileName;
        protected override void OnDisposing()
        {
            FileName = null;
        }

        public ChangeBGMEvent() { }
        public ChangeBGMEvent Init(string file)
        {
            FileName = file;
            return this;
        }

        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(FileName);
        }

        override public void ReadExternal(IInputStream input)
        {
            FileName = input.GetUTF();
        }
    }


    [MessageType(BattleConstants.FlagEnableChangedEvent)]
    public class FlagEnableChangedEvent : ZoneNotify
    {
        public string Name;
        public bool Enable;
        protected override void OnDisposing()
        {
            Name = null; Enable = false;
        }

        public FlagEnableChangedEvent() { }
        public FlagEnableChangedEvent Init(string name, bool enable)
        {
            Name = name;
            Enable = enable;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(Name);
            output.PutBool(Enable);
        }
        override public void ReadExternal(IInputStream input)
        {
            Name = input.GetUTF();
            Enable = input.GetBool();
        }
    }
    [MessageType(BattleConstants.FlagTagChangedEvent)]
    public class FlagTagChangedEvent : ZoneNotify
    {
        public string Name;
        public string Tag;
        protected override void OnDisposing()
        {
            Name = null; Tag = default;
        }


        public FlagTagChangedEvent() { }
        public FlagTagChangedEvent Init(string name, string tag = null)
        {
            Name = name;
            Tag = tag;
            return this;
        }
        override public void WriteExternal(IOutputStream output)
        {
            output.PutUTF(Name);
            output.PutUTF(Tag);
        }
        override public void ReadExternal(IInputStream input)
        {
            Name = input.GetUTF();
            Tag = input.GetUTF();
        }
    }


    /// <summary>
    /// 同步场景中所有Flag状态
    /// </summary>
    [MessageType(BattleConstants.SyncFlagsEvent)]
    public class SyncFlagsEvent : ZoneNotify
    {
        public struct FlagState
        {
            public string tag;
            public bool enable;
        }
        //public List<string> ClosedDecorations = new List<string>();
        public readonly HashMap<string, FlagState> Stats = new HashMap<string, FlagState>();

        protected override void OnDisposing()
        {
            Stats.Clear();
        }

        public SyncFlagsEvent()
        {
        }

        override public void WriteExternal(IOutputStream output)
        {
            //             output.PutList(ClosedDecorations,
            //                 static (o, v) => o.PutUTF(v));
            output.PutMap(Stats,
                static (o, v) => o.PutUTF(v),
                static (o, v) =>
                {
                    o.PutBool(v.enable);
                    o.PutUTF(v.tag);
                });
        }
        override public void ReadExternal(IInputStream input)
        {
            //ClosedDecorations = input.GetUTFList();
            input.GetMap(
               static i => i.GetUTF(),
               static i =>
               {
                   return new FlagState()
                   {
                       enable = i.GetBool(),
                       tag = i.GetUTF()
                   };
               },
               Stats);
        }
    }
}

