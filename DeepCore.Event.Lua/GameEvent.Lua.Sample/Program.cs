using DeepCore;
using DeepCore.GameEvent;
using DeepCore.GameEvent.Events;
using DeepCore.GameEvent.Lua;
using DeepCore.GameEvent.Message;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepCore.Template.SLua;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DeepCore.Template.MoonSharp;
using UnityEngine;

namespace GameEvent.Lua.Sample
{
    class Program
    {
        public class TestEventManagerFactory : EventManagerFactory
        {
            private Socket mServerSocket;
            private Socket mClientSocket;

            protected EventManager InternalCreateEventManager(string name, string id)
            {
                var sLua = new SLuaAdapter();
                switch (name)
                {
                    case "abc": return new EventManager(name, id);
                    case "hello": return new LuaEventManager(name, id, sLua);
                    case "world": return new LuaEventManager(name, id, sLua);
                    default: return new EventManager(name, id);
                }
            }

            public TestEventManagerFactory()
            {
                mServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                string host = "127.0.0.1";
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, Process.GetCurrentProcess().Id);
                mServerSocket.Bind(ipe);
                mServerSocket.Listen(5);
                StartListen();

                RegisterName("abc", InternalCreateEventManager);
                RegisterName("hello", InternalCreateEventManager);
                RegisterName("world", InternalCreateEventManager);
                //Thread.Sleep(2000);
                mClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //mClientSocket.Connect(ipe);
            }

            private void OnSendHandler(string to, byte[] bytes1)
            {
                if (!mClientSocket.Connected)
                {
                    var address = EventManagerAddress.Parse(to);
                    string host = "127.0.0.1";
                    IPAddress ip = IPAddress.Parse(host);
                    IPEndPoint ipe = new IPEndPoint(ip, int.Parse(address.UUID));
                    mClientSocket.Connect(ipe);
                    Console.WriteLine("connect to " + address.UUID);
                }

                mClientSocket.Send(bytes1);
            }

            public void Dispose()
            {
                mServerSocket.Dispose();
                mClientSocket.Dispose();
            }

            public void StartListen()
            {
                new Thread(StartListenSocket).Start();
            }

            private void StartListenSocket(object o)
            {
                var socket = mServerSocket.Accept();
                Console.WriteLine("connetcted ......");
                while (true)
                {
                    ContinuationAction(socket);
                    Thread.Sleep(1000);
                }
            }

            private void ContinuationAction(Socket socket)
            {
                Stream m = new MemoryStream();
                var numBytes = 0;
                var maxBuffer = 1024576;

                byte[] bytes = new byte[maxBuffer];
                numBytes = socket.Receive(bytes, bytes.Length, 0);
                m.Write(bytes, 0, numBytes);

                if (m.Length > 0)
                {
                    m.Seek(0, SeekOrigin.Begin);
                    var input = new InputStream(m, null);
                    try
                    {
                        //var broker = (RemoteEventMessageBroker) EventManager.MessageBroker;
                        //broker?.OnReciveMessage(input);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + e.StackTrace);
                    }
                }
            }
        }


        static void ProcessMain2(string id)
        {
            EventManagerFactory.SetFactory<TestEventManagerFactory>();
            var luaMgr = (LuaEventManager) EventManagerFactory.Instance.CreateEventManager("hello", Process.GetCurrentProcess().Id.ToString());
            luaMgr.Config = "event_script/config";
            luaMgr.CustomMainLua = "event_script/Main.lua";

            luaMgr.Start();


            var luaMgr1 = (LuaEventManager) EventManagerFactory.Instance.CreateEventManager("world", Process.GetCurrentProcess().Id.ToString());
            luaMgr1.Config = "event_script/config";
            luaMgr1.CustomMainLua = "event_script/Main.lua";

            luaMgr1.Start();


            //5秒后启动Test脚本 同时延迟2秒
            //luaMgr.StartEvent(new DelaySecEvent(5).ContinueWith((ee) =>
            //{
            //    //var remoteID1 = luaMgr1.StartJsonEvent(JsonHelper.Serialize(obj1), 10);

            //    var remoteID1 = luaMgr1.StartEvent(obj2);
            //    luaMgr.StartEvent(new DelaySecEvent(2).ContinueWith(() =>
            //    {
            //        //luaMgr1.StopEvent(remoteID1, false);
            //    }));
            //}));


            new Task(() =>
            {
                while (true)
                {
                    //rootact.Update();
                    luaMgr.Update();
                    luaMgr1.Update();
                    if (luaMgr.EventCount == 0 || luaMgr1.EventCount == 0)
                    {
                        GC.Collect();
                    }

                    Thread.Sleep(50);
                }
            }).Start();

            Console.ReadLine();
        }


