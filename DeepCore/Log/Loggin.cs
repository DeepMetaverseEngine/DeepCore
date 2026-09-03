using DeepCore.IO;
using DeepCore.Protocol;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using static DeepCore.Colors;

namespace DeepCore.Log
{
    public class LoggerFactory
    {
        public static LoggerFactory CurrentFactory { get; private set; } = new LoggerFactory();

        private uint mLevelFlag = 0xFFFFFFFF;
        public uint Level { get { return mLevelFlag; } }
        public void SetLevelFlag(uint level)
        {
            mLevelFlag = level;
        }
        public void SetLevelFlag(LoggerLevel level)
        {
            mLevelFlag = (uint)level;
        }
        public void SetLevelEnable(LoggerLevel level)
        {
            mLevelFlag |= (uint)level;
        }
        public void SetLevelDisable(LoggerLevel level)
        {
            mLevelFlag &= (~(uint)level);
        }
        public static void SetFactory(LoggerFactory factory)
        {
            CurrentFactory = factory;
        }
        public static Logger GetLogger(string name)
        {
            return CurrentFactory.CreateLogger(name);
        }
        public static Logger GetLogger(object owner)
        {
            return CurrentFactory.CreateLogger(owner);
        }

        virtual protected Logger CreateLogger(object owner)
        {
            Logger logger = new ConsoleLogger(owner);
            return logger;
        }

        public static Logger Dummy { get { return new DummyLogger(); } }

    }

    public class DummyLogger : Logger
    {
        public DummyLogger() : base(LoggerFactory.CurrentFactory, string.Empty) { }
        protected internal override void Print(LoggerLevel level, object format, string text, Exception err) { }
    }

    public enum LoggerLevel : uint
    {
        FINE     /**/= 0x00000001,
        DEBUG    /**/= 0x00000002,
        TRACE    /**/= 0x00000004,
        LOG      /**/= 0x00000008,
        INFO     /**/= 0x00000010,
        WARNNING /**/= 0x00000020,
        ERROR    /**/= 0x00000040,
        FATAL    /**/= 0x00000080,
        BI       /**/= 0x00000100,
        RELEASE  /**/= INFO | WARNNING | ERROR | FATAL | BI,
        ALL      /**/= 0xFFFFFFFF, // 全部日志
        ALL_ERROR /**/= ERROR | FATAL, // 仅错误日志
    }

