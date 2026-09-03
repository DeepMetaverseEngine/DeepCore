using System;
using Common.Logging;
using DeepCore.Log;

namespace DeepCrystal.Schedule.QuartzImpl
{
    public class LoggerAdapter : Common.Logging.ILoggerFactoryAdapter
    {
        public Common.Logging.ILog GetLogger(string key)
        {
            return new WLog(key);
        }
        public Common.Logging.ILog GetLogger(Type type)
        {
            return new WLog(type.Name);
        }
        public class WLog : Common.Logging.Simple.AbstractSimpleLogger
        {
            private Logger log;
            public WLog(string name) : base(name, LogLevel.All, false, true, false, "")
            {
                this.log = LoggerFactory.GetLogger(name);
            }
            protected override void WriteInternal(LogLevel level, object message, Exception exception)
            {
                switch (level)
                {
//                     case LogLevel.Debug:
//                         log.Debug(message, exception);
//                         return;
                    case LogLevel.Error:
                        log.Error(message, exception);
                        return;
                    case LogLevel.Fatal:
                        log.Fatal(message, exception);
                        return;
                    case LogLevel.Warn:
                        log.Warn(message, exception);
                        return;
//                     case LogLevel.Info:
//                         log.Info(message, exception);
//                         return;
//                     case LogLevel.Trace:
//                         log.Debug(message, exception);
//                         return;
//                     case LogLevel.Off:
//                         return;
                }
            }
        }
    }
}