        static void ProcessMain(string id)
        {
            var luaMgr = (LuaEventManager) EventManagerFactory.Instance.CreateEventManager("hello", Process.GetCurrentProcess().Id.ToString());
            luaMgr.Config = "event_script/config";
            luaMgr.CustomMainLua = "event_script/Main.lua";
            luaMgr.Start();


            var luaMgr1 = (LuaEventManager) EventManagerFactory.Instance.CreateEventManager("world", Process.GetCurrentProcess().Id.ToString());
            luaMgr1.Config = "event_script/config";
            luaMgr1.CustomMainLua = "event_script/Main.lua";
            luaMgr1.Start();


            new Task(() =>
            {
                while (true)
                {
                    //rootact.Update();
                    luaMgr.Update();
                    luaMgr1.Update();
                    if (luaMgr.EventCount == 0 || luaMgr1.EventCount == 0)
                    {
                        GC.Collect();
                    }

                    Thread.Sleep(50);
                }
            }).Start();


            Console.ReadLine();
        }

        public class TestUpdateLocker
        {
            private readonly object locker = new object();
            private LuaEventManager mTestLuaMgr;
            private Timer s_timer1;
            private Timer s_timer2;
            private Timer s_timer3;

            public void Start()
            {
                var luaMgr = (LuaEventManager) EventManagerFactory.Instance.CreateEventManager("hello", "abcdedf");
                luaMgr.Config = "event_script/config";
                luaMgr.CustomMainLua = "event_script/Main.lua";
                luaMgr.Start();
                mTestLuaMgr = luaMgr;
                s_timer1 = new Timer(timer_update, null, 50, 50);
                s_timer2 = new Timer(event_update, null, 50, 50);
                luaMgr.StartEvent(new TestLockEvent());
            }


            public class TestLockEvent : CustomEvent
            {
                protected override void OnStart()
                {
                    base.OnStart();
                    var tasklist = new List<Task>();
                    for (var i = 0; i < 100; i++)
                    {
                        var i1 = i;
                        tasklist.Add(Task.Run(() => { TriggerNow(i1); }));
                    }
                    //Task.WhenAll(tasklist).ContinueWith((t) =>
                    //{
                    //    Stop(true);
                    //});
                }

                protected override void OnUpdate(int ms)
                {
                    base.OnUpdate(ms);
                    TriggerNow(ms);
                }

                protected override void OnTriggered(UnionValue eventValue)
                {
                    base.OnTriggered(eventValue);
                    var msg = new SendNamedMessageEvent() {ManagerName = "hello", UUID = "abcdedf", MessageName = "TestLockEvent", Content = eventValue};
                    AddChild(msg);
                }
            }

            private void event_update(object state)
            {
                Thread.Sleep(50);
                var msg = new SendNamedMessageEvent() {ManagerName = "hello", UUID = "abcdedf", MessageName = "TickMS", Content = CUtils.TickTimeMS};
                lock (locker)
                {
                    mTestLuaMgr.StartEvent(msg);
                }
            }

            private void timer_update(object state)
            {
                //mTestLuaMgr.CreateEvent<SendNamedMessageEvent>();
                lock (locker)
                {
                    mTestLuaMgr.Update();
                    Thread.Sleep(50);
                }
            }
        }


        static void Main(string[] args)
        {
            //var v3 = new Vector3(3.3f, 4.5f, 55);
            //var v = UnionValueSerializer.Serialize(v3);
            //Console.WriteLine(v.Value.GetType());
            //return;
            EventManagerFactory.SetFactory<TestEventManagerFactory>();
            var tu = new TestUpdateLocker();
            tu.Start();
            return;
            if (args.Length == 1)
            {
                Console.WriteLine(" progress fork " + Process.GetCurrentProcess().Id);
                ProcessMain2(args[0]);
                return;
            }

            Console.WriteLine(" progress main " + Process.GetCurrentProcess().Id);
            ProcessMain("0");
            var path = Path.GetFullPath("bin/debug/GameEvent.Lua.Sample.exe");
            //var p = Process.Start(path, Process.GetCurrentProcess().Id.ToString());
            //AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => { p.Close(); };
            //ProcessMain(p.Id.ToString());
        }
    }
}