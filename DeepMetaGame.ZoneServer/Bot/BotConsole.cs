using DeepCore;
using DeepCore.Reflection;
using DeepCrystal.Command;

namespace DeepMetaGame.ZoneServer.Bot
{
    public class BotConsoleCommand : ConsoleCommandList
    {
        public static BotConsoleCommand Instance { get; private set; }
        private BotRunner server;
        public BotConsoleCommand(BotRunner server)
        {
            Instance = this;
            this.server = server;
        }

        public void MainLoop()
        {
            this.server.Start();
            try
            {
                base.MainLoop("使用cmdlist列出所有指令");
            }
            finally
            {
                this.server.Dispose();
            }
        }



        // -------------------------------------------------------------------------------------------------


        public abstract class Cmd : AbstractCommand
        {
        }

        [DescAttribute("清理内存")]
        public class CMD_GC : Cmd
        {
            public override string Key { get { return "gc"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                GC.Collect();
                output.WriteLine(string.Format("HEAP = {0}", CUtils.ToBytesSizeString(GC.GetTotalMemory(false))));
            }
        }

        //---------------------------------------------------------------------------------------------------
        [DescAttribute("添加机器人", "机器人")]
        public class CMD_AddBots : Cmd
        {
            public override string Key { get { return "add"; } }
            public override string Help { get { return "  add <数量> <阵营> <模板组>"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                int count = 1;
                int force = 2;
                List<int> templates = null;
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    var args = ToArgs(arg);
                    if (args.Length >= 1)
                    {
                        count = Parser.ParseInt(args[0]);
                    }
                    if (args.Length >= 2)
                    {
                        force = Parser.ParseInt(args[1]);
                    }
                    if (args.Length >= 3)
                    {
                        Parser.TryStringToObject(args[2], out templates);
                    }
                }
                BotRunner.Instance.AddBots(count, force, templates);
            }
        }
        [DescAttribute("清理机器人", "机器人")]
        public class CMD_CleanupBots : Cmd
        {
            public override string Key { get { return "clean"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.WriteLine("stop bots " + BotRunner.Instance.BotsCount);
                BotRunner.Instance.StopAllBots();
            }
        }
        [DescAttribute("机器人状态", "机器人")]
        public class CMD_ListBots : Cmd
        {
            public override string Key { get { return "ls"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.WriteLine(BotRunner.Instance.BotsStatus);
            }
        }

        //---------------------------------------------------------------------------------------------------
        [DescAttribute("开始批量增加定时器", "机器人")]
        public class CMD_StartTimer : Cmd
        {
            private static AtomicState<bool> mIsRunning = new AtomicState<bool>(false, static (a, b) => a == b);
            public static bool IsRunning
            {
                get { return mIsRunning.Value; }
                set { mIsRunning.Update(value); }
            }

            public override string Key { get { return "start"; } }
            public override string Help { get { return "  start <数量> <阵营组> <模板组>"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                var args = ToArgs(arg);
                int count = Parser.ParseInt(args[0]);
                List<int> forces = null;
                List<int> templates = null;
                Parser.TryStringToObject(args[1], out forces);
                Parser.TryStringToObject(args[2], out templates);
                Random rd = new Random();
                IsRunning = true;
                BotRunner.Instance.AddBots(count, rd.GetRandomInCollection(forces), templates);
                System.Threading.Thread timer = new System.Threading.Thread((o) =>
                {
                    System.Threading.Thread.Sleep(1000);
                    while (IsRunning)
                    {
                        try
                        {
                            var list = BotRunner.Instance.BotsList;
                            foreach (var bot in list)
                            {
                                if (bot.IsRunning)
                                {
                                    BotRunner.Instance.StopBot(bot);
                                    break;
                                }
                            }
                            System.Threading.Thread.Sleep(1000);
                            BotRunner.Instance.AddBots(1, rd.GetRandomInCollection(forces), templates);
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message + "\n" + err.StackTrace);
                        }
                    }
                });
                timer.Start();
            }
        }
        [DescAttribute("停止批量增加定时器", "机器人")]
        public class CMD_StopTimer : Cmd
        {
            public override string Key { get { return "stop"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                CMD_StartTimer.IsRunning = false;
            }
        }
        //---------------------------------------------------------------------------------------------------


    }
}
