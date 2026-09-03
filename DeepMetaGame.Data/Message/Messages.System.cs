using DeepCore;
using DeepCore.IO;
using DeepCore.Protocol;
using DeepMetaGame.Data.Template;
using System;
using System.Collections.Generic;

namespace DeepMetaGame.Data.Message
{


    [MessageType(BattleConstants.Ping)]
    public class Ping : BattleAction, SystemMessage
    {
        public double DayOfMS;
        public string provider;
        public byte[] input;
        protected override void OnDisposing()
        {
            DayOfMS = 0;
            provider = null;
            input = null;
        }
        public Ping()
        {
            DayOfMS = CUtils.TickTimeMS;
        }

        public void UpdateTime()
        {
            DayOfMS = CUtils.TickTimeMS;
        }

        public override void WriteExternal(IOutputStream output)
        {
            output.PutF64(DayOfMS);
            output.PutUTF(provider);
            if (input != null)
            {
                var zip = IOUtil.Zip(input);
                output.PutVS32(zip.Length);
                output.PutRawBytes(zip, 0, zip.Length);
            }
            else
            {
                output.PutVS32(0);
            }
        }
        public override void ReadExternal(IInputStream input)
        {
            DayOfMS = input.GetF64();
            provider = input.GetUTF();
            int count = input.GetVS32();
            if (count > 0)
            {
                byte[] zip = new byte[count];
                input.GetRawBytes(zip, 0, count);
                this.input = IOUtil.Unzip(zip);
            }
        }

    }

    [MessageType(BattleConstants.Pong)]
    public class Pong : BattleNotify, SystemMessage
    {
        public double ClientTimeDayOfMS;
        public double ServerTimeDayOfMS;
        public string output;
        protected override void OnDisposing()
        {
            ClientTimeDayOfMS = 0;
            ServerTimeDayOfMS = 0;
            output = default;
        }
        public Pong() { }
        public Pong Init(Ping ping)
        {
            this.ClientTimeDayOfMS = ping.DayOfMS;
            this.ServerTimeDayOfMS = CUtils.TickTimeMS;
            return this;
        }

        public int CurrentPing
        {
            get
            {
                var ctime = CUtils.TickTimeMS; ;
                return (int)(ctime - this.ClientTimeDayOfMS);
            }
        }

        public override void WriteExternal(IOutputStream output)
        {
            output.PutF64(ClientTimeDayOfMS);
            output.PutF64(ServerTimeDayOfMS);

            if (this.output != null)
            {
                var zip = IOUtil.Zip(CUtils.UTF8.GetBytes(this.output));
                output.PutVS32(zip.Length);
                output.PutRawBytes(zip, 0, zip.Length);
            }
            else
            {
                output.PutVS32(0);
            }
        }
        public override void ReadExternal(IInputStream input)
        {
            ClientTimeDayOfMS = input.GetF64();
            ServerTimeDayOfMS = input.GetF64();
            int count = input.GetVS32();
            if (count > 0)
            {
                byte[] zip = new byte[count];
                input.GetRawBytes(zip, 0, count);
                output = CUtils.UTF8.GetString(IOUtil.Unzip(zip));
            }
        }



    }

    [MessageType(BattleConstants.NetPong)]
    public class NetPong : BattleNotify, SystemMessage
    {
        public double ClientTimeDayOfMS;
        public double ServerTimeDayOfMS;
        protected override void OnDisposing()
        {
            ClientTimeDayOfMS = 0;
            ServerTimeDayOfMS = 0;
        }
        public NetPong() { }
        public NetPong Init(Ping ping)
        {
            this.ClientTimeDayOfMS = ping.DayOfMS;
            this.ServerTimeDayOfMS = CUtils.TickTimeMS;
            return this;
        }
        public float CurrentPing
        {
            get
            {
                var ctime = CUtils.TickTimeMS; ;
                return (float)(ctime - this.ClientTimeDayOfMS);
            }
        }

        public override void WriteExternal(IOutputStream output)
        {
            output.PutF64(ClientTimeDayOfMS);
            output.PutF64(ServerTimeDayOfMS);
        }
        public override void ReadExternal(IInputStream input)
        {
            ClientTimeDayOfMS = input.GetF64();
            ServerTimeDayOfMS = input.GetF64();
        }
    }

    /// <summary>
    /// 系统消息框，测试用
    /// </summary>
    [MessageType(BattleConstants.TestMessageBox)]
    public class TestMessageBox : BattleNotify, SystemMessage
    {
        public string msg;
        protected override void OnDisposing()
        {
            msg = default;
        }
        public TestMessageBox() { }
        public TestMessageBox Init(string msg)
        {
            this.msg = msg;
            return this;
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(msg);
        }
        public override void ReadExternal(IInputStream input)
        {
            msg = input.GetUTF();
        }
    }

