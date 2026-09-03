using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DeepCrystal.Command
{
    //-------------------------------------------------------------------------------------------------------------------
    #region Command
    //-------------------------------------------------------------------------------------------------------------------
    [Reflectible]
    public abstract class AbstractCommand
    {
        public abstract string Key { get; }
        public virtual void DoCommand(string arg, TextWriter output) { }
        public virtual Task DoCommandAsync(string arg, TextWriter output)
        {
            this.DoCommand(arg, output);
            return Task.CompletedTask;
        }
        public AbstractCommandList CmdList { get; internal set; }
        public virtual string Help { get => string.Empty; }
        public override string ToString()
        {
            return $"{Key}\t{Help}";
        }
    }
    public abstract class AbstractCommand<T> : AbstractCommand where T : AbstractCommandList
    {
        new public T CmdList { get => base.CmdList as T; }
    }
    //-------------------------------------------------------------------------------------------------------------------
    public abstract class NameCommand : AbstractCommand
    {
        public override string Key => GetType().Name;
    }
    public abstract class NameCommand<T> : NameCommand where T : AbstractCommandList
    {
        new public T CmdList { get => base.CmdList as T; }
    }
    //-------------------------------------------------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandMethodAttribute : System.Attribute
    {
        public readonly string Desc = "";
        public readonly string Helper = "";
        public CommandMethodAttribute(string desc = "", string helper = "")
        {
            this.Desc = desc;
            this.Helper = helper;
        }
    }
    #endregion
    //-------------------------------------------------------------------------------------------------------------------
    public abstract class AbstractCommandList
    {
        class CommandMethodInfo
        {
            public object obj;
            public CommandMethodAttribute attr;
            public MethodInfo method;
        }
        private SortedDictionary<string, CommandMethodInfo> methods_list = new SortedDictionary<string, CommandMethodInfo>();
        private SortedDictionary<string, AbstractCommand> cmd_list = new SortedDictionary<string, AbstractCommand>();
        private static Regex split = new Regex(@"\s+");
        public DirectoryInfo OutputDir { get; set; }
        private Logger log = new LazyLogger("CommandList");
        #region Events
        private Func<string, bool> event_OnHandleUnknowCommand;
        private Func<string, Task<bool>> event_OnHandleUnknowCommandAsync;
        private Action<string, AbstractCommand> event_OnHandleCommand;
        public event Func<string, bool> OnHandleUnknowCommand
        {
            add { event_OnHandleUnknowCommand += value; }
            remove { event_OnHandleUnknowCommand -= value; }
        }
        public event Func<string, Task<bool>> OnHandleUnknowCommandAsync
        {
            add { event_OnHandleUnknowCommandAsync += value; }
            remove { event_OnHandleUnknowCommandAsync -= value; }
        }
        public event Action<string, AbstractCommand> OnHandleCommand
        {
            add { event_OnHandleCommand += value; }
            remove { event_OnHandleCommand -= value; }
        }
        #endregion
        public AbstractCommandList(params Type[] types)
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            this.RegistMethods(this);
            if (types == null || types.Length == 0)
            {
                this.RegistNestedTypes(GetType());
            }
            else
            {
                this.RegistTypes(types);
            }
        }
        protected virtual void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            log.Error("TaskScheduler_UnobservedTaskException");
            log.Error(e.Exception);
        }
        protected virtual void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            log.Error("AppDomain_UnhandledException");
            if (e.ExceptionObject is Exception err)
            {
                log.Error(err);
            }
            else
            {
                log.Error(e.ToString());
            }
        }

        public void RegistMethods(object obj)
        {
            var type = obj.GetType();
            var mts = type.GetMethods((BindingFlags)(0x7FFFFFFF) ^ BindingFlags.DeclaredOnly);
            foreach (var m in mts)
            {
                if (m.TryGetAttribute<CommandMethodAttribute>(out var attr))
                {
                    methods_list.Add(m.Name.ToLower(), new CommandMethodInfo() { obj = obj, attr = attr, method = m });
                }
            }
        }
        public void RegistCommand(Type tcmd)
        {
            if (tcmd != null && typeof(AbstractCommand).IsAssignableFrom(tcmd) && !tcmd.IsAbstract)
            {
                try
                {
                    var cmd = ReflectionUtil.CreateInstance(tcmd) as AbstractCommand;
                    if (cmd != null)
                    {
                        cmd.CmdList = this;
                        cmd_list.Add(cmd.Key.ToLower(), cmd);
                    }
                }
                catch (Exception err)
                {
                    err.PrintStackTrace();
                }
            }
        }
        public void RegistTypes(Type[] types)
        {
            foreach (Type tcmd in types)
            {
                RegistCommand(tcmd);
            }
        }
        public void RegistTypes(Type baseTypes)
        {
            List<Type> allcmd = ReflectionUtil.GetNoneVirtualSubTypes(baseTypes);
            RegistTypes(allcmd.ToArray());
        }
        public void RegistNestedTypes(Type listType)
        {
            RegistTypes(listType.GetNestedTypes());
            if (listType.BaseType != null)
            {
                RegistNestedTypes(listType.BaseType);
            }
        }
        public void RemoveType(Type type)
        {
            foreach (var e in cmd_list)
            {
                if (e.Value.GetType() == type)
                {
                    cmd_list.Remove(e.Key.ToLower());
                }
            }
        }
        public bool DoCommand(string line, out string output)
        {
            var sb = new StringWriter();
            var ret = DoCommand(line, sb);
            output = sb.ToString();
            return ret;
        }
        public bool DoCommand(string line, TextWriter output)
        {
            var args = ToArgs(line);
            if (methods_list.TryGetValue(args[0], out var m))
            {
                try
                {
                    var gargs = new string[args.Length - 1];
                    Array.Copy(args, 1, gargs, 0, gargs.Length);
                    var targs = m.method.GetParameters();
                    var pargs = new object[targs.Length];
                    for (int i = 0; i < targs.Length && i < gargs.Length; i++)
                    {
                        try
                        {
                            pargs[i] = Parser.StringToObject(gargs[i], targs[i].ParameterType);
                        }
                        catch (Exception err)
                        {
                            output.WriteLine($"parse args error : '{gargs[i]}' => {targs[i].ParameterType} : {err.Message}");
                        }
                    }
                    var result = m.method.Invoke(m.obj, pargs);
                    if (result is Task task)
                    {
                        try
                        {
                            task.Wait();
                            if (task.IsCompleted)
                            {
                                try
                                {
                                    dynamic tr = task;
                                    return tr.Result;
                                }
                                catch
                                {
                                    result = string.Empty;
                                }
                            }
                            else if (task.IsFaulted)
                            {
                                result = task.Exception.ToFullMessage();
                            }
                        }
                        catch (Exception ex)
                        {
                            result = ex.ToFullMessage();
                        }
                    }
                    if (result != null)
                    {
                        output.Write($"{result}");
                    }
                    try
                    {
                        if (OutputDir != null)
                        {
                            DeepCore.IO.CFiles.CreateDir(OutputDir);
                            File.WriteAllText(OutputDir.FullName + Path.DirectorySeparatorChar + args[0] + ".txt", $"{result}", CUtils.UTF8);
                        }
                    }
                    catch { }
                }
                catch (Exception err)
                {
                    output.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                }
                return true;
            }
            foreach (var cmd in cmd_list.Values)
            {
                try
                {
                    if (TryParseCommand(line, cmd.Key, out var arg))
                    {
                        using (var sb = new StringWriter())
                        {
                            try
                            {
                                cmd.DoCommand(arg, sb);
                                try
                                {
                                    if (OutputDir != null)
                                    {
                                        DeepCore.IO.CFiles.CreateDir(OutputDir);
                                        File.WriteAllText(OutputDir.FullName + Path.DirectorySeparatorChar + cmd.Key + ".txt", sb.ToString(), CUtils.UTF8);
                                    }
                                }
                                catch { }
                            }
                            catch (Exception err)
                            {
                                output.WriteLine(cmd);
                                sb.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                            }
                            finally
                            {
                                output.Write(sb.ToString());
                                event_OnHandleCommand?.Invoke(line, cmd);
                            }
                        }
                        return true;
                    }
                }
                catch (Exception err)
                {
                    output.WriteLine(err.Message + Environment.NewLine + err.StackTrace);
                    output.WriteLine(cmd);
                }
            }
            if (event_OnHandleUnknowCommand != null)
            {
                return event_OnHandleUnknowCommand.Invoke(line);
            }
            if (event_OnHandleUnknowCommandAsync != null)
            {
                return event_OnHandleUnknowCommandAsync.Invoke(line).WaitForResult();
            }
            return false;
        }


        public string ListCommand()
        {
            return ListCommand(string.Empty);
        }
        public string ListCommand(string prefix)
        {
            prefix = prefix.ToLower();
            StringBuilder sb = new StringBuilder();
            {
                var catgory_map = new SortedDictionary<string, List<AbstractCommand>>();
                foreach (var cmd in this.cmd_list.Values)
                {
                    var desc = PropertyUtil.GetAttribute<DescAttribute>(cmd.GetType());
                    var catgory = (desc == null || string.IsNullOrEmpty(desc.Category)) ? string.Empty : desc.Category;
                    if (!catgory_map.TryGetValue(catgory, out var catgoryList))
                    {
                        catgoryList = new List<AbstractCommand>();
                        catgory_map.Add(catgory, catgoryList);
                    }
                    catgoryList.Add(cmd);
                }
                foreach (var catgory in catgory_map)
                {
                    if (!string.IsNullOrEmpty(catgory.Key)) sb.AppendLine("[" + catgory.Key + "]");
                    bool show_prefix = string.IsNullOrEmpty(prefix);
                    foreach (var cmd in catgory.Value)
                    {
                        if (show_prefix || cmd.Key.StartsWith(prefix))
                        {
                            sb.Append(cmd.Key);
                            var desc = PropertyUtil.GetAttribute<DescAttribute>(cmd.GetType());
                            if (desc != null)
                            {
                                if (!string.IsNullOrEmpty(desc.Desc))
                                {
                                    sb.Append("\t- " + desc.Desc);
                                }
                                if (!string.IsNullOrEmpty(desc.Detail))
                                {
                                    sb.AppendLine();
                                    sb.Append("\t- " + desc.Detail);
                                }
                            }
                            if (!string.IsNullOrEmpty(cmd.Help))
                            {
                                var helps = cmd.Help.Split('\n');
                                foreach (var h in helps)
                                {
                                    sb.AppendLine();
                                    sb.Append("\t  " + h);
                                }
                            }
                            sb.AppendLine();
                        }
                    }
                }
            }
            {
                var catgory_map = new SortedDictionary<string, List<CommandMethodInfo>>();
                foreach (var cmd in this.methods_list.Values)
                {
                    var desc = PropertyUtil.GetAttribute<DescAttribute>(cmd.method);
                    var catgory = (desc == null || string.IsNullOrEmpty(desc.Category)) ? string.Empty : desc.Category;
                    if (!catgory_map.TryGetValue(catgory, out var catgoryList))
                    {
                        catgoryList = new List<CommandMethodInfo>();
                        catgory_map.Add(catgory, catgoryList);
                    }
                    catgoryList.Add(cmd);
                }
                foreach (var catgory in catgory_map)
                {
                    if (!string.IsNullOrEmpty(catgory.Key)) sb.AppendLine("[" + catgory.Key + "]");
                    bool show_prefix = string.IsNullOrEmpty(prefix);
                    foreach (var method in catgory.Value)
                    {
                        if (show_prefix || method.method.Name.StartsWith(prefix))
                        {
                            sb.Append(method.method.Name);
                            if (!string.IsNullOrEmpty(method.attr.Desc))
                            {
                                sb.Append("\t- " + method.attr.Desc);
                            }
                            foreach (var parm in method.method.GetParameters())
                            {
                                sb.AppendLine();
                                sb.Append($"\t  <{parm.Name.ToLower()}> : {parm.ParameterType.Name}");
                            }
                            if (!string.IsNullOrEmpty(method.attr.Helper))
                            {
                                var helps = method.attr.Helper.Split('\n');
                                foreach (var h in helps)
                                {
                                    sb.AppendLine();
                                    sb.Append("\t  " + h);
                                }
                            }
                            sb.AppendLine();
                        }
                    }
                }
            }
            return sb.ToString();
        }

        public static bool TryParseCommand(string line, string command, out string arg)
        {
            if (line.StartsWith(command, CUtils.StringComparisonIgnoreCase))
            {
                arg = line.Substring(command.Length);
                if (arg.Length > 0)
                {
                    //保证命令后参数跟一个空白字符//
                    var mat = split.Match(arg);
                    if (mat.Success && mat.Index == 0)
                    {
                        arg = arg.Trim();
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            arg = null;
            return false;
        }
        public static string[] ToArgs(string arg)
        {
            return split.Split(arg);
        }
        //---------------------------------------------------------------------------------------------------------------
        [Desc("列出所有命令")]
        public class CMD_LIST : AbstractCommand
        {
            public override string Key { get { return "cmdlist"; } }
            public override string Help
            {
                get
                {
                    return
                        "cmdlist 列出所有控制台命令\n" +
                        "cmdlist [前缀] 列出所有前缀相符的控制台命令";
                }
            }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.WriteLine(CmdList.ListCommand(arg));
            }
        }
        [Desc("列出所有命令")]
        public class CMD_HELP : AbstractCommand
        {
            public override string Key { get { return "help"; } }
            public override string Help
            {
                get
                {
                    return
                        "help 列出所有控制台命令\n" +
                        "help [前缀] 列出所有前缀相符的控制台命令";
                }
            }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.WriteLine(CmdList.ListCommand(arg));
            }
        }
        [Desc("列出所有命令")]
        public class CMD_HELP_ : AbstractCommand
        {
            public override string Key { get { return "?"; } }
            public override string Help
            {
                get
                {
                    return
                        "? 列出所有控制台命令\n" +
                        "? [前缀] 列出所有前缀相符的控制台命令";
                }
            }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.WriteLine(CmdList.ListCommand(arg));
            }
        }
        //---------------------------------------------------------------------------------------------------------------
    }
    //-------------------------------------------------------------------------------------------------------------------
    public class ConsoleCommandList : AbstractCommandList
    {
        public event Action<ConsoleCommandList> Done { add { event_Done += value; } remove { event_Done -= value; } }
        private Action<ConsoleCommandList> event_Done;
        private bool exit_mainloop = false;
        private bool is_done = false;
        private TaskCompletionSource<ConsoleCommandList> tcs_done;
        public bool IsExit { get => exit_mainloop; }
        public bool IsDone { get => is_done; }
        public ConsoleCommandList(params Type[] types) : base(types)
        {
        }
        public void MainLoop(string helper = null)
        {
            this.MainLoop(Console.In, Console.Out, helper);
        }
        public void MainLoop(TextReader input, TextWriter output, string helper = null)
        {
            tcs_done = new TaskCompletionSource<ConsoleCommandList>();
            exit_mainloop = false;
            is_done = false;
            try
            {
                if (helper != null)
                {
                    output.WriteLine(helper);
                }
                output.Write(">");
                while (!exit_mainloop)
                {
                    try
                    {
                        if (TryReadLine(input, out var cmd))
                        {
                            try
                            {
                                if (!DoCommand(cmd, output))
                                {
                                    output.WriteLine();
                                    output.WriteLine("Unknow Command ：" + cmd);
                                    if (helper != null) output.WriteLine(helper);
                                }
                                else
                                {
                                    output.WriteLine();
                                }
                            }
                            finally
                            {
                                output.Write(">");
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        err.PrintStackTrace(output);
                    }
                    finally
                    {
                        Thread.Sleep(100);
                        Thread.Yield();
                    }
                }
            }
            catch (Exception err)
            {
                tcs_done.TrySetException(err);
            }
            finally
            {
                is_done = true;
                event_Done?.Invoke(this);
                tcs_done.TrySetResult(this);
            }
        }
        protected virtual bool TryReadLine(TextReader input, out string cmd)
        {
            cmd = String.Empty;
            try { cmd = input.ReadLine(); } catch { }
            return !string.IsNullOrEmpty(cmd);
        }
        public virtual void PostExitMainLoop()
        {
            this.exit_mainloop = true;
        }
        public async Task WaitForExitAsync()
        {
            if (tcs_done != null)
            {
                await tcs_done.Task;
            }
        }
        public void WaitForExit()
        {
            WaitForExitAsync().Wait();
        }
        [Desc("关闭")]
        public class CMD_EXIT : AbstractCommand
        {
            public override string Key { get { return "exit"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (CmdList is ConsoleCommandList cmdlist) { cmdlist.PostExitMainLoop(); }
            }
        }
        [Desc("清屏")]
        public class CMD_CLS : AbstractCommand
        {
            public override string Key { get { return "cls"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                Console.Clear();
            }
        }
    }
    //-------------------------------------------------------------------------------------------------------------------

    public class ServerConsoleCommandList : ConsoleCommandList
    {
        [Desc("Get Current Process Info")]
        public class CMD_PI : AbstractCommand
        {
            public override string Key { get { return "pi"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                TypeAllocRecorder.PrintProcessStatus(output, System.Diagnostics.Process.GetCurrentProcess(), " ", 32);
            }
        }
        [Desc("Show Alloc Info")]
        public class CMD_AC : AbstractCommand
        {
            public override string Key { get { return "ac"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.PrintLineSeparator();
                TypeAllocRecorder.PrintMemoryStatus(output);
                output.PrintLineSeparator();
            }
        }
        [Desc("Show Alloc Info And GC")]
        public class CMD_GC : AbstractCommand
        {
            public override string Key { get { return "gc"; } }
            public override string Help { get { return "gc <generation>"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (Parser.TryParseInt(arg, out var gen)) { GC.Collect(gen); }
                else { GC.Collect(); }
                output.PrintLineSeparator();
                TypeAllocRecorder.PrintMemoryStatus(output);
                output.PrintLineSeparator();
            }
        }

        [Desc("Set Alloc Info ON/OFF")]
        public class CMD_ALLOC_ON : AbstractCommand
        {
            public override string Key { get { return "alloc"; } }
            public override string Help { get { return "alloc <1:0(on:off)>"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (Parser.TryParseBool(arg, out var gen))
                {
                    TypeAllocRecorder.AllEnable(gen);
                }
                else
                {
                    TypeAllocRecorder.AllEnable(true);
                }
                output.PrintLineSeparator();
                TypeAllocRecorder.PrintMemoryStatus(output);
                output.PrintLineSeparator();
            }
        }


        [Desc("Set Alloc Verbos ON/OFF")]
        public class CMD_ALLOC_VERBOS : AbstractCommand
        {
            public override string Key { get { return "allov"; } }
            public override string Help { get { return "allov <1:0(on:off)>"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                if (Parser.TryParseBool(arg, out var gen))
                {
                    TypeAllocRecorder.AllVerbos(gen);
                }
                else
                {
                    TypeAllocRecorder.AllVerbos(true);
                }
                output.PrintLineSeparator();
                TypeAllocRecorder.PrintMemoryStatus(output);
                output.PrintLineSeparator();
            }
        }


        [Desc("Show MemoryPool Info")]
        public class CMD_POOL : AbstractCommand
        {
            public override string Key { get { return "pool"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                output.PrintLineSeparator();
                ObjectPools.PrintStatus(output);
                output.PrintLineSeparator();
            }
        }
        [Desc("Clear MemoryPool")]
        public class CMD_POOL_CLEAR : AbstractCommand
        {
            public override string Key { get { return "poolc"; } }
            public override void DoCommand(string arg, TextWriter output)
            {
                ObjectPools.ClearPool();
                output.PrintLineSeparator();
                ObjectPools.PrintStatus(output);
                output.PrintLineSeparator();
            }
        }

        //         [Desc("Show IO Statistics Info")]
        //         public class CMD_SIO : AbstractCommand
        //         {
        //             //private Type etype = typeof(DeepCore.IO.IOStream.StatisticsSortField);
        //             public override string Key { get { return "sio"; } }
        //             public override string Help
        //             {
        //                 get
        //                 {
        //                     return "sio <sort(" + CUtils.ListToString(Enum.GetNames(etype)) + ")>";
        //                 }
        //             }
        //             public override void DoCommand(string arg, TextWriter output)
        //             {
        // //                 var sort = DeepCore.IO.IOStream.StatisticsSortField.SENDB;
        // //                 if (CUtils.TryParseEnum(etype, arg, true, out var sortobj))
        // //                 {
        // //                     sort = (DeepCore.IO.IOStream.StatisticsSortField)sortobj;
        // //                 }
        // //                 DeepCore.IO.IOStream.PrintStatisticsStatus(output, sort, " ", 64, 150);
        //             }
        //         }
        [Desc("Show ORM Statistics Info")]
        public class CMD_OST : AbstractCommand
        {
            private Type etype = typeof(DeepCrystal.ORM.ORMStatistics.StatisticsSortField);
            public override string Key { get { return "ost"; } }
            public override string Help
            {
                get
                {
                    return "ost <sort(" + CUtils.ListToString(Enum.GetNames(etype)) + ")>";
                }
            }
            public override void DoCommand(string arg, TextWriter output)
            {
                var sort = DeepCrystal.ORM.ORMStatistics.StatisticsSortField.SAVE;
                if (CUtils.TryParseEnum(etype, arg, true, out var sortobj))
                {
                    sort = (DeepCrystal.ORM.ORMStatistics.StatisticsSortField)sortobj;
                }
                DeepCrystal.ORM.ORMStatistics.PrintStatisticsStatus(output, sort, " ", 64, 150);
            }
        }
        [Desc("Show TimeStatisticsRecoder Info")]
        public class CMD_STT : AbstractCommand
        {
            private Type etype = typeof(DeepCore.Statistics.TimeStatisticsRecoder.SortField);
            public override string Key { get { return "stt"; } }
            public override string Help
            {
                get
                {
                    return "stt <sort(" + CUtils.ListToString(Enum.GetNames(etype)) + ")>";
                }
            }
            public override void DoCommand(string arg, TextWriter output)
            {
                var sort = DeepCore.Statistics.TimeStatisticsRecoder.SortField.MAX;
                if (CUtils.TryParseEnum(etype, arg, true, out var sortobj))
                {
                    sort = (DeepCore.Statistics.TimeStatisticsRecoder.SortField)sortobj;
                }
                DeepCore.Statistics.TimeStatisticsRecoder.PrintAllStatus(output, sort, " ", 64, 150);
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------



}
