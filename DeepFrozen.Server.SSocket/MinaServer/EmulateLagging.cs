using DeepCore.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DeepFrozen.Server.SSocket.NetServer
{
    public class EmulateLagging
    {
        private Logger log = LoggerFactory.GetLogger("EmulateLagging");

        private TaskRunner<EmuSendPack> emu_send_thread;
        private TaskRunner<EmuRecvPack> emu_recv_thread;

        private int mLagMinTimeMS = 0;
        private int mLagMaxTimeMS = 0;
        private static Random random = new Random();

        public bool EmulateLag
        {
            get { return mLagMaxTimeMS > 0; }
        }

        public EmulateLagging()
        {
            this.emu_send_thread = new TaskRunner<EmuSendPack>(this);
            this.emu_recv_thread = new TaskRunner<EmuRecvPack>(this);
        }
        public void SetEmulateLaggingMS(int min, int max)
        {
            this.mLagMinTimeMS = Math.Min(min, max);
            this.mLagMaxTimeMS = Math.Max(min, max);
            if (EmulateLag == false)
            {
                emu_send_thread.Flush();
                emu_recv_thread.Flush();
            }
        }
        public void GetEmulateLaggingMS(out int min, out int max)
        {
            min = this.mLagMinTimeMS;
            max = this.mLagMaxTimeMS;
        }
        public void Start()
        {
            this.emu_send_thread.Start();
            this.emu_recv_thread.Start();
        }
        public void Dispose()
        {
            this.emu_send_thread.Dispose();
            this.emu_recv_thread.Dispose();
        }

        public void SendDelay(SSMniaSession session, DeepCore.IO.MemoryStream stream)
        {
            emu_send_thread.Enqueue(new EmuSendPack(session, stream));
        }
        public void RecvDelay(SSMniaSession session, object obj)
        {
            emu_recv_thread.Enqueue(new EmuRecvPack(session, obj));
        }

        //-----------------------------------------------------------------------------------------------------

        private interface IRunnable
        {
            void Run();
        }

        private class TaskRunner<T> where T : IRunnable
        {
            private Logger log = LoggerFactory.GetLogger("EmulateLagging");
            private bool running = false;
            private readonly EmulateLagging owner;
            private readonly Thread thread;
            private readonly Queue<T> queue = new Queue<T>();

            public TaskRunner(EmulateLagging owner)
            {
                this.owner = owner;
                this.thread = new Thread(loop);
            }

            public void Start()
            {
                this.running = true;
                this.thread.Start();
            }
            public void Dispose()
            {
                this.running = false;
                if (thread.IsAlive)
                {
                    lock (queue)
                    {
                        Monitor.Pulse(queue);
                    }
                    this.thread.Join();
                }
            }
            public void Enqueue(T task)
            {
                lock (queue)
                {
                    queue.Enqueue(task);
                    Monitor.Pulse(queue);
                }
            }
            public void Flush()
            {
                lock (queue)
                {
                    while (queue.Count > 0)
                    {
                        try
                        {
                            var pak = queue.Dequeue();
                            pak.Run();
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                    }
                }
            }

            private void loop()
            {
                while (running)
                {
                    try
                    {
                        lock (queue)
                        {
                            while (queue.Count > 0)
                            {
                                var pak = queue.Dequeue();
                                pak.Run();
                            }
                            Monitor.Wait(queue, 1000);
                        }
                        Thread.Sleep(random.Next(owner.mLagMinTimeMS, owner.mLagMaxTimeMS));
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }


        private class EmuSendPack : IRunnable
        {
            private SSMniaSession session;
            private DeepCore.IO.MemoryStream stream;
            public EmuSendPack(SSMniaSession session, DeepCore.IO.MemoryStream stream)
            {
                this.session = session;
                this.stream = stream;
            }
            public void Run()
            {
                try
                {
                    session.Send(stream.GetBuffer(), 0, (int)stream.Position);
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }
        private class EmuRecvPack : IRunnable
        {
            private SSMniaSession session;
            private object obj;
            public EmuRecvPack(SSMniaSession session, object obj)
            {
                this.session = session;
                this.obj = obj;
            }
            public void Run()
            {
                try
                {
                    session.Listener.OnReceivedMessage(session, obj);
                }
                finally { }
            }
        }
    }
}