    /// <summary>
    /// 通知客户端服务器运行状态
    /// </summary>
    [MessageType(BattleConstants.ServerStatusB2C)]
    public class ServerStatusB2C : BattleNotify, SystemMessage
    {
        public int ActiveGameObjectCount;
        public int ActiveInstanceZoneCount;
        public int AllocGameObjectCount;
        public int AllocInstanceZoneCount;
        protected override void OnDisposing()
        {
            ActiveGameObjectCount = 0;
            ActiveInstanceZoneCount = 0;
            AllocGameObjectCount = 0;
            AllocInstanceZoneCount = 0;
        }
        public ServerStatusB2C() { }
        public void Update(int activeObjectCount, int activeZoneCount, int allocObjectCount, int allocZoneCount)
        {
            ActiveGameObjectCount = activeObjectCount;
            ActiveInstanceZoneCount = activeZoneCount;
            AllocGameObjectCount = allocObjectCount;
            AllocInstanceZoneCount = allocZoneCount;
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutVS32(ActiveGameObjectCount);
            output.PutVS32(ActiveInstanceZoneCount);
            output.PutVS32(AllocGameObjectCount);
            output.PutVS32(AllocInstanceZoneCount);
        }
        public override void ReadExternal(IInputStream input)
        {
            ActiveGameObjectCount = input.GetVS32();
            ActiveInstanceZoneCount = input.GetVS32();
            AllocGameObjectCount = input.GetVS32();
            AllocInstanceZoneCount = input.GetVS32();
        }
    }

    /// <summary>
    /// 通知客户端服务器报错
    /// </summary>
    [MessageType(BattleConstants.ServerExceptionB2C)]
    public class ServerExceptionB2C : BattleNotify, SystemMessage
    {
        public string Message;
        public string StackTrace;
        protected override void OnDisposing()
        {
            Message = null;
            StackTrace = null;
        }
        public ServerExceptionB2C() { }
        public ServerExceptionB2C Init(string message, string stackTrace)
        {
            Message = message;
            StackTrace = stackTrace;
            return this;
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutUTF(Message);
            output.PutUTF(StackTrace);
        }
        public override void ReadExternal(IInputStream input)
        {
            Message = input.GetUTF();
            StackTrace = input.GetUTF();
        }
    }


    /// <summary>
    /// 打包一组消息，一次性发出
    /// </summary>
    [MessageType(BattleConstants.PackAction)]
    public class PackAction : ObjectAction
    {
        public readonly List<BattleAction> actions = new List<BattleAction>();
        protected override void OnDisposing(uint objID)
        {
            actions.Clear();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutList(actions, static (output, v) => output.PutExt(v));
        }
        public override void ReadExternal(IInputStream input)
        {
            actions.Clear();
            input.GetList(input => input.GetExt<BattleAction>(), (IList<BattleAction>)actions);
        }
        public override void BeforeWrite(TemplateManager templates)
        {
            foreach (var e in actions)
            {
                if (e is BattleAction oa) oa.BeforeWrite(templates);
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            foreach (var e in actions)
            {
                if (e is BattleAction oa) oa.EndRead(templates);
            }
        }
    }

    /// <summary>
    /// 打包一组消息，一次性发出
    /// </summary>
    [MessageType(BattleConstants.PackNotify)]
    public class PackNotify : BattleNotify, SystemMessage
    {
        public ulong sequenceNo;
        public readonly List<object> events = new List<object>();
        public override string ToString()
        {
            return $"PackEvent: no={sequenceNo} count={events.Count}";
        }
        protected override void OnDisposing()
        {
            sequenceNo = 0;
            events.Clear();
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutVU64(sequenceNo);
            output.PutVU32((uint)events.Count);
            foreach (var e in events)
            {
                if (e is IMessage msg)
                {
                    output.PutExt(msg);
                }
                else if (e is DeepCore.IO.MemoryStream mem)
                {
                    output.PutRawBytes(mem.GetBuffer(), 0, (int)mem.Length);
                }
            }
        }
        public override void ReadExternal(IInputStream input)
        {
            sequenceNo = input.GetVU64();
            uint count = input.GetVU32();
            events.Clear();
            events.Capacity = (int)count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    IMessage e = (IMessage)input.GetExtAny();
                    events.Add(e);
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message, err);
                    throw;
                }
            }
        }
        public override void BeforeWrite(TemplateManager templates)
        {
            foreach (var e in events)
            {
                if (e is IBattleMessage)
                {
                    (e as IBattleMessage).BeforeWrite(templates);
                }
            }
        }
        public override void EndRead(TemplateManager templates)
        {
            foreach (var e in events)
            {
                if (e is IBattleMessage)
                {
                    (e as IBattleMessage).EndRead(templates);
                }
            }
        }
    }


    [MessageType(BattleConstants.ZonePauseNotify)]
    public class ZonePauseNotify : BattleNotify, SystemMessage
    {
        public bool? Pause = false;
        public float? TimeScale = 1f;
        public ZonePauseNotify Init(bool? pause, float? timeScale)
        {
            this.Pause = pause;
            this.TimeScale = timeScale;
            return this;
        }
        protected override void OnDisposing()
        {
            Pause = false;
            TimeScale = 1f;
        }
        public override void WriteExternal(IOutputStream output)
        {
            output.PutNullable(Pause, static (o, v) => o.PutBool(v));
            output.PutNullable(TimeScale, static (o, v) => o.PutF32(v));
        }
        public override void ReadExternal(IInputStream input)
        {
            Pause = input.GetNullable(static (i) => i.GetBool());
            TimeScale = input.GetNullable(static (i) => i.GetF32());
        }
    }

}

