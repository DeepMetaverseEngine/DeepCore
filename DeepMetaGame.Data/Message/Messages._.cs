using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepCore.Xml;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeepMetaGame.Data.Message
{
    public interface IBattleMessage : IRecyclable
    {
        void BeforeWrite(TemplateManager templates);
        void EndRead(TemplateManager templates);
    }
    abstract public class BattleMessage : Recyclable, IMessage, IBattleMessage
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(BattleMessage));
        new public static bool EnableAlloc { get => Alloc.Enable; set => Alloc.Enable = value; }
        new public static bool VerbosAlloc { get => Alloc.Verbos; set => Alloc.Verbos = value; }
        public static int ActiveObjectCount { get { return Alloc.ActiveCount; } }
        public static int AllocObjectCount { get { return Alloc.AllocCount; } }
        protected BattleMessage()
        {
            Alloc.RecordConstructor(GetType());
        }
        ~BattleMessage()
        {
            if (!IsDisposed)
            {
                Alloc.RecordDispose(GetType());
            }
            Alloc.RecordDestructor(GetType());
        }
        sealed protected override void RecordDisposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        sealed protected override void RecordReuse()
        {
            Alloc.RecordReuse(GetType());
        }


        [XmlSerializable(XmlProperty.NoSerialize)] public object sender { get; set; }
        [XmlSerializable] public int MessageID { get; set; }
        [XmlSerializable] public string ErrorMessage { get; set; }
        sealed protected override void Disposing()
        {
            sender = default;
            MessageID = default;
            ErrorMessage = default;
            OnDisposing();
        }
        protected abstract void OnDisposing();
        abstract public void WriteExternal(IOutputStream output);
        abstract public void ReadExternal(IInputStream input);
        virtual public void BeforeWrite(TemplateManager templates) { }
        virtual public void EndRead(TemplateManager templates) { }
    }
    //--------------------------------------------------------------------------
    /// <summary>
    /// 向服务器端发送的指令
    /// </summary>
    abstract public class BattleAction : BattleMessage
    {
    }
    /// <summary>
    /// 客户端接收到的消息
    /// </summary>
    abstract public class BattleNotify : BattleMessage
    {
    }
    //--------------------------------------------------------------------------


    /// <summary>
    /// 某个位置上的事件
    /// </summary>
    public interface PositionMessage
    {
        DeepCore.Geometry.Vector3 Position { get; }
    }
    /// <summary>
    /// 主角的事件
    /// </summary>
    public interface ActorMessage
    {
        uint ObjectID { get; }
    }
    /// <summary>
    /// 系统消息
    /// </summary>
    public interface SystemMessage
    {
    }

    //--------------------------------------------------------------------------
    /// <summary>
    /// 向服务器端某个单位发送的指令
    /// </summary>
    abstract public class ObjectAction : BattleAction
    {
        /// <summary>
        /// 不需要传输object_id, 使用sender对象来绑定Session和IZoneUnit的关联//
        /// </summary>
        public uint object_id;
        sealed protected override void OnDisposing()
        {
            OnDisposing(object_id);
            object_id = default;
        }
        protected abstract void OnDisposing(uint objID);
        override public void WriteExternal(IOutputStream output)
        {
            // 不需要传输object_id, 使用sender对象来绑定Session和IZoneUnit//
        }
        override public void ReadExternal(IInputStream input)
        {
            // 不需要传输object_id, 使用sender对象来绑定Session和IZoneUnit//
        }
    }
    /// <summary>
    /// 某个单位的事件消息
    /// </summary>
    abstract public class ObjectNotify : BattleNotify
    {
        public uint object_id;
        public uint ObjectID { get { return object_id; } }
        sealed protected override void OnDisposing()
        {
            OnDisposing(object_id);
            object_id = default;
        }
        protected abstract void OnDisposing(uint objID);
        override public void WriteExternal(IOutputStream output)
        {
            output.PutVU32(object_id);
        }
        override public void ReadExternal(IInputStream input)
        {
            object_id = input.GetVU32();
        }
    }

    //--------------------------------------------------------------------------
    /// <summary>
    /// 向服务器端某个单位发送的请求
    /// </summary>
    abstract public class ActorRequest : ObjectAction
    {
        protected ActorRequest() { }
        protected ActorRequest(uint obj_id) { base.object_id = obj_id; }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVS32(MessageID);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            MessageID = input.GetVS32();
        }
    }
    /// <summary>
    /// 向服务器端某个单位发送请求对应的回馈
    /// </summary>
    abstract public class ActorResponse : ObjectNotify, ActorMessage
    {
        protected ActorResponse() { }
        protected ActorResponse(uint obj_id) { base.object_id = obj_id; }
        public override void WriteExternal(IOutputStream output)
        {
            base.WriteExternal(output);
            output.PutVS32(MessageID);
        }
        public override void ReadExternal(IInputStream input)
        {
            base.ReadExternal(input);
            MessageID = input.GetVS32();
        }
    }

    public abstract class ZoneNotify : BattleNotify
    {

    }

    public abstract class ClientNotify : ZoneNotify
    {
    }

    public abstract class PlayerNotify : ObjectNotify
    {
    }
    //--------------------------------------------------------------------------


    /// <summary>
    /// 单位的客户端显示数据，通常存储横向功能，比如Avatar
    /// </summary>
    public interface IUnitVisibleData : ISerializable
    {
        //         /// <summary>
        //         /// 合并外观
        //         /// </summary>
        //         /// <param name="visible"></param>
        //         void Combine(IUnitVisibleData visible);
        // 
        //         /// <summary>
        //         /// 拆离外观
        //         /// </summary>
        //         void Depart(IUnitVisibleData visible);
    }
    public struct PreEncodeEntry<T>
    {
        public T message;
        public byte[] binary;
    }

}