    public abstract class Logger
    {
        private LoggerFactory mParent;
        private List<Logger> mBindLogs;
        protected readonly string mName;
        protected readonly object mOwner;
        public uint Level { get { return mParent.Level; } }
        public string Name { get => mName; }
        public object Owner { get => mOwner; }
        protected Logger(LoggerFactory parent, object owner)
        {
            this.mParent = parent;
            this.mOwner = owner;
            this.mName = owner.ToString();
        }
        //---------------------------------------------------------------------------------------
        private Stack<ConsoleColor?> colors;
        public virtual ConsoleColor? Color { get; set; } = null;
        public virtual void PushColor()
        {
            if (colors == null) colors = new();
            colors.Push(this.Color);
        }
        public virtual void PopColor()
        {
            if (colors != null && colors.Count > 0)
            {
                this.Color = colors.Pop();
            }
        }
        //---------------------------------------------------------------------------------------
        public void Attach(Logger log)
        {
            if (mBindLogs == null) mBindLogs = new List<Logger>();
            mBindLogs.Add(log);
        }
        public void Detach(Logger log)
        {
            if (mBindLogs != null)
            {
                mBindLogs.Remove(log);
            }
        }
        public delegate void AttachLoggerPrint(Logger log, LoggerLevel level, object format, string text, Exception err);
        protected void PrintAttach(LoggerLevel level, object format, string text, Exception err, AttachLoggerPrint action)
        {
            if (mBindLogs != null)
            {
                foreach (var log in mBindLogs) { action(log, level, format, text, err); }
            }
        }
        //---------------------------------------------------------------------------------------
        public bool IsDebugEnabled { get { return IsLevelEnable(LoggerLevel.DEBUG); } }
        public bool IsErrorEnabled { get { return IsLevelEnable(LoggerLevel.ERROR); } }
        public bool IsFatalEnabled { get { return IsLevelEnable(LoggerLevel.FATAL); } }
        public bool IsInfoEnabled { get { return IsLevelEnable(LoggerLevel.INFO); } }
        public bool IsWarnEnabled { get { return IsLevelEnable(LoggerLevel.WARNNING); } }
        public bool IsBIEnabled { get { return IsLevelEnable(LoggerLevel.BI); } }
        //---------------------------------------------------------------------------------------
        public bool IsLevelEnable(LoggerLevel level)
        {
            return (((uint)level & mParent.Level) != 0);
        }
        //---------------------------------------------------------------------------------------
        #region Internal
        public Func<object, string> Decoder;
        internal string DecodeFormat(object format)
        {
            if (format is Exception error)
            {
                return error.Message;
            }
            else if (format == null)
            {
                return "null";
            }
            else if (format is string str)
            {
                return str;
            }
            else if (format.GetType().IsPrimitive)
            {
                return format.ToString();
            }
            else if (format.GetType().IsEnum)
            {
                return format.ToString();
            }
            else if (Decoder != null)
            {
                return Decoder(format);
            }
            else
            {
                return format.ToString();
            }
        }
        protected void LogLevelInternal(LoggerLevel level, IFormatProvider provider, object format, Exception exception)
        {
            if (IsLevelEnable(level))
            {
                string message = DecodeFormat(format);
                Print(level, format, message, exception);
            }
        }
        protected void LogLevelInternal(LoggerLevel level, IFormatProvider provider, object format, Exception exception, object arg0)
        {
            if (IsLevelEnable(level))
            {
                string message = DecodeFormat(format);
                if (provider == null)
                    message = string.Format(message, arg0);
                else
                    message = string.Format(provider, message, arg0);
                Print(level, format, message, exception);
            }
        }
        protected void LogLevelInternal(LoggerLevel level, IFormatProvider provider, object format, Exception exception, object arg0, object arg1)
        {
            if (IsLevelEnable(level))
            {
                string message = DecodeFormat(format);
                if (provider == null)
                    message = string.Format(message, arg0, arg1);
                else
                    message = string.Format(provider, message, arg0, arg1);
                Print(level, format, message, exception);
            }
        }
        protected void LogLevelInternal(LoggerLevel level, IFormatProvider provider, object format, Exception exception, object arg0, object arg1, object arg2)
        {
            if (IsLevelEnable(level))
            {
                string message = DecodeFormat(format);
                if (provider == null)
                    message = string.Format(message, arg0, arg1, arg2);
                else
                    message = string.Format(provider, message, arg0, arg1, arg2);
                Print(level, format, message, exception);
            }
        }
        protected void LogLevelInternal(LoggerLevel level, IFormatProvider provider, object format, Exception exception, object[] args)
        {
            if (IsLevelEnable(level))
            {
                string message = DecodeFormat(format);
                if (args != null && args.Length > 0)
                {
                    if (provider == null)
                        message = string.Format(message, args);
                    else
                        message = string.Format(provider, message, args);
                }
                Print(level, format, message, exception);
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------- 
        #region Override
        protected internal virtual void Print(LoggerLevel level, object format, string text, Exception err)
        {
            var sb = new StringBuilder();
            if (err != null)
            {
                sb.AppendLine(mName + " - " + text + err.StackTrim());
                err = err.InnerException;
                while (err != null)
                {
                    sb.AppendLine("InnerException : " + err.Message + err.StackTrim());
                    err = err.InnerException;
                }
            }
            else
            {
                sb.AppendLine(mName + " - " + text);
            }
            PrintText(sb.ToString(), level);
            PrintAttach(level, format, text, err, static (log, level, format, text, err) => log.Print(level, format, text, err));
        }
        protected internal virtual void PrintText(string text, LoggerLevel level)
        {
            lock (console_lock)
            {
                try
                {
                    if (Color.HasValue)
                    {
                        Console.ForegroundColor = Color.Value;
                    }
                    else if (level >= LoggerLevel.ERROR)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (level >= LoggerLevel.WARNNING)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (level < LoggerLevel.LOG)
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    Console.Write(text);
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
        private static object console_lock = new object();
        #endregion
        //---------------------------------------------------------------------------------------
        #region Logger
        public void LogLevel(LoggerLevel level, object message)/*                                 */=> LogLevelInternal(level, null, message, null);
        //---------------------------------------------------------------------------------------
        public void Fine(object message)/*                                                        */{ LogLevelInternal(LoggerLevel.FINE, null, message, null); }
        public void Trace(object message)/*                                                       */{ LogLevelInternal(LoggerLevel.TRACE, null, message, null); }
        public void Log(object message)/*                                                         */{ LogLevelInternal(LoggerLevel.LOG, null, message, null); }
        //---------------------------------------------------------------------------------------
        public void Debug(string message)/*                                                       */{ LogLevelInternal(LoggerLevel.DEBUG, null, message, null); }
        public void Debug(object message)/*                                                       */{ LogLevelInternal(LoggerLevel.DEBUG, null, message, null); }
        public void Debug(object message, Exception exception)/*                                  */{ LogLevelInternal(LoggerLevel.DEBUG, null, message, exception); }
        public void Debug(string message, Exception exception)/*                                  */{ LogLevelInternal(LoggerLevel.DEBUG, null, message, exception); }
        public void DebugFormat(string format, params object[] args)/*                            */{ LogLevelInternal(LoggerLevel.DEBUG, null, format, null, args); }
        public void DebugFormat(string format, object arg0)/*                                     */{ LogLevelInternal(LoggerLevel.DEBUG, null, format, null, arg0); }
        public void DebugFormat(string format, object arg0, object arg1)/*                        */{ LogLevelInternal(LoggerLevel.DEBUG, null, format, null, arg0, arg1); }
        public void DebugFormat(string format, object arg0, object arg1, object arg2)/*           */{ LogLevelInternal(LoggerLevel.DEBUG, null, format, null, arg0, arg1, arg2); }
        public void DebugFormat(IFormatProvider provider, string format, params object[] args)/*  */{ LogLevelInternal(LoggerLevel.DEBUG, provider, format, null, args); }
        //---------------------------------------------------------------------------------------
        public void Error(Exception err)/*                                                        */{ Error(err.Message, err); }
        public void Error(string message)/*                                                       */{ LogLevelInternal(LoggerLevel.ERROR, null, message, null); }
        public void Error(object message)/*                                                       */{ LogLevelInternal(LoggerLevel.ERROR, null, message, null); }
        public void Error(object message, Exception exception)/*                                  */{ LogLevelInternal(LoggerLevel.ERROR, null, message, exception); }
        public void Error(string message, Exception exception)/*                                  */{ LogLevelInternal(LoggerLevel.ERROR, null, message, exception); }
        public void ErrorFormat(string format, params object[] args)/*                            */{ LogLevelInternal(LoggerLevel.ERROR, null, format, null, args); }
        public void ErrorFormat(string format, object arg0)/*                                     */{ LogLevelInternal(LoggerLevel.ERROR, null, format, null, arg0); }
        public void ErrorFormat(string format, object arg0, object arg1)/*                        */{ LogLevelInternal(LoggerLevel.ERROR, null, format, null, arg0, arg1); }
        public void ErrorFormat(string format, object arg0, object arg1, object arg2)/*           */{ LogLevelInternal(LoggerLevel.ERROR, null, format, null, arg0, arg1, arg2); }
        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)/*  */{ LogLevelInternal(LoggerLevel.ERROR, provider, format, null, args); }
        //---------------------------------------------------------------------------------------
        public void Fatal(Exception err)/*                                                        */{ Fatal(err.Message, err); }
        public void Fatal(string message)/*                                                       */{ LogLevelInternal(LoggerLevel.FATAL, null, message, null); }
        public void Fatal(object message)/*                                                       */{ LogLevelInternal(LoggerLevel.FATAL, null, message, null); }
        public void Fatal(object message, Exception exception)/*                                  */{ LogLevelInternal(LoggerLevel.FATAL, null, message, exception); }
        public void Fatal(string message, Exception exception)/*                                  */{ LogLevelInternal(LoggerLevel.FATAL, null, message, exception); }
        public void FatalFormat(string format, params object[] args)/*                            */{ LogLevelInternal(LoggerLevel.FATAL, null, format, null, args); }
        public void FatalFormat(string format, object arg0)/*                                     */{ LogLevelInternal(LoggerLevel.FATAL, null, format, null, arg0); }
        public void FatalFormat(string format, object arg0, object arg1)/*                        */{ LogLevelInternal(LoggerLevel.FATAL, null, format, null, arg0, arg1); }
        public void FatalFormat(string format, object arg0, object arg1, object arg2)/*           */{ LogLevelInternal(LoggerLevel.FATAL, null, format, null, arg0, arg1, arg2); }
        public void FatalFormat(IFormatProvider provider, string format, params object[] args)/*  */{ LogLevelInternal(LoggerLevel.FATAL, provider, format, null, args); }
        //---------------------------------------------------------------------------------------
        public void Info(params object[] message)/*                                               */{ foreach (var msg in message) { LogLevelInternal(LoggerLevel.INFO, null, msg, null); } }
        public void Info(string message)/*                                                        */{ LogLevelInternal(LoggerLevel.INFO, null, message, null); }
        public void Info(object message)/*                                                        */{ LogLevelInternal(LoggerLevel.INFO, null, message, null); }
        public void Info(object message, Exception exception)/*                                   */{ LogLevelInternal(LoggerLevel.INFO, null, message, exception); }
        public void Info(string message, Exception exception)/*                                   */{ LogLevelInternal(LoggerLevel.INFO, null, message, exception); }
        public void InfoFormat(string format, params object[] args)/*                             */{ LogLevelInternal(LoggerLevel.INFO, null, format, null, args); }
        public void InfoFormat(string format, object arg0)/*                                      */{ LogLevelInternal(LoggerLevel.INFO, null, format, null, arg0); }
        public void InfoFormat(string format, object arg0, object arg1)/*                         */{ LogLevelInternal(LoggerLevel.INFO, null, format, null, arg0, arg1); }
        public void InfoFormat(string format, object arg0, object arg1, object arg2)/*            */{ LogLevelInternal(LoggerLevel.INFO, null, format, null, arg0, arg1, arg2); }
        public void InfoFormat(IFormatProvider provider, string format, params object[] args)/*   */{ LogLevelInternal(LoggerLevel.INFO, provider, format, null, args); }
        //---------------------------------------------------------------------------------------
        public void Warn(Exception err)/*                                                         */{ Warn(err.Message, err); }
        public void Warn(string message)/*                                                        */{ LogLevelInternal(LoggerLevel.WARNNING, null, message, null); }
        public void Warn(object message)/*                                                        */{ LogLevelInternal(LoggerLevel.WARNNING, null, message, null); }
        public void Warn(object message, Exception exception)/*                                   */{ LogLevelInternal(LoggerLevel.WARNNING, null, message, exception); }
        public void Warn(string message, Exception exception)/*                                   */{ LogLevelInternal(LoggerLevel.WARNNING, null, message, exception); }
        public void WarnFormat(string format, params object[] args)/*                             */{ LogLevelInternal(LoggerLevel.WARNNING, null, format, null, args); }
        public void WarnFormat(string format, object arg0)/*                                      */{ LogLevelInternal(LoggerLevel.WARNNING, null, format, null, arg0); }
        public void WarnFormat(string format, object arg0, object arg1)/*                         */{ LogLevelInternal(LoggerLevel.WARNNING, null, format, null, arg0, arg1); }
        public void WarnFormat(string format, object arg0, object arg1, object arg2)/*            */{ LogLevelInternal(LoggerLevel.WARNNING, null, format, null, arg0, arg1, arg2); }
        public void WarnFormat(IFormatProvider provider, string format, params object[] args)/*   */{ LogLevelInternal(LoggerLevel.WARNNING, provider, format, null, args); }
        //---------------------------------------------------------------------------------------
        public void BI(string message)/*                                                        */{ LogLevelInternal(LoggerLevel.BI, null, message, null); }
        public void BI(object message)/*                                                        */{ LogLevelInternal(LoggerLevel.BI, null, message, null); }
        public void BI(object message, Exception exception)/*                                   */{ LogLevelInternal(LoggerLevel.BI, null, message, exception); }
        public void BI(string message, Exception exception)/*                                   */{ LogLevelInternal(LoggerLevel.BI, null, message, exception); }
        public void BIFormat(string format, params object[] args)/*                             */{ LogLevelInternal(LoggerLevel.BI, null, format, null, args); }
        public void BIFormat(string format, object arg0)/*                                      */{ LogLevelInternal(LoggerLevel.BI, null, format, null, arg0); }
        public void BIFormat(string format, object arg0, object arg1)/*                         */{ LogLevelInternal(LoggerLevel.BI, null, format, null, arg0, arg1); }
        public void BIFormat(string format, object arg0, object arg1, object arg2)/*            */{ LogLevelInternal(LoggerLevel.BI, null, format, null, arg0, arg1, arg2); }
        public void BIFormat(IFormatProvider provider, string format, params object[] args)/*   */{ LogLevelInternal(LoggerLevel.BI, provider, format, null, args); }
        //---------------------------------------------------------------------------------------
        #endregion
        //---------------------------------------------------------------------------------------
    }

    public class BlankLogger : Logger
    {
        public BlankLogger() : base(LoggerFactory.CurrentFactory, string.Empty) { }
        protected internal override void Print(LoggerLevel level, object format, string text, Exception err)
        {
        }
    }

    public class TextWriterLogger : Logger
    {
        private StreamWriter output;
        public TextWriterLogger(StreamWriter output, string name) : base(LoggerFactory.CurrentFactory, name)
        {
            this.output = output;
            this.output.AutoFlush = true;
        }
        protected internal override void Print(LoggerLevel level, object format, string text, Exception err)
        {
            if (err != null)
            {
                output.WriteLine(mName + " - " + text + " : " + err.Message + err.StackTrim());
                err = err.InnerException;
                while (err != null)
                {
                    output.WriteLine("InnerException : " + err.Message + err.StackTrim());
                    err = err.InnerException;
                }
            }
            else
            {
                output.WriteLine(mName + " - " + text);
            }
        }
    }

    public class ConsoleLogger : Logger
    {
        public ConsoleLogger(object name) : base(LoggerFactory.CurrentFactory, name) { }
    }

    public class ListLogger : Logger
    {
        public static bool EnableConsole = true;
        private List<string> output = new List<string>();
        public ListLogger(string name) : base(LoggerFactory.CurrentFactory, name)
        {
        }
        protected internal override void PrintText(string text, LoggerLevel level)
        {
            lock (output)
            {
                output.Add(text);
                if (EnableConsole)
                {
                    Console.WriteLine(text);
                }
            }
        }
        public void PopLogs(List<string> input)
        {
            lock (output)
            {
                input.AddRange(output);
                output.Clear();
            }
        }
    }

    public class FileLogger : Logger, IDisposable
    {
        private FileStream fs;
        private TextWriter tw;
        public FileLogger(FileStream fs, string name = "") : base(LoggerFactory.CurrentFactory, name)
        {
            this.fs = fs;
            this.SetStream(fs);
        }
        public FileLogger(FileInfo ff, string name = "") : base(LoggerFactory.CurrentFactory, name)
        {
            CFiles.CreateFile(ff);
            this.fs = ff.OpenWrite();
            this.SetStream(fs);
        }
        public FileLogger(string fileName, string name = "") : base(LoggerFactory.CurrentFactory, name)
        {
            var ff = new FileInfo(fileName);
            CFiles.CreateFile(ff);
            this.fs = ff.OpenWrite();
            this.SetStream(fs);
        }
        private void SetStream(FileStream fs)
        {
            this.tw = new StreamWriter(fs, CUtils.UTF8);
        }
        public void Dispose()
        {
            fs?.Flush();
            tw?.Dispose();
            fs?.Dispose();
        }
        protected internal override void PrintText(string text, LoggerLevel level)
        {
            base.PrintText(text, level);
            this.tw.Write(text);
            this.tw.Flush();
        }

    }
    /// <summary>
    /// 延时工厂初始化
    /// </summary>
    public class LazyLogger : Logger
    {
        private LoggerFactory __factory;
        private Logger __log;
        private Func<object, Logger> __create;
        private Logger log
        {
            get
            {
                if (__factory != LoggerFactory.CurrentFactory)
                {
                    __log = null;
                }
                if (__log == null)
                {
                    lock (this)
                    {
                        if (__log == null)
                        {
                            __factory = LoggerFactory.CurrentFactory;
                            if (__create != null)
                                __log = __create(this.Owner);
                            else
                                __log = LoggerFactory.GetLogger(this.Owner);
                            __log.Color = this.Color;
                        }
                    }
                }
                return __log;
            }
        }
        public LazyLogger(object owner) : base(LoggerFactory.CurrentFactory, owner) { }
        public LazyLogger(object owner, Func<object, Logger> create) : base(LoggerFactory.CurrentFactory, owner)
        {
            this.__create = create;
        }
        public override ConsoleColor? Color { get => log.Color; set => log.Color = value; }
        public override void PushColor() => log.PushColor();
        public override void PopColor() => log.PopColor();

        protected internal override void PrintText(string text, LoggerLevel level)
        {
            log.PrintText(text, level);
        }
        protected internal override void Print(LoggerLevel level, object format, string text, Exception err)
        {
            log.Print(level, format, text, err);
        }
    }
}

